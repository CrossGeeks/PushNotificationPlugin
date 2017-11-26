using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Plugin.PushNotification;

namespace PushNotificationSample.Droid
{
    [Application]
    public class MainApplication : Application
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transer) : base(handle, transer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            //If debug you should reset the token each time.
#if DEBUG
            PushNotificationManager.Initialize(this, true);
#else
            PushNotificationManager.Initialize(this,false);
#endif
            //Set the activity type that will handle your notifications
            PushNotificationManager.NotificationActivityType = typeof(MainActivity);


            //Handle notification when app is closed here
            CrossPushNotification.Current.OnNotificationReceived += (s, p) =>
            {


            };
        }
    }
}