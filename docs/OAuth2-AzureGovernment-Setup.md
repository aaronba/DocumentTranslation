# OAuth2 Authentication for Azure Government

This document describes how to set up and use OAuth2 authentication with Azure Government Entra ID for the Document Translation application.

## Overview

The Document Translation application now supports OAuth2 authentication using Azure Government Entra ID (Azure Active Directory). This authentication method provides enhanced security and supports modern authentication flows required by many organizations.

## Prerequisites

1. An Azure Government subscription
2. Access to Azure Government Entra ID (Azure AD)
3. Permissions to register applications in Azure Government AD
4. Document Translation application with OAuth2 support

## Azure Government Entra ID Application Registration

### Step 1: Register a New Application

1. Sign in to the [Azure Government Portal](https://portal.azure.us)
2. Navigate to **Azure Active Directory** > **App registrations**
3. Click **New registration**
4. Fill in the application details:
   - **Name**: `Document Translation OAuth2`
   - **Supported account types**: `Accounts in this organizational directory only`
   - **Redirect URI**: Select `Public client/native (mobile & desktop)` and enter `http://localhost`
5. Click **Register**

### Step 2: Configure API Permissions

1. In your registered app, navigate to **API permissions**
2. Click **Add a permission**
3. Select **Azure Service Management**
4. Choose **Delegated permissions**
5. Select **user_impersonation**
6. Click **Add permissions**
7. If required by your organization, click **Grant admin consent**

### Step 3: Note Application Details

Copy the following values from your registered application:
- **Application (client) ID**: Found on the Overview page
- **Directory (tenant) ID**: Found on the Overview page

## Configuration

### Method 1: Using Configuration File

Create or update your `appsettings.json` file with OAuth2 settings:

```json
{
  "AzureResourceName": "https://your-translator-resource.cognitiveservices.azure.us/",
  "ConnectionStrings": {
    "StorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=yourstorageaccount;AccountKey=your-storage-key;EndpointSuffix=core.usgovcloudapi.net"
  },
  "AzureRegion": "usgovvirginia",
  "TextTransEndpoint": "https://api.cognitive.microsofttranslator.us/",
  "UseOAuth2Authentication": true,
  "OAuth2": {
    "ClientId": "your-azure-ad-app-client-id",
    "TenantId": "your-azure-ad-tenant-id",
    "RedirectUri": "http://localhost",
    "Scopes": [
      "https://cognitiveservices.azure.us/.default"
    ],
    "CloudEnvironment": "AzureGovernment"
  }
}
```

### Method 2: Using Command Line

Configure OAuth2 using the CLI:

```bash
# Enable OAuth2 authentication
doctr config set --oauth2 true

# Set Azure AD application details
doctr config set --oauth2-client-id "your-azure-ad-app-client-id"
doctr config set --oauth2-tenant-id "your-azure-ad-tenant-id"

# Set cloud environment (optional, defaults to AzureGovernment)
doctr config set --oauth2-cloud "AzureGovernment"
```

### Method 3: Using Environment Variables

Set the following environment variables:

```bash
export OAUTH2_ENABLED=true
export OAUTH2_CLIENT_ID="your-azure-ad-app-client-id"
export OAUTH2_TENANT_ID="your-azure-ad-tenant-id"
export OAUTH2_REDIRECT_URI="http://localhost"
export OAUTH2_CLOUD_ENVIRONMENT="AzureGovernment"
export OAUTH2_SCOPES="https://cognitiveservices.azure.us/.default"
```

## Usage

### Authentication Commands

Check OAuth2 status:
```bash
doctr oauth2 status
```

Authenticate with Azure Government:
```bash
doctr oauth2 login
```

Sign out:
```bash
doctr oauth2 logout
```

### Document Translation

Once OAuth2 is configured and you've authenticated, you can use document translation as usual:

```bash
doctr translate -f /path/to/input/file.pdf -t /path/to/output/ --to es
```

## Authentication Flow

1. **Initial Authentication**: When you run `doctr oauth2 login`, the application will:
   - Open your default web browser
   - Redirect to Azure Government sign-in page (`login.microsoftonline.us`)
   - Prompt you to sign in with your Azure Government credentials
   - Request consent for the required permissions
   - Return an access token to the application

2. **Token Management**: The application automatically:
   - Stores authentication tokens securely
   - Refreshes expired tokens as needed
   - Uses tokens to authenticate with Azure services

3. **Service Access**: OAuth2 tokens are used to authenticate with:
   - Azure Translator Service
   - Azure Storage (if using managed identity)
   - Other Azure Government services

## Cloud Environment Support

The OAuth2 implementation supports multiple Azure clouds:

- **Azure Government** (default): `login.microsoftonline.us`
- **Azure China**: `login.chinacloudapi.cn`
- **Azure Public**: `login.microsoftonline.com`

Configure the cloud environment using:
```bash
doctr config set --oauth2-cloud "AzureGovernment"
```

## Security Considerations

1. **Client ID and Tenant ID**: These are not secrets and can be stored in configuration files
2. **Access Tokens**: Stored temporarily in memory and managed by the MSAL library
3. **Refresh Tokens**: Securely stored by the MSAL token cache
4. **Network Traffic**: All authentication traffic uses HTTPS
5. **Local Storage**: Token cache is encrypted when stored locally

## Troubleshooting

### Common Issues

1. **Authentication Failed**
   - Verify Client ID and Tenant ID are correct
   - Ensure the application is registered in the correct Azure Government tenant
   - Check that required permissions are granted

2. **Invalid Cloud Environment**
   - Verify the cloud environment setting matches your Azure subscription
   - For Azure Government, use `AzureGovernment`

3. **Browser Not Opening**
   - Check if you're running in a headless environment
   - Verify firewall settings allow outbound HTTPS connections
   - Try using the embedded web view option

4. **Token Refresh Failures**
   - Sign out and sign in again: `doctr oauth2 logout && doctr oauth2 login`
   - Clear the token cache if issues persist

### Error Messages

- `OAuth2 authentication is not configured`: Run configuration commands first
- `Failed to initialize OAuth2 authentication`: Check Client ID and Tenant ID
- `OAuth2 authentication failed: AADSTS70002`: Invalid credentials or tenant
- `OAuth2 authentication failed: AADSTS65001`: User consent required

### Logging

Enable detailed logging by setting the environment variable:
```bash
export MSAL_LOG_LEVEL=Verbose
```

## Migration from Key-based Authentication

OAuth2 can be used alongside existing authentication methods. To migrate:

1. Set up OAuth2 as described above
2. Test OAuth2 authentication with `doctr oauth2 login`
3. Enable OAuth2 with `doctr config set --oauth2 true`
4. Optionally remove old credentials from configuration

## Support

For issues specific to OAuth2 authentication:
1. Check this documentation
2. Review Azure Government Entra ID configuration
3. Submit an issue on the project repository

## Related Documentation

- [Azure Government Documentation](https://docs.microsoft.com/en-us/azure/azure-government/)
- [Azure Active Directory Authentication](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [MSAL Authentication Library](https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-overview)