using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            //body-in parunakuma QUICKBOOKS accounting systemi komic uxarkvac infon eventi het kapvac, hima es body-in pti pars arvi u haskacvi inch eventa exel, orinak te inch kara parunaki ira mej body-in`
            /*
             {
  "eventNotifications": [
    {
      "realmId": "9341455550214329",
      "dataChangeEvent": {
        "entities": [
          {
            "id": "61",
            "operation": "Create",
            "name": "Customer",
            "lastUpdated": "2025-10-29T18:33:08.000Z"
          }
        ]
      }
    }
  ]
}
            */
             try
            {
                using var doc = JsonDocument.Parse(body);

                var events = doc.RootElement.GetProperty("eventNotifications");

                foreach (var evt in events.EnumerateArray())
                {
                    var dataChange = evt.GetProperty("dataChangeEvent");

                    if (!dataChange.TryGetProperty("entities", out var entities))
                        continue;

                    foreach (var entity in entities.EnumerateArray())
                    {
                        var entityName = entity.GetProperty("name").GetString();
                        var operation = entity.GetProperty("operation").GetString();
                        var id = entity.GetProperty("id").GetString();

                        _logger.LogInformation("📦 Webhook entity: {Entity} Operation: {Operation} Id: {Id}", entityName, operation, id);

                        switch (entityName)
                        {
                            case "Customer":
                                // For 'Create' or 'Update' → sync this customer
                                if (operation == "Create" || operation == "Update")
                                {
                                    _logger.LogInformation("🔄 Syncing QuickBooks customer {Id} to local DB...", id);
                                    await _syncManager.HandleQuickBooksCustomerChangedAsync(id);
                                }
                                break;

                            //case "Invoice":
                            //    if (operation == "Create" || operation == "Update")
                            //    {
                            //        _logger.LogInformation("🔄 Syncing QuickBooks invoice {Id}...", id);
                            //        await _syncManager.HandleQuickBooksInvoiceChangedAsync(id);
                            //    }
                            //    break;

                            //case "Estimate": // QuickBooks name for quotes
                            //    if (operation == "Create" || operation == "Update")
                            //    {
                            //        _logger.LogInformation("🔄 Syncing QuickBooks quote {Id}...", id);
                            //        await _syncManager.HandleQuickBooksQuoteChangedAsync(id);
                            //    }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing QuickBooks webhook.");
                return StatusCode(500, "Error processing webhook: " + ex.Message);
            }
             
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
