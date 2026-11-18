using ShareNest.Services;

namespace ShareNest.Pages;

public partial class ItemDetailsPage : ContentPage
{
    public ItemDetailsPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var it = AppState.SelectedItem ?? (await Db.GetItemsAsync()).First();
        AppState.SelectedItem = it;
        var first = it.Owner.Split(' ')[0];
        bool isOwn = string.Equals(it.Owner, Session.User.FullName, StringComparison.OrdinalIgnoreCase);

        OwnerLbl.Text = isOwn ? $"{it.Owner} (You)" : it.Owner;
        Avatar.IsVisible = it.Owner == "Amir Hassan";
        Initial.IsVisible = !Avatar.IsVisible;
        Initial.Text = it.Owner.Substring(0, 1);

        var listingCount = (await Db.GetItemsAsync()).Count(i => i.Owner == it.Owner);
        OwnerStatsLbl.Text = $"★ 4.9 · {listingCount} listings · Block C";

        var activeBookings = await Db.GetActiveBookingsForItemAsync(it.Name);
        bool unavailable = it.IsUnavailable;

        NameLbl.Text = it.Name.ToUpper();
        StatusLbl.Text = isOwn
            ? $"Listed by you (Owner) · {(it.IsBorrowed ? "Borrowed right now" : it.IsReserved ? "Reserved by someone" : "Available in catalog")}"
            : $"Listed by {first} · {(it.IsBorrowed ? "Borrowed right now" : it.IsReserved ? "Reserved" : "Available now")}";
        SpecLbl.Text = it.Specs;
        DepositLbl.Text = $"RM {it.Deposit:0.00}";

        if (activeBookings.Count > 0)
        {
            var next = activeBookings.OrderBy(b => b.PickupDate).First();
            AvailLbl.Text = $"Booked {next.PickupDate:d MMM} – {next.ReturnDate:d MMM}";
        }
        else
            AvailLbl.Text = isOwn ? "Your active listing" : "Available — no upcoming bookings";

        BigIcon.IsVisible = false;
        if (!string.IsNullOrEmpty(it.PhotoPath)) Photo.Source = ImageSource.FromFile(it.PhotoPath);
        else if (!string.IsNullOrEmpty(it.Photo)) Photo.Source = it.Photo;
        else { Photo.Source = null; BigIcon.Source = it.IconFile; BigIcon.IsVisible = true; }

        if (isOwn)
        {
            MessageBtn.Text = "Delete listing";
            MessageBtn.TextColor = (Color)Application.Current!.Resources["PinkText"];
            MessageBtn.BorderColor = (Color)Application.Current!.Resources["PinkText"];

            ReserveBtn.Text = "Your listing (Cannot reserve)";
            ReserveBtn.BackgroundColor = (Color)Application.Current!.Resources["LineGrey"];
            ReserveBtn.IsEnabled = false;
        }
        else
        {
            MessageBtn.Text = $"Message {first}";
            MessageBtn.TextColor = (Color)Application.Current!.Resources["Navy"];
            MessageBtn.BorderColor = (Color)Application.Current!.Resources["Navy"];

            ReserveBtn.Text = unavailable ? "Currently unavailable" : "Reserve this item";
            ReserveBtn.BackgroundColor = (Color)Application.Current!.Resources[unavailable ? "LineGrey" : "Navy"];
            ReserveBtn.IsEnabled = !unavailable;
        }
    }

    async void OnMessage(object? sender, EventArgs e)
    {
        var it = AppState.SelectedItem!;
        bool isOwn = string.Equals(it.Owner, Session.User.FullName, StringComparison.OrdinalIgnoreCase);

        if (isOwn)
        {
            bool del = await DisplayAlert("Delete listing", $"Are you sure you want to remove “{it.Name}” from the catalog?", "Delete", "Cancel");
            if (!del) return;
            await Db.DeleteItemAsync(it.Id);
            await DisplayAlert("Listing deleted", $"“{it.Name}” was removed from the catalog.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        var msg = await DisplayPromptAsync($"Message {it.Owner.Split(' ')[0]}", "Write your message", "Send", "Cancel",
                                           $"Hi, is the {it.Name} still available?");
        if (!string.IsNullOrWhiteSpace(msg))
            await DisplayAlert("Message sent", $"{it.Owner.Split(' ')[0]} will reply in your inbox.", "OK");
    }

    async void OnReserve(object? sender, EventArgs e)
    {
        var it = AppState.SelectedItem;
        if (it is not null && string.Equals(it.Owner, Session.User.FullName, StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlert("Cannot reserve", "You cannot reserve your own item.", "OK");
            return;
        }
        await Shell.Current.GoToAsync("reserve");   // Item Details -> Reserve
    }
}
