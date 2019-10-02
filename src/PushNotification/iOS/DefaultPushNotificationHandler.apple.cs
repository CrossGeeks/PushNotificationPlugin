using Plugin.PushNotification.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.PushNotification
{
    public class DefaultPushNotificationHandler : IPushNotificationHandler
    {
        public const string DomainTag = "DefaultPushNotificationHandler";

        public void OnError(string error)
        {
            System.Diagnostics.Debug.WriteLine($"{DomainTag} - OnError - {error}");
        }

        public void OnOpened(NotificationResponse response)
        {
            System.Diagnostics.Debug.WriteLine($"{DomainTag} - OnOpened");
        }

        public void OnReceived(IDictionary<string, object> parameters)
        {
            System.Diagnostics.Debug.WriteLine($"{DomainTag} - OnReceived");
        }
    }
}
