using Autofac;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Stylet.Avalonia;
using System.Reflection;

namespace AvaloniaApplication1;

public class AutofacBootstrapper<TRootViewModel> : BootstrapperBase where TRootViewModel : class
{
    private IContainer container;

    private TRootViewModel _rootViewModel;

    protected virtual TRootViewModel RootViewModel
    {
        get { return this._rootViewModel ?? (this._rootViewModel = (TRootViewModel)this.GetInstance(typeof(TRootViewModel))); }
    }

    protected override void ConfigureBootstrapper()
    {
        var builder = new ContainerBuilder();
        this.DefaultConfigureIoC(builder);
        this.ConfigureIoC(builder);
        this.container = builder.Build();
    }

    /// <summary>
    /// Carries out default configuration of the IoC container. Override if you don't want to do this
    /// </summary>
    protected virtual void DefaultConfigureIoC(ContainerBuilder builder)
    {
        var viewManagerConfig = new ViewManagerConfig()
        {
            ViewFactory = this.GetInstance,
            ViewAssemblies = new List<Assembly>() { this.GetType().Assembly }
        };
        builder.RegisterInstance<IViewManager>(new ViewManager(viewManagerConfig));

        builder.RegisterInstance<IWindowManagerConfig>(this).ExternallyOwned();
        builder.RegisterType<WindowManager>().As<IWindowManager>().SingleInstance();

        // See https://github.com/canton7/Stylet/discussions/211
        builder.RegisterAssemblyTypes(this.GetType().Assembly)
            .Where(x => !x.Name.Contains("ProcessedByFody"))
            .Where(x => !x.FullName?.StartsWith("CompiledAvaloniaXaml") ?? true)    // compiled xaml
            .Where(x => !x.FullName?.Contains("+XamlClosure") ?? true)              // compiled xaml
            .ExternallyOwned();
    }

    /// <summary>
    /// Override to add your own types to the IoC container.
    /// </summary>
    protected virtual void ConfigureIoC(ContainerBuilder builder) { }

    protected override void Launch()
    {
        var lifetime = (ClassicDesktopStyleApplicationLifetime)Application.ApplicationLifetime!;
        lifetime.MainWindow = base.GetRootView(this.RootViewModel);
        lifetime.MainWindow.Show();
    }

    public override object GetInstance(Type type)
    {
        return this.container.Resolve(type);
    }

    public override void Dispose()
    {
        ScreenExtensions.TryDispose(this._rootViewModel);
        if (this.container != null)
            this.container.Dispose();

        base.Dispose();
    }
}
