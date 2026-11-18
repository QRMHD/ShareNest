using Microsoft.Maui.Controls.Shapes;
using ShareNest.Models;
using ShareNest.Services;

namespace ShareNest.Pages;

public partial class DepositTrustPage : ContentPage
{
    static readonly Color Teal = Color.FromArgb("#3FC1C9"), Navy = Color.FromArgb("#364F6B");
    Booking? _b;

    public DepositTrustPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _b = AppState.SelectedBooking ?? (await Db.GetBookingsAsync(false)).FirstOrDefault() ?? new Booking { ItemName = "Item", Deposit = 20 };
        bool returned = _b.Status == "Returned";
        bool pickedUp = _b.Status is "Active" or "Lent" or "Returned";

        StatusBadgeLbl.Text = returned ? "🟢 100% REFUNDED" : "🔒 HELD IN ESCROW";
        StatusBadgeLbl.TextColor = Color.FromArgb(returned ? "#0F6F78" : "#178C95");

        HeldLbl.Text = returned ? $"RM {_b.Deposit:0.00} Refunded" : $"RM {_b.Deposit:0.00} Held in Escrow";
        DeadlineLbl.Text = returned ? $"Item returned safely · Escrow released" : $"Return deadline: {_b.ReturnDate:d MMM}";
        
        string txn = string.IsNullOrEmpty(_b.TransactionId) ? $"SN-{10482 + _b.Id}" : _b.TransactionId;
        TxnLbl.Text = $"ShareNest Escrow Ref: #{txn}";

        PayMethodSummary.Text = string.IsNullOrEmpty(_b.PaymentMethod) ? "DuitNow QR" : _b.PaymentMethod;
        DepositPaidLbl.Text = $"RM {_b.Deposit:0.00}";
        NetRefundLbl.Text = $"RM {_b.Deposit:0.00}";
        RefundStatusTitle.Text = returned ? "Total Refunded" : "Estimated Refund upon Return";
        RefundDescLbl.Text = returned 
            ? "✓ Refund released to your original payment method." 
            : "✓ Deposit is protected and refunded in full upon safe handover.";

        StepsHost.Children.Clear();
        AddStep($"Reserved & Deposit Held — {_b.ReservedOn:d MMM}", true);
        AddStep($"Item Handed Over — {_b.PickupDate:d MMM}", pickedUp);
        AddStep($"Returned & Deposit Released — {_b.ReturnDate:d MMM}{(returned ? "" : " (pending)")}", returned);
    }

    void AddStep(string text, bool done)
    {
        var box = new Border
        {
            WidthRequest = 16, HeightRequest = 16, Padding = 0, StrokeThickness = 1.6, Stroke = Teal,
            StrokeShape = new RoundRectangle { CornerRadius = 4 }, BackgroundColor = done ? Teal : Colors.White
        };
        var row = new HorizontalStackLayout { Spacing = 10 };
        row.Add(box);
        row.Add(new Label { Text = text, FontSize = 14, TextColor = Navy, VerticalOptions = LayoutOptions.Center });
        StepsHost.Add(row);
    }

    async void OnReport(object? s, TappedEventArgs e)
    {
        var text = await DisplayPromptAsync("Report an issue", "Describe the problem with this deposit", "Send", "Cancel");
        if (!string.IsNullOrWhiteSpace(text)) await DisplayAlert("Report sent", "Thanks — the neighbourhood admins will review it.", "OK");
    }

    async void OnDone(object? s, EventArgs e) => await Shell.Current.GoToAsync("//bookings");
}
