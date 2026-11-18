namespace ShareNest.Controls;

/// <summary>Persistent 4-tab bottom navigation (Catalog · Bookings · Activity · Profile) drawn exactly like the Figma frames.</summary>
public class BottomNav : ContentView
{
    static readonly Color Navy = Color.FromArgb("#364F6B");
    static readonly Color Teal = Color.FromArgb("#0F6F78");
    static readonly Color Grey = Color.FromArgb("#55655F");

    static readonly (string Title, string Icon, string Route)[] Tabs =
    {
        ("Catalog", "nav_catalog", "//catalog"),
        ("Bookings", "nav_bookings", "//bookings"),
        ("Activity", "nav_activity", "//activity"),
        ("Profile", "nav_profile", "//profile"),
    };

    public static readonly BindableProperty ActiveProperty = BindableProperty.Create(nameof(Active), typeof(int), typeof(BottomNav), 0,
        propertyChanged: (b, _, _) => ((BottomNav)b).Build());

    public int Active { get => (int)GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }

    public BottomNav() => Build();

    void Build()
    {
        var grid = new Grid { Padding = new Thickness(0, 8, 0, 8), BackgroundColor = Colors.White };
        for (int i = 0; i < Tabs.Length; i++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            bool on = i == Active;
            var (title, icon, route) = Tabs[i];

            var tab = new VerticalStackLayout
            {
                Spacing = 3,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Image { Source = $"{icon}_{(on ? "on" : "off")}.png", WidthRequest = 24, HeightRequest = 24, HorizontalOptions = LayoutOptions.Center },
                    new Label { Text = title, FontSize = 11, FontAttributes = on ? FontAttributes.Bold : FontAttributes.None,
                                TextColor = on ? Teal : Grey, HorizontalOptions = LayoutOptions.Center }
                }
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) => { if (!on) await Shell.Current.GoToAsync(route); };
            tab.GestureRecognizers.Add(tap);
            grid.Add(tab, i);
        }

        Content = new VerticalStackLayout { Spacing = 0, BackgroundColor = Colors.White,
            Children = { new BoxView { HeightRequest = 1.5, Color = Navy }, grid } };
    }
}
