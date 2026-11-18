using ShareNest.ViewModels;

namespace ShareNest.Pages;

public partial class AddListingPage : ContentPage
{
    readonly AddListingViewModel _vm = new();

    public AddListingPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    async void OnThumbTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is string path) await _vm.RemovePhotoAsync(path);
    }
}
