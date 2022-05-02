using Autofac;
using AvaloniaApplication1.ViewModels;
using Stylet.Avalonia;

namespace AvaloniaApplication1;

public class Bootstrapper : AutofacBootstrapper<MainViewModel>
{
    protected override void OnStart()
    {
        Stylet.Avalonia.Logging.LogManager.Enabled = true;
    }

    protected override void ConfigureIoC(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(Bootstrapper).Assembly)
            .Where(t => typeof(IScreen).IsAssignableFrom(t))
            .Where(t => t.IsClass)
            .Where(t => !t.IsAbstract)
            .AsSelf();

        builder.RegisterAssemblyTypes(typeof(Bootstrapper).Assembly)
            .Where(t => typeof(Avalonia.Controls.Window).IsAssignableFrom(t))
            .Where(t => t.IsClass)
            .Where(t => !t.IsAbstract)
            .AsSelf();

        builder.RegisterAssemblyTypes(typeof(Bootstrapper).Assembly)
            .Where(t => typeof(Avalonia.Controls.UserControl).IsAssignableFrom(t))
            .Where(t => t.IsClass)
            .Where(t => !t.IsAbstract)
            .AsSelf();
    }
}
