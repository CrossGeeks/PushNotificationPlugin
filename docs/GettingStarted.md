## Starting with Android

### Android Configuration

Edit AndroidManifest.xml and insert the following receiver elements **inside** the **application** section:

```xml
<receiver 
    android:name="com.google.firebase.iid.FirebaseInstanceIdInternalReceiver" 
    android:exported="false" />
<receiver 
    android:name="com.google.firebase.iid.FirebaseInstanceIdReceiver" 
    android:exported="true" 
    android:permission="com.google.android.c2dm.permission.SEND">
    <intent-filter>
        <action android:name="com.google.android.c2dm.intent.RECEIVE" />
        <action android:name="com.google.android.c2dm.intent.REGISTRATION" />
        <category android:name="${applicationId}" />
    </intent-filter>
</receiver>
```
Also add this permission:

```xml
<uses-permission android:name="android.permission.INTERNET" />
```

Add google-services.json to Android project. Make sure build action is GoogleServicesJson

![ADD JSON](https://github.com/CrossGeeks/FirebasePushNotificationPlugin/blob/master/images/android-googleservices-json.png?raw=true)

Must compile against 24+ as plugin is using API 24 specific things. Here is a great breakdown: http://redth.codes/such-android-api-levels-much-confuse-wow/ (Android project must be compiled using 7.0+ target framework)

### Android Initialization

You should initialize the plugin on an Android Application class if you don't have one on your project, should create an application class. Then call **PushNotificationManager.Initialize** method on OnCreate.

There are 3 overrides to **PushNotificationManager.Initialize**:

- **PushNotificationManager.Initialize(Context context, bool resetToken)** : Default method to initialize plugin without supporting any user notification categories. Uses a DefaultPushHandler to provide the ui for the notification.

- **PushNotificationManager.Initialize(Context context, NotificationUserCategory[] categories, bool resetToken)**  : Initializes plugin using user notification categories. Uses a DefaultPushHandler to provide the ui for the notification supporting buttons based on the action_click send on the notification

- **PushNotificationManager.Initialize(Context context,IPushNotificationHandler pushHandler, bool resetToken)** : Initializes the plugin using a custom push notification handler to provide custom ui and behaviour notifications receipt and opening.

**Important: While debugging set resetToken parameter to true.**

Example of initialization:

```csharp

    [Application]
    public class MainApplication : Application
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transer) :base(handle, transer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            
            //If debug you should reset the token each time.
            #if DEBUG
              PushNotificationManager.Initialize(this,true);
            #else
              PushNotificationManager.Initialize(this,false);
            #endif

              //Handle notification when app is closed here
              CrossPushNotification.Current.OnNotificationReceived += (s,p) =>
              {


              };
         }
    }

```

On your main launcher activity **OnCreate** method

```csharp
 PushNotificationManager.ProcessIntent(Intent);
 ```

 **Note: When using Xamarin Forms do it just after LoadApplication call.**

## Starting with iOS 

### iOS Configuration

On Info.plist enable remote notification background mode

![Remote notifications](https://github.com/CrossGeeks/FirebasePushNotificationPlugin/blob/master/images/iOS-enable-remote-notifications.png?raw=true)

### iOS Initialization

There are 3 overrides to **PushNotificationManager.Initialize**:

- **PushNotificationManager.Initialize(NSDictionary options,bool autoRegistration)** : Default method to initialize plugin without supporting any user notification categories. Auto registers for push notifications if second parameter is true.

- **PushNotificationManager.Initialize(NSDictionary options, NotificationUserCategory[] categories)**  : Initializes plugin using user notification categories to support iOS notification actions.

- **PushNotificationManager.Initialize(NSDictionary options,IPushNotificationHandler pushHandler)** : Initializes the plugin using a custom push notification handler to provide native feedback of notifications event on the native platform.


Call  **PushNotificationManager.Initialize** on AppDelegate FinishedLaunching
```csharp

PushNotificationManager.Initialize(options,true);

```
 **Note: When using Xamarin Forms do it just after LoadApplication call.**

Also should override these methods and make the following calls:
```csharp
        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
             PushNotificationManager.DidRegisterRemoteNotifications(deviceToken);
        }

        public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        {
            PushNotificationManager.RemoteNotificationRegistrationFailed(error);

        }
        // To receive notifications in foregroung on iOS 9 and below.
        // To receive notifications in background in any iOS version
        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            
            PushNotificationManager.DidReceiveMessage(userInfo);
        }

      
```


## Using Push Notification APIs
It is drop dead simple to gain access to the PushNotification APIs in any project. All you need to do is get a reference to the current instance of IPushNotification via `CrossPushNotification.Current`:

### Events

Once token is registered/refreshed you will get it on **OnTokenRefresh** event.


```csharp
   /// <summary>
   /// Event triggered when token is refreshed
   /// </summary>
    event PushNotificationTokenEventHandler OnTokenRefresh;
```

```csharp        
  /// <summary>
  /// Event triggered when a notification is received
  /// </summary>
  event PushNotificationResponseEventHandler OnNotificationReceived;
```


```csharp        
  /// <summary>
  /// Event triggered when a notification is opened
  /// </summary>
  event PushNotificationResponseEventHandler OnNotificationOpened;
```

```csharp        
  /// <summary>
  /// Event triggered when there's an error
  /// </summary>
  event PushNotificationErrorEventHandler OnNotificationError;
```

Token event usage sample:
```csharp

  CrossPushNotification.Current.OnTokenRefresh += (s,p) =>
  {
        System.Diagnostics.Debug.WriteLine($"TOKEN : {p.Token}");
  };

```

Push message received event usage sample:
```csharp

  CrossPushNotification.Current.OnNotificationReceived += (s,p) =>
  {
 
        System.Diagnostics.Debug.WriteLine("Received");
    
  };

```

Push message opened event usage sample:
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




<= Back to [Table of Contents](../README.md)
