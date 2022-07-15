# Stylet Avalonia

This is a proof of concept port of [Stylet](https://github.com/canton7/Stylet) to [Avalonia](https://github.com/AvaloniaUI/Avalonia). The goal of this port was to get Conductors and Screens working in Avalonia as I much prefer that architecture over ReactiveUI.

See further discussion here: https://github.com/canton7/Stylet/discussions/342

This implementation depends on Avalonia, similarly to how Stylet depends on WPF. Additionally, this version depends on `Microsoft.Toolkit.Mvvm`. This allows for the removal of  `PropertyChangedBase` and `EventAggregator`.

## Working Stylet Features

- Conductors
- Screens
- BindableCollection
- Execute

## Unsupported Features

I found a few unsupported features in Avalonia. I'm not too familiar with Avalonia yet, some of these can probably be worked around.

1. There is no `Loaded` and `Unloaded` events, ~~so `OnViewLoaded()` does not work~~. See https://github.com/AvaloniaUI/Avalonia/issues/7908. There is a workaround using events for attaching and detaching from the visual tree.

2. Avalonia windows do not allow the owner to be explicitly set.

3. Avalonia windows do not have `Top` and `Left`, so the start up location logic in WindowManager is disabled. Perhaps we need to listen to changes in `BoundsProperty`?
