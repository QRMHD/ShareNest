using ShareNest.Models;

namespace ShareNest;

public sealed class ItemDetailsPage : ContentPage, IQueryAttributable
{
    public CommunityItem? Item { get; private set; }

    public ItemDetailsPage()
    {
        Title = "Item details";
        BackgroundColor = Color.FromArgb("#F7F8FA");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(nameof(Item), out var value) || value is not CommunityItem item)
            return;

        Item = item;
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 16,
                Children =
                {
                    new Label { Text = item.Icon, FontSize = 56, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = item.Name, FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#202923") },
                    new Label { Text = $"{item.Category} · {item.Distance}", FontSize = 14, TextColor = Color.FromArgb("#547A64") },
                    new Label { Text = item.Description, FontSize = 16, TextColor = Color.FromArgb("#404A43") },
                    new Label { Text = $"Shared by {item.Owner}", FontSize = 15, TextColor = Color.FromArgb("#404A43") },
                    new Label { Text = item.Tags, FontSize = 14, TextColor = Color.FromArgb("#68736B") },
                    new Label { Text = $"${item.Deposit} refundable deposit · ${item.LateFeePerDay}/day late fee", FontSize = 14, TextColor = Color.FromArgb("#404A43") },
                    new Label { Text = $"★ {item.Rating:F1} · {(item.IsAvailable ? "Available" : "Currently borrowed")}", FontSize = 14, TextColor = Color.FromArgb("#547A64") }
                }
            }
        };
    }
}
