using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Networking.PushNotifications;
using Windows.Storage;
using Windows.UI.Notifications;

namespace Plugin.PushNotification
{
    public class PushNotificationManager : IPushNotification
    {
        const string TokenKey = "Token";
        const string NotificationIdKey = "id";
        const string NotificationTagKey = "tag";
        const string NotificationArgumentKey = "argument";
        const string NotificationInputsKey = "inputs";
        const string TaskName = "ToastBackgroundTask";

        static IList<NotificationUserCategory> UserNotificationCategories { get; } = new List<NotificationUserCategory>();

        private PushNotificationChannel channel;

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
            return ApplicationData.Current.LocalSettings.Values.ContainsKey(TokenKey) ? ApplicationData.Current.LocalSettings.Values[TokenKey]?.ToString() : null;
        }

        internal static void InternalSaveToken(string token)
        {
            ApplicationData.Current.LocalSettings.Values[TokenKey] = token;
        }



       public IPushNotificationHandler NotificationHandler { get; set; }

        public event PushNotificationTokenEventHandler OnTokenRefresh;
        public event PushNotificationResponseEventHandler OnNotificationOpened;
        public event PushNotificationResponseEventHandler OnNotificationAction;
        public event PushNotificationDataEventHandler OnNotificationReceived;
        public event PushNotificationDataEventHandler OnNotificationDeleted;
        public event PushNotificationErrorEventHandler OnNotificationError;

        public static void Initialize()
        {
            CrossPushNotification.Current.NotificationHandler = CrossPushNotification.Current.NotificationHandler ?? new DefaultPushNotificationHandler();
        }

        public static void Initialize(IPushNotificationHandler pushNotificationHandler)
        {
            CrossPushNotification.Current.NotificationHandler = pushNotificationHandler;
            Initialize();
        }

        async Task RegisterBackgroundTask()
        {
           
            // If background task is already registered, do nothing
            if (BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals(TaskName)))
                return;

            // Otherwise request access
            BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();

            // Create the background task
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder()
            {
                Name = TaskName
            };

            // Assign the toast action trigger
            builder.SetTrigger(new ToastNotificationActionTrigger());

            // And register the task
            BackgroundTaskRegistration registration = builder.Register();
        }
        public async Task OnLaunchedOrActivated(IActivatedEventArgs e)
        {
            await RegisterBackgroundTask();
            // Handle toast activation
            if (e is ToastNotificationActivatedEventArgs)
            {
                var details = e as ToastNotificationActivatedEventArgs;
                string arguments = details.Argument;
                var userInput = details.UserInput;

                NotificationResponse notificationResponse;
                PushNotificationResponseEventArgs notificationArgs;
                // Perform tasks
                if (userInput.Any())
                {
                    var input = userInput.FirstOrDefault();
                    var dict = new Dictionary<string, object>()
                            {
                                { NotificationArgumentKey,details.Argument },
                                { NotificationInputsKey,details.UserInput.ToDictionary(d=>d.Key,d=>d.Value) }
                            };
                    notificationArgs = new PushNotificationResponseEventArgs(dict, input.Key, result: $"{input.Value}");
                    notificationResponse = new NotificationResponse(dict, input.Key, result: $"{input.Value}");
                }
                else
                {
                    var dict = new Dictionary<string, object>()
                            {
                                { NotificationArgumentKey,details.Argument }
                            };
                    notificationArgs = new PushNotificationResponseEventArgs(dict);
                    notificationResponse = new NotificationResponse(dict);
                }


                OnNotificationOpened?.Invoke(this, notificationArgs);

                CrossPushNotification.Current.NotificationHandler?.OnOpened(notificationResponse);


            }

        }
        public void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            var deferral = args.TaskInstance.GetDeferral();

            switch (args.TaskInstance.Task.Name)
            {
                case "ToastBackgroundTask":
                    var details = args.TaskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
                    if (details != null)
                    {
                        string arguments = details.Argument;
                        var userInput = details.UserInput;
                        NotificationResponse notificationResponse;
                        PushNotificationResponseEventArgs notificationArgs;
                        // Perform tasks
                        if(userInput.Any())
                        {
                            var input = userInput.FirstOrDefault();
                            var dict = new Dictionary<string, object>()
                            {
                                { NotificationArgumentKey,details.Argument },
                                { NotificationInputsKey,details.UserInput.ToDictionary(d=>d.Key,d=>d.Value) }
                            };
                            notificationArgs = new PushNotificationResponseEventArgs(dict, input.Key, result: $"{input.Value}");
                            notificationResponse = new NotificationResponse(dict, input.Key, result: $"{input.Value}");
                        }
                        else
                        {
                            var dict = new Dictionary<string, object>()
                            {
                                { NotificationArgumentKey,details.Argument }
                            };
                            notificationArgs = new PushNotificationResponseEventArgs(dict);
                            notificationResponse = new NotificationResponse(dict);
                        }


                        OnNotificationAction?.Invoke(this, notificationArgs);

                        CrossPushNotification.Current.NotificationHandler?.OnAction(notificationResponse);
                    }
                    break;

            }

            deferral.Complete();
        }
        public void ClearAllNotifications()
        {
            ToastNotificationManager.History.Clear();
        }

        public NotificationUserCategory[] GetUserNotificationCategories()
        {
            return UserNotificationCategories?.ToArray();
        }

        public void RegisterUserNotificationCategories(NotificationUserCategory[] userCategories)
        {
            UserNotificationCategories.Clear();

            foreach (NotificationUserCategory userCategory in userCategories)
                UserNotificationCategories.Add(userCategory);
        }

        public async void RegisterForPushNotifications()
        {
            channel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
            channel.PushNotificationReceived += OnPushNotificationReceived;
            InternalSaveToken(channel.Uri);
            OnTokenRefresh?.Invoke(CrossPushNotification.Current, new PushNotificationTokenEventArgs(channel.Uri));
        }

        public void RemoveNotification(int id)
        {
            foreach (ToastNotification notification in ToastNotificationManager.History.GetHistory().Where(n => n.Data.Values.ContainsKey(NotificationIdKey) && n.Data.Values[NotificationIdKey] == id.ToString()).ToList())
                ToastNotificationManager.History.Remove(notification.Tag, notification.Group);
        }

        public void RemoveNotification(string tag, int id)
        {
            if (string.IsNullOrEmpty(tag))
            {
                RemoveNotification(id);
            }
            else
            {
                foreach (ToastNotification notification in ToastNotificationManager.History.GetHistory().Where(n => n.Data.Values.ContainsKey(NotificationTagKey) && n.Data.Values.ContainsKey(NotificationIdKey) && n.Data.Values[NotificationTagKey] == tag && n.Data.Values[NotificationIdKey] == id.ToString()).ToList())
                    ToastNotificationManager.History.Remove(notification.Tag, notification.Group);
            }
        }

        public void UnregisterForPushNotifications()
        {
            if (channel != null)
                channel.PushNotificationReceived -= OnPushNotificationReceived;

            ApplicationData.Current.LocalSettings.Values.Remove(TokenKey);
        }

        private void OnPushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            if (args.NotificationType == PushNotificationType.Raw)
            {
                foreach (var pair in JsonConvert.DeserializeObject<Dictionary<string, string>>(args.RawNotification.Content))
                    data.Add(pair.Key, pair.Value);
            }
            else if (args.NotificationType == PushNotificationType.Toast)
            {
                foreach (XmlAttribute attribute in args.ToastNotification.Content.DocumentElement.Attributes)
                    data.Add(attribute.Name, attribute.Value);
            }
            else if (args.NotificationType == PushNotificationType.Tile || args.NotificationType == PushNotificationType.TileFlyout)
            {
                foreach (XmlAttribute attribute in args.TileNotification.Content.DocumentElement.Attributes)
                    data.Add(attribute.Name, attribute.Value);
            }
           
            OnNotificationReceived?.Invoke(CrossPushNotification.Current, new PushNotificationDataEventArgs(data));

            CrossPushNotification.Current.NotificationHandler?.OnReceived(data);
        }
    }
}
