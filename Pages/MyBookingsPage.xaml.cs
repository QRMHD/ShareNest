using ShareNest.Models;
using ShareNest.Services;
using ShareNest.ViewModels;

namespace ShareNest.Pages;

public partial class MyBookingsPage : ContentPage
{
    readonly BookingsViewModel _vm = new();

    public MyBookingsPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }

    async void OnBookingTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Booking b) return;
        if (b.Status == "Returned")
        {
            await DisplayAlert("Returned", $"{b.ItemName} was already returned. Nothing left to do.", "OK");
            return;
        }
        AppState.SelectedBooking = b;
        await Shell.Current.GoToAsync("handover");        // Bookings -> Handover Log
    }
}
