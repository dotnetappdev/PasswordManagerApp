namespace VaultGuard.Mobile.Presentation.Pages.Categories;

public sealed partial class CategoriesPage : Page
{
    public CategoriesPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (DataContext is CategoriesModel viewModel)
        {
            await viewModel.LoadCategoriesAsync();
        }
    }
}
