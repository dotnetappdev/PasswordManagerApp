namespace VaultGuard.Mobile.Presentation.Pages.Passwords;

public sealed partial class PasswordsPage : Page
{
    public PasswordsPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (DataContext is PasswordsModel viewModel)
        {
            await viewModel.LoadPasswordsAsync();
        }
    }
}
