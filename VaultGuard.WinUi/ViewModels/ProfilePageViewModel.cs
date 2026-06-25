using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Models.DTOs.Auth;
using PasswordManager.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PasswordManager.WinUi.ViewModels;

public class ProfilePageViewModel : BaseViewModel
{
    private readonly IUserProfileService _userProfileService;
    private readonly IAuthService _authService;
    private readonly IServiceProvider _serviceProvider;

    private UserDto? _currentUser;
    private ObservableCollection<UserDto> _allProfiles = new();
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _email = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isEditing = false;
    private bool _showProfileManagement = false;

    public ProfilePageViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _userProfileService = serviceProvider.GetRequiredService<IUserProfileService>();
        _authService = serviceProvider.GetRequiredService<IAuthService>();

        _ = LoadProfileDataAsync();
    }

    public UserDto? CurrentUser
    {
        get => _currentUser;
        set => SetProperty(ref _currentUser, value);
    }

    public ObservableCollection<UserDto> AllProfiles
    {
        get => _allProfiles;
        set => SetProperty(ref _allProfiles, value);
    }

    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public bool ShowProfileManagement
    {
        get => _showProfileManagement;
        set => SetProperty(ref _showProfileManagement, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string CurrentUserDisplayName => GetDisplayName(CurrentUser);

    private async Task LoadProfileDataAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            // Load current user
            CurrentUser = await _userProfileService.GetCurrentUserAsync();

            if (CurrentUser != null)
            {
                FirstName = CurrentUser.FirstName ?? string.Empty;
                LastName = CurrentUser.LastName ?? string.Empty;
                Email = CurrentUser.Email;
            }

            // Load all profiles for management
            var allUsers = await _userProfileService.GetAllUsersAsync();
            AllProfiles.Clear();
            foreach (var user in allUsers.Where(u => u.IsActive))
            {
                AllProfiles.Add(user);
            }

            OnPropertyChanged(nameof(CurrentUserDisplayName));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading profile: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
        }
    }

    public void StartEditing()
    {
        IsEditing = true;
        ErrorMessage = string.Empty;
    }

    public void CancelEditing()
    {
        IsEditing = false;
        ErrorMessage = string.Empty;

        // Reset to current user values
        if (CurrentUser != null)
        {
            FirstName = CurrentUser.FirstName ?? string.Empty;
            LastName = CurrentUser.LastName ?? string.Empty;
            Email = CurrentUser.Email;
        }
    }

    public async Task<bool> SaveProfileAsync()
    {
        try
        {
            if (CurrentUser == null)
            {
                ErrorMessage = "No current user found.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            {
                ErrorMessage = "First name and last name are required.";
                return false;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            var updateDto = new UpdateUserProfileDto
            {
                Id = CurrentUser.Id,
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                Email = Email.Trim()
            };

            var (success, errorMessage) = await _userProfileService.UpdateAsync(updateDto);

            if (success)
            {
                // Update the current user object
                CurrentUser.FirstName = FirstName.Trim();
                CurrentUser.LastName = LastName.Trim();
                CurrentUser.Email = Email.Trim();

                OnPropertyChanged(nameof(CurrentUser));
                OnPropertyChanged(nameof(CurrentUserDisplayName));

                IsEditing = false;
                return true;
            }
            else
            {
                ErrorMessage = errorMessage ?? "Failed to update profile.";
                return false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving profile: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasError));
        }
    }

    public void ShowManageProfiles()
    {
        ShowProfileManagement = true;
    }

    public void HideManageProfiles()
    {
        ShowProfileManagement = false;
    }

    public async Task<bool> SwitchToProfileAsync(UserDto profile)
    {
        try
        {
            // For a full implementation, we'd need to update the auth service to switch users
            // For now, we'll just update the current user display
            CurrentUser = profile;
            FirstName = profile.FirstName ?? string.Empty;
            LastName = profile.LastName ?? string.Empty;
            Email = profile.Email;

            OnPropertyChanged(nameof(CurrentUser));
            OnPropertyChanged(nameof(CurrentUserDisplayName));

            HideManageProfiles();
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error switching profile: {ex.Message}";
            return false;
        }
    }

    public string GetDisplayName(UserDto? user)
    {
        if (user == null) return "Unknown User";

        if (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
        {
            return $"{user.FirstName} {user.LastName}";
        }
        else if (!string.IsNullOrEmpty(user.FirstName))
        {
            return user.FirstName;
        }
        else if (!string.IsNullOrEmpty(user.LastName))
        {
            return user.LastName;
        }
        else
        {
            return user.Email;
        }
    }

    public string GetInitials(UserDto? user)
    {
        if (user == null) return "?";

        var firstName = user.FirstName?.Trim();
        var lastName = user.LastName?.Trim();

        if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
        {
            return $"{firstName[0]}{lastName[0]}".ToUpper();
        }
        else if (!string.IsNullOrEmpty(firstName))
        {
            return firstName[0].ToString().ToUpper();
        }
        else if (!string.IsNullOrEmpty(lastName))
        {
            return lastName[0].ToString().ToUpper();
        }
        else
        {
            return user.Email.Length > 0 ? user.Email[0].ToString().ToUpper() : "?";
        }
    }
}