using ShareNest.Models;
using ShareNest.Services;
using ShareNest.ViewModels;

namespace ShareNest.Pages;

public partial class CatalogPage : ContentPage
{
    readonly CatalogViewModel _vm = new();

    public CatalogPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }

    void OnFilter(object? sender, EventArgs e) => _vm.FilterCommand.Execute(null);

    async void OnItemTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Item item) return;
        AppState.SelectedItem = item;
        await Shell.Current.GoToAsync("details");         // Catalog -> Item Details
    }

    async void OnAdd(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync("add");   // FAB -> Add Listing
}
