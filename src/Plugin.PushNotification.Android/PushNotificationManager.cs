using Android.App;
using Android.Content;
using Android.OS;
using Plugin.PushNotification.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.Content.PM;
using Android.Graphics;
using Android.Util;
using Firebase.Iid;
using Plugin.PushNotification.Shared;
using Encoding = System.Text.Encoding;

namespace Plugin.PushNotification
{
    /// <summary>
    /// Implementation for Feature
    /// </summary>
    public class PushNotificationManager : IPushNotification
    {
        //internal static PushNotificationActionReceiver ActionReceiver = null;

        static NotificationResponse delayedNotificationResponse = null;
        internal static string KeyGroupName = "KJt2CTan" + Crypto.Reverse("DG7ZbRpYvS0M");
        internal static string TokenKey = "w3IJux7Rx" + Crypto.Reverse("uwWkjGqWnXW");
        internal static string AppVersionCodeKey = Crypto.Reverse("ITs84ZItL7") + "d6y5gqx6vA";
        internal static string AppVersionNameKey = "5zA3VnV6" + Crypto.Reverse("uTdUjLl164o2");
        internal static string AppVersionPackageNameKey = Crypto.Reverse("9JMCgtya") + "IFQDXZqF00Ty";

        private static readonly IList<NotificationUserCategory> UserNotificationCategories =
            new List<NotificationUserCategory>();

        public static string NotificationContentTitleKey { get; set; }
        public static string NotificationContentTextKey { get; set; }
        public static string NotificationContentDataKey { get; set; }
        public static int IconResource { get; set; }
        public static Android.Net.Uri SoundUri { get; set; }
        public static Color? Color { get; set; }
        public static Type NotificationActivityType { get; set; }

        public static ActivityFlags? NotificationActivityFlags { get; set; } =
            ActivityFlags.ClearTop | ActivityFlags.SingleTop;

        public static string DefaultNotificationChannelId { get; set; } = "PushNotificationChannel";
        public static string DefaultNotificationChannelName { get; set; } = "General";

        internal static Type DefaultNotificationActivityType { get; set; } = null;

        static Context _context;

        [Obsolete("ProcessIntent with these parameters is deprecated, please use the other override instead.")]
        public static void ProcessIntent(Intent intent, bool enableDelayedResponse = true)
        {
            Bundle extras = intent?.Extras;
            if (extras != null && !extras.IsEmpty)
            {
                var parameters = new Dictionary<string, object>();
                foreach (var key in extras.KeySet())
                {
                    if (!parameters.ContainsKey(key) && extras.Get(key) != null)
                        parameters.Add(key, $"{extras.Get(key)}");
                }

                NotificationManager manager =
                    _context.GetSystemService(Context.NotificationService) as NotificationManager;
                var notificationId = extras.GetInt(DefaultPushNotificationHandler.ActionNotificationIdKey, -1);
                if (notificationId != -1)
                {
                    var notificationTag = extras.GetString(DefaultPushNotificationHandler.ActionNotificationTagKey,
                        string.Empty);
                    if (notificationTag == null)
                        manager.Cancel(notificationId);
                    else
                        manager.Cancel(notificationTag, notificationId);
                }


                var response = new NotificationResponse(parameters,
                    extras.GetString(DefaultPushNotificationHandler.ActionIdentifierKey, string.Empty));

                if (_onNotificationOpened == null && enableDelayedResponse)
                    delayedNotificationResponse = response;
                else
                    _onNotificationOpened?.Invoke(CrossPushNotification.Current,
                        new PushNotificationResponseEventArgs(response.Data, response.Identifier, response.Type));

                CrossPushNotification.Current.NotificationHandler?.OnOpened(response);
            }
        }

        public static void ProcessIntent(Activity activity, Intent intent, bool enableDelayedResponse = true)
        {
            DefaultNotificationActivityType = activity.GetType();

            Bundle extras = intent?.Extras;
            if (extras != null && !extras.IsEmpty)
            {
                var parameters = new Dictionary<string, object>();
                foreach (var key in extras.KeySet())
                {
                    if (!parameters.ContainsKey(key) && extras.Get(key) != null)
                        parameters.Add(key, $"{extras.Get(key)}");
                }

                NotificationManager manager =
                    _context.GetSystemService(Context.NotificationService) as NotificationManager;
                var notificationId = extras.GetInt(DefaultPushNotificationHandler.ActionNotificationIdKey, -1);
                if (notificationId != -1)
                {
                    var notificationTag = extras.GetString(DefaultPushNotificationHandler.ActionNotificationTagKey,
                        string.Empty);
                    if (notificationTag == null)
                        manager.Cancel(notificationId);
                    else
                        manager.Cancel(notificationTag, notificationId);
                }


                var response = new NotificationResponse(parameters,
                    extras.GetString(DefaultPushNotificationHandler.ActionIdentifierKey, string.Empty));

                if (_onNotificationOpened == null && enableDelayedResponse)
                    delayedNotificationResponse = response;
                else
                    _onNotificationOpened?.Invoke(CrossPushNotification.Current,
                        new PushNotificationResponseEventArgs(response.Data, response.Identifier, response.Type));

                CrossPushNotification.Current.NotificationHandler?.OnOpened(response);
            }
        }

        public static void Initialize(Context context, bool resetToken, bool createDefaultNotificationChannel = true,
            bool autoRegistration = true)
        {
            _context = context;

            CrossPushNotification.Current.NotificationHandler =
                CrossPushNotification.Current.NotificationHandler ?? new DefaultPushNotificationHandler();
            if (autoRegistration)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    var packageName = Application.Context.PackageManager
                        .GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).PackageName;
                    var versionCode = Application.Context.PackageManager
                        .GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).VersionCode;
                    var versionName = Application.Context.PackageManager
                        .GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).VersionName;

                    var prefs = Android.App.Application.Context.GetSharedPreferences(
                        PushNotificationManager.KeyGroupName, FileCreationMode.Private);

                    try
                    {
                        var storedVersionName =
                            Crypto.Decrypt(prefs.GetString(PushNotificationManager.AppVersionNameKey, string.Empty),
                                RestSecurity);
                        var storedVersionCode =
                            Crypto.Decrypt(prefs.GetString(PushNotificationManager.AppVersionCodeKey, string.Empty),
                                RestSecurity);
                        var storedPackageName =
                            Crypto.Decrypt(
                                prefs.GetString(PushNotificationManager.AppVersionPackageNameKey, string.Empty),
                                RestSecurity);

                        if (resetToken || (!string.IsNullOrEmpty(storedPackageName) &&
                                           (!storedPackageName.Equals(packageName,
                                                StringComparison.CurrentCultureIgnoreCase) ||
                                            !storedVersionName.Equals(versionName,
                                                StringComparison.CurrentCultureIgnoreCase) ||
                                            !storedVersionCode.Equals($"{versionCode}",
                                                StringComparison.CurrentCultureIgnoreCase))))
                        {
                            CleanUp();
                        }
                    }
                    catch (Exception ex)
                    {
                        _onNotificationError?.Invoke(CrossPushNotification.Current,
                            new PushNotificationErrorEventArgs(PushNotificationErrorType.UnregistrationFailed,
                                ex.ToString()));
                    }
                    finally
                    {
                        var editor = prefs.Edit();
                        editor.PutString(PushNotificationManager.AppVersionNameKey,
                            Crypto.Encrypt($"{versionName}", RestSecurity));
                        editor.PutString(PushNotificationManager.AppVersionCodeKey,
                            Crypto.Encrypt($"{versionCode}", RestSecurity));
                        editor.PutString(PushNotificationManager.AppVersionPackageNameKey,
                            Crypto.Encrypt($"{packageName}", RestSecurity));
                        editor.Commit();
                    }


                    CrossPushNotification.Current.RegisterForPushNotifications();
                });
            }


            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O && createDefaultNotificationChannel)
            {
                // Create channel to show notifications.
                string channelId = DefaultNotificationChannelId;
                string channelName = DefaultNotificationChannelName;
                NotificationManager notificationManager =
                    (NotificationManager) context.GetSystemService(Context.NotificationService);

                notificationManager.CreateNotificationChannel(new NotificationChannel(channelId,
                    channelName, NotificationImportance.Default));
            }


            System.Diagnostics.Debug.WriteLine(CrossPushNotification.Current.Token);
        }

        public static void Initialize(Context context, NotificationUserCategory[] notificationCategories,
            bool resetToken, bool createDefaultNotificationChannel = true, bool autoRegistration = true)
        {
            Initialize(context, resetToken, createDefaultNotificationChannel, autoRegistration);
            RegisterUserNotificationCategories(notificationCategories);
        }

        public async System.Threading.Tasks.Task RegisterForPushNotifications()
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                var token = FirebaseInstanceId.Instance.Token;
                if (!string.IsNullOrEmpty(token))
                {
                    SaveToken(token);
                }
            });
        }

        public void UnregisterForPushNotifications()
        {
            Reset();
        }

        public static void Reset()
        {
            try
            {
                ThreadPool.QueueUserWorkItem(state => { CleanUp(); });
            }
            catch (Exception ex)
            {
                _onNotificationError?.Invoke(CrossPushNotification.Current,
                    new PushNotificationErrorEventArgs(PushNotificationErrorType.UnregistrationFailed, ex.ToString()));
            }
        }

        static void CleanUp()
        {
            Firebase.Iid.FirebaseInstanceId.Instance.DeleteInstanceId();
            SaveToken(string.Empty);
        }


        public static void Initialize(Context context, IPushNotificationHandler pushNotificationHandler,
            bool resetToken, bool createDefaultNotificationChannel = true, bool autoRegistration = true)
        {
            CrossPushNotification.Current.NotificationHandler = pushNotificationHandler;
            Initialize(context, resetToken, createDefaultNotificationChannel, autoRegistration);
        }

        public static void ClearUserNotificationCategories()
        {
            UserNotificationCategories.Clear();
        }

        public string Token =>
            Crypto.Decrypt(
                Android.App.Application.Context.GetSharedPreferences(KeyGroupName, FileCreationMode.Private)
                    .GetString(TokenKey, string.Empty), TokenSecurity);

        static PushNotificationDataEventHandler _onNotificationReceived;

        public event PushNotificationDataEventHandler OnNotificationReceived
        {
            add { _onNotificationReceived += value; }
            remove { _onNotificationReceived -= value; }
        }

        static PushNotificationDataEventHandler _onNotificationDeleted;

        public event PushNotificationDataEventHandler OnNotificationDeleted
        {
            add { _onNotificationDeleted += value; }
            remove { _onNotificationDeleted -= value; }
        }


        public IPushNotificationHandler NotificationHandler { get; set; }

        static PushNotificationResponseEventHandler _onNotificationOpened;

        public event PushNotificationResponseEventHandler OnNotificationOpened
        {
            add
            {
                var previousVal = _onNotificationOpened;
                _onNotificationOpened += value;
                if (delayedNotificationResponse != null && previousVal == null)
                {
                    var tmpParams = delayedNotificationResponse;
                    _onNotificationOpened?.Invoke(CrossPushNotification.Current,
                        new PushNotificationResponseEventArgs(tmpParams.Data, tmpParams.Identifier, tmpParams.Type));
                    delayedNotificationResponse = null;
                }
            }
            remove { _onNotificationOpened -= value; }
        }

        static PushNotificationTokenEventHandler _onTokenRefresh;

        public event PushNotificationTokenEventHandler OnTokenRefresh
        {
            add { _onTokenRefresh += value; }
            remove { _onTokenRefresh -= value; }
        }

        static PushNotificationErrorEventHandler _onNotificationError;

        public event PushNotificationErrorEventHandler OnNotificationError
        {
            add { _onNotificationError += value; }
            remove { _onNotificationError -= value; }
        }

        public NotificationUserCategory[] GetUserNotificationCategories()
        {
            return UserNotificationCategories?.ToArray();
        }

        public static void RegisterUserNotificationCategories(NotificationUserCategory[] notificationCategories)
        {
            if (notificationCategories != null && notificationCategories.Length > 0)
            {
                ClearUserNotificationCategories();

                foreach (var userCat in notificationCategories)
                {
                    UserNotificationCategories.Add(userCat);
                }
            }
            else
            {
                ClearUserNotificationCategories();
            }
        }

        #region internal methods

        //Raises event for push notification token refresh
        internal static void RegisterToken(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                SaveToken(token);
            }

            _onTokenRefresh?.Invoke(CrossPushNotification.Current, new PushNotificationTokenEventArgs(token));
        }

        internal static void RegisterData(IDictionary<string, object> data)
        {
            _onNotificationReceived?.Invoke(CrossPushNotification.Current, new PushNotificationDataEventArgs(data));
        }

        internal static void RegisterDelete(IDictionary<string, object> data)
        {
            _onNotificationDeleted?.Invoke(CrossPushNotification.Current, new PushNotificationDataEventArgs(data));
        }

        internal static void SaveToken(string token)
        {
            var editor = Application.Context
                .GetSharedPreferences(PushNotificationManager.KeyGroupName, FileCreationMode.Private).Edit();
            editor.PutString(PushNotificationManager.TokenKey, Crypto.Encrypt(token, TokenSecurity));
            editor.Commit();
        }

        private static readonly string TokenSecurity =
            Crypto.Reverse("G8V0Qzj") + "G8V0QzjP" + Crypto.Reverse("xs8tjmVhVGoP") +
            "4iM16g8hUQ1" + Crypto.Reverse("XzlSv3FAF");

        private static readonly string RestSecurity =
            Crypto.Reverse("eoxJs0LVi") + "GMz1sKMDZ03" + Crypto.Reverse("tgjded") +
            "Q3kHrd6" + Crypto.Reverse("GMdT6q6");

        #endregion
    }
}