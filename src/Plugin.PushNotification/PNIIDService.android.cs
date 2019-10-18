using Android.App;
using Android.Content;
using Firebase.Iid;

namespace Plugin.PushNotification
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class PNIIDService : FirebaseInstanceIdService
    {
        /**
        * Called if InstanceID token is updated. This may occur if the security of
        * the previous token had been compromised. Note that this is called when the InstanceID token
        * is initially generated so this is where you would retrieve the token.
        */
        public override void OnTokenRefresh()
        {
            // Get updated InstanceID token.
            var refreshedToken = FirebaseInstanceId.Instance.Token;

            if (!string.IsNullOrEmpty(refreshedToken))
                ((PushNotificationManager)CrossPushNotification.Current).Token = refreshedToken;

            // CrossPushNotification.Current.OnTokenRefresh?.Invoke(this,refreshedToken);
            PushNotificationManager.RegisterToken(refreshedToken);
            System.Diagnostics.Debug.WriteLine($"REFRESHED TOKEN: {refreshedToken}");
        }
    }
}
