using ShareNest.Models;
using ShareNest.Services;

namespace ShareNest.Pages;

public partial class HandoverLogPage : ContentPage
{
    static readonly Color Navy = Color.FromArgb("#364F6B");
    static readonly Color LineGrey = Color.FromArgb("#98A8A3");

    string _step = "Pickup";
    string _selectedCondition = "";
    bool _isConfirmed = true;
    Booking? _booking;

    public HandoverLogPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _booking = AppState.SelectedBooking ?? (await Db.GetBookingsAsync(false)).FirstOrDefault();
        _step = (_booking?.Status is "Active" or "Lent") ? "Return" : "Pickup";      // already picked up -> return step
        BuildChips();
        UpdateCheckVisual();
        UpdateConditionChips();
    }

    void OnChipGoodClicked(object? sender, EventArgs e)
    {
        _selectedCondition = "good";
        NoteEntry.Text = "Item is in good and clean working condition. No issues noted.";
        UpdateConditionChips();
    }

    void OnChipMarksClicked(object? sender, EventArgs e)
    {
        _selectedCondition = "marks";
        NoteEntry.Text = "Minor cosmetic marks/scratches present, but functions properly.";
        UpdateConditionChips();
    }

    void OnChipDamagedClicked(object? sender, EventArgs e)
    {
        _selectedCondition = "damaged";
        NoteEntry.Text = "Noticeable damage or missing parts inspected during handover.";
        UpdateConditionChips();
    }

    void UpdateConditionChips()
    {
        void StyleBtn(Button btn, bool active)
        {
            btn.BackgroundColor = active ? Navy : Colors.Transparent;
            btn.TextColor = active ? Colors.White : Navy;
            btn.BorderColor = Navy;
        }

        StyleBtn(ChipGood, _selectedCondition == "good");
        StyleBtn(ChipMarks, _selectedCondition == "marks");
        StyleBtn(ChipDamaged, _selectedCondition == "damaged");
    }

    void BuildChips()
    {
        StepChips.Children.Clear();
        foreach (var s in new[] { "Pickup", "Return" })
        {
            var b = new Button { Text = s, Style = (Style)Application.Current!.Resources[s == _step ? "ChipOn" : "Chip"] };
            b.Clicked += (_, _) => { _step = s; BuildChips(); };
            StepChips.Children.Add(b);
        }
        ItemLbl.Text = _booking is null ? "" : $"{_booking.ItemName} · {_step.ToLower()} handover";
    }

    void UpdateCheckVisual()
    {
        CheckSquare.BackgroundColor = _isConfirmed ? Navy : Colors.White;
        CheckSquare.Stroke = _isConfirmed ? Navy : LineGrey;
        CheckIcon.IsVisible = _isConfirmed;
    }

    void OnConfirmToggle(object? sender, TappedEventArgs e)
    {
        _isConfirmed = !_isConfirmed;
        UpdateCheckVisual();
    }

    async void OnPhotoTapped(object? s, TappedEventArgs e)
    {
        try
        {
            FileResult? f = MediaPicker.Default.IsCaptureSupported ? await MediaPicker.Default.CapturePhotoAsync()
                                                                   : await MediaPicker.Default.PickPhotoAsync();
            if (f is null) return;
            var path = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}{Path.GetExtension(f.FileName)}");
            await using (var src = await f.OpenReadAsync())
            await using (var dst = File.Create(path))
                await src.CopyToAsync(dst);

            PhotoImg.Source = ImageSource.FromFile(path);
            PhotoHint.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Photo", ex.Message, "OK");
        }
    }

    async void OnConfirm(object? s, EventArgs e)
    {
        if (!_isConfirmed)
        {
            await DisplayAlert("Please confirm", "Tap the verification checkbox to confirm item inspection.", "OK");
            return;
        }

        var now = DateTime.Now;
        StampLbl.Text = $"Confirmed {now:d MMM yyyy, h:mm tt}";

        if (_booking is not null)
        {
            var item = await Db.FindItemAsync(_booking.ItemName);          // keep the catalog status in sync
            if (_step == "Pickup")
            {
                _booking.Status = _booking.IsLending ? "Lent" : "Active";
                _booking.Detail = $"Picked up {now:d MMM}";
                if (item is not null) { item.Status = "Borrowed"; await Db.UpdateItemAsync(item); }
            }
            else
            {
                _booking.Status = "Returned";
                _booking.Detail = $"Returned {now:d MMM}";
                await Db.SetIntAsync("loan_count", await Db.GetIntAsync("loan_count", 12) + 1);
                await Db.AddHistoryAsync($"Borrowed “{_booking.ItemName}” — {now:d MMM}");
                if (item is not null) { item.Status = "Available"; await Db.UpdateItemAsync(item); }
            }
            await Db.UpdateBookingAsync(_booking);                          // UPDATE in SQLite
        }
        await DisplayAlert("Handover confirmed", $"{_step} saved at {now:h:mm tt}.", "OK");
        await Shell.Current.GoToAsync("deposit");        // Handover Log -> Deposit & Trust
    }
}
