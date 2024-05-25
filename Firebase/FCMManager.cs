using FGB.Network;
using Firebase;
using Firebase.Extensions;
using Firebase.Messaging;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class fcmManager : MonoBehaviour
{
    public Text text;
    public FirebaseApp app = null;
    public static string fcmToken;

    private void Awake() {
        DontDestroyOnLoad(this);
    }

    void Start()
    {

        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                FirebaseMessaging.GetTokenAsync().ContinueWithOnMainThread(task => {
                    if (task.IsCompletedSuccessfully) {
                        fcmToken = task.Result;
                    } else {
                        
                    }
                    Debug.Log("Firebase Token async: " + fcmToken);
                });
                FirebaseMessaging.TokenReceived += FirebaseMessagingOnTokenReceived;
                FirebaseMessaging.MessageReceived += FirebaseMessagingOnMessageReceived;
                Debug.Log("Firebase success");
                app = FirebaseApp.DefaultInstance;
                // text.text = "�غ�Ϸ�";
            }
            else
            {
                // text.text = "����" + dependencyStatus;
            }
        });
    }


    private async void FirebaseMessagingOnTokenReceived(object sender, TokenReceivedEventArgs e)
    {
        if (e != null) {
            Debug.Log("Firebase Token: " + e.Token);
            fcmToken = e.Token;
            
            // text.text = e.Token;
        }
        else {
            Debug.Log("Firebase Error");
        }
    }

    private void FirebaseMessagingOnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("Firebase Message: " + e.Message);
        // text.text += e.Message.Notification.Body;
    }
}