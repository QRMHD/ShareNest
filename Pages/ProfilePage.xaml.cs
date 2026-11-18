using ShareNest.Services;
using ShareNest.ViewModels;

namespace ShareNest.Pages;

public partial class ProfilePage : ContentPage
{
    readonly ProfileViewModel _vm = new();

    public ProfilePage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing() { base.OnAppearing(); await _vm.LoadAsync(); }

    async void OnListings(object? s, TappedEventArgs e)
    {
        var me = Session.User.FullName;
        var names = (await Db.GetItemsAsync()).Where(i => i.Owner == me).Select(i => $"{i.Name} · {i.StatusText}").ToArray();
        if (names.Length == 0) { await DisplayAlert("My listings", "You haven't listed anything yet. Tap + on the Catalog.", "OK"); return; }
        await DisplayActionSheet("My listings", "Close", null, names);
    }

    async void OnPaymentHistory(object? s, TappedEventArgs e)
    {
        var payments = await Db.GetAllPaymentsAsync();
        if (payments.Count == 0)
        {
            await DisplayAlert("Payment & Escrow History",
                "No digital payments recorded yet. Reserve an item to test DuitNow QR or FPX online banking payments.", "OK");
            return;
        }

        var lines = payments.Select(p =>
            $"{p.ReceiptNumber} · {p.ItemName}\nRM {p.Amount:0.00} · {p.MethodDisplayName} · {p.StatusDisplayName}").ToArray();

        await DisplayActionSheet("Payment & Escrow History", "Close", null, lines);
    }

    async void OnSignOut(object? s, TappedEventArgs e)
    {
        bool confirm = await DisplayAlert("Sign out", "Are you sure you want to sign out?", "Sign out", "Cancel");
        if (!confirm) return;
        await Db.SetSettingAsync("current_user", "");
        await Shell.Current.GoToAsync("login");           // Profile -> Login / Register
    }
    async void OnAbout(object? s, TappedEventArgs e) => await Shell.Current.GoToAsync("about");      // Profile -> About

    async void OnSettings(object? s, TappedEventArgs e)
    {
        bool notifications = await Db.GetBoolAsync("notifications", true);
        bool reminders = await Db.GetBoolAsync("reminders", true);
        var pick = await DisplayActionSheet("Settings & utilities", "Close", null,
            $"Notifications: {(notifications ? "On" : "Off")}",
            $"Return reminders: {(reminders ? "On" : "Off")}",
            "🔄 Reset Demo Data (Seed Data)");

        if (pick is not null && pick.StartsWith("Notifications"))
            await Db.SetBoolAsync("notifications", !notifications);
        else if (pick is not null && pick.StartsWith("Return"))
            await Db.SetBoolAsync("reminders", !reminders);
        else if (pick == "🔄 Reset Demo Data (Seed Data)")
        {
            bool confirm = await DisplayAlert("Reset Demo Data",
                "Are you sure you want to reset all items, bookings, and payments back to original seed data?",
                "Reset", "Cancel");
            if (confirm)
            {
                await Db.ResetDemoDataAsync();
                await _vm.LoadAsync();
                await DisplayAlert("Success", "All seed data has been restored to initial state.", "OK");
            }
        }
    }
}
