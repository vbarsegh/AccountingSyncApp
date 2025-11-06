//This service will handle all requests to Xero using the stored token.
using Application.DTOs;
using Application_Layer.DTO.Customers;
using Application_Layer.Interfaces.Xero;
using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace Application_Layer.Services.Xero
{
    //stex inch linum katarvuma Xero accounting system-i het
    public class XeroApiManager : IXeroApiManager
    {
        private readonly IXeroTokenRepository _tokenRepository;
        private readonly IXeroAuthService _xeroAuthService; // ✅ add this
        private readonly IConfiguration _config;
        public XeroApiManager(IXeroTokenRepository tokenRepository, IXeroAuthService xeroAuthService, IConfiguration config)
        {
            _tokenRepository = tokenRepository;
            _xeroAuthService = xeroAuthService;
            _config = config;
        }
     

        /// <summary>
        /// Karevor:ete petq exav piti regresh token anenq
        public async Task<string> GetValidAccessTokenAsync()
        {
            var token = await _tokenRepository.GetTokenAsync();

            if (token == null)
                throw new Exception("No Xero token found. Please log in again.");

            // Check if token is expired or close to expiring
            if (token.UpdatedAt.AddSeconds(token.ExpiresIn) < DateTime.UtcNow)
            {
                // Refresh the token
                var newToken = await _xeroAuthService.RefreshAccessTokenAsync(token.RefreshToken);
                await _tokenRepository.SaveTokenAsync(newToken);
                return newToken.AccessToken;
            }
            
            return token.AccessToken;
        }
        // Helper: Get access token from DB

        #region Customers
        /// ////////////////////
        public async Task<string> GetConnectionsAsync()//for recieve the tenantId,The tenantId is a unique identifier of the Xero organization your app is connected to.
                                                       //You must include it in every API call:
        {
            var accessToken = await GetValidAccessTokenAsync();

            var client = new RestClient("https://api.xero.com/connections");
            var request = new RestRequest();
            request.Method = Method.Get;
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Accept", "application/json");

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"Failed to get connections: {response.Content}");

            return response.Content; // JSON that includes your tenantId
        }
        ///////////////////////////////
        public async Task<string> GetCustomerByXeroIdAsync(string xeroId)
        {
            var accessToken = await GetValidAccessTokenAsync();
            var client = new RestClient($"https://api.xero.com/api.xro/2.0/Contacts/{xeroId}");
            var request = new RestRequest();
            request.Method = Method.Get;

            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Accept", "application/json");

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                //_logger.LogWarning("Contact not found in Xero or request failed: {xeroId}", xeroId);
                return null; // ✅ skip instead of throwing
            }
            return response.Content;
        }
        public async Task<string> GetCustomerByEmailAsync(string email)
        {
            var accessToken = await GetValidAccessTokenAsync();
            var client = new RestClient("https://api.xero.com/api.xro/2.0/Contacts");
            var request = new RestRequest($"https://api.xero.com/api.xro/2.0/Contacts?where=EmailAddress==\"{email}\"");
            request.Method = Method.Get;

            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Accept", "application/json");

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                //_logger.LogWarning("Contact not found in Xero or request failed: {xeroId}", xeroId);
                return null; // ✅ skip instead of throwing
            }
            return response.Content;
        }

        public async Task<string> GetLatestCustomerAsync()
        {
            var accessToken = await GetValidAccessTokenAsync();
            if (accessToken == null) throw new Exception("No Xero token found");

            // Sort by UpdatedDateUTC descending, take only the most recent
            var client = new RestClient("https://api.xero.com/api.xro/2.0/Contacts?order=UpdatedDateUTC%20DESC");
            var request = new RestRequest();
            request.Method = Method.Get;

            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Accept", "application/json");

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"Xero API error: {response.Content}");

            return response.Content;
        }

        public async Task<string> GetCustomersAsync()
        {
            //var accessToken = await GetAccessTokenAsync();
            var accessToken = await GetValidAccessTokenAsync();
            if (accessToken == null) throw new Exception("No Xero token found");
            // 2. Create a request to Xero API
            var client = new RestClient("https://api.xero.com/api.xro/2.0/Contacts");//Creates a client to call Xero API’s Contacts endpoint (customers).
            ///api.xro/2.0/Contacts?where=IsCustomer==true    vor menak customernerin ta ,hetagayum vor quote ban anem karoxa filtrelu hamar petq ga!!!!
            var request = new RestRequest("https://api.xero.com/api.xro/2.0/Contacts?where=ContactStatus==\"ACTIVE\"");
            request.Method = Method.Get;
            request.AddHeader("Authorization", $"Bearer {accessToken}");//Adds the access token in the HTTP header so Xero knows who you are.
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Accept", "application/json");
            // 3. Send request and get response// 3. Send request and get response
            var response = await client.ExecuteAsync(request);//Sends the GET request to Xero and waits for the response asynchronously.
            // 4. Check if the request was successful
            if (!response.IsSuccessful)
                throw new Exception($"Xero API error: {response.Content}");
            //Console.WriteLine("📡 Xero response:");
            //Console.WriteLine(response.Content);

            return response.Content;
        }

            public async Task<string> CreateCustomerAsync(CustomerCreateDto customer)
            {
                //these only talk to Xero API, not your local database yet.
                //var accessToken = await GetAccessTokenAsync();
                var accessToken = await GetValidAccessTokenAsync();
                var client = new RestClient("https://api.xero.com/api.xro/2.0/Contacts");
                var request = new RestRequest();
                request.Method = Method.Post;

                request.AddHeader("Authorization", $"Bearer {accessToken}");
                request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);

                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                Contacts = new[]
    {
                new
                {
                    customer.Name,
                    EmailAddress = customer.Email,
                    Phones = new[]
                    {
                        new { PhoneType = "DEFAULT", PhoneNumber = customer.Phone }
                    },
                    Addresses = new[]
                    {
                        new { AddressType = "STREET", AddressLine1 = customer.Address }
                    },
                    // Optional: mark as customer
                    IsCustomer = true
                }
        }
            };
            request.AddJsonBody(body);

                var response = await client.ExecuteAsync(request);
            Console.WriteLine("\n\n\n"  + "hasavXeroApimanager\n");
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                //_logger.LogWarning("Contact not found in Xero or request failed: {xeroId}", xeroId);
                return null; // ✅ skip instead of throwing
            }
            return response.Content;
        }
        //The difference is the URL and whether XeroId exists. That’s how Xero knows “create” vs “update.”
        public async Task<string> UpdateCustomerAsync(CustomerCreateDto customer)
        {
            var accessToken = await GetValidAccessTokenAsync();

            // Xero’s update endpoint still uses /Contacts, not /Contacts/{id}
            var client = new RestClient("https://api.xero.com/api.xro/2.0/Contacts");
            var request = new RestRequest();
            //Xero’s API is not a standard REST API.and that reason Xero uses POST for both create & update`
            request.Method = Method.Post;

            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            // ✅ Wrap inside "Contacts" array
            var body = new
            {
                Contacts = new[]
                {
            new
            {
                ContactID = customer.XeroId,//ete menq include enq anum XeroId-n(ContactID) body-ii mej Xero-n sranova haskanum vor pti update ani vochte create
                customer.Name,
                EmailAddress = customer.Email,
                Phones = new[]
                {
                    new { PhoneType = "DEFAULT", PhoneNumber = customer.Phone }
                },
                Addresses = new[]
                {
                    new { AddressType = "STREET", AddressLine1 = customer.Address }
                }
            }
        }
            };

            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                //_logger.LogWarning("Contact not found in Xero or request failed: {xeroId}", xeroId);
                return null; // ✅ skip instead of throwing
            }
            return response.Content;
        }

        #endregion

        // ✅ FIXED AND CLEANED INVOICE SECTION
        #region Invoices

        public async Task<string> GetInvoiceByXeroIdAsync(string invoiceXeroId)
        {
            var accessToken = await GetValidAccessTokenAsync();
            var client = new RestClient($"https://api.xero.com/api.xro/2.0/Invoices/{invoiceXeroId}");
            var request = new RestRequest();
            request.Method = Method.Get;

            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Accept", "application/json");

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                Console.WriteLine("❗ Xero invoice not found or request failed: {invoiceId}. Response: {response}"+ invoiceXeroId + response.Content);
                return null; // skip this invoice in webhook processing
            }

            return response.Content;

        }

        public async Task<string> GetInvoicesAsync()
        {
            var accessToken = await GetValidAccessTokenAsync();
            var client = new RestClient("https://api.xero.com/api.xro/2.0/Invoices");
            var request = new RestRequest();
            request.Method = Method.Get;

            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Accept", "application/json");

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"Xero API error: {response.Content}");

            return response.Content;
        }

        public async Task<string> CreateInvoiceAsync(InvoiceCreateDto invoice)
        {
            var accessToken = await GetValidAccessTokenAsync();
            var client = new RestClient("https://api.xero.com/api.xro/2.0/Invoices");
            var request = new RestRequest();
            request.Method = Method.Post;

            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                Invoices = new[]
                {
                    new
                    {
                        Type = "ACCREC",
                        Contact = new { ContactID = invoice.CustomerXeroId },
                        Date = DateTime.UtcNow,
                        invoice.DueDate,
                        LineItems = new[]
                        {
                            new { invoice.Description, Quantity = 1, UnitAmount = invoice.TotalAmount }
                        },
                        invoice.InvoiceNumber
                    }
                }
            };

            request.AddJsonBody(body);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                //_logger.LogWarning("Contact not found in Xero or request failed: {xeroId}", xeroId);
                return null; // ✅ skip instead of throwing
            }
            return response.Content;
        }

        public async Task<string> UpdateInvoiceAsync(InvoiceCreateDto dto)
        {
            // ✅ Ensure we have the Xero InvoiceID
            if (string.IsNullOrWhiteSpace(dto.InvoiceXeroId))
                throw new ArgumentException("InvoiceXeroId is required to update an invoice.");

            var accessToken = await GetValidAccessTokenAsync();
            var client = new RestClient($"https://api.xero.com/api.xro/2.0/Invoices/{dto.InvoiceXeroId}");
            var request = new RestRequest();
            request.Method = Method.Post; // ✅ Xero expects POST for updates
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Content-Type", "application/json");

            // ✅ Properly structured JSON for Xero
            var invoicePayload = new
            {
                Invoices = new[]
                {
            new
            {
                InvoiceID = dto.InvoiceXeroId,
                Type = "ACCREC",
                Contact = new { ContactID = dto.CustomerXeroId },
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DueDate = dto.DueDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                LineAmountTypes = "Exclusive",
                LineItems = new[]
                {
                    new
                    {
                        dto.Description,
                        Quantity = 1,
                        UnitAmount = dto.TotalAmount,
                        AccountCode = "200",   // ✅ REQUIRED
                        TaxType = "NONE"        // ✅ REQUIRED
                    }
                },
                dto.InvoiceNumber
            }
        }
            };

            request.AddJsonBody(invoicePayload);

            var response = await client.ExecuteAsync(request);

            // ✅ Handle response logging and errors
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
            {
                //_logger.LogWarning("Contact not found in Xero or request failed: {xeroId}", xeroId);
                return null; // ✅ skip instead of throwing
            }
            return response.Content;
        }


        #endregion


        // ✅ FIXED AND CLEANED QUOTE SECTION
        #region Quotes
        public async Task<string> GetQuoteByXeroIdAsync(string quoteXeroId)
        {
            var accessToken = await GetValidAccessTokenAsync();
            var client = new RestClient($"https://api.xero.com/api.xro/2.0/Quotes/{quoteXeroId}");
            var request = new RestRequest();
            request.Method = Method.Get;

            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Accept", "application/json");

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"Xero API error: {response.Content}");

            return response.Content;
        }

        public async Task<string> GetQuotesAsync()
        {
            var accessToken = await GetValidAccessTokenAsync();
            var client = new RestClient("https://api.xero.com/api.xro/2.0/Quotes");
            var request = new RestRequest();
            request.Method = Method.Get;

            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Accept", "application/json");

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"Xero API error: {response.Content}");

            return response.Content;
        }

        public async Task<string> CreateQuoteAsync(QuoteCreateDto quote)
        {
            var accessToken = await GetValidAccessTokenAsync();
            var client = new RestClient("https://api.xero.com/api.xro/2.0/Quotes");
            var request = new RestRequest();
            request.Method = Method.Post;

            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                Quotes = new[]
     {
        new
        {
            Contact = new { ContactID = quote.CustomerXeroId },    // ✅ required by Xero
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),          // ✅ must be string, not DateTime
            ExpiryDate = quote.ExpiryDate?.ToString("yyyy-MM-dd")
                        ?? DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),  // ✅ always valid
            LineItems = new[]
            {
                new
                {
                    Description = quote.Description,
                    Quantity = 1,
                    UnitAmount = quote.TotalAmount
                }
            },
            Reference = quote.Description,
            Status = "DRAFT"
        }
    }
            };


            request.AddJsonBody(body);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"Xero API error: {response.Content}");

            return response.Content;
        }

        public async Task<string> UpdateQuoteAsync(QuoteCreateDto quote)
        {
            if (string.IsNullOrEmpty(quote.QuoteXeroId))
                throw new ArgumentException("XeroId is required to update a quote.");

            var accessToken = await GetValidAccessTokenAsync();
            var client = new RestClient($"https://api.xero.com/api.xro/2.0/Quotes/{quote.QuoteXeroId}");
            var request = new RestRequest();
            request.Method = Method.Post;

            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                Quotes = new[]
                {
                    new
                    {
                        QuoteID = quote.QuoteXeroId,
                        Contact = new { ContactID = quote.CustomerXeroId },  // ✅ REQUIRED
                        Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),        // ✅ REQUIRED
                        ExpiryDate = quote.ExpiryDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
                        LineItems = new[]
                        {
                            new { Description = quote.Description, Quantity = 1, UnitAmount = quote.TotalAmount }
                        },
                        Status = "SENT"
                    }
                }
            };

            request.AddJsonBody(body);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"Xero API error: {response.Content}");

            return response.Content;
        }

        #endregion

    }
}
