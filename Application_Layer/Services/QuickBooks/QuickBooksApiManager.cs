using Application_Layer.Interfaces;
using Application_Layer.Interfaces.QuickBooks;
using Domain_Layer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace Application_Layer.Services
{
    /// Calls QBO APIs for Customers, Invoices, and Quotes (Estimates).
    public class QuickBooksApiManager : IQuickBooksApiManager
    {
        private readonly IQuickBooksAuthService _auth;
        private readonly IConfiguration _config;
        private readonly ILogger<QuickBooksApiManager> _logger;

        public QuickBooksApiManager(
            IQuickBooksAuthService auth,
            IConfiguration config,
            ILogger<QuickBooksApiManager> logger)
        {
            _auth = auth;
            _config = config;
            _logger = logger;
        }

        private RestClient BuildClient()
        {
            var baseUrl = _config["QuickBooks:BaseUrl"]; // e.g. https://sandbox-quickbooks.api.intuit.com/v3/company
            var companyId = _config["QuickBooks:CompanyId"];
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

        // ---------------- CUSTOMERS ----------------
        public async Task<Customer> CreateOrUpdateCustomerAsync(Customer c)
        {
            var client = BuildClient();
            var isUpdate = !string.IsNullOrWhiteSpace(c.QuickBooksId);

            // Minimal QBO Customer payload
            var payload = new
            {
                DisplayName = c.Name,
                PrimaryEmailAddr = string.IsNullOrWhiteSpace(c.Email) ? null : new { Address = c.Email },
                PrimaryPhone = string.IsNullOrWhiteSpace(c.Phone) ? null : new { FreeFormNumber = c.Phone },
                BillAddr = string.IsNullOrWhiteSpace(c.Address) ? null : new { Line1 = c.Address }
            };

            RestRequest req;
            if (isUpdate)
            {
                // Sparse update: POST /customer?operation=update
                req = await MakeJsonRequestAsync("customer?operation=update", Method.Post);
                req.AddJsonBody(new { Id = c.QuickBooksId, sparse = true, payload.DisplayName, payload.PrimaryEmailAddr, payload.PrimaryPhone, payload.BillAddr });
            }
            else
            {
                // Create: POST /customer
                req = await MakeJsonRequestAsync("customer", Method.Post);
                req.AddJsonBody(payload);
            }

            var resp = await BuildClient().ExecuteAsync(req);
            if (!resp.IsSuccessful)
                throw new Exception($"QBO customer {(isUpdate ? "update" : "create")} failed: {resp.StatusCode} {resp.Content}");

            // Parse response: {"Customer":{"Id":"123","DisplayName":"..."}} etc.
            using var doc = System.Text.Json.JsonDocument.Parse(resp.Content!);
            var root = doc.RootElement;
            if (root.TryGetProperty("Customer", out var cust))
            {
                c.QuickBooksId = cust.GetProperty("Id").GetString();
                c.SyncedToQuickBooks = true;
            }
            return c;
        }

        // ---------------- INVOICES ----------------
        public async Task<Invoice> CreateOrUpdateInvoiceAsync(Invoice inv)
        {
            var isUpdate = !string.IsNullOrWhiteSpace(inv.QuickBooksId);

            // Minimal payload. In real usage you should map Lines, Amount, CustomerRef, etc.
            var payload = new
            {
                CustomerRef = new { value = inv.Customer?.QuickBooksId ?? "" },
                DocNumber = inv.InvoiceNumber,
                PrivateNote = inv.Description,
                TxnDate = inv.CreatedAt.ToString("yyyy-MM-dd"),
                DueDate = inv.DueDate.ToString("yyyy-MM-dd"),
                // A bare-minimum line so QBO accepts:
                Line = new object[]
                {
                    new {
                        DetailType = "SalesItemLineDetail",
                        Amount = (double)inv.TotalAmount,
                        SalesItemLineDetail = new { ItemRef = new { value = "1" } } // demo itemRef
                    }
                }
            };

            RestRequest req;
            if (isUpdate)
            {
                req = await MakeJsonRequestAsync("invoice?operation=update", Method.Post);
                req.AddJsonBody(new { Id = inv.QuickBooksId, sparse = true, payload.CustomerRef, payload.DocNumber, payload.PrivateNote, payload.TxnDate, payload.DueDate, payload.Line });
            }
            else
            {
                req = await MakeJsonRequestAsync("invoice", Method.Post);
                req.AddJsonBody(payload);
            }

            var resp = await BuildClient().ExecuteAsync(req);
            if (!resp.IsSuccessful)
                throw new Exception($"QBO invoice {(isUpdate ? "update" : "create")} failed: {resp.StatusCode} {resp.Content}");

            using var doc = System.Text.Json.JsonDocument.Parse(resp.Content!);
            if (doc.RootElement.TryGetProperty("Invoice", out var invJson))
            {
                inv.QuickBooksId = invJson.GetProperty("Id").GetString();
                inv.SyncedToQuickBooks = true;
            }
            return inv;
        }

        // ---------------- QUOTES (ESTIMATES) ----------------
        public async Task<Quote> CreateOrUpdateQuoteAsync(Quote q)
        {
            var isUpdate = !string.IsNullOrWhiteSpace(q.QuickBooksId);

            var payload = new
            {
                CustomerRef = new { value = q.Customer?.QuickBooksId ?? "" },
                PrivateNote = q.Description,
                TxnDate = q.CreatedAt.ToString("yyyy-MM-dd"),
                ExpiryDate = q.ExpiryDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),

                Line = new object[]
                {
                    new {
                        DetailType = "SalesItemLineDetail",
                        Amount = (double)q.TotalAmount,
                        SalesItemLineDetail = new { ItemRef = new { value = "1" } }
                    }
                }
            };

            RestRequest req;
            if (isUpdate)
            {
                req = await MakeJsonRequestAsync("estimate?operation=update", Method.Post);
                req.AddJsonBody(new
                {
                    Id = q.QuickBooksId,
                    sparse = true,
                    CustomerRef = payload.CustomerRef,
                    PrivateNote = payload.PrivateNote,
                    TxnDate = payload.TxnDate,
                    ExpiryDate = payload.ExpiryDate,
                    Line = payload.Line
                });
            }
            else
            {
                req = await MakeJsonRequestAsync("estimate", Method.Post);
                req.AddJsonBody(payload);
            }


            var resp = await BuildClient().ExecuteAsync(req);
            if (!resp.IsSuccessful)
                throw new Exception($"QBO estimate {(isUpdate ? "update" : "create")} failed: {resp.StatusCode} {resp.Content}");

            using var doc = System.Text.Json.JsonDocument.Parse(resp.Content!);
            if (doc.RootElement.TryGetProperty("Estimate", out var est))
            {
                q.QuickBooksId = est.GetProperty("Id").GetString();
                q.SyncedToQuickBooks = true;
            }
            return q;
        }
    }
}
