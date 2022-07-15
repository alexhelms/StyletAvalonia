using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Threading;

namespace Stylet.Avalonia;

public abstract class BootstrapperBase : IWindowManagerConfig, IDisposable, IDataTemplate
{
    /// <summary>
    /// Gets the current application
    /// </summary>
    public Application Application { get; private set; }

    /// <summary>
    /// Gets the command line arguments that were passed to the application from either the command prompt or the desktop.
    /// </summary>
    public string[] Args { get; private set; }

    /// <summary>
    /// Called by the ApplicationLoader when this bootstrapper is loaded
    /// </summary>
    /// <remarks>
    /// If you're constructing the bootstrapper yourself, call this manully and pass in the Application
    /// (probably <see cref="Application.Current"/>). Stylet will start when <see cref="Application.Startup"/>
    /// is fired. If no Application is available, do not call this but instead call <see cref="Start(string[])"/>.
    /// (In this case, note that the <see cref="Execute"/> methods will all dispatch synchronously, unless you
    /// set <see cref="Execute.Dispatcher"/> yourself).
    /// </remarks>
    /// <param name="application">Application within which Stylet is running</param>
    public void Setup(Application application)
    {
        if (application == null)
            throw new ArgumentNullException("application");

        this.Application = application;

        // Use the current application's dispatcher for Execute
        Execute.Dispatcher = new ApplicationDispatcher(Dispatcher.UIThread);

        var lifetime = this.Application.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        //lifetime!.Startup += (o, e) => this.Start(e.Args);
        // Make life nice for the app - they can handle these by overriding Bootstrapper methods, rather than adding event handlers
        lifetime!.Exit += (o, e) =>
        {
            this.OnExit(e);
            this.Dispose();
        };
    }

    public IControl Build(object data)
    {
        var viewManager = (IViewManager)this.GetInstance(typeof(IViewManager));
        return viewManager.CreateAndBindViewForModelIfNecessary(data);
    }

    public bool Match(object data)
    {
        return data is IScreen;
    }

    /// <summary>
    /// Called on Application.Startup, this does everything necessary to start the application
    /// </summary>
    /// <remarks>
    /// If you're constructing the bootstrapper yourself, and aren't able to call <see cref="Setup(Application)"/>,
    /// (e.g. because an Application isn't available), you must call this yourself.
    /// </remarks>
    /// <param name="args">Command-line arguments used to start this executable</param>
    public virtual void Start(string[] args)
    {
        // Set this before anything else, so everything can use it
        this.Args = args;
        this.OnStart();

        this.ConfigureBootstrapper();

        this.Configure();

        this.Launch();
        this.OnLaunch();
    }

    /// <summary>
    /// Hook called after the IoC container has been set up
    /// </summary>
    protected virtual void Configure() { }

    /// <summary>
    /// Called when the application is launched. Should display the root view using <see cref="DisplayRootView(object)"/>
    /// </summary>
    protected abstract void Launch();

    /// <summary>
    /// Launch the root view
    /// </summary>
    protected virtual Window GetRootView(object rootViewModel)
    {
        var windowManager = (IWindowManager)this.GetInstance(typeof(IWindowManager));
        return windowManager.GetRootWindow(rootViewModel);
    }

    /// <summary>
    /// Returns the currently-displayed window, or null if there is none (or it can't be determined)
    /// </summary>
    /// <returns>The currently-displayed window, or null</returns>
    public virtual Window GetActiveWindow()
    {
        var lifetime = this.Application.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        return lifetime!.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive) ?? lifetime!.MainWindow;
    }

    /// <summary>
    /// Override to configure your IoC container, and anything else
    /// </summary>
    protected virtual void ConfigureBootstrapper() { }

    /// <summary>
    /// Given a type, use the IoC container to fetch an instance of it
    /// </summary>
    /// <param name="type">Type of instance to fetch</param>
    /// <returns>Fetched instance</returns>
    public abstract object GetInstance(Type type);

    /// <summary>
    /// Called on application startup. This occur after this.Args has been assigned, but before the IoC container has been configured
    /// </summary>
    protected virtual void OnStart() { }

    /// <summary>
    /// Called just after the root View has been displayed
    /// </summary>
    protected virtual void OnLaunch() { }

    /// <summary>
    /// Hook called on application exit
    /// </summary>
    /// <param name="e">The exit event data</param>
    protected virtual void OnExit(ControlledApplicationLifetimeExitEventArgs e) { }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public virtual void Dispose() { }
}
