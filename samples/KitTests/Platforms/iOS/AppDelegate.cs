using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace KitTests;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
