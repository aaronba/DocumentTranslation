# Azure Government Entra ID Authentication Setup

This document describes how to configure client-side OAuth2 authentication with Azure Government Entra ID for the Document Translation service.

## Prerequisites

1. Access to an Azure Government tenant
2. Administrative permissions to register applications in Azure AD
3. An Azure Government subscription

## Azure AD Application Registration

### Step 1: Register a New Application

1. Navigate to the Azure Government portal: https://portal.azure.us
2. Go to **Azure Active Directory** > **App registrations**
3. Click **New registration**
4. Configure the application:
   - **Name**: Document Translation Service
   - **Supported account types**: Accounts in this organizational directory only (Single tenant)
   - **Redirect URI**: 
     - Type: **Web**
     - URL: `https://your-domain.com/signin-oidc` (replace with your actual domain)

### Step 2: Configure Authentication

1. In your registered app, go to **Authentication**
2. Under **Web**, add additional redirect URIs if needed:
   - `https://your-domain.com/signin-oidc`
   - `https://localhost:5001/signin-oidc` (for development)
3. Under **Front-channel logout URL**, add:
   - `https://your-domain.com/signout-callback-oidc`
4. Under **Implicit grant and hybrid flows**, enable:
   - ✅ **ID tokens (used for implicit and hybrid flows)**
5. Click **Save**

### Step 3: Configure API Permissions

1. Go to **API permissions**
2. Click **Add a permission**
3. Select **Microsoft Graph**
4. Choose **Delegated permissions**
5. Add the following permissions:
   - `openid`
   - `profile`
   - `User.Read`
6. Click **Add permissions**
7. Click **Grant admin consent** (requires admin privileges)

### Step 4: Get Application Details

1. Go to **Overview** page of your app registration
2. Copy the following values:
   - **Application (client) ID**
   - **Directory (tenant) ID**

## Application Configuration

### Step 1: Update appsettings.json

Update your `appsettings.json` file with the Azure AD configuration:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.us/",
    "Domain": "your-domain.onmicrosoft.us",
    "TenantId": "your-tenant-id-from-step-4",
    "ClientId": "your-client-id-from-step-4",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

### Step 2: Configure for Development

For development, you can use `appsettings.Development.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.us/",
    "Domain": "your-domain.onmicrosoft.us",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

## Azure Government Endpoints

The application is configured to use Azure Government specific endpoints:

- **Authentication Authority**: `https://login.microsoftonline.us/`
- **Microsoft Graph API**: `https://graph.microsoft.us/`

These endpoints are different from the commercial Azure cloud and are automatically configured by the application.

## Testing Authentication

### Step 1: Run the Application

1. Start the application:
   ```bash
   dotnet run
   ```

2. Navigate to `https://localhost:5001` (or your configured URL)

### Step 2: Test Sign-In Flow

1. Click the **Sign In** button in the top navigation
2. You should be redirected to the Azure Government login page
3. Sign in with your Azure Government credentials
4. After successful authentication, you should be redirected back to the application
5. Your user name should appear in the top navigation dropdown

### Step 3: Test Protected Features

1. Navigate to the **Translate** page
2. Try to upload and translate a document
3. The operation should work with authentication headers included

## Troubleshooting

### Common Issues

1. **"Client ID not configured" error**
   - Ensure the `ClientId` is properly set in `appsettings.json`
   - Check that the configuration is being loaded correctly

2. **Redirect URI mismatch**
   - Verify the redirect URI in Azure AD matches your application URL
   - For development, ensure `https://localhost:5001/signin-oidc` is registered

3. **CORS errors**
   - Ensure your domain is properly configured in Azure AD
   - Check that the application is running over HTTPS

4. **Permission denied errors**
   - Verify API permissions are granted in Azure AD
   - Ensure admin consent has been provided

### Debug Steps

1. Check browser console for JavaScript errors
2. Verify configuration endpoint: `/api/config/auth`
3. Check application logs for authentication errors
4. Use browser developer tools to inspect network requests

## Security Considerations

1. **HTTPS Only**: Always use HTTPS in production
2. **Token Storage**: Tokens are stored in session storage (client-side)
3. **Session Management**: Tokens are automatically refreshed when needed
4. **Logout**: Proper token cleanup on logout
5. **Azure Government Compliance**: Uses Azure Government endpoints for compliance

## Production Deployment

1. Update redirect URIs in Azure AD to include production URLs
2. Configure proper SSL certificates
3. Set up appropriate logging and monitoring
4. Consider using Azure Key Vault for sensitive configuration
5. Implement proper error handling and user feedback

## Additional Resources

- [Azure Government Developer Guide](https://docs.microsoft.com/en-us/azure/azure-government/documentation-government-developer-guide)
- [Microsoft Identity Platform](https://docs.microsoft.com/en-us/azure/active-directory/develop/)
- [MSAL.js Documentation](https://docs.microsoft.com/en-us/azure/active-directory/develop/msal-js-initializing-client-applications)