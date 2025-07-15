# OAuth2 Quick Reference

## Configuration Commands

### Enable OAuth2 Authentication
```bash
doctr config set --oauth2 true
```

### Set Azure AD Application Details
```bash
doctr config set --oauth2-client-id "your-azure-ad-app-client-id"
doctr config set --oauth2-tenant-id "your-azure-ad-tenant-id"
```

### Set Cloud Environment (Optional)
```bash
# For Azure Government (default)
doctr config set --oauth2-cloud "AzureGovernment"

# For Azure China
doctr config set --oauth2-cloud "AzureChina"

# For Azure Public/Commercial
doctr config set --oauth2-cloud "AzurePublic"
```

### Complete Setup in One Command
```bash
doctr config set --oauth2 true --oauth2-client-id "your-client-id" --oauth2-tenant-id "your-tenant-id" --oauth2-cloud "AzureGovernment"
```

## Authentication Commands

### Sign In
```bash
doctr oauth2 login
```

### Check Authentication Status
```bash
doctr oauth2 status
```

### Sign Out
```bash
doctr oauth2 logout
```

## Configuration Management

### View All Settings
```bash
doctr config list
```

### Clear OAuth2 Settings
```bash
doctr config set --oauth2 false
doctr config set --oauth2-client-id "clear"
doctr config set --oauth2-tenant-id "clear"
```

## Example Usage

### Complete Setup and Translation
```bash
# 1. Configure OAuth2
doctr config set --oauth2 true --oauth2-client-id "12345678-1234-1234-1234-123456789abc" --oauth2-tenant-id "87654321-4321-4321-4321-cba987654321"

# 2. Set Azure Government endpoints
doctr config set --endpoint "https://your-translator.cognitiveservices.azure.us/" --region "usgovvirginia"

# 3. Authenticate
doctr oauth2 login

# 4. Translate documents
doctr translate -f input.pdf -t output/ --to es,fr,de
```

## Cloud Environment Details

| Cloud | Authority URL | Default Scope |
|-------|---------------|---------------|
| Azure Government | `login.microsoftonline.us` | `https://cognitiveservices.azure.us/.default` |
| Azure China | `login.chinacloudapi.cn` | `https://cognitiveservices.azure.cn/.default` |
| Azure Public | `login.microsoftonline.com` | `https://cognitiveservices.azure.com/.default` |

## Error Handling

Common errors and solutions:

- **"OAuth2 authentication is not configured"**: Run configuration commands first
- **"OAuth2 authentication failed: AADSTS70002"**: Invalid credentials or tenant ID
- **"OAuth2 authentication failed: AADSTS65001"**: User consent required in Azure AD
- **"Failed to initialize OAuth2 authentication"**: Check Client ID and Tenant ID format

## Environment Variables Alternative

Set these environment variables instead of using CLI configuration:

```bash
export OAUTH2_ENABLED=true
export OAUTH2_CLIENT_ID="your-azure-ad-app-client-id"
export OAUTH2_TENANT_ID="your-azure-ad-tenant-id"
export OAUTH2_CLOUD_ENVIRONMENT="AzureGovernment"
```