using Android.App;
using Android.Content;
using Android.OS;
using Plugin.PushNotification.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.Content.PM;
using System.Collections.ObjectModel;
using Android.Graphics;

namespace Plugin.PushNotification
{
  /// <summary>
  /// Implementation for Feature
  /// </summary>
  public class PushNotificationManager : IPushNotification
  {
        internal static PushNotificationActionReceiver ActionReceiver = null;
        static NotificationResponse delayedNotificationResponse = null;
        internal const string KeyGroupName = "Plugin.PushNotification";
        internal const string TokenKey = "TokenKey";
        internal const string AppVersionCodeKey = "AppVersionCodeKey";
        internal const string AppVersionNameKey = "AppVersionNameKey";
        internal const string AppVersionPackageNameKey = "AppVersionPackageNameKey";
        internal const string NotificationDeletedActionId = "Plugin.PushNotification.NotificationDeletedActionId";
        static IList<NotificationUserCategory> userNotificationCategories = new List<NotificationUserCategory>();
        public static string NotificationContentTitleKey { get; set; }
        public static string NotificationContentTextKey { get; set; }
        public static string NotificationContentDataKey { get; set; }
        public static int IconResource { get; set; }
        public static Android.Net.Uri SoundUri { get; set; }
        public static Color? Color { get; set; }
        public static Type NotificationActivityType { get; set; }
        public static ActivityFlags? NotificationActivityFlags { get; set; } = ActivityFlags.ClearTop | ActivityFlags.SingleTop;
        public static string DefaultNotificationChannelId { get; set; } = "PushNotificationChannel";
        public static string DefaultNotificationChannelName { get; set; } = "General";

        static Context _context;
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

                NotificationManager manager = _context.GetSystemService(Context.NotificationService) as NotificationManager;
                var notificationId = extras.GetInt(DefaultPushNotificationHandler.ActionNotificationIdKey, -1);
                if (notificationId != -1)
                {
                    var notificationTag = extras.GetString(DefaultPushNotificationHandler.ActionNotificationTagKey, string.Empty);
                    if (notificationTag == null)
                        manager.Cancel(notificationId);
                    else
                        manager.Cancel(notificationTag, notificationId);
                }


                var response = new NotificationResponse(parameters, extras.GetString(DefaultPushNotificationHandler.ActionIdentifierKey, string.Empty));

                if (_onNotificationOpened == null && enableDelayedResponse)
                    delayedNotificationResponse = response;
                else
                    _onNotificationOpened?.Invoke(CrossPushNotification.Current, new PushNotificationResponseEventArgs(response.Data, response.Identifier, response.Type));

                CrossPushNotification.Current.NotificationHandler?.OnOpened(response);
            }
        }

        public static void Initialize(Context context, bool resetToken, bool createDefaultNotificationChannel = true)
        {
           //FirebaseApp.InitializeApp(context);
     
            _context = context;

            CrossPushNotification.Current.NotificationHandler = CrossPushNotification.Current.NotificationHandler ?? new DefaultPushNotificationHandler();

            ThreadPool.QueueUserWorkItem(state =>
            {

                var packageName = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).PackageName;
                var versionCode = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).VersionCode;
                var versionName = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).VersionName;
                var prefs = Android.App.Application.Context.GetSharedPreferences(PushNotificationManager.KeyGroupName, FileCreationMode.Private);

                try
                {

                    var storedVersionName = prefs.GetString(PushNotificationManager.AppVersionNameKey, string.Empty);
                    var storedVersionCode = prefs.GetString(PushNotificationManager.AppVersionCodeKey, string.Empty);
                    var storedPackageName = prefs.GetString(PushNotificationManager.AppVersionPackageNameKey, string.Empty);


                    if (resetToken || (!string.IsNullOrEmpty(storedPackageName) && (!storedPackageName.Equals(packageName, StringComparison.CurrentCultureIgnoreCase) || !storedVersionName.Equals(versionName, StringComparison.CurrentCultureIgnoreCase) || !storedVersionCode.Equals($"{versionCode}", StringComparison.CurrentCultureIgnoreCase))))
                    {
                        CleanUp();

                    }

                }
                catch (Exception ex)
                {
                    _onNotificationError?.Invoke(CrossPushNotification.Current, new PushNotificationErrorEventArgs(ex.ToString()));
                }
                finally
                {
                    var editor = prefs.Edit();
                    editor.PutString(PushNotificationManager.AppVersionNameKey, $"{versionName}");
                    editor.PutString(PushNotificationManager.AppVersionCodeKey, $"{versionCode}");
                    editor.PutString(PushNotificationManager.AppVersionPackageNameKey, $"{packageName}");
                    editor.Commit();
                }


                var token = Firebase.Iid.FirebaseInstanceId.Instance.Token;
                if (!string.IsNullOrEmpty(token))
                {

                    SaveToken(token);
                }


            });

            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O && createDefaultNotificationChannel)
            {
                // Create channel to show notifications.
                string channelId = DefaultNotificationChannelId;
                string channelName = DefaultNotificationChannelName;
                NotificationManager notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);

                notificationManager.CreateNotificationChannel(new NotificationChannel(channelId,
                    channelName, NotificationImportance.Default));
            }


            System.Diagnostics.Debug.WriteLine(CrossPushNotification.Current.Token);
        }
        public static void Initialize(Context context, NotificationUserCategory[] notificationCategories, bool resetToken, bool createDefaultNotificationChannel = true)
        {

            Initialize(context, resetToken,createDefaultNotificationChannel);
            RegisterUserNotificationCategories(notificationCategories);

        }
        public static void Reset()
        {
            try
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    CleanUp();
                });
            }
            catch (Exception ex)
            {
                _onNotificationError?.Invoke(CrossPushNotification.Current, new PushNotificationErrorEventArgs(ex.ToString()));
            }


        }

        static void CleanUp()
        {
            Firebase.Iid.FirebaseInstanceId.Instance.DeleteInstanceId();
            SaveToken(string.Empty);
        }


        public static void Initialize(Context context, IPushNotificationHandler pushNotificationHandler, bool resetToken, bool createDefaultNotificationChannel = true)
        {
            CrossPushNotification.Current.NotificationHandler = pushNotificationHandler;
            Initialize(context, resetToken,createDefaultNotificationChannel);
        }

        public static void ClearUserNotificationCategories()
        {
            userNotificationCategories.Clear();
        }

        public string Token { get { return Android.App.Application.Context.GetSharedPreferences(KeyGroupName, FileCreationMode.Private).GetString(TokenKey, string.Empty); } }

        static PushNotificationDataEventHandler _onNotificationReceived;
        public event PushNotificationDataEventHandler OnNotificationReceived
        {
            add
            {
                _onNotificationReceived += value;
            }
            remove
            {
                _onNotificationReceived -= value;
            }
        }

        static PushNotificationDataEventHandler _onNotificationDeleted;
        public event PushNotificationDataEventHandler OnNotificationDeleted
        {
            add
            {
                _onNotificationDeleted += value;
            }
            remove
            {
                _onNotificationDeleted -= value;
            }
        }


        public IPushNotificationHandler NotificationHandler { get; set; }

        static PushNotificationResponseEventHandler _onNotificationOpened;
        public event PushNotificationResponseEventHandler OnNotificationOpened
        {
            add
            {
                _onNotificationOpened += value;
                if (delayedNotificationResponse != null && _onNotificationOpened == null)
                {
                    var tmpParams = delayedNotificationResponse;
                    _onNotificationOpened?.Invoke(CrossPushNotification.Current, new PushNotificationResponseEventArgs(tmpParams.Data, tmpParams.Identifier, tmpParams.Type));
                    delayedNotificationResponse = null;
                }
            }
            remove
            {
                _onNotificationOpened -= value;
            }
        }

        static PushNotificationTokenEventHandler _onTokenRefresh;
        public event PushNotificationTokenEventHandler OnTokenRefresh
        {
            add
            {
                _onTokenRefresh += value;
            }
            remove
            {
                _onTokenRefresh -= value;
            }
        }

        static PushNotificationErrorEventHandler _onNotificationError;
        public event PushNotificationErrorEventHandler OnNotificationError
        {
            add
            {
                _onNotificationError += value;
            }
            remove
            {
                _onNotificationError -= value;
            }
        }

        public NotificationUserCategory[] GetUserNotificationCategories()
        {
            return userNotificationCategories?.ToArray();
        }
        public static void RegisterUserNotificationCategories(NotificationUserCategory[] notificationCategories)
        {
            if (notificationCategories != null && notificationCategories.Length > 0)
            {
                ClearUserNotificationCategories();

                foreach (var userCat in notificationCategories)
                {
                    userNotificationCategories.Add(userCat);
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
            var editor = Android.App.Application.Context.GetSharedPreferences(PushNotificationManager.KeyGroupName, FileCreationMode.Private).Edit();
            editor.PutString(PushNotificationManager.TokenKey, token);
            editor.Commit();
        }

        #endregion
    }
}