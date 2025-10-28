using System;
using System.Net;
using System.Threading.Tasks;
using Application_Layer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AccountingSyncApp.Controllers
{
    [ApiController]
    [Route("api/quickbooks/auth")]
    public class QuickBooksAuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IQuickBooksAuthService _auth;
        private readonly ILogger<QuickBooksAuthController> _logger;

        public QuickBooksAuthController(
            IConfiguration config,
            IQuickBooksAuthService auth,
            ILogger<QuickBooksAuthController> logger)
        {
            _config = config;
            _auth = auth;
            _logger = logger;
        }

        // Step 1: Redirect user to Intuit OAuth consent page
        [HttpGet("connect")]
        public IActionResult Connect()
        {
            var clientId = _config["QuickBooks:ClientId"];
            var redirectUri = WebUtility.UrlEncode(_config["QuickBooks:RedirectUri"]);
            var scopes = WebUtility.UrlEncode(_config["QuickBooks:Scopes"]);
            var state = Guid.NewGuid().ToString("N");

            var url =
                $"https://appcenter.intuit.com/connect/oauth2?client_id={clientId}" +
                $"&response_type=code&scope={scopes}&redirect_uri={redirectUri}&state={state}";

            return Redirect(url);
        }

        // Step 2: OAuth callback (QuickBooks redirects here with ?code=...&realmId=...)
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string realmId, [FromQuery] string state)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(realmId))
                return BadRequest("Missing code or realmId.");

            await _auth.HandleAuthCallbackAsync(code, realmId);
            return Ok("QuickBooks authorization completed. Tokens stored.");
        }
    }
}
