using Azure.Core;
using Azure.Identity;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DocumentTranslationService.Core
{
    /// <summary>
    /// Custom credential provider that wraps OAuth2 authentication for Azure services
    /// </summary>
    public class OAuth2CredentialProvider : TokenCredential
    {
        private readonly OAuth2Authentication _oauth2Auth;
        private readonly string _defaultScope;

        public OAuth2CredentialProvider(OAuth2Authentication oauth2Auth, string defaultScope = "https://cognitiveservices.azure.us/.default")
        {
            _oauth2Auth = oauth2Auth ?? throw new ArgumentNullException(nameof(oauth2Auth));
            _defaultScope = defaultScope;
        }

        /// <summary>
        /// Get an access token for the specified scopes
        /// </summary>
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            try
            {
                var accessToken = await _oauth2Auth.GetAccessTokenAsync();
                
                // Parse the token to get expiry information
                var userInfo = _oauth2Auth.GetCurrentUser();
                var expiresOn = userInfo?.ExpiresOn ?? DateTimeOffset.UtcNow.AddHours(1);

                Debug.WriteLine($"OAuth2CredentialProvider: Provided access token, expires on {expiresOn}");
                
                return new AccessToken(accessToken, expiresOn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OAuth2CredentialProvider Error: {ex.Message}");
                throw new OAuth2AuthenticationException("Failed to obtain access token", ex);
            }
        }

        /// <summary>
        /// Get an access token for the specified scopes (synchronous version)
        /// </summary>
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            // For synchronous calls, we'll run the async version synchronously
            // This is not ideal but necessary for compatibility with TokenCredential interface
            try
            {
                return GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OAuth2CredentialProvider Sync Error: {ex.Message}");
                throw new OAuth2AuthenticationException("Failed to obtain access token synchronously", ex);
            }
        }
    }

    /// <summary>
    /// Factory class for creating OAuth2-enabled Azure service clients
    /// </summary>
    public static class OAuth2ServiceClientFactory
    {
        /// <summary>
        /// Create a TokenCredential for Azure Government services using OAuth2
        /// </summary>
        public static TokenCredential CreateCredential(DocTransAppSettings settings)
        {
            if (!settings.UsingOAuth2)
            {
                throw new InvalidOperationException("OAuth2 authentication is not enabled in settings");
            }

            var oauth2Auth = new OAuth2Authentication(settings.OAuth2);
            return new OAuth2CredentialProvider(oauth2Auth, GetDefaultScope(settings.OAuth2.CloudEnvironment));
        }

        /// <summary>
        /// Get the appropriate default scope for the specified cloud environment
        /// </summary>
        private static string GetDefaultScope(string cloudEnvironment)
        {
            return cloudEnvironment?.ToLowerInvariant() switch
            {
                "azuregovernment" or "government" or "gov" => "https://cognitiveservices.azure.us/.default",
                "azurechina" or "china" => "https://cognitiveservices.azure.cn/.default",
                "azurepublic" or "public" or "commercial" => "https://cognitiveservices.azure.com/.default",
                _ => "https://cognitiveservices.azure.us/.default" // Default to Azure Government
            };
        }
    }
}