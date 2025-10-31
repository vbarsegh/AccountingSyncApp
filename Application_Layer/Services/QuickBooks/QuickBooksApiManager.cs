using Application_Layer.Interfaces;
using Application_Layer.Interfaces.QuickBooks;
using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application_Layer.Services
{
    /// <summary>
    /// Handles all QuickBooks API operations for Customers, Invoices, and Estimates (Quotes).
    /// </summary>
    public class QuickBooksApiManager : IQuickBooksApiManager
    {
        private readonly IQuickBooksAuthService _auth;
        private readonly IQuickBooksTokenRepository _tokenRepo;
        private readonly IConfiguration _config;

        private readonly ILogger<QuickBooksApiManager> _logger;

        public QuickBooksApiManager(
            IQuickBooksAuthService auth,
            IQuickBooksTokenRepository tokenRepo,
            IConfiguration config,
            ILogger<QuickBooksApiManager> logger)
        {
            _auth = auth;
            _tokenRepo = tokenRepo;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Builds the RestClient dynamically using the RealmId (CompanyId) from the DB or appsettings fallback.
        /// </summary>
        private async Task<RestClient> BuildClientAsync()
        {
            var baseUrl = _config["QuickBooks:BaseUrl"];
            var token = await _tokenRepo.GetLatestAsync();
            string companyId = token?.RealmId;

            if (string.IsNullOrWhiteSpace(companyId))
            {
                companyId = _config["QuickBooks:CompanyId"];
                _logger.LogWarning("⚠️ RealmId not found in DB. Using fallback from appsettings.json: {CompanyId}", companyId);
            }
            else
            {
                _logger.LogInformation("✅ Using RealmId from DB: {CompanyId}", companyId);
            }

            return new RestClient($"{baseUrl}/{companyId}");
        }

        private async Task<RestRequest> MakeJsonRequestAsync(string resource, Method method)
        {
            var token = await _auth.GetAccessTokenAsync();
            var req = new RestRequest(resource, method);
            req.AddHeader("Authorization", $"Bearer {token}");
            req.AddHeader("Accept", "application/json");
            req.AddHeader("Content-Type", "application/json");
            return req;
        }

        // -------------------- CUSTOMERS --------------------
        // Minimal shape just for what we need
        private sealed class QboCustomerLight
        {
            public string Id { get; set; } = "";
            public string SyncToken { get; set; } = "";
            public string DisplayName { get; set; } = "";
        }
        // Parse QBO JSON -> QboCustomerLight (supports both direct and query responses)
        private static QboCustomerLight ParseCustomer(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Case 1: direct object -> { "Customer": { ... } }
            if (root.TryGetProperty("Customer", out var cust))
            {
                return new QboCustomerLight
                {
                    Id = cust.TryGetProperty("Id", out var id) ? id.GetString() ?? "" : "",
                    SyncToken = cust.TryGetProperty("SyncToken", out var st) ? st.GetString() ?? "" : "",
                    DisplayName = cust.TryGetProperty("DisplayName", out var dn) ? dn.GetString() ?? "" : ""
                };
            }

            // Case 2: query response -> { "QueryResponse": { "Customer": [ {...} ] } }
            if (root.TryGetProperty("QueryResponse", out var qr) &&
                qr.TryGetProperty("Customer", out var arr) &&
                arr.ValueKind == JsonValueKind.Array &&
                arr.GetArrayLength() > 0)
            {
                var first = arr[0];
                return new QboCustomerLight
                {
                    Id = first.TryGetProperty("Id", out var id) ? id.GetString() ?? "" : "",
                    SyncToken = first.TryGetProperty("SyncToken", out var st) ? st.GetString() ?? "" : "",
                    DisplayName = first.TryGetProperty("DisplayName", out var dn) ? dn.GetString() ?? "" : ""
                };
            }

            // Nothing recognized
            return new QboCustomerLight();
        }
        public async Task<string> GetCustomerByIdAsync(string quickBooksCustomerId)
        {
            var client = await BuildClientAsync();
            var req = await MakeJsonRequestAsync($"customer/{quickBooksCustomerId}", Method.Get);

            var resp = await client.ExecuteAsync(req);

            if (!resp.IsSuccessful)
                throw new Exception($"QuickBooks GET Customer failed: {resp.StatusCode} {resp.Content}");

            return resp.Content; // ✅ raw JSON string to be parsed by AccountingSyncManager
        }

        public async Task<Customer> CreateOrUpdateCustomerAsync(Customer c)
        {
            var client = await BuildClientAsync();

            // STEP 1️⃣: Check for existing customer by DisplayName
            var query = $"select * from Customer where DisplayName = '{c.Name.Replace("'", "''")}'";
            var queryReq = await MakeJsonRequestAsync($"query?query={Uri.EscapeDataString(query)}", Method.Get);
            var queryResp = await client.ExecuteAsync(queryReq);

            if (queryResp.IsSuccessful && queryResp.Content!.Contains("\"Customer\""))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(queryResp.Content!);
                var queryResponse = doc.RootElement.GetProperty("QueryResponse");

                if (queryResponse.TryGetProperty("Customer", out var customers) && customers.GetArrayLength() > 0)
                {
                    c.QuickBooksId = customers[0].GetProperty("Id").GetString();
                    _logger.LogInformation("✅ Found existing customer '{Name}' (Id={Id}), updating.", c.Name, c.QuickBooksId);
                }
            }

            // STEP 2️⃣: Build JSON body according to QBO spec
            var jsonBuilder = new Dictionary<string, object>();
            jsonBuilder["DisplayName"] = c.Name;

            if (!string.IsNullOrWhiteSpace(c.Email))
                jsonBuilder["PrimaryEmailAddr"] = new { Address = c.Email };

            if (!string.IsNullOrWhiteSpace(c.Phone))
                jsonBuilder["PrimaryPhone"] = new { FreeFormNumber = c.Phone };

            if (!string.IsNullOrWhiteSpace(c.Address))
                jsonBuilder["BillAddr"] = new { Line1 = c.Address };

            RestRequest req;

            // STEP 3️⃣: CREATE vs UPDATE
            if (string.IsNullOrEmpty(c.QuickBooksId))
            {
                req = await MakeJsonRequestAsync("customer", Method.Post);
            }
            else
            {
                // BEFORE UPDATE: always fetch latest
                var latestJson = await GetCustomerByIdAsync(c.QuickBooksId);
                var latestCustomer = ParseCustomer(latestJson);
                if (string.IsNullOrWhiteSpace(latestCustomer.SyncToken))
                    throw new Exception("QBO update requires SyncToken, but none was returned.");
                // use latest SyncToken
                jsonBuilder["Id"] = c.QuickBooksId;
                jsonBuilder["SyncToken"] = latestCustomer.SyncToken;//QuickBooks requires SyncToken when updating an existing customer.
                //Every time QuickBooks modifies a record, its SyncToken increments.
                jsonBuilder["sparse"] = true;
                req = await MakeJsonRequestAsync("customer?operation=update", Method.Post);
            }

            // ✅ Serialize & attach JSON
            var jsonBody = System.Text.Json.JsonSerializer.Serialize(jsonBuilder,
                new System.Text.Json.JsonSerializerOptions { IgnoreNullValues = true });
            req.AddStringBody(jsonBody, DataFormat.Json);

            // 🧠 Logging request safely (RestSharp no longer has req.Body)
            _logger.LogInformation("📤 Sending JSON body to QuickBooks:\n{Json}", jsonBody);

            // STEP 4️⃣: Execute
            var resp = await client.ExecuteAsync(req);

            if (!resp.IsSuccessful)
                throw new Exception($"QBO customer {(string.IsNullOrEmpty(c.QuickBooksId) ? "create" : "update")} failed: {resp.StatusCode} {resp.Content}");

            // STEP 5️⃣: Parse successful response
            // 5) Parse response and persist Id/SyncToken
            var created = ParseCustomer(resp.Content!);
            if (!string.IsNullOrWhiteSpace(created.Id))
            {
                c.QuickBooksId = created.Id;
                c.SyncToken = created.SyncToken; // ensure your Domain Customer has this property
                c.SyncedToQuickBooks = true;
            }

            _logger.LogInformation("✅ Customer '{Name}' synced successfully with QuickBooks (Id={Id})", c.Name, c.QuickBooksId);
            return c;
        }



        // -------------------- TODO: Invoices & Quotes --------------------
        // add CreateOrUpdateInvoiceAsync and CreateOrUpdateQuoteAsync.
        private sealed class QboInvoiceLight
        {
            public string Id { get; set; } = "";
            public string SyncToken { get; set; } = "";
            public string DocNumber { get; set; } = "";
        }

        private sealed class QboEstimateLight
        {
            public string Id { get; set; } = "";
            public string SyncToken { get; set; } = "";
            public string DocNumber { get; set; } = "";
        }
        public async Task<string> GetInvoiceByIdAsync(string id)
        {
            var client = await BuildClientAsync();
            var req = await MakeJsonRequestAsync($"invoice/{id}", Method.Get);
            var resp = await client.ExecuteAsync(req);

            if (!resp.IsSuccessful)
                throw new Exception($"Failed to get QBO invoice: {resp.StatusCode} {resp.Content}");

            return resp.Content!;
        }
        public async Task<string> GetEstimateByIdAsync(string id)
        {
            var client = await BuildClientAsync();
            var req = await MakeJsonRequestAsync($"estimate/{id}", Method.Get);
            var resp = await client.ExecuteAsync(req);

            if (!resp.IsSuccessful)
                throw new Exception($"Failed to get QBO estimate: {resp.StatusCode} {resp.Content}");

            return resp.Content!;
        }
        private static QboInvoiceLight ParseInvoice(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Invoice", out var inv))
                return new QboInvoiceLight
                {
                    Id = inv.GetProperty("Id").GetString() ?? "",
                    SyncToken = inv.GetProperty("SyncToken").GetString() ?? "",
                    DocNumber = inv.GetProperty("DocNumber").GetString() ?? ""
                };

            return new QboInvoiceLight();
        }
        private static QboEstimateLight ParseEstimate(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Estimate", out var est))
                return new QboEstimateLight
                {
                    Id = est.GetProperty("Id").GetString() ?? "",
                    SyncToken = est.GetProperty("SyncToken").GetString() ?? "",
                    DocNumber = est.GetProperty("DocNumber").GetString() ?? ""
                };

            return new QboEstimateLight();
        }
        public async Task<Invoice> CreateOrUpdateInvoiceAsync(Invoice inv)
        {
            var client = await BuildClientAsync();

            var body = new Dictionary<string, object?>
            {
                ["CustomerRef"] = new { value = inv.CustomerQuickBooksId },
                ["Line"] = new object[]
                {
                    new 
                    {
                        DetailType = "SalesItemLineDetail",
                        //we include DetailType and SalesItemLineDetail Because QuickBooks API does not allow creating an invoice without at least one line item.
                        SalesItemLineDetail = new { ItemRef = new { value = "1" } },
                        Amount = inv.TotalAmount,
                        Description = inv.Description
                    }
                },
                ["TxnDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
            };

            RestRequest req;

            if (string.IsNullOrEmpty(inv.QuickBooksId))
            {
                req = await MakeJsonRequestAsync("invoice", Method.Post);
            }
            else
            {
                var latest = ParseInvoice(await GetInvoiceByIdAsync(inv.QuickBooksId));
                body["Id"] = latest.Id;
                body["SyncToken"] = latest.SyncToken;
                body["sparse"] = true;

                req = await MakeJsonRequestAsync("invoice?operation=update", Method.Post);
            }

            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { IgnoreNullValues = true });
            req.AddStringBody(json, DataFormat.Json);

            var resp = await client.ExecuteAsync(req);
            if (!resp.IsSuccessful)
                throw new Exception($"Invoice sync failed: {resp.StatusCode} {resp.Content}");

            var parsed = ParseInvoice(resp.Content!);
            inv.QuickBooksId = parsed.Id;
            inv.SyncedToQuickBooks = true;

            return inv;
        }

        public async Task<Quote> CreateOrUpdateQuoteAsync(Quote quote)
        {
            var client = await BuildClientAsync();

            var body = new Dictionary<string, object?>
            {
                ["CustomerRef"] = new { value = quote.CustomerQuickBooksId },
                ["Line"] = new object[]
                {
            new
            {
                DetailType = "SalesItemLineDetail",
                SalesItemLineDetail = new { ItemRef = new { value = "1" } }, // default item
                Amount = quote.TotalAmount,
                Description = quote.Description
            }
                },
                ["TxnDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["ExpirationDate"] = (quote.ExpiryDate ?? DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd")
            };

            RestRequest req;

            // ✅ CREATE
            if (string.IsNullOrEmpty(quote.QuickBooksId))
            {
                req = await MakeJsonRequestAsync("estimate", Method.Post);
            }
            // ✅ UPDATE
            else
            {
                var latest = ParseEstimate(await GetEstimateByIdAsync(quote.QuickBooksId));

                body["Id"] = latest.Id;
                body["SyncToken"] = latest.SyncToken;
                body["sparse"] = true;

                req = await MakeJsonRequestAsync("estimate?operation=update", Method.Post);
            }

            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { IgnoreNullValues = true });
            req.AddStringBody(json, DataFormat.Json);

            var resp = await client.ExecuteAsync(req);
            if (!resp.IsSuccessful)
                throw new Exception($"Quote sync failed: {resp.StatusCode} {resp.Content}");

            var parsed = ParseEstimate(resp.Content!);
            quote.QuickBooksId = parsed.Id;
            quote.SyncedToQuickBooks = true;

            return quote;
        }



    }
}
