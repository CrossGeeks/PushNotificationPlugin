using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin.PushNotification.Abstractions
{
    public class NotificationResponse
    {
        public string Identifier { get; }

        public IDictionary<string, string> Data { get; }

        public NotificationCategoryType Type { get; }

        public NotificationResponse(IDictionary<string, string> data, string identifier = "", NotificationCategoryType type = NotificationCategoryType.Default)
        {
            Identifier = identifier;
            Data = data;
            Type = type;
        }
    }
}
