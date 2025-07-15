/**
 * Authentication service for Azure Government Entra ID integration
 * Uses MSAL.js for client-side OAuth2 authentication
 */

class AuthService {
    constructor() {
        this.msalInstance = null;
        this.account = null;
        this.isAuthenticated = false;
        
        // Azure Government cloud configuration
        this.config = {
            auth: {
                clientId: '', // Will be set from configuration
                authority: 'https://login.microsoftonline.us/', // Azure Government authority
                redirectUri: window.location.origin,
                postLogoutRedirectUri: window.location.origin
            },
            cache: {
                cacheLocation: 'sessionStorage',
                storeAuthStateInCookie: false
            },
            system: {
                loggerOptions: {
                    loggerCallback: (level, message, containsPii) => {
                        if (containsPii) return;
                        console.log(`[MSAL] ${message}`);
                    },
                    piiLoggingEnabled: false,
                    logLevel: 'Info'
                }
            }
        };
        
        this.loginRequest = {
            scopes: ['openid', 'profile', 'User.Read'],
            prompt: 'select_account'
        };
        
        this.tokenRequest = {
            scopes: ['openid', 'profile', 'User.Read'],
            account: null
        };
        
        this.graphApiRequest = {
            scopes: ['User.Read'],
            account: null
        };

        this.authCallbacks = [];
        this.userInfoCallbacks = [];
    }

    /**
     * Initialize MSAL instance with configuration
     * @param {string} clientId - Azure AD Application (client) ID
     * @param {string} tenantId - Azure AD Tenant ID (optional)
     */
    async initialize(clientId, tenantId = null) {
        if (!clientId) {
            throw new Error('Client ID is required for authentication');
        }

        this.config.auth.clientId = clientId;
        
        if (tenantId) {
            this.config.auth.authority = `https://login.microsoftonline.us/${tenantId}`;
        }

        // Check if MSAL is loaded
        if (typeof msal === 'undefined') {
            throw new Error('MSAL.js library is not loaded. Please include it in your page.');
        }

        try {
            this.msalInstance = new msal.PublicClientApplication(this.config);
            await this.msalInstance.initialize();
            
            // Handle redirect response
            const response = await this.msalInstance.handleRedirectPromise();
            if (response) {
                this.account = response.account;
                this.isAuthenticated = true;
                this.notifyAuthStateChanged();
                await this.loadUserInfo();
            } else {
                // Check if user is already signed in
                const accounts = this.msalInstance.getAllAccounts();
                if (accounts.length > 0) {
                    this.account = accounts[0];
                    this.isAuthenticated = true;
                    this.notifyAuthStateChanged();
                    await this.loadUserInfo();
                }
            }
            
            console.log('[AuthService] Initialized successfully');
            return true;
        } catch (error) {
            console.error('[AuthService] Initialization failed:', error);
            throw error;
        }
    }

    /**
     * Sign in the user
     */
    async signIn() {
        if (!this.msalInstance) {
            throw new Error('Authentication service not initialized');
        }

        try {
            console.log('[AuthService] Starting sign-in process...');
            await this.msalInstance.loginRedirect(this.loginRequest);
        } catch (error) {
            console.error('[AuthService] Sign-in failed:', error);
            throw error;
        }
    }

    /**
     * Sign out the user
     */
    async signOut() {
        if (!this.msalInstance) {
            throw new Error('Authentication service not initialized');
        }

        try {
            console.log('[AuthService] Starting sign-out process...');
            this.account = null;
            this.isAuthenticated = false;
            this.notifyAuthStateChanged();
            
            const logoutRequest = {
                account: this.msalInstance.getAllAccounts()[0],
                postLogoutRedirectUri: this.config.auth.postLogoutRedirectUri
            };
            
            await this.msalInstance.logoutRedirect(logoutRequest);
        } catch (error) {
            console.error('[AuthService] Sign-out failed:', error);
            throw error;
        }
    }

    /**
     * Get access token for API calls
     * @param {string[]} scopes - Scopes required for the token
     * @returns {Promise<string>} Access token
     */
    async getAccessToken(scopes = ['User.Read']) {
        if (!this.isAuthenticated || !this.account) {
            throw new Error('User not authenticated');
        }

        try {
            const request = {
                scopes: scopes,
                account: this.account
            };

            const response = await this.msalInstance.acquireTokenSilent(request);
            return response.accessToken;
        } catch (error) {
            console.warn('[AuthService] Silent token acquisition failed, trying interactive:', error);
            
            try {
                const request = {
                    scopes: scopes,
                    account: this.account
                };
                
                const response = await this.msalInstance.acquireTokenRedirect(request);
                return response.accessToken;
            } catch (interactiveError) {
                console.error('[AuthService] Interactive token acquisition failed:', interactiveError);
                throw interactiveError;
            }
        }
    }

    /**
     * Load user information from Microsoft Graph API
     */
    async loadUserInfo() {
        if (!this.isAuthenticated) {
            return null;
        }

        try {
            const accessToken = await this.getAccessToken(['User.Read']);
            
            // Use Azure Government Graph API endpoint
            const response = await fetch('https://graph.microsoft.us/v1.0/me', {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${accessToken}`,
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                const userInfo = await response.json();
                console.log('[AuthService] User info loaded:', userInfo);
                this.notifyUserInfoChanged(userInfo);
                return userInfo;
            } else {
                console.error('[AuthService] Failed to load user info:', response.status, response.statusText);
                return null;
            }
        } catch (error) {
            console.error('[AuthService] Error loading user info:', error);
            return null;
        }
    }

    /**
     * Get current authentication state
     */
    getAuthState() {
        return {
            isAuthenticated: this.isAuthenticated,
            account: this.account,
            userInfo: this.userInfo
        };
    }

    /**
     * Subscribe to authentication state changes
     * @param {Function} callback - Callback function to call on state change
     */
    onAuthStateChanged(callback) {
        this.authCallbacks.push(callback);
    }

    /**
     * Subscribe to user info changes
     * @param {Function} callback - Callback function to call on user info change
     */
    onUserInfoChanged(callback) {
        this.userInfoCallbacks.push(callback);
    }

    /**
     * Notify all subscribers of authentication state changes
     */
    notifyAuthStateChanged() {
        const state = this.getAuthState();
        this.authCallbacks.forEach(callback => {
            try {
                callback(state);
            } catch (error) {
                console.error('[AuthService] Error in auth state callback:', error);
            }
        });
    }

    /**
     * Notify all subscribers of user info changes
     */
    notifyUserInfoChanged(userInfo) {
        this.userInfo = userInfo;
        this.userInfoCallbacks.forEach(callback => {
            try {
                callback(userInfo);
            } catch (error) {
                console.error('[AuthService] Error in user info callback:', error);
            }
        });
    }

    /**
     * Handle authentication errors
     * @param {Error} error - The error to handle
     */
    handleAuthError(error) {
        console.error('[AuthService] Authentication error:', error);
        
        if (error.name === 'InteractionRequiredAuthError') {
            console.log('[AuthService] User interaction required, redirecting to sign-in...');
            this.signIn();
        } else if (error.name === 'BrowserAuthError') {
            console.error('[AuthService] Browser authentication error:', error.message);
        } else {
            console.error('[AuthService] Unknown authentication error:', error);
        }
    }

    /**
     * Check if the current session is valid
     */
    async validateSession() {
        if (!this.isAuthenticated || !this.account) {
            return false;
        }

        try {
            await this.getAccessToken(['User.Read']);
            return true;
        } catch (error) {
            console.warn('[AuthService] Session validation failed:', error);
            this.isAuthenticated = false;
            this.account = null;
            this.notifyAuthStateChanged();
            return false;
        }
    }
}

// Create global auth service instance
window.authService = new AuthService();

// Export for use in modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AuthService;
}