using CommunityToolkit.Mvvm.ComponentModel;

namespace VaultGuard.Mobile.Presentation.Pages.Vaults;

public partial class VaultsModel : ObservableObject
{
    [ObservableProperty]
    private bool isLoading;

    public VaultsModel()
    {
    }

    public async Task LoadVaultsAsync()
    {
        IsLoading = true;
        try
        {
            // TODO: Implement vault loading from local database
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            // Log error
        }
        finally
        {
            IsLoading = false;
        }
    }
}
