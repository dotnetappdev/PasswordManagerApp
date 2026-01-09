using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;

namespace PasswordManager.WPF.ViewModels;

public class VaultsViewModel : BaseViewModel
{
    private readonly IVaultService _vaultService;
    private readonly IAuthService _authService;
    private string _searchText = string.Empty;
    private Vault? _selectedVault;

    public VaultsViewModel(IServiceProvider serviceProvider)
    {
        _vaultService = serviceProvider.GetRequiredService<IVaultService>();
        _authService = serviceProvider.GetRequiredService<IAuthService>();
        
        Vaults = new ObservableCollection<Vault>();
        
        // Use fire-and-forget with proper error handling
        _ = Task.Run(async () =>
        {
            try
            {
                await LoadVaultsAsync();
            }
            catch
            {
                // Error already logged in LoadVaultsAsync
            }
        });
    }

    public ObservableCollection<Vault> Vaults { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            FilterVaults();
        }
    }

    public Vault? SelectedVault
    {
        get => _selectedVault;
        set => SetProperty(ref _selectedVault, value);
    }

    public bool HasNoVaults => !IsLoading && Vaults.Count == 0;

    private async Task LoadVaultsAsync()
    {
        try
        {
            IsLoading = true;
            
            var vaults = await _vaultService.GetAllAsync();
            
            Vaults.Clear();
            
            foreach (var vault in vaults)
            {
                Vaults.Add(vault);
            }
        }
        catch (Exception ex)
        {
            // Log error
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoVaults));
        }
    }

    private void FilterVaults()
    {
        // This would need a more sophisticated implementation
        // For now, we'll reload all vaults when search changes
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            _ = LoadVaultsAsync();
        }
    }

    public async Task<bool> CreateVaultAsync(string name, string? description = null, bool isDefault = false)
    {
        try
        {
            IsLoading = true;
            
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var vault = new Vault
            {
                Name = name.Trim(),
                Description = description?.Trim(),
                IsDefault = isDefault
            };

            // Set user ID from current authenticated user
            if (_authService.CurrentUser != null)
            {
                vault.UserId = _authService.CurrentUser.Id;
            }

            var createdVault = await _vaultService.CreateAsync(vault);
            
            if (createdVault != null)
            {
                Vaults.Add(createdVault);
                OnPropertyChanged(nameof(HasNoVaults));
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> UpdateVaultAsync(Vault vault)
    {
        try
        {
            IsLoading = true;
            
            var updatedVault = await _vaultService.UpdateAsync(vault);
            
            if (updatedVault != null)
            {
                // Update the item in the collection
                for (int i = 0; i < Vaults.Count; i++)
                {
                    if (Vaults[i].Id == vault.Id)
                    {
                        Vaults[i] = updatedVault;
                        break;
                    }
                }
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> DeleteVaultAsync(Vault vault)
    {
        try
        {
            IsLoading = true;
            
            await _vaultService.DeleteAsync(vault.Id);
            Vaults.Remove(vault);
            OnPropertyChanged(nameof(HasNoVaults));
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshAsync()
    {
        await LoadVaultsAsync();
    }
}
