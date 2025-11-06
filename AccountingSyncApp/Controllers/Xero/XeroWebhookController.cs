using Application_Layer.Interfaces;
using Application_Layer.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

[ApiController]
[Route("api/webhooks/xeroW")]
public class XeroWebhookController : ControllerBase
{
    private readonly IAccountingSyncManager _syncManager;
    private readonly ILogger<XeroWebhookController> _logger;
    private readonly IConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    public XeroWebhookController(IAccountingSyncManager syncManager, ILogger<XeroWebhookController> logger, IConfiguration config , IServiceProvider serviceProvider)
    {
        _syncManager = syncManager;
        _logger = logger;
        _config = config;
        _serviceProvider = serviceProvider;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook()//This method is automatically called when Xero sends an HTTP POST request to your webhook URL (for example, via ngrok).
    {
        ////Accept webhook from Xero, verify signature, call sync
        //Read and log the webhook body (JSON describing what changed)
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();
        Console.WriteLine("\n\n\nwebhook send->" + payload + "\n\n\n");
        //_logger.LogInformation("Received Xero webhook payload: {payload}", payload);//fpr example`{
        /*  {"events":[{
  "resourceUrl": "https://api.xero.com/api.xro/2.0/Contacts/9980a9a8-dbbe-4c61-88a0-1c0971f54475",
  "resourceId": "9980a9a8-dbbe-4c61-88a0-1c0971f54475",
  "tenantId": "77ff4158-f638-4c62-8467-5345d8ad4007",
  "tenantType": "ORGANISATION",
  "eventCategory": "CONTACT",
  "eventType": "CREATE",
  "eventDateUtc": "2025-10-19T07:47:38.497"
}],"firstEventSequence": 10,"lastEventSequence": 10, "entropy": "HOTAMHMQMVYVKXFNBPKP"
 }           eventType info-n es pahin chi ogtagorcvum bayc hetagayum karanq sa ogtagoprcenq vor haskananq orinak ete Category-n Invoice a uremn call anenq SyncInvoices-@ etc...
 */

        // 2️.Verify webhook signature (important for security)
        //Verify the request really came from Xero(not from some hacker sending fake POSTs.) using your secret key
        var webhookKey = _config["XeroSettings:WebhookKey"];
        var xeroSignature = Request.Headers["x-xero-signature"].ToString();
        var computedSignature = ComputeHmacSha256(payload, webhookKey);

        if (computedSignature != xeroSignature)
        {
            _logger.LogWarning("Invalid Xero webhook signature — ignoring request.");
            return Unauthorized();
        }

        // 3️⃣ This request might be the “intent to receive” test OR a real webhook
        //if (string.IsNullOrWhiteSpace(payload))
        //{
        //    //When you first register your webhook URL in Xero developer portal, Xero sends an empty POST request to test if your endpoint is reachable.
        //    //That empty POST is the “intent to receive” handshake.
        //    //If payload is empty → you simply return 200 OK to confirm your endpoint is working.
        //    //After that, real webhook events will contain JSON data.
        //    _logger.LogInformation("Received 'Intent to receive' handshake from Xero ✅");
        //    return Ok();
        //}

        // ✅ Always return 200 OK quickly (ACKNOWLEDGE FIRST)
        // Respond immediately to Xero (so it doesn't retry)
        _logger.LogInformation("✅ Webhook accepted by server, starting async processing...");
        Task.Run(async () =>
        {
            try
            {
                // 🔹 Create a completely new scope for background execution
                using var scope = _serviceProvider.CreateScope();
                var scopedSyncManager = scope.ServiceProvider.GetRequiredService<IAccountingSyncManager>();
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<XeroWebhookController>>();

                if (string.IsNullOrWhiteSpace(payload))
                {
                    scopedLogger.LogInformation("Received 'Intent to receive' handshake from Xero ✅");
                    return;
                }

                var json = JObject.Parse(payload);
                var events = json["events"]?.ToObject<List<JObject>>();

                if (events == null || events.Count == 0)
                {
                    scopedLogger.LogWarning("No events found in Xero webhook payload.");
                    return;
                }

                foreach (var evt in events)
                {
                    try
                    {
                        var resourceId = evt["resourceId"]?.ToString();
                        var eventCategory = evt["eventCategory"]?.ToString();
                        var eventType = evt["eventType"]?.ToString();

                        scopedLogger.LogInformation("🔔 Xero event: {Category} - {Type} (ID={Id})",
                            eventCategory, eventType, resourceId);

                        switch (eventCategory?.ToUpperInvariant())
                        {
                            case "CONTACT":
                                await scopedSyncManager.SyncCustomersFromXeroAsync(resourceId);
                                break;

                            case "INVOICE":
                                await scopedSyncManager.SyncInvoicesFromXeroAsync(resourceId);
                                break;

                            case "QUOTE":
                                await scopedSyncManager.SyncQuotesFromXeroPeriodicallyAsync();
                                break;

                            default:
                                scopedLogger.LogInformation("⚠️ Ignored unknown eventCategory: {eventCategory}", eventCategory);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.LogError(ex, "❌ Failed to process webhook event, continuing with next...");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during async webhook processing");
            }
        });

        return Ok();
    }
    private string ComputeHmacSha256(string message, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }
}
