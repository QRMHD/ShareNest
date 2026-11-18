using System;
using ShareNest.Models;
using ShareNest.Services;

namespace ShareNest.Pages;

public partial class ReceiptPage : ContentPage
{
    public ReceiptPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var payment = AppState.RecentPayment;
        var booking = AppState.SelectedBooking;
        var item = AppState.SelectedItem;

        decimal amount = payment?.Amount ?? booking?.Deposit ?? item?.Deposit ?? 20;
        string receiptNo = payment?.ReceiptNumber ?? $"SN-REC-{Random.Shared.Next(100000, 999999)}";
        string txn = payment?.Reference ?? booking?.TransactionId ?? "SN-TXN";
        string method = payment?.MethodDisplayName ?? booking?.PaymentMethod ?? "DuitNow QR";
        string itemName = payment?.ItemName ?? booking?.ItemName ?? item?.Name ?? "Equipment";

        ReceiptNoLbl.Text = receiptNo;
        TxnRefLbl.Text = txn;
        DateTimeLbl.Text = DateTime.Now.ToString("dd MMM yyyy, h:mm tt");
        MethodLbl.Text = method;
        ItemNameLbl.Text = itemName;
        DepositLbl.Text = $"RM {amount:0.00}";
        TotalPaidLbl.Text = $"RM {amount:0.00}";

        if (booking is not null)
        {
            DatesLbl.Text = $"{booking.PickupDate:d MMM} → {booking.ReturnDate:d MMM yyyy}";
        }
        else
        {
            DatesLbl.Text = $"{DateTime.Today.AddDays(1):d MMM} → {DateTime.Today.AddDays(3):d MMM yyyy}";
        }
    }

    async void OnViewEscrowTimeline(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("deposit");
    }

    async void OnDoneToBookings(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//bookings");
    }

    async void OnShareReceipt(object? sender, EventArgs e)
    {
        string receiptNo = ReceiptNoLbl.Text;
        await DisplayAlert("Receipt Saved",
            $"Electronic Receipt #{receiptNo} has been saved to your downloads and transaction record.",
            "OK");
    }
}
