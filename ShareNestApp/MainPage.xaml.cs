using ShareNest.Models;
using ShareNest.Services;

namespace ShareNest;

public partial class MainPage : ContentPage
{
	public MainPage(ShareNestStore store)
	{
		InitializeComponent();
		BindingContext = store;
	}

	private async void OnItemSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not CommunityItem item)
			return;

		if (sender is CollectionView collectionView)
			collectionView.SelectedItem = null;

		await Shell.Current.GoToAsync(nameof(ItemDetailsPage), new Dictionary<string, object>
		{
			[nameof(ItemDetailsPage.Item)] = item
		});
	}
}
