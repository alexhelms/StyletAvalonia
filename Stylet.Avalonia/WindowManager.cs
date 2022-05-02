using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Stylet.Avalonia.Logging;
using System.ComponentModel;
using AvaloniaApp = Avalonia.Application;

namespace Stylet.Avalonia;

/// <summary>
/// Manager capable of taking a ViewModel instance, instantiating its View and showing it as a dialog or window
/// </summary>
public interface IWindowManager
{
    /// <summary>
    /// Create or get the root window.
    /// </summary>
    /// <param name="rootViewModel">The root view model instance.</param>
    /// <returns>The root window.</returns>
    Window GetRootWindow(object rootViewModel);

    /// <summary>
    /// Given a ViewModel, show its corresponding View as a window
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    void ShowWindow(object viewModel);

    /// <summary>
    /// Given a ViewModel, show its corresponding View as a window, and set its owner
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <param name="ownerViewModel">The ViewModel for the View which should own this window</param>
    void ShowWindow(object viewModel, IViewAware? ownerViewModel);

    /// <summary>
    /// Given a ViewModel, show its corresponding View as a Dialog
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <returns>DialogResult of the View</returns>
    Task<TResult> ShowDialog<TResult>(object viewModel);
}

/// <summary>
/// Configuration passed to WindowManager (normally implemented by BootstrapperBase)
/// </summary>
public interface IWindowManagerConfig
{
    /// <summary>
    /// Returns the currently-displayed window, or null if there is none (or it can't be determined)
    /// </summary>
    /// <returns>The currently-displayed window, or null</returns>
    Window GetActiveWindow();
}

public class WindowManager : IWindowManager
{
    private static readonly ILogger logger = LogManager.GetLogger(typeof(WindowManager));
    private readonly IViewManager viewManager;
    private readonly Func<Window> getActiveWindow;

    public WindowManager(IViewManager viewManager, IWindowManagerConfig config)
    {
        this.viewManager = viewManager;
        this.getActiveWindow = config.GetActiveWindow;
    }

    public Window GetRootWindow(object rootViewModel)
    {
        var window = CreateWindow(rootViewModel, false);
        return window;
    }

    public void ShowWindow(object viewModel)
    {
        ShowWindow(viewModel, null);
    }

    public void ShowWindow(object viewModel, IViewAware? ownerViewModel)
    {
        var window = CreateWindow(viewModel, false);

        var explicitOwnerWindow = ownerViewModel as Window;
        if (explicitOwnerWindow is not null)
        {
            window.Show(explicitOwnerWindow);
        }
        else
        {
            var mainWindow = GetWindow();
            window.Show(mainWindow);
        }
    }

    public async Task<TResult> ShowDialog<TResult>(object viewModel)
    {
        var mainWindow = GetWindow();
        var dialogWindow = CreateWindow(viewModel, true);
        var result = await dialogWindow.ShowDialog<TResult>(mainWindow);
        return result;
    }

    private static Window? GetWindow()
    {
        var lifetime = AvaloniaApp.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        return lifetime?.MainWindow;
    }

    /// <summary>
    /// Given a ViewModel, create its View, ensure that it's a Window, and set it up
    /// </summary>
    /// <param name="viewModel">ViewModel to create the window for</param>
    /// <param name="isDialog">True if the window will be used as a dialog</param>
    /// <returns>Window which was created and set up</returns>
    protected virtual Window CreateWindow(object viewModel, bool isDialog)
    {
        var view = this.viewManager.CreateAndBindViewForModelIfNecessary(viewModel);
        var window = view as Window;
        if (window == null)
        {
            var e = new StyletInvalidViewTypeException(String.Format("WindowManager.ShowWindow or .ShowDialog tried to show a View of type '{0}', but that View doesn't derive from the Window class. " +
                    "Make sure any Views you display using WindowManager.ShowWindow or .ShowDialog derive from Window (not UserControl, etc)",
                    view == null ? "(null)" : view.GetType().Name));
            logger.Error(e);
            throw e;
        }

        // Only set this it hasn't been set / bound to anything
        var haveDisplayName = viewModel as IHaveDisplayName;
        if (haveDisplayName != null && (String.IsNullOrEmpty(window.Title) || window.Title == view.GetType().Name) && window.GetValue<string>(Window.TitleProperty) == null)
        {
            var binding = new Binding(nameof(IHaveDisplayName.DisplayName), BindingMode.TwoWay)
            {
                Source = viewModel,
            };
            window.Bind(Window.TitleProperty, binding);
        }

        if (isDialog)
        {
            var owner = this.InferOwnerOf(window);
            if (owner != null)
            {
                // We can end up in a really weird situation if they try and display more than one dialog as the application's closing
                // Basically the MainWindow's no long active, so the second dialog chooses the first dialog as its owner... But the first dialog
                // hasn't yet been shown, so we get an exception ("cannot set owner property to a Window which has not been previously shown").
                try
                {
                    // TODO: avalonia does not support setting the owner
                    throw new NotImplementedException();
                    //window.Owner = owner;
                }
                catch (InvalidOperationException e)
                {
                    logger.Error(e, "This can occur when the application is closing down");
                }
            }
        }

        if (isDialog)
        {
            logger.Info("Displaying ViewModel {0} with View {1} as a Dialog", viewModel, window);
        }
        else
        {
            logger.Info("Displaying ViewModel {0} with View {1} as a Window", viewModel, window);
        }

        // TODO: Fix me, window.Top and window.Left, as well as the bindings do not exist

        //// If and only if they haven't tried to position the window themselves...
        //// Has to be done after we're attempted to set the owner
        //if (window.WindowStartupLocation == WindowStartupLocation.Manual && Double.IsNaN(window.Top) && Double.IsNaN(window.Left) &&
        //    BindingOperations.GetBinding(window, Window.TopProperty) == null && BindingOperations.GetBinding(window, Window.LeftProperty) == null)
        //{
        //    window.WindowStartupLocation = window.Owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
        //}

        // This gets itself retained by the window, by registering events
        // ReSharper disable once ObjectCreationAsStatement
        new WindowConductor(window, viewModel);

        return window;
    }

    private Window InferOwnerOf(Window window)
    {
        var active = this.getActiveWindow();
        return ReferenceEquals(active, window) ? null : active;
    }

    private class WindowConductor : IChildDelegate
    {
        private readonly Window window;
        private readonly object viewModel;
        private readonly IDisposable _windowStateChangedDisposable;

        public WindowConductor(Window window, object viewModel)
        {
            this.window = window;
            this.viewModel = viewModel;

            // They won't be able to request a close unless they implement IChild anyway...
            var viewModelAsChild = this.viewModel as IChild;
            if (viewModelAsChild != null)
                viewModelAsChild.Parent = this;

            ScreenExtensions.TryActivate(this.viewModel);

            var viewModelAsScreenState = this.viewModel as IScreenState;
            if (viewModelAsScreenState != null)
            {
                var observable = window.GetObservable(Window.WindowStateProperty);
                _windowStateChangedDisposable = observable.Subscribe(windowState => WindowStateChanged(windowState));

                window.Closed += this.WindowClosed;
            }

            if (this.viewModel is IGuardClose)
                window.Closing += this.WindowClosing;
        }

        private void WindowStateChanged(WindowState windowState)
        {
            switch (this.window.WindowState)
            {
                case WindowState.Maximized:
                case WindowState.Normal:
                    logger.Info("Window {0} maximized/restored: activating", this.window);
                    ScreenExtensions.TryActivate(this.viewModel);
                    break;

                case WindowState.Minimized:
                    logger.Info("Window {0} minimized: deactivating", this.window);
                    ScreenExtensions.TryDeactivate(this.viewModel);
                    break;
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            // Logging was done in the Closing handler

            _windowStateChangedDisposable.Dispose();
            
            this.window.Closed -= this.WindowClosed;
            this.window.Closing -= this.WindowClosing; // Not sure this is required

            ScreenExtensions.TryClose(this.viewModel);
        }

        private async void WindowClosing(object sender, CancelEventArgs e)
        {
            if (e.Cancel)
                return;

            logger.Info("ViewModel {0} close requested because its View was closed", this.viewModel);

            // See if the task completed synchronously
            var task = ((IGuardClose)this.viewModel).CanCloseAsync();
            if (task.IsCompleted)
            {
                // The closed event handler will take things from here if we don't cancel
                if (!task.Result)
                    logger.Info("Close of ViewModel {0} cancelled because CanCloseAsync returned false", this.viewModel);
                e.Cancel = !task.Result;
            }
            else
            {
                e.Cancel = true;
                logger.Info("Delaying closing of ViewModel {0} because CanCloseAsync is completing asynchronously", this.viewModel);
                if (await task)
                {
                    this.window.Closing -= this.WindowClosing;
                    this.window.Close();
                    // The Closed event handler handles unregistering the events, and closing the ViewModel
                }
                else
                {
                    logger.Info("Close of ViewModel {0} cancelled because CanCloseAsync returned false", this.viewModel);
                }
            }
        }

        /// <summary>
        /// Close was requested by the child
        /// </summary>
        /// <param name="item">Item to close</param>
        /// <param name="dialogResult">DialogResult to close with, if it's a dialog</param>
        async void IChildDelegate.CloseItem(object item)
        {
            if (item != this.viewModel)
            {
                logger.Warn("IChildDelegate.CloseItem called with item {0} which is _not_ our ViewModel {1}", item, this.viewModel);
                return;
            }

            var guardClose = this.viewModel as IGuardClose;
            if (guardClose != null && !await guardClose.CanCloseAsync())
            {
                logger.Info("Close of ViewModel {0} cancelled because CanCloseAsync returned false", this.viewModel);
                return;
            }

            _windowStateChangedDisposable.Dispose();

            this.window.Closed -= this.WindowClosed;
            this.window.Closing -= this.WindowClosing;

            ScreenExtensions.TryClose(this.viewModel);

            this.window.Close();
        }
    }
}