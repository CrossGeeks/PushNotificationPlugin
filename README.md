## Push Notification Plugin for Xamarin iOS and Android
Simple cross platform plugin for handling push notifications. Uses FCM for Android and APS for iOS.

### Setup
* Available on NuGet: http://www.nuget.org/packages/Plugin.PushNotification [![NuGet](https://img.shields.io/nuget/v/Plugin.PushNotification.svg?label=NuGet)](https://www.nuget.org/packages/Plugin.PushNotification/)
* Install into your PCL project and Client projects.

**Platform Support**

|Platform|Version|Tested
| ------------------- | :------------------: |
|Xamarin.iOS|iOS 8+,|iOS13|
|Xamarin.Android|API 15+|API 28|

### API Usage

Call **CrossPushNotification.Current** from any project or PCL to gain access to APIs.

## Features

- Receive push notifications
- Support for push notification category actions
- Customize push notifications
- Localization
- Storage of the push token can be overwritten by a delegate.


## Documentation

Here you will find detailed documentation on setting up and using the Push Notification Plugin for Xamarin

* [Getting Started](docs/GettingStarted.md)
* [Receiving Push Notifications](docs/ReceivingNotifications.md)
* [Android Customization](docs/AndroidCustomization.md)
* [iOS Customization](docs/iOSCustomization.md)
* [Notification Category Actions](docs/NotificationActions.md)
* [Notification Localization](docs/LocalizedPushNotifications.md)
* [FAQ](docs/FAQ.md)

#### Contributors

* [Rendy Del Rosario](https://github.com/rdelrosario)
* [Charlin Agramonte](https://github.com/char0394)
* [Alberto Florenzan](https://github.com/aflorenzan)
* [Angel Andres Mañon](https://github.com/AngelAndresM)
* [Tymen Steur](https://github.com/TymenSteur)
* [Sameer Khandekar](https://github.com/sameerkapps)
* [Px7-941](https://github.com/Px7-941)

