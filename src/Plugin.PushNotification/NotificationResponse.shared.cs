using System.Collections.Generic;

namespace Plugin.PushNotification
{
    public class NotificationResponse
    {
        public string Identifier { get; }

        public IDictionary<string, object> Data { get; }

        public NotificationCategoryType Type { get; }

        public string? Result { get; }

        public NotificationResponse(IDictionary<string, object> data, string identifier = "", NotificationCategoryType type = NotificationCategoryType.Default,string? result = null)
        {
            Identifier = identifier;
            Data = data;
            Type = type;
            Result = result;
        }
    }
}
