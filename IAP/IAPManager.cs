using System;
using System.Collections;
using System.Collections.Generic;
using FGB.Data;
using FGB.Network;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;

public enum PurchaseStatus {
    CantPurchase = -2,
    FailPurchase = -1,
    Processing = 0,
    SuccessPurchase = 1,
}

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    bool isInitialized = false;
    public int purchaseStatus = (int)PurchaseStatus.Processing;
    public bool buyConsumable1 = false;

//Android, IOS 상품 ID
    public const string productID_Consumable1 = "consumable1";

    //* IAP Manager 싱글톤
    private static IAPManager instance;
    public static IAPManager Instance {
        get {
            if (instance != null) {
                return instance;
            }

            instance = FindObjectOfType<IAPManager>();

            if (instance == null) {
                instance = new GameObject("IAP Manager").AddComponent<IAPManager>();
            }

            return instance;
        }
    }

    //* IAP를 초기화한다.
    private void InitUnityIAP() {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        builder.AddProduct(productID_Consumable1, ProductType.Consumable, new IDs(){
            //* 구글 플레이 IAP
            {productID_Consumable1, GooglePlay.Name},
            //* 애플 IAP
            {productID_Consumable1, AppleAppStore.Name}
        });

        // //* Subscription 추가
        // builder.AddProduct(productIDSubscription, ProductType.Subscription, new IDs(){
        //     {productIDSubscription, GooglePlay.Name},
        //     {productIDSubscription, AppleAppStore.Name}
        // });

        UnityPurchasing.Initialize(this, builder);
    }

    private IStoreController storeController;
    private IExtensionProvider storeExtensionProvider;

    public void OnInitialized(IStoreController controller, IExtensionProvider extension) {
        Debug.Log("유니티 IAP 초기화 성공");
        storeController = controller;
        storeExtensionProvider = extension;
        isInitialized = true;
        
        
    }

    public void OnInitializeFailed(InitializationFailureReason error, string str) {
        Debug.LogError($"유니티 IAP 초기화 실패 {error}");
        Debug.Log($"{str}");
        purchaseStatus = (int)PurchaseStatus.CantPurchase;
    }
    public void OnInitializeFailed(InitializationFailureReason error) {
        Debug.LogError($"유니티 IAP 초기화 실패 {error}");
        purchaseStatus = (int)PurchaseStatus.CantPurchase;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason reason) {
        Debug.LogWarning($"구매 실패 - {product.definition.id}, {reason}");
        purchaseStatus = (int)PurchaseStatus.FailPurchase;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureDescription description) {
        Debug.LogWarning($"구매 실패 - {product.definition.id}, {description}");
        purchaseStatus = (int)PurchaseStatus.FailPurchase;
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) {
        Debug.Log($"iap-test 구매 성공 - ID : {args.purchasedProduct.definition.id}");

        //* Unity IAP 구매 영수증
        CrossPlatformValidator validator = new CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);
        var products = storeController.products.all;
        foreach(var product in products) {
            if (product.hasReceipt) {
                var result = validator.Validate(product.receipt);

                foreach(IPurchaseReceipt productReceipt in result) {
                    int price = Convert.ToInt32(args.purchasedProduct.metadata.localizedPrice);
                    Analytics.Transaction(productReceipt.productID, args.purchasedProduct.metadata.localizedPrice, args.purchasedProduct.metadata.isoCurrencyCode, productReceipt.transactionID, null);
                    GooglePlayReceipt googlePlayReceipt = productReceipt as GooglePlayReceipt;
                    if (googlePlayReceipt.productID == args.purchasedProduct.definition.id) {
                    // if (null != googlePlayReceipt) {
                        // Debug.Log($"iap-test Google Receipt: {googlePlayReceipt.productID}");
                        
                        SendReceipt(JsonUtility.ToJson(CreateGoogleReceipt(googlePlayReceipt, price, false)));
                        
                        
                    }

                    // AppleInAppPurchaseReceipt appleReceipt = productReceipt as AppleInAppPurchaseReceipt;
                    // if (null != appleReceipt) {
                    //     Debug.Log($"Apple Product ID : {appleReceipt.productID}");
                    //     Debug.Log($"Apple Purchase Date : {appleReceipt.purchaseDate.ToLocalTime()}");
                    //     Debug.Log($"Apple Transaction ID : {appleReceipt.transactionID}");
                    // }
                }
            }
        }
        Debug.Log($"iap-test {args.purchasedProduct.definition.id}");

        if (args.purchasedProduct.definition.id == productID_Consumable1) {
            Debug.Log("iap-test consumable1 구매 완료");
            buyConsumable1 = true;
        }

        purchaseStatus = (int)PurchaseStatus.SuccessPurchase;
        return PurchaseProcessingResult.Complete;
    }

    //# 영수증을 보낼 서버 url
    public string PostServerUrl = "";
    public class Receipt {
        public string productID;
        public string transactionID;
        public string packageName;
        public string purchaseToken;
        public GooglePurchaseState purchaseState;
        public string platform;
        public int price;
        public bool isSubscription;
        public bool isSending;
    }
    public Receipt CreateGoogleReceipt(GooglePlayReceipt rec, int price, bool isSubscription) {
        var receipt = new Receipt();

        receipt.productID = rec.productID;
        receipt.transactionID = rec.transactionID;
        receipt.packageName = rec.packageName;
        receipt.purchaseToken = rec.purchaseToken;
        receipt.purchaseState = rec.purchaseState;
        receipt.platform = SystemInfo.operatingSystem;
        receipt.price = price;
        receipt.isSubscription = isSubscription;

        return receipt;
    }
        public void SendReceipt(string json) {
        // GetReceipt("123");
        try {
            // Debug.Log("Send Receipt to server");
            StartCoroutine(PostJson(PostServerUrl, json));
        } catch (Exception error) {
            Debug.LogError(error);
        }
    }

    private IEnumerator PostJson(string url, string json) {
        using (var uwr = new UnityWebRequest(url, "POST")) {
            Debug.Log($"iap-test Post Json: " + json);

            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError) {
            // if (uwr.isNetworkError) {
                Debug.Log("iap-test Error While Sending: " + uwr.error);
            }
            else {
                Debug.LogFormat("iap-test Received Post Json Data: {0}", uwr.downloadHandler.text);
                // var receiptSave = bool.Parse(uwr.downloadHandler.text);
                // Debug.Log(receiptSave);
            }
        }
    }
    

    public void Purchase(string productId) {
        if (!isInitialized) {
            return;
        }
        var product = storeController.products.WithID(productId);
        if (product != null && product.availableToPurchase) {
            Debug.Log($"구매 시도 - {product.definition.id}");
            storeController.InitiatePurchase(product);
            purchaseStatus = (int)PurchaseStatus.Processing;
        } else {
            Debug.Log($"구매 시도 불가 - {productId}");
            purchaseStatus = (int)PurchaseStatus.CantPurchase;
        }
    }

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        InitUnityIAP();
    }
}
