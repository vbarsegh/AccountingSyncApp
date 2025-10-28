using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application_Layer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AccountingSyncApp.Controllers
{
    [ApiController]
    [Route("api/webhooks/quickbooks")]
    public class QuickBooksWebhookController : ControllerBase
    {
        private readonly ILogger<QuickBooksWebhookController> _logger;
        private readonly IConfiguration _config;
        private readonly IAccountingSyncManager _syncManager; // you already use this for coordination

        public QuickBooksWebhookController(
            ILogger<QuickBooksWebhookController> logger,
            IConfiguration config,
            IAccountingSyncManager syncManager)
        {
            _logger = logger;
            _config = config;
            _syncManager = syncManager;
        }

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            var body = await new StreamReader(Request.Body).ReadToEndAsync();

            // Verify signature
            var signatureHeader = Request.Headers["intuit-signature"].ToString();
            var secret = _config["QuickBooks:WebhookVerificationToken"];
            if (!VerifyHmac(body, secret, signatureHeader))
            {
                _logger.LogWarning("Invalid QuickBooks webhook signature, ignoring.");
                return Unauthorized();
            }

            _logger.LogInformation("QBO webhook received: {Body}", body);

            // TODO: Parse entities & operations; call your sync manager idempotently.
            // Example (pseudo):
            // foreach (var change in QuickBooksEventParser.Parse(body))
            // {
            //     switch (change.Entity)
            //     {
            //         case "Customer": await _syncManager.HandleQuickBooksCustomerChangedAsync(change.Customer); break;
            //         case "Invoice":  await _syncManager.HandleQuickBooksInvoiceChangedAsync(change.Invoice); break;
            //         case "Estimate": await _syncManager.HandleQuickBooksQuoteChangedAsync(change.Quote); break;
            //     }
            // }

            return Ok();
        }

        private static bool VerifyHmac(string body, string key, string signatureBase64)
        {
            if (string.IsNullOrWhiteSpace(signatureBase64) || string.IsNullOrWhiteSpace(key))
                return false;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            var expected = Convert.ToBase64String(hash);
            return string.Equals(expected, signatureBase64, StringComparison.Ordinal);
        }
    }
}
