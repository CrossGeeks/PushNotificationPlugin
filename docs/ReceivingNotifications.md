## Android Firebase Push Notifications

On Firebase Cloud Messaging there are two types of messages that you can send to clients:

**Notification messages** - Delivered when the application is in background. These messages trigger the onMessageReceived() callback only when your app is in foreground. Firebase will provide the ui for the notification shown on Android device.

```json
{
   "notification" : 
   {
    "body" : "hello",
    "title": "firebase",
    "sound": "default"
   },
    "registration_ids" : ["eoPr8fUIPns:APA91bEVYODiWaL9a9JidumLRcFjVrEC4iHY80mHSE1e-udmPB32RjMiYL2P2vWTIUgYCJYVOx9SGSN_R4Ksau3vFX4WvkDj4ZstxqmcBhM3K-NMrX8P6U0sdDEqDpr-lD_N5JWBLCoV"]
}
```

**Data messages** - Handled by the client app. These messages trigger the onMessageReceived() callback even if your app is in foreground/background/killed. When using this type of message you are the one providing the UI and handling when push notification is received on an Android device.

```json
{
    "data": {
        "message" : "my_custom_value",
        "other_key" : true,
        "body":"test"
     },
     "priority": "high",
     "registration_ids" : ["eoPr8fUIPns:APA91bEVYODiWaL9a9JidumLRcFjVrEC4iHY80mHSE1e-udmPB32RjMiYL2P2vWTIUgYCJYVOx9SGSN_R4Ksau3vFX4WvkDj4ZstxqmcBhM3K-NMrX8P6U0sdDEqDpr-lD_N5JWBLCoV"]
}
```

Note: Data messages let's you customize notifications ui while notification messages don't (ui is renderered by firebase)

For more information: 

https://firebase.google.com/docs/cloud-messaging/concept-options

https://firebase.google.com/docs/cloud-messaging/http-server-ref

## iOS APS Push Notifications

https://developer.apple.com/library/content/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/CreatingtheNotificationPayload.html#//apple_ref/doc/uid/TP40008194-CH10-SW1

## Configuring a Silent Notification

On iOS to get a silent notification you should send "content_available" : 1 inside the "aps" payload key.

iOS Silent Payload Sample:
```json
{
    "aps" : {
        "content-available" : 1
    },
    "acme1" : "bar",
    "acme2" : 42
}
```

On Android to get a silent notification you should send "silent" : true inside the "data" payload key.

Android Silent Payload Sample:
```json
{
    "data": {
        "message" : "my_custom_value",
        "other_key" : true,
        "body":"test",
	"silent" : true
     },
     "priority": "high",
     "registration_ids" : ["eoPr8fUIPns:APA91bEVYODiWaL9a9JidumLRcFjVrEC4iHY80mHSE1e-udmPB32RjMiYL2P2vWTIUgYCJYVOx9SGSN_R4Ksau3vFX4WvkDj4ZstxqmcBhM3K-NMrX8P6U0sdDEqDpr-lD_N5JWBLCoV"]
}
```

### Notification Events

**OnNotificationReceived**
```csharp

  CrossPushNotification.Current.OnNotificationReceived += (s,p) =>
  {
 
        System.Diagnostics.Debug.WriteLine("Received");
    
  };

```

**OnNotificationOpened**
```csharp
  
  CrossPushNotification.Current.OnNotificationOpened += (s,p) =>
  {
                System.Diagnostics.Debug.WriteLine("Opened");
                foreach(var data in p.Data)
                {
                    System.Diagnostics.Debug.WriteLine($"{data.Key} : {data.Value}");
                }

                if(!string.IsNullOrEmpty(p.Identifier))
                {
                    System.Diagnostics.Debug.WriteLine($"ActionId: {p.Identifier}");
                }
             
 };
```

**Note: This is the event were you will navigate to an specific page/activity/viewcontroller, if needed**

### Push Notification Handler

A push notification handler is the way to provide ui push notification customization(on Android) and events feedback on native platforms by using **IPushNotificationHandler** interface. The plugin has a default push notification handler implementation and it's the one used by default.

On most common use cases the default implementation might be enough so a custom implementation might not be needed at all.

```csharp
public interface IPushNotificationHandler
{
        //Method triggered when an error occurs
        void OnError(string error);
        //Method triggered when a notification is opened
        void OnOpened(NotificationResponse response);
        //Method triggered when a notification is received
        void OnReceived(IDictionary<string, string> parameters);
}
```
An example of a custom handler use is the [DefaultPushNotificationHandler](../src/Plugin.PushNotification.Android/DefaultPushNotificationHandler.cs) which is the plugin default implementation to render the push notification ui when sending data messages and supporting notification actions on Android.

### Initialize using a PushHandler on Application class on Android and AppDelegate on iOS:

Application class **OnCreate** on Android:

```csharp
    #if DEBUG
      PushNotificationManager.Initialize(this,new CustomPushHandler(),true);
    #else
      PushNotificationManager.Initialize(this,new CustomPushHandler(),false);
    #endif
```
To keep the default push notification handler functionality but make small adjustments or customizations to the notification. You can inherit from **DefaultPushNotificationHandler** and implement **OnBuildNotification** method which can be used to e.g. load an image from the web and set it as the 'LargeIcon' of a notification (notificationBuilder.SetLargeIcon) or other modifications to the resulting notification.

**Note: If you use a custom push notification handler on Android, you will have full control on the notification arrival, so will be in charge of creating the notification ui for data messages when needed.**

AppDelegate **FinishLaunching** on iOS:
```csharp
      PushNotificationManager.Initialize(options,new CustomPushHandler());
```

<= Back to [Table of Contents](../README.md)

