using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using ShareNest.Models;
using ShareNest.Services;

namespace ShareNest.ViewModels;

public class AddListingViewModel : BaseViewModel
{
    static readonly Color Navy = Color.FromArgb("#364F6B"), Muted = Color.FromArgb("#5B716C");

    string _name = "", _description = "", _deposit = "", _tags = "", _errorText = "";
    string? _category;
    bool _visible = true;

    public ObservableCollection<string> Photos { get; } = new();

    public string Name { get => _name; set => Set(ref _name, value ?? ""); }
    public string Description { get => _description; set => Set(ref _description, value ?? ""); }
    public string DepositText { get => _deposit; set => Set(ref _deposit, value ?? ""); }
    public string Tags { get => _tags; set => Set(ref _tags, value ?? ""); }
    public bool IsVisibleToNeighbours { get => _visible; set => Set(ref _visible, value); }

    public string CategoryText => _category ?? "Choose a category";
    public Color CategoryColor => _category is null ? Muted : Navy;

    public string ErrorText { get => _errorText; private set { if (Set(ref _errorText, value)) Raise(nameof(HasError)); } }
    public bool HasError => _errorText.Length > 0;

    public ICommand UploadCommand { get; }
    public ICommand CategoryCommand { get; }
    public ICommand PreviewCommand { get; }
    public ICommand PublishCommand { get; }

    public AddListingViewModel()
    {
        UploadCommand = new Command(async () => await Upload());
        CategoryCommand = new Command(async () => await ChooseCategory());
        PreviewCommand = new Command(async () => await Preview());
        PublishCommand = new Command(async () => await Publish());
    }

    bool TryDeposit(out decimal value)
    {
        var t = DepositText.Trim();
        return decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out value) || decimal.TryParse(t, out value);
    }

    async Task Upload()
    {
        if (Photos.Count >= 4) { await Shell.Current.DisplayAlert("Photos", "You can add up to 4 photos.", "OK"); return; }
        try
        {
            var file = await MediaPicker.Default.PickPhotoAsync();
            if (file is null) return;
            var path = Path.Combine(FileSystem.CacheDirectory, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
            await using (var src = await file.OpenReadAsync())
            await using (var dst = File.Create(path))
                await src.CopyToAsync(dst);
            Photos.Add(path);
        }
        catch (Exception ex) { await Shell.Current.DisplayAlert("Photo", ex.Message, "OK"); }
    }

    public async Task RemovePhotoAsync(string path)
    {
        if (await Shell.Current.DisplayAlert("Remove photo", "Remove this photo?", "Remove", "Keep")) Photos.Remove(path);
    }

    async Task ChooseCategory()
    {
        var pick = await Shell.Current.DisplayActionSheet("Choose a category", "Cancel", null, "Tools", "Yard", "Event", "Moving");
        if (pick is null or "Cancel") return;
        _category = pick;
        Raise(nameof(CategoryText)); Raise(nameof(CategoryColor));
    }

    async Task Preview()
    {
        TryDeposit(out var dep);
        await Shell.Current.DisplayAlert(string.IsNullOrWhiteSpace(Name) ? "(Untitled item)" : Name.Trim(),
            $"Category: {_category ?? "—"}\nDeposit: RM {dep:0.00}\nPhotos: {Photos.Count}\nVisible to neighbours: {(IsVisibleToNeighbours ? "Yes" : "No")}\n\n{Description}\n{Tags}",
            "Close");
    }

    async Task Publish()
    {
        string? error = null;
        TryDeposit(out var dep);
        if (string.IsNullOrWhiteSpace(Name)) error = "Please enter the item name.";
        else if (_category is null) error = "Please choose a category.";
        else if (dep <= 0) error = "Enter a deposit amount greater than 0.";
        ErrorText = error ?? "";
        if (error is not null) return;

        var desc = Description.Trim();
        await Db.AddItemAsync(new Item                                   // INSERT into SQLite
        {
            Name = Name.Trim(), Category = _category!, Deposit = dep, Owner = Session.User.FullName,
            Summary = desc.Length > 0 ? desc : "New listing",
            Specs = desc.Length > 0 ? desc : "No description provided.",
            PhotoPath = Photos.FirstOrDefault()
        });
        await Shell.Current.DisplayAlert("Listing published", $"“{Name.Trim()}” is now visible in the catalog.", "OK");
        await Shell.Current.GoToAsync("//catalog");
    }
}
