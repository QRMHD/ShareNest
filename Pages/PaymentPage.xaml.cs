using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using ShareNest.Controls;
using ShareNest.Models;
using ShareNest.Services;

namespace ShareNest.Pages;

public partial class PaymentPage : ContentPage
{
    record BankItem(string Name, string PortalName, string HeaderBg, string HeaderFg, string BadgeColor, string Initials);

    readonly List<BankItem> _banks = new()
    {
        new("Maybank", "Maybank2u", "#FFBC00", "#1C355E", "#FFBC00", "MBB"),
        new("CIMB Bank", "CIMB Clicks", "#ED1C24", "#FFFFFF", "#ED1C24", "CIMB"),
        new("Public Bank", "PBe Online Banking", "#D90000", "#FFFFFF", "#D90000", "PBB"),
        new("RHB Bank", "RHB Now", "#0072CE", "#FFFFFF", "#0072CE", "RHB"),
        new("Hong Leong Bank", "HLB Connect", "#0C2340", "#FFFFFF", "#0C2340", "HLB"),
        new("Bank Islam", "BIMB Online", "#007A87", "#FFFFFF", "#007A87", "BIMB"),
        new("AmBank", "AmOnline", "#E21836", "#FFFFFF", "#E21836", "AMB")
    };

    BankItem _selectedBank;
    IDispatcherTimer? _qrTimer;
    int _secondsRemaining = 299; // 4 mins 59 secs
    decimal _deposit = 20;
    string _reference = "SN-84920";
    string _activeMethodKey = "fpx";

    public PaymentPage()
    {
        InitializeComponent();
        _selectedBank = _banks[0]; // Maybank default
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var item = AppState.SelectedItem;
        var booking = AppState.SelectedBooking;
        if (item is null || booking is null) return;

        _deposit = item.Deposit;
        _reference = string.IsNullOrEmpty(booking.TransactionId) ? $"SN-{Random.Shared.Next(10000, 99999)}" : booking.TransactionId;

        ItemNameLbl.Text = item.Name;
        DepositLbl.Text = $"RM {_deposit:0.00}";
        DatesLbl.Text = $"{booking.PickupDate:d MMM yyyy} → {booking.ReturnDate:d MMM yyyy}";

        QrAmountLbl.Text = $"RM {_deposit:0.00}";
        QrRefLbl.Text = $"Ref: {_reference} · Acquirer: PayNet";
        GatewayAmountLbl.Text = $"RM {_deposit:0.00}";
        GatewayRefLbl.Text = $"FPX-{_reference}";
        ProceedFpxBtn.Text = $"Proceed to {_selectedBank.PortalName} (RM {_deposit:0.00})";
        PayCardBtn.Text = $"Pay RM {_deposit:0.00} Securely";

        // Cash on Pickup labels
        CashItemNameLbl.Text = item.Name;
        CashOwnerLbl.Text = $"{item.Owner} (Equipment Owner)";
        CashPickupDateLbl.Text = $"{booking.PickupDate:d MMM yyyy} at {DateTime.Today.Add(AppState.PickupTime):h:mm tt}";
        CashAmountLbl.Text = $"RM {_deposit:0.00}";
        ConfirmCashBtn.Text = $"Confirm Cash on Pickup Reservation (RM {_deposit:0.00})";

        // Setup QR Drawable
        QrCanvas.Drawable = new DuitNowQrDrawable
        {
            Payload = $"DuitNow.ShareNest.Escrow.{_reference}.RM{_deposit:0.00}"
        };

        // Initialize bank card visuals
        UpdateBankSelectionVisuals();

        // Switch to the specific payment method selected on Reserve screen
        string method = booking.PaymentMethod;
        if (method.Contains("Cash", StringComparison.OrdinalIgnoreCase))
            SetMethod("cash");
        else if (method.Contains("FPX", StringComparison.OrdinalIgnoreCase))
            SetMethod("fpx");
        else if (method.Contains("Card", StringComparison.OrdinalIgnoreCase))
            SetMethod("card");
        else
            SetMethod("qr");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopQrTimer();
    }

    // ---------------------------------------------------------------- Method Switching
    void SetMethod(string key)
    {
        _activeMethodKey = key;

        SectionFpx.IsVisible = (key == "fpx");
        SectionQr.IsVisible = (key == "qr");
        SectionCard.IsVisible = (key == "card");
        SectionCash.IsVisible = (key == "cash");

        if (key == "cash")
        {
            PageAppBar.Title = "Cash on Pickup";
            SelectedMethodLbl.Text = "💵 Cash on Pickup (In-Person Handover)";
            StopQrTimer();
        }
        else if (key == "fpx")
        {
            PageAppBar.Title = "Online Banking (FPX)";
            SelectedMethodLbl.Text = "🏦 Online Banking (FPX)";
            StopQrTimer();
        }
        else if (key == "card")
        {
            PageAppBar.Title = "Card Payment";
            SelectedMethodLbl.Text = "💳 Debit / Credit Card";
            StopQrTimer();
        }
        else // qr
        {
            PageAppBar.Title = "DuitNow QR Payment";
            SelectedMethodLbl.Text = "📱 DuitNow QR";
            StartQrTimer();
        }
    }

    async void OnChangeMethodTapped(object? sender, TappedEventArgs e)
    {
        var pick = await DisplayActionSheet("Switch payment method", "Cancel", null,
            "💵 Cash on Pickup (Direct to Owner)",
            "🏦 Online Banking (FPX)",
            "📱 DuitNow QR / TNG eWallet",
            "💳 Debit / Credit Card (Visa/Mastercard)");

        if (pick is null or "Cancel") return;

        var booking = AppState.SelectedBooking;
        if (pick.Contains("Cash"))
        {
            SetMethod("cash");
            if (booking is not null) booking.PaymentMethod = "Cash on Pickup";
        }
        else if (pick.Contains("FPX"))
        {
            SetMethod("fpx");
            if (booking is not null) booking.PaymentMethod = "Online Banking (FPX)";
        }
        else if (pick.Contains("Card"))
        {
            SetMethod("card");
            if (booking is not null) booking.PaymentMethod = "Credit / Debit Card";
        }
        else
        {
            SetMethod("qr");
            if (booking is not null) booking.PaymentMethod = "DuitNow QR";
        }
    }

    // ---------------------------------------------------------------- Live QR Timer
    void StartQrTimer()
    {
        StopQrTimer();
        _secondsRemaining = 299;
        UpdateTimerDisplay();

        _qrTimer = Dispatcher.CreateTimer();
        _qrTimer.Interval = TimeSpan.FromSeconds(1);
        _qrTimer.Tick += (_, _) =>
        {
            if (_secondsRemaining > 0)
            {
                _secondsRemaining--;
                UpdateTimerDisplay();
            }
            else
            {
                StopQrTimer();
                TimerLbl.Text = "QR Code Expired. Tap to refresh.";
            }
        };
        _qrTimer.Start();
    }

    void StopQrTimer()
    {
        if (_qrTimer is not null)
        {
            _qrTimer.Stop();
            _qrTimer = null;
        }
    }

    void UpdateTimerDisplay()
    {
        int minutes = _secondsRemaining / 60;
        int seconds = _secondsRemaining % 60;
        TimerLbl.Text = $"Valid for {minutes:00}:{seconds:00}";
    }

    // ---------------------------------------------------------------- Bank Selection Handlers
    void OnSelectMaybank(object? s, TappedEventArgs e) => SelectBankByIndex(0);
    void OnSelectCimb(object? s, TappedEventArgs e) => SelectBankByIndex(1);
    void OnSelectPublic(object? s, TappedEventArgs e) => SelectBankByIndex(2);
    void OnSelectRhb(object? s, TappedEventArgs e) => SelectBankByIndex(3);
    void OnSelectHlb(object? s, TappedEventArgs e) => SelectBankByIndex(4);
    void OnSelectBimb(object? s, TappedEventArgs e) => SelectBankByIndex(5);
    void OnSelectAmb(object? s, TappedEventArgs e) => SelectBankByIndex(6);

    void SelectBankByIndex(int index)
    {
        _selectedBank = _banks[index];
        ProceedFpxBtn.Text = $"Proceed to {_selectedBank.PortalName} (RM {_deposit:0.00})";
        UpdateBankSelectionVisuals();
    }

    void UpdateBankSelectionVisuals()
    {
        var activeTeal = Color.FromArgb("#178C95");
        var lineGrey = Color.FromArgb("#98A8A3");
        var activeBg = Color.FromArgb("#F0FAF9");

        void SetCard(Border card, Border radio, Label check, bool isSelected)
        {
            card.Stroke = isSelected ? activeTeal : Color.FromArgb("#E0E0E0");
            card.StrokeThickness = isSelected ? 2 : 1;
            card.BackgroundColor = isSelected ? activeBg : Colors.White;

            radio.Stroke = isSelected ? activeTeal : lineGrey;
            radio.BackgroundColor = isSelected ? activeTeal : Colors.Transparent;
            check.Text = isSelected ? "✓" : "";
        }

        SetCard(BankCard_Maybank, Radio_Maybank, Check_Maybank, _selectedBank.Name == "Maybank");
        SetCard(BankCard_Cimb, Radio_Cimb, Check_Cimb, _selectedBank.Name == "CIMB Bank");
        SetCard(BankCard_Public, Radio_Public, Check_Public, _selectedBank.Name == "Public Bank");
        SetCard(BankCard_Rhb, Radio_Rhb, Check_Rhb, _selectedBank.Name == "RHB Bank");
        SetCard(BankCard_Hlb, Radio_Hlb, Check_Hlb, _selectedBank.Name == "Hong Leong Bank");
        SetCard(BankCard_Bimb, Radio_Bimb, Check_Bimb, _selectedBank.Name == "Bank Islam");
        SetCard(BankCard_Amb, Radio_Amb, Check_Amb, _selectedBank.Name == "AmBank");
    }

    // ---------------------------------------------------------------- FPX Gateway Simulation
    void OnProceedToFpxGateway(object? sender, EventArgs e)
    {
        BankHeaderBar.BackgroundColor = Color.FromArgb(_selectedBank.HeaderBg);
        BankPortalTitle.Text = $"{_selectedBank.PortalName} Secure FPX Gateway";
        BankPortalTitle.TextColor = Color.FromArgb(_selectedBank.HeaderFg);

        int code = Random.Shared.Next(111000, 999000);
        TacCodeLbl.Text = $"{code / 1000} {code % 1000}";

        AuthorizeFpxBtn.Text = $"✓ Authorize & Pay RM {_deposit:0.00}";
        BankGatewayModal.IsVisible = true;
    }

    void OnUseTacCode(object? sender, EventArgs e)
    {
        DisplayAlert("TAC Verified", "TAC code successfully applied and verified for this session.", "OK");
    }

    void OnCancelBankGateway(object? sender, EventArgs e)
    {
        BankGatewayModal.IsVisible = false;
    }

    async void OnAuthorizeBankPayment(object? sender, EventArgs e)
    {
        BankGatewayModal.IsVisible = false;
        await RunProcessingSequenceAsync(PaymentMethod.FPX, _selectedBank.PortalName);
    }

    // ---------------------------------------------------------------- DuitNow QR Actions
    async void OnConfirmQrPaid(object? sender, EventArgs e)
    {
        await RunProcessingSequenceAsync(PaymentMethod.DuitNowQR, "DuitNow PayNet");
    }

    async void OnSaveQrTapped(object? sender, EventArgs e)
    {
        await DisplayAlert("QR Saved",
            "DuitNow QR image has been saved to your photo album. You can upload it in your banking app (Maybank MAE, TNG eWallet, CIMB OCTO).",
            "OK");
    }

    // ---------------------------------------------------------------- Credit / Debit Card Form
    void OnCardFieldChanged(object? sender, TextChangedEventArgs e)
    {
        CardHolderDisplay.Text = string.IsNullOrWhiteSpace(CardHolderEntry.Text)
            ? "CARDHOLDER NAME"
            : CardHolderEntry.Text.ToUpperInvariant();

        string raw = CardNumberEntry.Text?.Replace(" ", "") ?? "";
        if (raw.StartsWith("4")) CardBrandLbl.Text = "VISA";
        else if (raw.StartsWith("5")) CardBrandLbl.Text = "MASTERCARD";
        else CardBrandLbl.Text = "CARD";

        if (raw.Length > 0)
        {
            var formatted = "";
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && i % 4 == 0) formatted += " ";
                formatted += raw[i];
            }
            CardNumberDisplay.Text = formatted;
        }
        else
        {
            CardNumberDisplay.Text = "•••• •••• •••• ••••";
        }

        CardExpiryDisplay.Text = string.IsNullOrWhiteSpace(CardExpiryEntry.Text) ? "MM/YY" : CardExpiryEntry.Text;
    }

    async void OnPayWithCard(object? sender, EventArgs e)
    {
        string raw = CardNumberEntry.Text?.Replace(" ", "") ?? "";
        if (raw.Length < 16)
        {
            await DisplayAlert("Invalid Card Number", "Please enter a valid 16-digit debit or credit card number.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(CardExpiryEntry.Text) || !CardExpiryEntry.Text.Contains("/"))
        {
            await DisplayAlert("Invalid Expiry", "Please enter expiry date in MM/YY format.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(CardCvvEntry.Text) || CardCvvEntry.Text.Length < 3)
        {
            await DisplayAlert("Invalid CVV", "Please enter a 3 or 4-digit CVV security code.", "OK");
            return;
        }

        string brand = CardBrandLbl.Text;
        string last4 = raw[^4..];
        await RunProcessingSequenceAsync(PaymentMethod.Card, $"{brand} •••• {last4}");
    }

    // ---------------------------------------------------------------- Cash on Pickup
    async void OnConfirmCashReservation(object? sender, EventArgs e)
    {
        await RunProcessingSequenceAsync(PaymentMethod.CashOnPickup, "Direct to Owner");
    }

    // ---------------------------------------------------------------- Realistic Processing Sequence
    async Task RunProcessingSequenceAsync(PaymentMethod method, string provider)
    {
        ProcessingOverlay.IsVisible = true;

        ProcessingStepLbl.Text = method switch
        {
            PaymentMethod.CashOnPickup => "Registering Cash on Pickup Handover Agreement...",
            PaymentMethod.FPX => $"Connecting to {_selectedBank.PortalName} secure switch...",
            PaymentMethod.DuitNowQR => "Querying PayNet DuitNow network for incoming push...",
            _ => "Connecting to 3D-Secure Card Network..."
        };
        await Task.Delay(1000);

        ProcessingStepLbl.Text = method switch
        {
            PaymentMethod.CashOnPickup => $"Recording cash deposit terms: RM {_deposit:0.00} due on handover...",
            _ => $"Authorizing transaction reference {_reference}..."
        };
        await Task.Delay(1000);

        ProcessingStepLbl.Text = method switch
        {
            PaymentMethod.CashOnPickup => "Generating official Handover Voucher & instructions...",
            _ => $"Securing RM {_deposit:0.00} into ShareNest Escrow Vault..."
        };
        await Task.Delay(900);

        ProcessingStepLbl.Text = method switch
        {
            PaymentMethod.CashOnPickup => "✓ Reservation Confirmed & Cash Voucher Generated!",
            _ => "✓ Payment Approved & Escrow Guaranteed!"
        };
        await Task.Delay(700);

        ProcessingOverlay.IsVisible = false;

        // Save Payment record in SQLite
        var booking = AppState.SelectedBooking;
        string receiptNo = $"SN-REC-{Random.Shared.Next(100000, 999999)}";

        var payment = new Payment
        {
            BookingId = booking?.Id ?? 0,
            ItemName = AppState.SelectedItem?.Name ?? "Item",
            Amount = _deposit,
            Method = method,
            ProviderOrBank = provider,
            Reference = _reference,
            ReceiptNumber = receiptNo,
            Status = PaymentStatus.Held,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await Db.InsertPaymentAsync(payment);
        AppState.RecentPayment = payment;

        if (booking is not null)
        {
            booking.PaymentMethod = payment.MethodDisplayName;
            await Db.UpdateBookingAsync(booking);
        }

        await Shell.Current.GoToAsync("receipt");
    }
}
