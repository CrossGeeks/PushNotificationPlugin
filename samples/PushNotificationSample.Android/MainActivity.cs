using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Plugin.PushNotification;

namespace PushNotificationSample.Droid
{
    [Activity(Label = "PushNotificationSample", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
            PushNotificationManager.ProcessIntent(this, Intent);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            PushNotificationManager.ProcessIntent(this, intent);
        }
    }
}
