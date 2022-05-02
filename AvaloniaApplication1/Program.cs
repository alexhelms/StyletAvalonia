using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace AvaloniaApplication1;

internal class Program
{
    public static Bootstrapper Bootstrapper { get; } = new();

    [STAThread]
    public static void Main(string[] args)
        => BuildAvaloniaApp()
            .Start(AppMain, args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    static void AppMain(Application application, string[] args)
    {
        var lifetime = new ClassicDesktopStyleApplicationLifetime
        {
            ShutdownMode = ShutdownMode.OnMainWindowClose,
            Args = args,
        };

        var app = (App)application;
        app.ApplicationLifetime = lifetime;
        app.DataTemplates.Add(Bootstrapper);

        Bootstrapper.Setup(app);
        Bootstrapper.Start(args);

        lifetime.Start(args);
    }
}
