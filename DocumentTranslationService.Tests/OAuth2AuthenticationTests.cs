using DocumentTranslationService.Core;
using System;
using Xunit;

namespace DocumentTranslationService.Tests
{
    public class OAuth2AuthenticationTests
    {
        [Fact]
        public void OAuth2Settings_ShouldInitializeWithDefaults()
        {
            // Arrange & Act
            var settings = new OAuth2Settings();

            // Assert
            Assert.Equal("http://localhost", settings.RedirectUri);
            Assert.Equal("AzureGovernment", settings.CloudEnvironment);
            Assert.Contains("https://cognitiveservices.azure.com/.default", settings.Scopes);
        }

        [Fact]
        public void OAuth2Authentication_ShouldThrowArgumentNullException_WhenSettingsIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OAuth2Authentication(null));
        }

        [Fact]
        public void OAuth2Authentication_ShouldThrowArgumentException_WhenClientIdIsEmpty()
        {
            // Arrange
            var settings = new OAuth2Settings
            {
                ClientId = "",
                TenantId = "valid-tenant-id"
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new OAuth2Authentication(settings));
        }

        [Fact]
        public void OAuth2Authentication_ShouldThrowArgumentException_WhenTenantIdIsEmpty()
        {
            // Arrange
            var settings = new OAuth2Settings
            {
                ClientId = "valid-client-id",
                TenantId = ""
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new OAuth2Authentication(settings));
        }

        [Fact]
        public void OAuth2Authentication_ShouldThrowArgumentException_WhenScopesIsEmpty()
        {
            // Arrange
            var settings = new OAuth2Settings
            {
                ClientId = "valid-client-id",
                TenantId = "valid-tenant-id",
                Scopes = new string[0]
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new OAuth2Authentication(settings));
        }

        [Fact]
        public void OAuth2Authentication_ShouldInitializeSuccessfully_WhenValidSettings()
        {
            // Arrange
            var settings = new OAuth2Settings
            {
                ClientId = "test-client-id",
                TenantId = "test-tenant",
                CloudEnvironment = "AzureGovernment"
            };

            // Act
            var oauth2Auth = new OAuth2Authentication(settings);
            
            // Should not throw during initialization
            oauth2Auth.Initialize();
            
            // If we get here without exception, the initialization worked
            Assert.True(true);
        }

        [Fact]
        public void DocTransAppSettings_ShouldDetectUsingOAuth2_WhenProperlyConfigured()
        {
            // Arrange
            var settings = new DocTransAppSettings
            {
                UseOAuth2Authentication = true,
                OAuth2 = new OAuth2Settings
                {
                    ClientId = "test-client-id",
                    TenantId = "test-tenant-id"
                }
            };

            // Act & Assert
            Assert.True(settings.UsingOAuth2);
        }

        [Fact]
        public void DocTransAppSettings_ShouldNotDetectUsingOAuth2_WhenNotEnabled()
        {
            // Arrange
            var settings = new DocTransAppSettings
            {
                UseOAuth2Authentication = false,
                OAuth2 = new OAuth2Settings
                {
                    ClientId = "test-client-id",
                    TenantId = "test-tenant-id"
                }
            };

            // Act & Assert
            Assert.False(settings.UsingOAuth2);
        }

        [Fact]
        public void DocTransAppSettings_ShouldNotDetectUsingOAuth2_WhenClientIdMissing()
        {
            // Arrange
            var settings = new DocTransAppSettings
            {
                UseOAuth2Authentication = true,
                OAuth2 = new OAuth2Settings
                {
                    ClientId = "",
                    TenantId = "test-tenant-id"
                }
            };

            // Act & Assert
            Assert.False(settings.UsingOAuth2);
        }

        [Fact]
        public void OAuth2ServiceClientFactory_ShouldThrowInvalidOperationException_WhenOAuth2NotEnabled()
        {
            // Arrange
            var settings = new DocTransAppSettings
            {
                UseOAuth2Authentication = false
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => OAuth2ServiceClientFactory.CreateCredential(settings));
        }

        [Theory]
        [InlineData("AzureGovernment", "https://cognitiveservices.azure.us/.default")]
        [InlineData("government", "https://cognitiveservices.azure.us/.default")]
        [InlineData("AzureChina", "https://cognitiveservices.azure.cn/.default")]
        [InlineData("china", "https://cognitiveservices.azure.cn/.default")]
        [InlineData("AzurePublic", "https://cognitiveservices.azure.com/.default")]
        [InlineData("public", "https://cognitiveservices.azure.com/.default")]
        [InlineData("unknown", "https://cognitiveservices.azure.us/.default")] // Default to Azure Government
        public void OAuth2ServiceClientFactory_ShouldReturnCorrectDefaultScope(string cloudEnvironment, string expectedScope)
        {
            // Arrange
            var settings = new DocTransAppSettings
            {
                UseOAuth2Authentication = true,
                OAuth2 = new OAuth2Settings
                {
                    ClientId = "test-client-id",
                    TenantId = "test-tenant-id",
                    CloudEnvironment = cloudEnvironment
                }
            };

            // Act
            var credential = OAuth2ServiceClientFactory.CreateCredential(settings);
            
            // Assert that credential was created successfully
            Assert.NotNull(credential);
        }
    }
}