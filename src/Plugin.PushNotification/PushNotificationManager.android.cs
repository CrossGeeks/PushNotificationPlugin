using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Tasks;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Firebase.Iid;
using Firebase.Messaging;
using Java.Interop;

namespace Plugin.PushNotification
{
    /// <summary>
    /// Implementation for Feature
    /// </summary>
    public class PushNotificationManager : Java.Lang.Object, IPushNotification, IOnCompleteListener
    {
        //internal static PushNotificationActionReceiver ActionReceiver = null;
        static NotificationResponse delayedNotificationResponse = null;
        internal const string KeyGroupName = "Plugin.PushNotification";
        internal const string TokenKey = "TokenKey";
        internal const string AppVersionCodeKey = "AppVersionCodeKey";
        internal const string AppVersionNameKey = "AppVersionNameKey";
        internal const string AppVersionPackageNameKey = "AppVersionPackageNameKey";
        static IList<NotificationUserCategory> userNotificationCategories = new List<NotificationUserCategory>();
        public static string NotificationContentTitleKey { get; set; }
        public static string NotificationContentTextKey { get; set; }
        public static string NotificationContentDataKey { get; set; }
        public static int IconResource { get; set; }
        public static int LargeIconResource { get; set; }
        public static bool ShouldShowWhen { get; set; } = true;
        public static Android.Net.Uri SoundUri { get; set; }
        public static Color? Color { get; set; }
        public static Type NotificationActivityType { get; set; }
        public static ActivityFlags? NotificationActivityFlags { get; set; } = ActivityFlags.ClearTop | ActivityFlags.SingleTop;

        /// <summary>
        /// to work with a singe notification channel
        /// </summary>
        public static string DefaultNotificationChannelId { get; set; } = "PushNotificationChannel";

        /// <summary>
        /// to work with a singe notification channel
        /// </summary>
        public static string DefaultNotificationChannelName { get; set; } = "General";

        public static NotificationImportance DefaultNotificationChannelImportance { get; set; } = NotificationImportance.Default;

        /// <summary>
        /// to work with a singe notification channels
        /// </summary>
        public static List<NotificationChannelProps> NotificationChannels { get; set; }

        static TaskCompletionSource<string> _tokenTcs;
        internal static Type DefaultNotificationActivityType { get; set; } = null;

        static Context _context;

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


                if (string.IsNullOrEmpty(response.Identifier))
                {
                    if (_onNotificationOpened == null && enableDelayedResponse)
                    {
                        delayedNotificationResponse = response;
                    }
                    else
                    {
                        _onNotificationOpened?.Invoke(CrossPushNotification.Current, new PushNotificationResponseEventArgs(response.Data, response.Identifier, response.Type,response.Result));
                    }
                    CrossPushNotification.Current.NotificationHandler?.OnOpened(response);
                }
                else
                {
                    if (_onNotificationAction == null && enableDelayedResponse)
                    {
                        delayedNotificationResponse = response;
                    }
                    else
                    {
                        _onNotificationAction?.Invoke(CrossPushNotification.Current, new PushNotificationResponseEventArgs(response.Data, response.Identifier, response.Type,response.Result));
                    }

                    CrossPushNotification.Current.NotificationHandler?.OnAction(response);
                }

               
            }
        }

        public static void Initialize(Context context, bool resetToken, bool createNotificationChannel = true, bool autoRegistration = true)
        {
            _context = context;

            CrossPushNotification.Current.NotificationHandler = CrossPushNotification.Current.NotificationHandler ?? new DefaultPushNotificationHandler();
            FirebaseMessaging.Instance.AutoInitEnabled = autoRegistration;
            if (autoRegistration)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    var packageName = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).PackageName;
                    var versionCode = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).VersionCode;
                    var versionName = Application.Context.PackageManager.GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).VersionName;
                    var prefs = Application.Context.GetSharedPreferences(KeyGroupName, FileCreationMode.Private);

                    try
                    {
                        var storedVersionName = prefs.GetString(AppVersionNameKey, string.Empty);
                        var storedVersionCode = prefs.GetString(AppVersionCodeKey, string.Empty);
                        var storedPackageName = prefs.GetString(AppVersionPackageNameKey, string.Empty);

                        if (resetToken || (!string.IsNullOrEmpty(storedPackageName) && (!storedPackageName.Equals(packageName, StringComparison.CurrentCultureIgnoreCase) || !storedVersionName.Equals(versionName, StringComparison.CurrentCultureIgnoreCase) || !storedVersionCode.Equals($"{versionCode}", StringComparison.CurrentCultureIgnoreCase))))
                        {
                            ((PushNotificationManager)CrossPushNotification.Current).CleanUp();
                        }
                    }
                    catch (Exception ex)
                    {
                        _onNotificationError?.Invoke(CrossPushNotification.Current, new PushNotificationErrorEventArgs(PushNotificationErrorType.UnregistrationFailed, ex.ToString()));
                    }
                    finally
                    {
                        var editor = prefs.Edit();
                        editor.PutString(AppVersionNameKey, $"{versionName}");
                        editor.PutString(AppVersionCodeKey, $"{versionCode}");
                        editor.PutString(AppVersionPackageNameKey, $"{packageName}");
                        editor.Commit();
                    }
                    CrossPushNotification.Current.RegisterForPushNotifications();
                });
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O && createNotificationChannel)
            {
                if (NotificationChannels == null || NotificationChannels.Count() == 0)
                {
                    NotificationChannels = new List<NotificationChannelProps>()
                    {
                        new NotificationChannelProps(DefaultNotificationChannelId,
                                         DefaultNotificationChannelName,
                                         DefaultNotificationChannelImportance)
                    };
                }

                foreach (NotificationChannelProps channel in NotificationChannels)
                {
                    // Create channel to show notifications.
                    var channelId = channel.NotificationChannelId;
                    var channelName = channel.NotificationChannelName;
                    var channelImportance = channel.NotificationChannelImportance;
                    var notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);
                    var notChannel = new NotificationChannel(channelId, channelName, channelImportance);

                    if (SoundUri != null)
                    {
                        try
                        {
                            var soundAttributes = new AudioAttributes.Builder()
                                                 .SetContentType(AudioContentType.Sonification)
                                                 .SetUsage(AudioUsageKind.Notification).Build();

                            notChannel.SetSound(SoundUri, soundAttributes);

                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex);
                        }

                    }

                    notificationManager.CreateNotificationChannel(notChannel);
                }
            }
            System.Diagnostics.Debug.WriteLine(CrossPushNotification.Current.Token);
        }

        public static void Initialize(Context context, NotificationUserCategory[] notificationCategories, bool resetToken, bool createNotificationChannel = true, bool autoRegistration = true)
        {
            Initialize(context, resetToken, createNotificationChannel, autoRegistration);
            RegisterUserNotificationCategories(notificationCategories);
        }

        public void RegisterForPushNotifications()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = true;
            System.Threading.Tasks.Task.Run(async () =>
            {
                var token = await GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    Token = token;
                }
            });
        }

        async Task<string> GetTokenAsync()
        {
            _tokenTcs = new TaskCompletionSource<string>();
            FirebaseMessaging.Instance.GetToken().AddOnCompleteListener(this);
            string retVal = null;

            try
            {
                retVal = await _tokenTcs.Task;
            }
            catch (Exception ex)
            {
                _onNotificationError?.Invoke(CrossPushNotification.Current, new PushNotificationErrorEventArgs(PushNotificationErrorType.RegistrationFailed, $"{ex}"));
            }

            return retVal;
        }

        public void UnregisterForPushNotifications()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = false;
            Reset();
        }

        public void Reset()
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
                _onNotificationError?.Invoke(CrossPushNotification.Current, new PushNotificationErrorEventArgs(PushNotificationErrorType.UnregistrationFailed, ex.ToString()));
            }
        }

        void CleanUp()
        {
            FirebaseMessaging.Instance.DeleteToken();
            Token = string.Empty;
        }


        public static void Initialize(Context context, IPushNotificationHandler pushNotificationHandler, bool resetToken, bool createNotificationChannel = true, bool autoRegistration = true)
        {
            CrossPushNotification.Current.NotificationHandler = pushNotificationHandler;
            Initialize(context, resetToken, createNotificationChannel, autoRegistration);
        }

        public static void ClearUserNotificationCategories()
        {
            userNotificationCategories.Clear();
        }

        public Func<string> RetrieveSavedToken { get; set; } = InternalRetrieveSavedToken;
        public Action<string> SaveToken { get; set; } = InternalSaveToken;

        public string Token
        {
            get
            {
                return RetrieveSavedToken?.Invoke() ?? string.Empty;
            }
            internal set
            {
                SaveToken?.Invoke(value);
            }
        }

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
                var previousVal = _onNotificationOpened;
                _onNotificationOpened += value;
                if (delayedNotificationResponse != null && previousVal == null)
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

        private static PushNotificationResponseEventHandler _onNotificationAction;
        public event PushNotificationResponseEventHandler OnNotificationAction
        {
            add
            {
                var previousVal = _onNotificationAction;
                _onNotificationAction += value;
                if (delayedNotificationResponse != null && previousVal == null)
                {
                    var tmpParams = delayedNotificationResponse;
                    if (!string.IsNullOrEmpty(tmpParams.Identifier))
                    {
                        _onNotificationAction?.Invoke(CrossPushNotification.Current, new PushNotificationResponseEventArgs(tmpParams.Data, tmpParams.Identifier, tmpParams.Type));
                        delayedNotificationResponse = null;
                    }

                }
            }
            remove
            {
                _onNotificationAction -= value;
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
        internal static void RegisterAction(IDictionary<string, object> data,string result = null)
        {
            var response = new NotificationResponse(data, data.ContainsKey(DefaultPushNotificationHandler.ActionIdentifierKey) ? $"{data[DefaultPushNotificationHandler.ActionIdentifierKey]}" : string.Empty, NotificationCategoryType.Default,result);

            _onNotificationAction?.Invoke(CrossPushNotification.Current, new PushNotificationResponseEventArgs(response.Data, response.Identifier, response.Type));
        }
        internal static void RegisterData(IDictionary<string, object> data)
        {
            _onNotificationReceived?.Invoke(CrossPushNotification.Current, new PushNotificationDataEventArgs(data));
        }
        internal static void RegisterDelete(IDictionary<string, object> data)
        {
            _onNotificationDeleted?.Invoke(CrossPushNotification.Current, new PushNotificationDataEventArgs(data));
        }

        internal static string InternalRetrieveSavedToken()
        {
            return Application.Context.GetSharedPreferences(KeyGroupName, FileCreationMode.Private).GetString(TokenKey, string.Empty);
        }

        internal static void InternalSaveToken(string token)
        {
            var editor = Application.Context.GetSharedPreferences(KeyGroupName, FileCreationMode.Private).Edit();
            editor.PutString(TokenKey, token);
            editor.Commit();
        }

        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            try
            {
                if (task.IsSuccessful)
                {
                    string token = (string)task.Result;
                    _tokenTcs?.TrySetResult(token);
                }
                else
                {
                    _tokenTcs?.TrySetException(task.Exception);
                }

            }
            catch (Exception ex)
            {
                _tokenTcs?.TrySetException(ex);
            }


        }

        public void ClearAllNotifications()
        {
            NotificationManager manager = Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
            manager.CancelAll();
        }

        public void RemoveNotification(int id)
        {
            NotificationManager manager = Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
            manager.Cancel(id);
        }

        public void RemoveNotification(string tag, int id)
        {
            if (string.IsNullOrEmpty(tag))
            {
                RemoveNotification(id);
            }
            else
            {
                NotificationManager manager = Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
                manager.Cancel(tag, id);
            }
        }

        #endregion
    }


}
