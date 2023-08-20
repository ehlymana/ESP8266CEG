using Android.App;
using MobileController;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

[assembly: Application(UsesCleartextTraffic = true)]

[assembly: UsesPermission(Android.Manifest.Permission.WriteExternalStorage)]

[assembly: UsesPermission(Android.Manifest.Permission.Camera)]