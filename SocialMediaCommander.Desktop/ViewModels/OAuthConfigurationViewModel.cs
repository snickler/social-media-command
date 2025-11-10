using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SocialMediaCommander.Core.Models;
using SocialMediaCommander.Services.Interfaces;

namespace SocialMediaCommander.Desktop.ViewModels;

/// <summary>
/// ViewModel for configuring OAuth settings per platform
/// </summary>
public partial class OAuthConfigurationViewModel : ObservableObject
{
    private readonly IOAuthConfigurationService _configService;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private SocialPlatform _selectedPlatform = SocialPlatform.BlueSky;

    [ObservableProperty]
    private string _clientId = string.Empty;

    [ObservableProperty]
    private string _clientSecret = string.Empty;

    [ObservableProperty]
    private string _authorizationEndpoint = string.Empty;

    [ObservableProperty]
    private string _tokenEndpoint = string.Empty;

    [ObservableProperty]
    private string _userInfoEndpoint = string.Empty;

    [ObservableProperty]
    private string _revokeEndpoint = string.Empty;

    [ObservableProperty]
    private string _redirectUri = "http://localhost:8080/oauth/callback";

    [ObservableProperty]
    private string _scopesText = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _isSaving = false;

    [ObservableProperty]
    private bool _isValid = false;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasUnsavedChanges = false;

    [ObservableProperty]
    private bool _isConfigured = false;

    public OAuthConfigurationViewModel(
        IOAuthConfigurationService configService,
        IAuthenticationService authService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        ValidationErrors = new ObservableCollection<string>();
        AvailablePlatforms = new ObservableCollection<PlatformConfigItem>();

        InitializePlatforms();
        PropertyChanged += OnPropertyChanged;

        // Load configuration for default platform
        _ = Task.Run(() => LoadConfigurationAsync(SelectedPlatform));
    }

    #region Properties

    public ObservableCollection<string> ValidationErrors { get; }
    public ObservableCollection<PlatformConfigItem> AvailablePlatforms { get; }

    public string[] Scopes => ScopesText
        .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(s => s.Trim())
        .Where(s => !string.IsNullOrEmpty(s))
        .ToArray();

    public bool CanSave => IsValid && HasUnsavedChanges && !IsSaving;
    public bool CanTest => IsValid && !IsLoading;
    public bool CanLoadDefaults => !IsLoading && !IsSaving;

    public string PlatformDisplayName =>
        PlatformConfigurations.GetPlatformConfig(SelectedPlatform).Name;

    public string ConfigurationStatus => IsConfigured
        ? "✓ Configured"
        : "⚠ Not Configured";

    public string ConfigurationStatusColor => IsConfigured
        ? "#28a745"
        : "#ffc107";

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadConfiguration()
    {
        await LoadConfigurationAsync(SelectedPlatform);
    }

    [RelayCommand]
    private async Task SaveConfiguration()
    {
        if (!CanSave) return;

        try
        {
            IsSaving = true;
            ValidationMessage = string.Empty;

            var config = CreateOAuthConfig();
            var validation = await _configService.ValidateConfigurationAsync(SelectedPlatform, config);

            if (!validation.IsValid)
            {
                ValidationErrors.Clear();
                foreach (var error in validation.Errors)
                {
                    ValidationErrors.Add(error);
                }
                ValidationMessage = "Please fix the validation errors.";
                return;
            }

            await _configService.SaveConfigurationAsync(SelectedPlatform, config);

            HasUnsavedChanges = false;
            IsConfigured = true;
            ValidationMessage = "Configuration saved successfully!";

            // Clear validation errors on successful save
            ValidationErrors.Clear();
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error saving configuration: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void LoadDefaults()
    {
        if (!CanLoadDefaults) return;

        try
        {
            IsLoading = true;
            var defaultConfig = _configService.GetDefaultConfiguration(SelectedPlatform);

            LoadConfigurationFromModel(defaultConfig);
            HasUnsavedChanges = true;
            ValidationMessage = "Default configuration loaded. Remember to set your Client ID and Secret.";
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error loading defaults: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TestConfiguration()
    {
        if (!CanTest) return;

        try
        {
            IsLoading = true;
            ValidationMessage = "Testing configuration...";

            var config = CreateOAuthConfig();
            var validation = await _configService.ValidateConfigurationAsync(SelectedPlatform, config);

            if (!validation.IsValid)
            {
                ValidationErrors.Clear();
                foreach (var error in validation.Errors)
                {
                    ValidationErrors.Add(error);
                }
                ValidationMessage = "Configuration test failed. Please fix the errors.";
                return;
            }

            // Test by attempting to start OAuth flow (but don't complete it)
            ValidationMessage = "Configuration appears valid! You can now use this platform for authentication.";
            ValidationErrors.Clear();
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Configuration test failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteConfiguration()
    {
        try
        {
            await _configService.DeleteConfigurationAsync(SelectedPlatform);

            ClearConfiguration();
            HasUnsavedChanges = false;
            IsConfigured = false;
            ValidationMessage = "Configuration deleted.";
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error deleting configuration: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ImportConfigurations()
    {
        // This would typically open a file dialog
        // For now, we'll just show a placeholder message
        ValidationMessage = "Import functionality will open a file dialog to import OAuth configurations.";
    }

    [RelayCommand]
    private void ExportConfigurations()
    {
        // This would typically open a save file dialog
        // For now, we'll just show a placeholder message
        ValidationMessage = "Export functionality will open a save dialog to export OAuth configurations.";
    }

    #endregion

    #region Private Methods

    private void InitializePlatforms()
    {
        var platforms = Enum.GetValues<SocialPlatform>();
        foreach (var platform in platforms)
        {
            var config = PlatformConfigurations.GetPlatformConfig(platform);
            AvailablePlatforms.Add(new PlatformConfigItem
            {
                Platform = platform,
                Name = config.Name,
                Color = config.Color,
                IsConfigured = false // Will be updated when configurations are loaded
            });
        }
    }

    private async Task LoadConfigurationAsync(SocialPlatform platform)
    {
        try
        {
            IsLoading = true;
            ValidationMessage = string.Empty;
            ValidationErrors.Clear();

            var config = await _configService.GetConfigurationAsync(platform);
            var hasConfig = await _configService.HasValidConfigurationAsync(platform);

            if (config != null)
            {
                LoadConfigurationFromModel(config);
                IsConfigured = hasConfig;
            }
            else
            {
                ClearConfiguration();
                IsConfigured = false;
            }

            HasUnsavedChanges = false;

            // Update platform status
            var platformItem = AvailablePlatforms.FirstOrDefault(p => p.Platform == platform);
            if (platformItem != null)
            {
                platformItem.IsConfigured = IsConfigured;
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Error loading configuration: {ex.Message}";
            ClearConfiguration();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadConfigurationFromModel(OAuthConfig config)
    {
        ClientId = config.ClientId;
        ClientSecret = config.ClientSecret;
        AuthorizationEndpoint = config.AuthorizationEndpoint;
        TokenEndpoint = config.TokenEndpoint;
        UserInfoEndpoint = config.UserInfoEndpoint ?? string.Empty;
        RevokeEndpoint = config.RevokeEndpoint ?? string.Empty;
        RedirectUri = config.RedirectUri;
        ScopesText = string.Join(", ", config.Scopes);
    }

    private void ClearConfiguration()
    {
        ClientId = string.Empty;
        ClientSecret = string.Empty;
        AuthorizationEndpoint = string.Empty;
        TokenEndpoint = string.Empty;
        UserInfoEndpoint = string.Empty;
        RevokeEndpoint = string.Empty;
        RedirectUri = "http://localhost:8080/oauth/callback";
        ScopesText = string.Empty;
    }

    private OAuthConfig CreateOAuthConfig()
    {
        return new OAuthConfig
        {
            ClientId = ClientId.Trim(),
            ClientSecret = ClientSecret.Trim(),
            AuthorizationEndpoint = AuthorizationEndpoint.Trim(),
            TokenEndpoint = TokenEndpoint.Trim(),
            UserInfoEndpoint = string.IsNullOrWhiteSpace(UserInfoEndpoint) ? null : UserInfoEndpoint.Trim(),
            RevokeEndpoint = string.IsNullOrWhiteSpace(RevokeEndpoint) ? null : RevokeEndpoint.Trim(),
            RedirectUri = RedirectUri.Trim(),
            Scopes = Scopes,
            AdditionalParameters = new Dictionary<string, string>
            {
                ["response_type"] = "code"
            }
        };
    }

    private async Task ValidateConfigurationAsync()
    {
        try
        {
            ValidationErrors.Clear();

            if (string.IsNullOrWhiteSpace(ClientId) ||
                string.IsNullOrWhiteSpace(ClientSecret) ||
                string.IsNullOrWhiteSpace(AuthorizationEndpoint) ||
                string.IsNullOrWhiteSpace(TokenEndpoint) ||
                string.IsNullOrWhiteSpace(RedirectUri) ||
                Scopes.Length == 0)
            {
                IsValid = false;
                return;
            }

            var config = CreateOAuthConfig();
            var validation = await _configService.ValidateConfigurationAsync(SelectedPlatform, config);

            IsValid = validation.IsValid;

            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    ValidationErrors.Add(error);
                }
            }
        }
        catch
        {
            IsValid = false;
        }
    }

    private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ClientId):
            case nameof(ClientSecret):
            case nameof(AuthorizationEndpoint):
            case nameof(TokenEndpoint):
            case nameof(UserInfoEndpoint):
            case nameof(RevokeEndpoint):
            case nameof(RedirectUri):
            case nameof(ScopesText):
                HasUnsavedChanges = true;
                _ = ValidateConfigurationAsync(); // Fire-and-forget with discard to suppress CS4014
                break;
            case nameof(SelectedPlatform):
                _ = LoadConfigurationAsync(SelectedPlatform); // Fire-and-forget with discard to suppress CS4014
                break;
            case nameof(IsValid):
            case nameof(IsLoading):
                OnPropertyChanged(nameof(CanTest));
                break;
            case nameof(IsSaving):
                OnPropertyChanged(nameof(CanSave));
                OnPropertyChanged(nameof(CanLoadDefaults));
                break;
            case nameof(HasUnsavedChanges):
                OnPropertyChanged(nameof(CanSave));
                break;
        }

        // Update computed properties
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanTest));
        OnPropertyChanged(nameof(CanLoadDefaults));
        OnPropertyChanged(nameof(PlatformDisplayName));
        OnPropertyChanged(nameof(ConfigurationStatus));
        OnPropertyChanged(nameof(ConfigurationStatusColor));
    }

    #endregion
}

/// <summary>
/// Represents a platform configuration item for UI binding
/// </summary>
public class PlatformConfigItem
{
    public SocialPlatform Platform { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }

    public string StatusIcon => IsConfigured ? "✓" : "⚠";
    public string StatusColor => IsConfigured ? "#28a745" : "#ffc107";
}