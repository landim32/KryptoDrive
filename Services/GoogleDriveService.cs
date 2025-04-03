using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace KryptoDrive.Services
{
    public class GoogleDriveService
    {
        readonly string _windowsClientId = "641670805880-9g0fc2rre6lcnlf4km22k2d12gj2f1vs.apps.googleusercontent.com";      // UWP client
        readonly string _androidClientId = "__ANDROID_CLIENT_ID_HERE__";  // Android client
        readonly string _androidRedirectScheme = AppInfo.Current.PackageName;

        Oauth2Service? _oauth2Service;
        DriveService? _driveService;
        GoogleCredential? _credential;
        string? _email;

        public bool IsSignedIn => _credential != null;
        public string? Email => _email;

        public async Task Init()
        {
            var hasRefreshToken = await SecureStorage.GetAsync("refresh_token") is not null;
            if (!IsSignedIn && hasRefreshToken)
            {
                await SignIn();
            }
        }

        public async Task SignIn()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var expiresIn = Preferences.Get("access_token_epires_in", 0L);
            var isExpired = now - 10 > expiresIn;   // 10 second buffer
            var hasRefreshToken = await SecureStorage.GetAsync("refresh_token") is not null;

            if (isExpired && hasRefreshToken)
            {
                Debug.WriteLine("Using refresh token");
                await RefreshToken();
            }
            else if (isExpired)     // No refresh token
            {
                Debug.WriteLine("Starting auth code flow");
                if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
                {
                    await DoAuthCodeFlowWindows();
                }
                else if (DeviceInfo.Current.Platform == DevicePlatform.Android)
                {
                    await DoAuthCodeFlowAndroid();
                }
                else
                {
                    throw new NotImplementedException($"Auth flow for platform {DeviceInfo.Current.Platform} not implemented.");
                }
            }

            var accesToken = await SecureStorage.GetAsync("access_token");
            _credential = GoogleCredential.FromAccessToken(accesToken);
            _oauth2Service = new Oauth2Service(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = AppInfo.Current.Name
            });
            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = AppInfo.Current.Name
            });
            var userInfo = await _oauth2Service.Userinfo.Get().ExecuteAsync();
            _email = userInfo.Email;
        }

        public async Task<string> ListFiles()
        {
            var request = _driveService!.Files.List();
            var fileList = await request.ExecuteAsync();
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("Files:");
            stringBuilder.AppendLine();
            if (fileList.Files != null && fileList.Files.Count > 0)
            {
                foreach (var file in fileList.Files)
                {
                    var folder = file.Parents != null ? string.Join(", ", file.Parents) : string.Empty;
                    stringBuilder.AppendLine($"- {file.Name} ({file.Id}), {folder}");
                }
            }
            else
            {
                stringBuilder.AppendLine("No files found.");
            }
            return stringBuilder.ToString();
        }

        public async Task SignOut()
        {
            await RevokeTokens();
        }

        private async Task DoAuthCodeFlowWindows()
        {
            var authUrl = "https://accounts.google.com/o/oauth2/v2/auth";
            var clientId = _windowsClientId;
            var localPort = 12345;
            var redirectUri = $"http://localhost:{localPort}";
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            var parameters = GenerateAuthParameters(redirectUri, clientId, codeChallenge);
            var queryString = string.Join("&", parameters.Select(param => $"{param.Key}={param.Value}"));
            var fullAuthUrl = $"{authUrl}?{queryString}";

            await Launcher.OpenAsync(fullAuthUrl);
            var authorizationCode = await StartLocalHttpServerAsync(localPort);

            await GetInitialToken(authorizationCode, redirectUri, clientId, codeVerifier);
        }

        private async Task DoAuthCodeFlowAndroid()
        {
            var authUrl = "https://accounts.google.com/o/oauth2/v2/auth";
            var clientId = _androidClientId;
            var redirectUri = $"{_androidRedirectScheme}://";  // requires a period: https://developers.google.com/identity/protocols/oauth2/native-app#android
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            var parameters = GenerateAuthParameters(redirectUri, clientId, codeChallenge);
            var queryString = string.Join("&", parameters.Select(param => $"{param.Key}={param.Value}"));
            var fullAuthUrl = $"{authUrl}?{queryString}";
#pragma warning disable CA1416
            var authCodeResponse = await WebAuthenticator.AuthenticateAsync(new Uri(fullAuthUrl), new Uri(redirectUri));
#pragma warning restore CA1416
            var authorizationCode = authCodeResponse.Properties["code"];

            await GetInitialToken(authorizationCode, redirectUri, clientId, codeVerifier);
        }

        private static Dictionary<string, string> GenerateAuthParameters(string redirectUri, string clientId, string codeChallenge)
        {
            return new Dictionary<string, string>
        {
            //{ "scope", "https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/drive.file https://www.googleapis.com/auth/drive.appdata" },
            { "scope", string.Join(' ', [Oauth2Service.Scope.UserinfoProfile, Oauth2Service.Scope.UserinfoEmail, DriveService.Scope.Drive, DriveService.Scope.DriveFile, DriveService.Scope.DriveAppdata]) },
            { "access_type", "offline" },
            { "include_granted_scopes", "true" },
            { "response_type", "code" },
            //{ "state", "state_parameter_passthrough_value" },
            { "redirect_uri", redirectUri },
            { "client_id", clientId },
            { "code_challenge_method", "S256" },
            { "code_challenge", codeChallenge },
            //{ "prompt", "consent" }
        };
        }

        private static async Task GetInitialToken(string authorizationCode, string redirectUri, string clientId, string codeVerifier)
        {
            var tokenEndpoint = "https://oauth2.googleapis.com/token";
            var client = new HttpClient();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", authorizationCode),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("code_verifier", codeVerifier)
                ])
            };

            var response = await client.SendAsync(tokenRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) throw new Exception($"Error requesting token: {responseBody}");

            Debug.WriteLine($"Access token: {responseBody}");
            var jsonToken = JsonObject.Parse(responseBody);
            var accessToken = jsonToken!["access_token"]!.ToString();
            var refreshToken = jsonToken!["refresh_token"]!.ToString();
            var accessTokenExpiresIn = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + int.Parse(jsonToken!["expires_in"]!.ToString());
            await SecureStorage.SetAsync("access_token", accessToken);
            await SecureStorage.SetAsync("refresh_token", refreshToken);
            Preferences.Set("access_token_epires_in", accessTokenExpiresIn);
        }

        private async Task RefreshToken()
        {
            var clientId = DeviceInfo.Current.Platform == DevicePlatform.WinUI ? _windowsClientId : _androidClientId;
            var tokenEndpoint = "https://oauth2.googleapis.com/token";
            var refreshToken = await SecureStorage.GetAsync("refresh_token");
            var client = new HttpClient();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(
                    [
                        new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken!)
                    ]
                )
            };

            var response = await client.SendAsync(tokenRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) throw new Exception($"Error requesting token: {responseBody}");

            Debug.WriteLine($"Refresh token: {responseBody}");
            var jsonToken = JsonObject.Parse(responseBody);
            var accessToken = jsonToken!["access_token"]!.ToString();
            var accessTokenExpiresIn = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + int.Parse(jsonToken!["expires_in"]!.ToString());
            await SecureStorage.SetAsync("access_token", accessToken);
            Preferences.Set("access_token_epires_in", accessTokenExpiresIn);
        }

        private async Task RevokeTokens()
        {
            var revokeEndpoint = "https://oauth2.googleapis.com/revoke";
            var access_token = await SecureStorage.GetAsync("access_token");
            var client = new HttpClient();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, revokeEndpoint)
            {
                Content = new FormUrlEncodedContent(
                    [
                        new KeyValuePair<string, string>("token", access_token!),
                ]
                )
            };

            var response = await client.SendAsync(tokenRequest);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) throw new Exception($"Error revoking token: {responseBody}");

            Debug.WriteLine($"Revoke token: {responseBody}");
            SecureStorage.Remove("access_token");
            SecureStorage.Remove("refresh_token");
            Preferences.Remove("access_token_epires_in");

            _credential = null;
            _oauth2Service = null;
            _driveService = null;
        }

        private static async Task<string> StartLocalHttpServerAsync(int port)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            Debug.WriteLine($"Listening on http://localhost:{port}/...");
            var context = await listener.GetContextAsync();

            var code = context.Request.QueryString["code"];
            var response = context.Response;
            var responseString = "Authorization complete. You can close this window.";
            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
            response.OutputStream.Close();

            listener.Stop();

            if (code is null) throw new Exception("Auth ode not returned");

            return code;
        }

        private static string GenerateCodeVerifier()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32]; // Length can vary, e.g., 43-128 characters
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string GenerateCodeChallenge(string codeVerifier)
        {
            var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
            return Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
