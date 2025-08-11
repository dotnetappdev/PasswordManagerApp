using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PasswordManager.Models;
using PasswordManager.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace PasswordManager.WinUi.ViewModels;

public class PasswordItemsViewModel : INotifyPropertyChanged
{
    private readonly IPasswordItemService _passwordItemService;
    private string _searchText = string.Empty;
    private bool _isLoading = false;

    public PasswordItemsViewModel(IServiceProvider serviceProvider)
    {
        _passwordItemService = serviceProvider.GetRequiredService<IPasswordItemService>();
        PasswordItems = new ObservableCollection<PasswordItem>();
        LoadPasswordItemsAsync();
    }

    public ObservableCollection<PasswordItem> PasswordItems { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
            _ = Task.Run(async () => await SearchPasswordItemsAsync());
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool HasNoItems => !IsLoading && PasswordItems.Count == 0;

    private async Task LoadPasswordItemsAsync()
    {
        try
        {
            IsLoading = true;
            var items = await _passwordItemService.GetAllAsync();
            
            PasswordItems.Clear();
            foreach (var item in items.Where(i => !i.IsDeleted && !i.IsArchived))
            {
                PasswordItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            // Handle error - could show a message to user
            System.Diagnostics.Debug.WriteLine($"Error loading password items: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoItems));
        }
    }

    private async Task SearchPasswordItemsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadPasswordItemsAsync();
            return;
        }

        try
        {
            IsLoading = true;
            var items = await _passwordItemService.SearchAsync(SearchText);
            
            PasswordItems.Clear();
            foreach (var item in items.Where(i => !i.IsDeleted && !i.IsArchived))
            {
                PasswordItems.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching password items: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoItems));
        }
    }

    public async Task RefreshAsync()
    {
        await LoadPasswordItemsAsync();
    }

    public async Task DeleteItemAsync(PasswordItem item)
    {
        try
        {
            await _passwordItemService.DeleteAsync(item.Id);
            PasswordItems.Remove(item);
            OnPropertyChanged(nameof(HasNoItems));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting password item: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}