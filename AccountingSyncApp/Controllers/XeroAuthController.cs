using Domain_Layer.Models;
using Application_Layer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System.Net;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Web;


namespace AccountingSyncApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class XeroAuthController : ControllerBase
    {
        //// In-memory storage for demonstration; later replace with DB
        //private static XeroTokenResponse _xeroTokens;

        //private readonly IConfiguration _config;

        private readonly IXeroAuthService _xeroAuthService;


        public XeroAuthController(IXeroAuthService xeroAuthService)
        {
            _xeroAuthService = xeroAuthService;
        }
        
        // Step 1: Redirect user to Xero authorization page
        //you’ll be redirected to the Xero login page`
        [HttpGet("login")]
        public async Task<IActionResult> Login()
        {
            var url = _xeroAuthService.GetLoginUrlAsync();
            return Redirect(url);//Redirects your browser to the full Xero login URL.At this point, the user will see the real Xero login page.
            //After logging in and allowing access, Xero will redirect back to your callback:https://localhost:7059/api/xeroauth/callback?code=XXXXXXXX

        }


        // Step 2: Xero redirects back here with ?code=xxxx
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery(Name = "code")] string authorizationCode, string state)//Receive the code (authorization code) from Xero via the query string.
        {
            // Check state
            //Prevents cross-site request forgery (CSRF).
            if (state != "12345") // match the state you sent in login
                return BadRequest("Invalid state parameter.");

            if (string.IsNullOrEmpty(authorizationCode)) //Validates that Xero actually sent a code parameter back.
                return BadRequest("Authorization code missing.");
            try
            {
                var token = await _xeroAuthService.HandleCallbackAsync(authorizationCode, state);
                return Ok(new
                {
                    message = "Xero tokens received successfully!",
                    accessToken = token.AccessToken,
                    refreshToken = token.RefreshToken,
                    expiresIn = token.ExpiresIn
                });
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
