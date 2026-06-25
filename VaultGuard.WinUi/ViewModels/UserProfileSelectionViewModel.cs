using Microsoft.Extensions.DependencyInjection;
using VaultGuard.Models.DTOs.Auth;
using VaultGuard.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace VaultGuard.WinUi.ViewModels;

public class UserProfileSelectionViewModel : BaseViewModel
{
    private readonly IUserProfileService _userProfileService;
    private readonly IServiceProvider _serviceProvider;
    private ObservableCollection<UserDto> _userProfiles = new();
    private UserDto? _selectedProfile;
    private bool _showCreateProfile = false;
    private string _createProfileFirstName = string.Empty;
    private string _createProfileLastName = string.Empty;
    private string _createProfileEmail = string.Empty;

    public UserProfileSelectionViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _userProfileService = serviceProvider.GetRequiredService<IUserProfileService>();
        _ = LoadUserProfilesAsync();
    }

    public ObservableCollection<UserDto> UserProfiles
    {
        get => _userProfiles;
        set => SetProperty(ref _userProfiles, value);
    }

    public UserDto? SelectedProfile
    {
        get => _selectedProfile;
        set => SetProperty(ref _selectedProfile, value);
    }

    public bool ShowCreateProfile
    {
        get => _showCreateProfile;
        set => SetProperty(ref _showCreateProfile, value);
    }

    public string CreateProfileFirstName
    {
        get => _createProfileFirstName;
        set => SetProperty(ref _createProfileFirstName, value);
    }

    public string CreateProfileLastName
    {
        get => _createProfileLastName;
        set => SetProperty(ref _createProfileLastName, value);
    }

    public string CreateProfileEmail
    {
        get => _createProfileEmail;
        set => SetProperty(ref _createProfileEmail, value);
    }

    public bool HasUserProfiles => UserProfiles.Count > 0;

    private async Task LoadUserProfilesAsync()
    {
        try
        {
            IsLoading = true;
            var users = await _userProfileService.GetAllUsersAsync();

            UserProfiles.Clear();
            if (users != null)
            {
                foreach (var user in users)
                {
                    if (user?.IsActive == true)
                    {
                        UserProfiles.Add(user);
                    }
                }
            }

            OnPropertyChanged(nameof(HasUserProfiles));
        }
        catch (Exception ex)
        {
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SelectProfile(UserDto profile)
    {
        SelectedProfile = profile;
    }

    public void ShowCreateProfileDialog()
    {
        ShowCreateProfile = true;
        CreateProfileFirstName = string.Empty;
        CreateProfileLastName = string.Empty;
        CreateProfileEmail = string.Empty;
    }

    public void HideCreateProfileDialog()
    {
        ShowCreateProfile = false;
    }

    public async Task<bool> CreateNewProfileAsync(string masterPassword, string confirmPassword, string? hint = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CreateProfileFirstName) ||
                string.IsNullOrWhiteSpace(CreateProfileLastName) ||
                string.IsNullOrWhiteSpace(CreateProfileEmail))
            {
                return false;
            }

            if (masterPassword != confirmPassword)
            {
                return false;
            }

            var createDto = new CreateUserProfileDto
            {
                FirstName = CreateProfileFirstName,
                LastName = CreateProfileLastName,
                Email = CreateProfileEmail,
                Password = masterPassword,
                ConfirmPassword = confirmPassword,
                MasterPasswordHint = hint
            };

            var (result, user, errorMessage) = await _userProfileService.CreateUserAsync(createDto);

            if (result.Succeeded && user != null)
            {
                // Add the new user to the list
                var newUserDto = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    IsActive = user.IsActive
                };

                UserProfiles.Add(newUserDto);
                SelectedProfile = newUserDto;
                OnPropertyChanged(nameof(HasUserProfiles));
                HideCreateProfileDialog();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public string GetProfileDisplayName(UserDto profile)
    {
        if (profile == null)
            return "Unknown User";
            
        if (!string.IsNullOrEmpty(profile.FirstName) && !string.IsNullOrEmpty(profile.LastName))
        {
            return $"{profile.FirstName} {profile.LastName}";
        }
        else if (!string.IsNullOrEmpty(profile.FirstName))
        {
            return profile.FirstName;
        }
        else if (!string.IsNullOrEmpty(profile.LastName))
        {
            return profile.LastName;
        }
        else
        {
            return profile.Email ?? "Unknown User";
        }
    }

    public string GetProfileInitials(UserDto profile)
    {
        if (profile == null)
            return "?";
            
        var firstName = profile.FirstName?.Trim();
        var lastName = profile.LastName?.Trim();

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
            return !string.IsNullOrEmpty(profile.Email) && profile.Email.Length > 0 ? profile.Email[0].ToString().ToUpper() : "?";
        }
    }
}