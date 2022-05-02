using Stylet.Avalonia;

namespace AvaloniaApplication1.ViewModels;

public class MainViewModel : Conductor<IScreen>.Collection.OneActive
{
    public MainViewModel(Func<TabViewModel> childFunc)
    {
        var child1 = childFunc();
        var child2 = childFunc();
        var child3 = childFunc();

        child1.DisplayName = "Child 1";
        child2.DisplayName = "Child 2";
        child3.DisplayName = "Child 3";

        Items.AddRange(new[] { child1, child2, child3 });

        ActiveItem = Items.First();
    }
}
