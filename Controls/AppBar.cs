using Microsoft.Maui.Controls.Shapes;

namespace ShareNest.Controls;

/// <summary>White top bar with navy underline, optional back arrow and optional trailing icon (matches the Figma app bar).</summary>
public class AppBar : ContentView
{
    static readonly Color Navy = Color.FromArgb("#364F6B");
    static readonly Color LineGrey = Color.FromArgb("#98A8A3");

    readonly Label _title = new() { FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Navy, VerticalOptions = LayoutOptions.Center };
    readonly Label _back = new() { Text = "<", FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Navy, VerticalOptions = LayoutOptions.Center, IsVisible = false, Padding = new Thickness(0, 0, 2, 0) };
    readonly Border _trail = new() { IsVisible = false, VerticalOptions = LayoutOptions.Center, BackgroundColor = Colors.White, Stroke = LineGrey, StrokeThickness = 1.4 };
    readonly Image _trailImg = new() { WidthRequest = 22, HeightRequest = 22, Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };

    public event EventHandler? TrailingTapped;

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(AppBar), "",
        propertyChanged: (b, _, n) => ((AppBar)b)._title.Text = (string)n);
    public static readonly BindableProperty ShowBackProperty = BindableProperty.Create(nameof(ShowBack), typeof(bool), typeof(AppBar), false,
        propertyChanged: (b, _, n) => ((AppBar)b)._back.IsVisible = (bool)n);
    public static readonly BindableProperty TrailingIconProperty = BindableProperty.Create(nameof(TrailingIcon), typeof(string), typeof(AppBar), "",
        propertyChanged: (b, _, n) => ((AppBar)b).ApplyTrailing((string)n));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public bool ShowBack { get => (bool)GetValue(ShowBackProperty); set => SetValue(ShowBackProperty, value); }
    public string TrailingIcon { get => (string)GetValue(TrailingIconProperty); set => SetValue(TrailingIconProperty, value); }

    public AppBar()
    {
        var back = new TapGestureRecognizer();
        back.Tapped += async (_, _) => await Shell.Current.GoToAsync("..");
        _back.GestureRecognizers.Add(back);

        var trail = new TapGestureRecognizer();
        trail.Tapped += (_, _) => TrailingTapped?.Invoke(this, EventArgs.Empty);
        _trail.GestureRecognizers.Add(trail);

        var row = new Grid
        {
            Padding = new Thickness(16, 14),
            ColumnSpacing = 6,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        row.Add(_back, 0); row.Add(_title, 1); row.Add(_trail, 2);

        Content = new VerticalStackLayout
        {
            Spacing = 0,
            BackgroundColor = Colors.White,
            Children = { row, new BoxView { HeightRequest = 1.5, Color = Navy } }
        };
    }

    void ApplyTrailing(string icon)
    {
        _trail.IsVisible = !string.IsNullOrEmpty(icon);
        if (string.IsNullOrEmpty(icon)) return;
        if (icon == "box")      // small outlined square (Activity screen)
        {
            _trail.WidthRequest = 20; _trail.HeightRequest = 20; _trail.Content = null;
            _trail.StrokeShape = new RoundRectangle { CornerRadius = 4 };
        }
        else
        {
            _trail.WidthRequest = 44; _trail.HeightRequest = 44;
            _trail.StrokeShape = new RoundRectangle { CornerRadius = 8 };
            _trailImg.Source = icon;
            _trail.Content = _trailImg;
        }
    }
}
