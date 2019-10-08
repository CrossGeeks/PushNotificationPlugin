using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Plugin.PushNotification.Abstractions;
using UIKit;
using UserNotifications;

namespace Plugin.PushNotification
{
    /// <summary>
    /// Implementation for PushNotification
    /// </summary>
    public class PushNotificationManager : NSObject, IPushNotification, IUNUserNotificationCenterDelegate
    {
        const string TokenKey = "Token";

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

        internal static string InternalRetrieveSavedToken()
        {
            return NSUserDefaults.StandardUserDefaults.StringForKey(TokenKey);
        }

        internal static void InternalSaveToken(string token)
        {
            NSUserDefaults.StandardUserDefaults.SetString(token, TokenKey);
        }

        public IPushNotificationHandler NotificationHandler { get; set; }

        public static UNNotificationPresentationOptions CurrentNotificationPresentationOption { get; set; } = UNNotificationPresentationOptions.None;

        static IList<NotificationUserCategory> UsernNotificationCategories { get; } = new List<NotificationUserCategory>();

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

        static PushNotificationResponseEventHandler _onNotificationOpened;
        public event PushNotificationResponseEventHandler OnNotificationOpened
        {
            add
            {
                _onNotificationOpened += value;
            }
            remove
            {
                _onNotificationOpened -= value;
            }
        }

        public NotificationUserCategory[] GetUserNotificationCategories()
        {
            return UsernNotificationCategories?.ToArray();
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

        public static async Task Initialize(NSDictionary options, bool autoRegistration = true)
        {
            CrossPushNotification.Current.NotificationHandler = CrossPushNotification.Current.NotificationHandler ?? new DefaultPushNotificationHandler();

            if (autoRegistration)
            {
                await CrossPushNotification.Current.RegisterForPushNotifications();
            }
        }

        public static async Task Initialize(NSDictionary options, IPushNotificationHandler pushNotificationHandler, bool autoRegistration = true)
        {
            CrossPushNotification.Current.NotificationHandler = pushNotificationHandler;
            await Initialize(options, autoRegistration);
        }

        public static async Task Initialize(NSDictionary options, NotificationUserCategory[] notificationUserCategories, bool autoRegistration = true)
        {
            await Initialize(options, autoRegistration);
            RegisterUserNotificationCategories(notificationUserCategories);
        }

        static void RegisterUserNotificationCategories(NotificationUserCategory[] userCategories)
        {
            if (userCategories != null && userCategories.Length > 0)
            {
                UsernNotificationCategories.Clear();
                IList<UNNotificationCategory> categories = new List<UNNotificationCategory>();
                foreach (var userCat in userCategories)
                {
                    IList<UNNotificationAction> actions = new List<UNNotificationAction>();

                    foreach (var action in userCat.Actions)
                    {
                        // Create action
                        var actionID = action.Id;
                        var title = action.Title;
                        var notificationActionType = UNNotificationActionOptions.None;
                        switch (action.Type)
                        {
                            case NotificationActionType.AuthenticationRequired:
                                notificationActionType = UNNotificationActionOptions.AuthenticationRequired;
                                break;
                            case NotificationActionType.Destructive:
                                notificationActionType = UNNotificationActionOptions.Destructive;
                                break;
                            case NotificationActionType.Foreground:
                                notificationActionType = UNNotificationActionOptions.Foreground;
                                break;
                        }

                        var notificationAction = UNNotificationAction.FromIdentifier(actionID, title, notificationActionType);
                        actions.Add(notificationAction);
                    }

                    // Create category
                    var categoryID = userCat.Category;
                    var notificationActions = actions.ToArray() ?? new UNNotificationAction[] { };
                    var intentIDs = new string[] { };
                    var categoryOptions = new UNNotificationCategoryOptions[] { };

                    var category = UNNotificationCategory.FromIdentifier(categoryID, notificationActions, intentIDs, userCat.Type == NotificationCategoryType.Dismiss ? UNNotificationCategoryOptions.CustomDismissAction : UNNotificationCategoryOptions.None);
                    categories.Add(category);

                    UsernNotificationCategories.Add(userCat);
                }

                // Register categories
                UNUserNotificationCenter.Current.SetNotificationCategories(new NSSet<UNNotificationCategory>(categories.ToArray()));
            }
        }

        public async Task RegisterForPushNotifications()
        {
            var permisionTask = new TaskCompletionSource<bool>();

            // Register your app for remote notifications.
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                // iOS 10 or later
                var authOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;

                // For iOS 10 display notification (sent via APNS)
                UNUserNotificationCenter.Current.Delegate = CrossPushNotification.Current as IUNUserNotificationCenterDelegate;

                UNUserNotificationCenter.Current.RequestAuthorization(authOptions, (granted, error) =>
                {
                    if (error != null)
                    {
                        _onNotificationError?.Invoke(CrossPushNotification.Current, new PushNotificationErrorEventArgs(PushNotificationErrorType.PermissionDenied, error.Description));
                    }
                    else if (!granted)
                    {
                        _onNotificationError?.Invoke(CrossPushNotification.Current, new PushNotificationErrorEventArgs(PushNotificationErrorType.PermissionDenied, "Push notification permission not granted"));
                    }
                    permisionTask.SetResult(granted);
                });
            }
            else
            {
                // iOS 9 or before
                var allNotificationTypes = UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound;
                var settings = UIUserNotificationSettings.GetSettingsForTypes(allNotificationTypes, null);
                UIApplication.SharedApplication.RegisterUserNotificationSettings(settings);
                permisionTask.SetResult(true);
            }

            var permissonGranted = await permisionTask.Task;
            if (permissonGranted)
            {
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            }
        }

        public void UnregisterForPushNotifications()
        {
            UIApplication.SharedApplication.UnregisterForRemoteNotifications();
            Token = string.Empty;
        }

        // To receive notifications in foreground on iOS 10 devices.
        [Export("userNotificationCenter:willPresentNotification:withCompletionHandler:")]
        public void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            // Do your magic to handle the notification data
            System.Console.WriteLine(notification.Request.Content.UserInfo);
            System.Diagnostics.Debug.WriteLine("WillPresentNotification");
            var parameters = GetParameters(notification.Request.Content.UserInfo);
            _onNotificationReceived?.Invoke(CrossPushNotification.Current, new PushNotificationDataEventArgs(parameters));
            //CrossPushNotification.Current.NotificationHandler?.OnReceived(parameters);
            completionHandler(CurrentNotificationPresentationOption);
        }

        [Export("userNotificationCenter:didReceiveNotificationResponse:withCompletionHandler:")]
        public void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            var parameters = GetParameters(response.Notification.Request.Content.UserInfo);

            NotificationCategoryType catType = NotificationCategoryType.Default;
            if (response.IsCustomAction)
                catType = NotificationCategoryType.Custom;
            else if (response.IsDismissAction)
                catType = NotificationCategoryType.Dismiss;

            var notificationResponse = new NotificationResponse(parameters, $"{response.ActionIdentifier}".Equals("com.apple.UNNotificationDefaultActionIdentifier", StringComparison.CurrentCultureIgnoreCase) ? string.Empty : $"{response.ActionIdentifier}", catType);
            _onNotificationOpened?.Invoke(this, new PushNotificationResponseEventArgs(notificationResponse.Data, notificationResponse.Identifier, notificationResponse.Type));

            CrossPushNotification.Current.NotificationHandler?.OnOpened(notificationResponse);

            // Inform caller it has been handled
            completionHandler();
        }

        public static void DidRegisterRemoteNotifications(NSData deviceToken)
        {
            var length = (int)deviceToken.Length;
            if (length == 0)
            {
                return;
            }

            var hex = new StringBuilder(length * 2);
            foreach (var b in deviceToken)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            var cleanedDeviceToken = hex.ToString();
            _onTokenRefresh?.Invoke(CrossPushNotification.Current, new PushNotificationTokenEventArgs(cleanedDeviceToken));
        }

        public static void DidReceiveMessage(NSDictionary data)
        {
            var parameters = GetParameters(data);

            _onNotificationReceived?.Invoke(CrossPushNotification.Current, new PushNotificationDataEventArgs(parameters));

            CrossPushNotification.Current.NotificationHandler?.OnReceived(parameters);
            System.Diagnostics.Debug.WriteLine("DidReceivedMessage");
        }

        public static void RemoteNotificationRegistrationFailed(NSError error)
        {
            _onNotificationError?.Invoke(CrossPushNotification.Current, new PushNotificationErrorEventArgs(PushNotificationErrorType.RegistrationFailed, error.Description));
        }

        static IDictionary<string, object> GetParameters(NSDictionary data)
        {
            var parameters = new Dictionary<string, object>();

            var keyAps = new NSString("aps");
            var keyAlert = new NSString("alert");

            foreach (var val in data)
            {
                if (val.Key.Equals(keyAps))
                {
                    if (data.ValueForKey(keyAps) is NSDictionary aps)
                    {
                        foreach (var apsVal in aps)
                        {
                            if (apsVal.Value is NSDictionary)
                            {
                                if (apsVal.Key.Equals(keyAlert))
                                {
                                    foreach (var alertVal in apsVal.Value as NSDictionary)
                                    {
                                        parameters.Add($"aps.alert.{alertVal.Key}", $"{alertVal.Value}");
                                    }
                                }
                            }
                            else
                            {
                                parameters.Add($"aps.{apsVal.Key}", $"{apsVal.Value}");
                            }
                        }
                    }
                }
                else
                {
                    parameters.Add($"{val.Key}", $"{val.Value}");
                }
            }

            return parameters;
        }
    }
}
