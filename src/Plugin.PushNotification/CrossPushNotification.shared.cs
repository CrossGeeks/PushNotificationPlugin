using System;

namespace Plugin.PushNotification
{
    /// <summary>
    /// Cross platform PushNotification implemenations
    /// </summary>
    public class CrossPushNotification
    {
        static readonly Lazy<IPushNotification> Implementation = new Lazy<IPushNotification>(() => CreatePushNotification(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Current settings to use
        /// </summary>
        public static IPushNotification Current
        {
            get
            {
                var ret = Implementation.Value;
                if (ret == null)
                {
                    throw NotImplementedInReferenceAssembly();
                }
                return ret;
            }
        }

        static IPushNotification CreatePushNotification()
        {
#if NETSTANDARD2_1
            return null;
#else
            return new PushNotificationManager();
#endif
        }

        internal static Exception NotImplementedInReferenceAssembly()
        {
            return new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
        }
    }
}
