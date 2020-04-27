using System;
using System.Collections.Generic;
using System.Text;
using Android.App;

namespace Plugin.PushNotification
{
    /// <summary>
    /// NotificationChannelId, NotificationChannelName, NotificationChannelImportance
    /// </summary>
    public class NotificationChannelProps
    {
        public string NotificationChannelId { get; set; }
        public string NotificationChannelName { get; set; }
        public NotificationImportance NotificationChannelImportance { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="channelName"></param>
        /// <param name="channelImportance"></param>
        public NotificationChannelProps(string channelId, string channelName, NotificationImportance channelImportance = NotificationImportance.Default)
        {
            NotificationChannelId = channelId;
            NotificationChannelName = channelName;
            NotificationChannelImportance = channelImportance;
        }
    }
}
