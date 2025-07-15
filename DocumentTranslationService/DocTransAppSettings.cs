/*
 * Holds the configuration information for the document translation settings
 */

namespace DocumentTranslationService.Core
{
    public class DocTransAppSettings
    {
        /// <summary>
        /// Azure Translator resource URI
        /// </summary>
        public string AzureResourceName { get; set; }
        /// <summary>
        /// Hold sthe connection strings.
        /// </summary>
        public Connectionstrings ConnectionStrings { get; set; }
        /// <summary>
        /// The resource key to use.
        /// </summary>
        public string SubscriptionKey { get; set; }
        /// <summary>
        /// Whether to show experimental languages
        /// </summary>
        public bool ShowExperimental { get; set; }
        /// <summary>
        /// The Custom Translator category ID to use. 
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// Hold the Azure region. Important only for text translation. This is the region ID, not the region friendly name.
        /// </summary>
        public string AzureRegion { get; set; }
        /// <summary>
        /// Holds the URI of the Azure key vault to use instead of local settings.
        /// If not null or empty, other secrets and region will be ignored. 
        /// </summary>
        public string AzureKeyVaultName { get; set; }
        /// <summary>
        /// Holds the Text Translation Endpoint
        /// </summary>
        public string TextTransEndpoint { get; set; }
        /// <summary>
        /// Holds the string for experimental flights
        /// </summary>
        public string FlightString { get; set; }
        public bool UsingKeyVault
        {
            get
            {
                if (string.IsNullOrEmpty(AzureKeyVaultName?.Trim())) return false;
                else return true;
            }
        }
        public bool UsingProxy
        {
            get
            {
                if (string.IsNullOrEmpty(ProxyAddress?.Trim())) return false;
                else if (ProxyUseDefaultCredentials) return true;
                else return true;
            }
        }
        /// <summary>
        /// Whether to use user credentials when using a proxy
        /// </summary>
        public bool ProxyUseDefaultCredentials { get; set; }

        /// <summary>
        /// Proxy server address
        /// </summary>
        public string ProxyAddress { get; set; }

        /// <summary>
        /// OAuth2 authentication settings for Azure Government Entra ID
        /// </summary>
        public OAuth2Settings OAuth2 { get; set; }

        /// <summary>
        /// Whether to use OAuth2 authentication instead of or in addition to other authentication methods
        /// </summary>
        public bool UseOAuth2Authentication { get; set; }

        public bool UsingOAuth2
        {
            get
            {
                return UseOAuth2Authentication && OAuth2 != null && 
                       !string.IsNullOrEmpty(OAuth2.ClientId?.Trim()) && 
                       !string.IsNullOrEmpty(OAuth2.TenantId?.Trim());
            }
        }
    }

    public class Connectionstrings
    {
        /// <summary>
        /// Azure storage connection string, copied from the portal.
        /// </summary>
        public string StorageConnectionString { get; set; }
    }
}
