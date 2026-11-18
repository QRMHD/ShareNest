using ShareNest.ViewModels;

namespace ShareNest.Pages;

public partial class ActivityPage : ContentPage
{
    readonly ActivityViewModel _vm = new();

    public ActivityPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }
}
