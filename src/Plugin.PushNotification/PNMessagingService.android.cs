using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Firebase.Messaging;

namespace Plugin.PushNotification
{
    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class PNMessagingService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage p0)
        {
            var parameters = new Dictionary<string, object>();
            var notification = p0.GetNotification();
            if (notification != null)
            {
                if (!string.IsNullOrEmpty(notification.Body))
                    parameters.Add("body", notification.Body);

                if (!string.IsNullOrEmpty(notification.BodyLocalizationKey))
                    parameters.Add("body_loc_key", notification.BodyLocalizationKey);

                var bodyLocArgs = notification.GetBodyLocalizationArgs();
                if (bodyLocArgs != null && bodyLocArgs.Any())
                    parameters.Add("body_loc_args", bodyLocArgs);

                if (!string.IsNullOrEmpty(notification.Title))
                    parameters.Add("title", notification.Title);

                if (!string.IsNullOrEmpty(notification.TitleLocalizationKey))
                    parameters.Add("title_loc_key", notification.TitleLocalizationKey);

                var titleLocArgs = notification.GetTitleLocalizationArgs();
                if (titleLocArgs != null && titleLocArgs.Any())
                    parameters.Add("title_loc_args", titleLocArgs);

                if (!string.IsNullOrEmpty(notification.Tag))
                    parameters.Add("tag", notification.Tag);

                if (!string.IsNullOrEmpty(notification.Sound))
                    parameters.Add("sound", notification.Sound);

                if (!string.IsNullOrEmpty(notification.Icon))
                    parameters.Add("icon", notification.Icon);

                if (notification.Link != null)
                    parameters.Add("link_path", notification.Link.Path);

                if (!string.IsNullOrEmpty(notification.ClickAction))
                    parameters.Add("click_action", notification.ClickAction);

                if (!string.IsNullOrEmpty(notification.Color))
                    parameters.Add("color", notification.Color);
            }

            foreach (var d in p0.Data)
            {
                if (!parameters.ContainsKey(d.Key))
                    parameters.Add(d.Key, d.Value);
            }

            //Fix localization arguments parsing
            string[] localizationKeys=new string[]{ "title_loc_args", "body_loc_args"};
            foreach(var locKey in localizationKeys)
            {
                if (parameters.ContainsKey(locKey) && parameters[locKey] is string parameterValue)
                {
                    if (parameterValue.StartsWith("[") && parameterValue.EndsWith("]") && parameterValue.Length > 2)
                    {

                        var arrayValues = parameterValue.Substring(1, parameterValue.Length - 2);
                        parameters[locKey] = arrayValues.Split(',').Select(t => t.Trim()).ToArray();
                    }
                    else
                    {
                        parameters[locKey] = new string[] { };
                    }
                }
            }
           
          
            PushNotificationManager.RegisterData(parameters);
            CrossPushNotification.Current.NotificationHandler?.OnReceived(parameters);
        }

        public override void OnNewToken(string p0)
        {
            ((PushNotificationManager)CrossPushNotification.Current).Token = p0;
            PushNotificationManager.RegisterToken(p0);
            System.Diagnostics.Debug.WriteLine($"REFRESHED TOKEN: {p0}");
        }
    }
}
