using Azure.Identity;
using Microsoft.Identity.Client;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentTranslationService.Core
{
    /// <summary>
    /// Provides OAuth2 authentication capabilities for Azure Government Entra ID
    /// </summary>
    public class OAuth2Authentication
    {
        private readonly OAuth2Settings _settings;
        private IPublicClientApplication _app;
        private AuthenticationResult _authResult;

        public OAuth2Authentication(OAuth2Settings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            ValidateSettings();
        }

        /// <summary>
        /// Initialize the OAuth2 authentication application
        /// </summary>
        public void Initialize()
        {
            try
            {
                var authority = GetAuthorityUrl();
                
                _app = PublicClientApplicationBuilder
                    .Create(_settings.ClientId)
                    .WithAuthority(authority)
                    .WithRedirectUri(_settings.RedirectUri)
                    .Build();

                Debug.WriteLine($"OAuth2: Initialized with authority: {authority}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OAuth2 Initialization Error: {ex.Message}");
                throw new OAuth2AuthenticationException("Failed to initialize OAuth2 authentication", ex);
            }
        }

        /// <summary>
        /// Authenticate the user interactively
        /// </summary>
        /// <returns>Access token for Azure services</returns>
        public async Task<string> AuthenticateInteractiveAsync()
        {
            if (_app == null)
            {
                Initialize();
            }

            try
            {
                var accounts = await _app.GetAccountsAsync();
                var firstAccount = accounts.FirstOrDefault();

                // Try silent authentication first
                if (firstAccount != null)
                {
                    try
                    {
                        _authResult = await _app.AcquireTokenSilent(_settings.Scopes, firstAccount)
                            .ExecuteAsync();
                        
                        Debug.WriteLine("OAuth2: Silent authentication successful");
                        return _authResult.AccessToken;
                    }
                    catch (MsalUiRequiredException)
                    {
                        Debug.WriteLine("OAuth2: Silent authentication failed, falling back to interactive");
                    }
                }

                // Fall back to interactive authentication
                _authResult = await _app.AcquireTokenInteractive(_settings.Scopes)
                    .WithUseEmbeddedWebView(false)
                    .ExecuteAsync();

                Debug.WriteLine($"OAuth2: Interactive authentication successful for user: {_authResult.Account.Username}");
                return _authResult.AccessToken;
            }
            catch (MsalException ex)
            {
                Debug.WriteLine($"OAuth2 Authentication Error: {ex.Message}");
                throw new OAuth2AuthenticationException($"OAuth2 authentication failed: {ex.ErrorCode}", ex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OAuth2 Unexpected Error: {ex.Message}");
                throw new OAuth2AuthenticationException("Unexpected error during OAuth2 authentication", ex);
            }
        }

        /// <summary>
        /// Get a fresh access token (refresh if necessary)
        /// </summary>
        /// <returns>Valid access token</returns>
        public async Task<string> GetAccessTokenAsync()
        {
            if (_authResult == null)
            {
                return await AuthenticateInteractiveAsync();
            }

            // Check if token is still valid (with 5 minute buffer)
            if (_authResult.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return _authResult.AccessToken;
            }

            // Token is expired or close to expiry, try to refresh
            try
            {
                var accounts = await _app.GetAccountsAsync();
                var account = accounts.FirstOrDefault(x => x.Username == _authResult.Account.Username);
                
                if (account != null)
                {
                    _authResult = await _app.AcquireTokenSilent(_settings.Scopes, account)
                        .ExecuteAsync();
                    
                    Debug.WriteLine("OAuth2: Token refreshed successfully");
                    return _authResult.AccessToken;
                }
            }
            catch (MsalUiRequiredException)
            {
                Debug.WriteLine("OAuth2: Token refresh failed, requiring interactive authentication");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OAuth2 Token Refresh Error: {ex.Message}");
            }

            // If refresh failed, fall back to interactive authentication
            return await AuthenticateInteractiveAsync();
        }

        /// <summary>
        /// Sign out the current user
        /// </summary>
        public async Task SignOutAsync()
        {
            if (_app != null && _authResult != null)
            {
                try
                {
                    var accounts = await _app.GetAccountsAsync();
                    var account = accounts.FirstOrDefault(x => x.Username == _authResult.Account.Username);
                    
                    if (account != null)
                    {
                        await _app.RemoveAsync(account);
                        Debug.WriteLine($"OAuth2: Signed out user: {account.Username}");
                    }
                    
                    _authResult = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"OAuth2 Sign Out Error: {ex.Message}");
                    throw new OAuth2AuthenticationException("Failed to sign out", ex);
                }
            }
        }

        /// <summary>
        /// Get the current authenticated user's information
        /// </summary>
        /// <returns>User information or null if not authenticated</returns>
        public OAuth2UserInfo GetCurrentUser()
        {
            if (_authResult?.Account != null)
            {
                return new OAuth2UserInfo
                {
                    Username = _authResult.Account.Username,
                    Name = _authResult.Account.Username, // MSAL doesn't always provide display name
                    TenantId = _authResult.TenantId,
                    ExpiresOn = _authResult.ExpiresOn
                };
            }
            return null;
        }

        /// <summary>
        /// Validate the OAuth2 settings
        /// </summary>
        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.ClientId))
                throw new ArgumentException("ClientId is required", nameof(_settings.ClientId));
            
            if (string.IsNullOrWhiteSpace(_settings.TenantId))
                throw new ArgumentException("TenantId is required", nameof(_settings.TenantId));
            
            if (_settings.Scopes == null || !_settings.Scopes.Any())
                throw new ArgumentException("At least one scope is required", nameof(_settings.Scopes));
        }

        /// <summary>
        /// Get the authority URL based on cloud configuration
        /// </summary>
        private string GetAuthorityUrl()
        {
            return _settings.CloudEnvironment.ToLowerInvariant() switch
            {
                "azuregovernment" or "government" or "gov" => 
                    $"https://login.microsoftonline.us/{_settings.TenantId}",
                "azurechina" or "china" => 
                    $"https://login.chinacloudapi.cn/{_settings.TenantId}",
                "azurepublic" or "public" or "commercial" => 
                    $"https://login.microsoftonline.com/{_settings.TenantId}",
                _ => $"https://login.microsoftonline.us/{_settings.TenantId}" // Default to Azure Government
            };
        }
    }

    /// <summary>
    /// OAuth2 configuration settings
    /// </summary>
    public class OAuth2Settings
    {
        /// <summary>
        /// Azure AD application (client) ID
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Azure AD tenant (directory) ID
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Redirect URI for the application
        /// </summary>
        public string RedirectUri { get; set; } = "http://localhost";

        /// <summary>
        /// OAuth2 scopes to request
        /// </summary>
        public string[] Scopes { get; set; } = new[] { "https://cognitiveservices.azure.com/.default" };

        /// <summary>
        /// Azure cloud environment (AzureGovernment, AzureChina, AzurePublic)
        /// </summary>
        public string CloudEnvironment { get; set; } = "AzureGovernment";
    }

    /// <summary>
    /// Information about the authenticated user
    /// </summary>
    public class OAuth2UserInfo
    {
        public string Username { get; set; }
        public string Name { get; set; }
        public string TenantId { get; set; }
        public DateTimeOffset ExpiresOn { get; set; }
    }

    /// <summary>
    /// Exception thrown during OAuth2 authentication operations
    /// </summary>
    public class OAuth2AuthenticationException : Exception
    {
        public OAuth2AuthenticationException(string message) : base(message) { }
        public OAuth2AuthenticationException(string message, Exception innerException) : base(message, innerException) { }
    }
}