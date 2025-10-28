using Domain_Layer.Models;
using Newtonsoft.Json;
using RestSharp;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Configuration;
using Application_Layer.Interfaces_Repository;
using Application_Layer.Interfaces.Xero;

namespace Application_Layer.Services.Xero
{
    public class XeroAuthService : IXeroAuthService
    {
        private readonly IXeroTokenRepository _tokenRepository;
        private readonly IConfiguration _config;

        public XeroAuthService(IXeroTokenRepository tokenRepository, IConfiguration config)
        {
            _tokenRepository = tokenRepository;
            _config = config;
        }

        public string GetLoginUrlAsync()
        {
            //Build the authorization URL (the full link to Xero’s login page).
            //Then call return Redirect(...)
            string clientId = _config["XeroSettings:ClientId"]; //Reads your app’s Client ID from appsettings.json.Xero uses this to know which app is asking for access.
            string redirectUri = _config["XeroSettings:RedirectUri"];//After login, Xero will send the authorization code back to this address.???

            string[] scopes = new string[] //Defines which permissions your app wants from Xero.
            {
                "openid",
                "profile",
                "email",
                "offline_access", //allows refresh token (so you can reconnect later)
                "accounting.contacts.read", //read customers
                "accounting.contacts", //create/update customers
                "accounting.transactions",
                "accounting.settings"
            };

            string scopeString = string.Join(" ", scopes); //Joins all those scopes into a single string separated by spaces.
                                                           //Xero expects scopes like this:openid profile email accounting.contacts.read accounting.contacts

            string authorizeUrl = "https://login.xero.com/identity/connect/authorize";//This is the official Xero OAuth 2.0 endpoint.We’ll redirect the user here to log in.

            var uriBuilder = new UriBuilder(authorizeUrl);//Creates a helper object to build a full URL (base + query parameters).
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);//Prepares a dictionary-like object where we’ll store our query parameters.??

            query["response_type"] = "code";//Tells Xero that we want an authorization code (standard OAuth flow).
            query["client_id"] = clientId;//Identifies your app.
            query["redirect_uri"] = redirectUri;//Where Xero should redirect after login.Esi pti anpayman imananq vor Xero-n imana te ur`Xero OAuth needs to know where to send the user back after login.dra hamarel aseci vor partadir pti unenanq es field-@.
            query["scope"] = scopeString;//Which permissions your app is asking for.
            query["state"] = "12345"; // optional state to verify later

            uriBuilder.Query = query.ToString();//Adds all those parameters to the base Xero URL
            return uriBuilder.ToString(); //return final URL
        }

        public async Task<XeroTokenResponse> HandleCallbackAsync(string authorizationCode, string state)
        {
            string clientId = _config["XeroSettings:ClientId"];
            string clientSecret = _config["XeroSettings:ClientSecret"]; //Reads app’s credentials from appsettings.json.
            string redirectUri = _config["XeroSettings:RedirectUri"];

            var client = new RestClient("https://identity.xero.com/connect/token");
            var request = new RestRequest()
            {
                Method = Method.Post
            };

            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            //In this method we must exchange the authorization code for an access token in Xero,
            //and for that there are specific required fields you must include in the body of your POST request.

            // Add body parameters (standard OAuth2 code exchange)
            request.AddParameter("grant_type", "authorization_code"); //Adds the required fields for the token request body.
            request.AddParameter("code", authorizationCode);
            request.AddParameter("redirect_uri", redirectUri);
            request.AddParameter("client_id", clientId);
            request.AddParameter("client_secret", clientSecret);
            //the above field are required for request body must be contain

            var response = await client.ExecuteAsync(request); //Sends the HTTP POST to Xero.Send a POST request to Xero’s token endpoint to exchange this code for an access token and refresh token.
            /*The response will contain:
            access_token → use this to call Xero API endpoints.
            refresh_token → use to get a new access token when it expires.
            expires_in → token lifetime in seconds.
            id_token → contains basic user info (optional, if requested with openid scope).
            */

            if (!response.IsSuccessful)
                throw new Exception("Failed to get access token: " + response.Content);

            //return Ok(response.Content); // later we’ll parse and store the token
            //Xero returns JSON with the tokens:
            //You now have the tokens needed to access Xero programmatically.
            //You can store access_token and refresh_token safely in your database(or memory) to call the API.
            //You must use the access_token in Authorization header for all Xero API calls://

            //Deserialize JSON into C# object
            //Console.WriteLine("🔍 Xero raw token response: " + response.Content);

            var tokenResponse = JsonConvert.DeserializeObject<XeroTokenResponse>(response.Content);
            if (tokenResponse == null)
                throw new Exception("Failed to parse token response.");
            tokenResponse.UpdatedAt = DateTime.UtcNow;
            // Convert to XeroToken entity and save
            var xeroToken = new XeroTokenResponse
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresIn = tokenResponse.ExpiresIn,
                TokenType = tokenResponse.TokenType,
                IdToken = tokenResponse.IdToken
            };
            await _tokenRepository.SaveTokenAsync(xeroToken);
            // Return success (tokens are stored in _xeroTokens for now)
            return tokenResponse;
        }
        public async Task SaveXeroTokenAsync(XeroTokenResponse token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            //Save token to Db
            await _tokenRepository.SaveTokenAsync(token);
        }






        public async Task<XeroTokenResponse> RefreshAccessTokenAsync(string refreshToken)
        {
            var client = new RestClient("https://identity.xero.com/connect/token");
            var request = new RestRequest();
            request.Method = Method.Post;
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");

            var clientId = _config["XeroSettings:ClientId"];
            var clientSecret = _config["XeroSettings:ClientSecret"];

            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", refreshToken);
            request.AddParameter("client_id", clientId);
            request.AddParameter("client_secret", clientSecret);

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                throw new Exception("❌ Failed to refresh token: " + response.Content);

            // 🔹 Declare the variable before using it
            XeroTokenResponse newToken;

            try
            {
                newToken = JsonConvert.DeserializeObject<XeroTokenResponse>(
                    response.Content,
                    new JsonSerializerSettings
                    {
                        MissingMemberHandling = MissingMemberHandling.Ignore
                    });
            }
            catch (Exception ex)
            {
                throw new Exception($"❌ Failed to deserialize Xero token response: {response.Content}\n{ex}");
            }

            if (newToken == null || string.IsNullOrEmpty(newToken.AccessToken))
            {
                throw new Exception($"❌ Xero token response did not contain AccessToken.\nResponse: {response.Content}");
            }

            newToken.UpdatedAt = DateTime.UtcNow;

            // 🔹 Save to DB
            await _tokenRepository.SaveTokenAsync(newToken);

            return newToken;
        }

    }
}