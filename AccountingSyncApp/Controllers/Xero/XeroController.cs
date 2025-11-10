using Application.DTOs;
using Application_Layer.DTO.Customers;
using Application_Layer.DTO.Invoices;
using Application_Layer.DTO.Quotes;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces.Xero;
using Application_Layer.Services;
using Azure;
using Domain_Layer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Reflection;
namespace AccountingSyncApp.Controllers.Xero
{
    [ApiController]
    [Route("api/[controller]")]
    public class XeroController : ControllerBase
    {
        private readonly IXeroApiManager _xeroApiManager;
        private readonly IAccountingSyncManager _accountingSyncManager;

        public XeroController(IXeroApiManager xeroApiManager, IAccountingSyncManager accountingSyncManager)
        {
            _xeroApiManager = xeroApiManager;
            _accountingSyncManager = accountingSyncManager;
        }
        [HttpGet("connections")]
        public async Task<IActionResult> GetConnections()
        {
            /////KAREVOR//////
            //es action methody mi angam enq kanchelu enqan vor reposnei body-ic vercnenq tenantId-n,vortev yuraqanchyur Xero Api call-i 
            //hamar pti ogtagorcenq tenantId-n` request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            //bayc depqer karar linen
            var result = await _xeroApiManager.GetConnectionsAsync();
            return Ok(result);
        }
        // GET api/xero/customers
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var json = await _xeroApiManager.GetCustomersAsync();

            // Deserialize Xero's response (which wraps contacts inside an object)
            var xeroResponse = JsonConvert.DeserializeObject<XeroContactsResponse>(json);

            if (xeroResponse?.Contacts == null)
                return BadRequest("No Contacts found in Xero response.");

            return Ok(xeroResponse.Contacts);
        }

        [HttpPost("create-customer")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto customerDto)
        {
            if (customerDto == null)
                return BadRequest("Customer data is required.");
            Console.WriteLine("hasa\n");
            var response = await _xeroApiManager.CreateCustomerAsync(customerDto);
            // Deserialize the created customer from Xero response
            var createdCustomer = JsonConvert.DeserializeObject<CustomerReadDto>(response);

            return Ok(createdCustomer);
        }

        [HttpPut("update-customer")]
        public async Task<IActionResult> UpdateCustomer([FromBody] CustomerUpdateDto customerDto)
        {
            if (customerDto == null)
                return BadRequest("Customer data is required.");

            var response = await _xeroApiManager.UpdateCustomerAsync(customerDto);
            var updatedCustomer = JsonConvert.DeserializeObject<CustomerReadDto>(response);
            return Ok(updatedCustomer);
        }
        // GET api/xero/invoices
        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices()
        {
            try
            {
                var response = await _xeroApiManager.GetInvoicesAsync();

                var root = JsonConvert.DeserializeObject<JObject>(response);//JObject as a dynamic JSON object in .NET:
                //It behaves like a dictionary — keys are strings, values are JTokens.
                //It represents one JSON object(like { "Invoices": [ ... ], "Status": "OK" }).
                //You can access properties just like dictionary keys.

                // ✅ Ensure the JSON contains an "Invoices" array
                var invoicesToken = root["Invoices"];//The type of invoicesToken is JToken (the base class for all JSON nodes in Newtonsoft).
                //If "Invoices" contains an array → invoicesToken.Type == JTokenType.Array
                //If "Invoices" is a single object → invoicesToken.Type == JTokenType.Object
                //If "Invoices" is missing → invoicesToken == null
                if (invoicesToken == null)
                    return Ok(new { message = "No Invoices found or response not in expected format.", json = response });

                var invoices = invoicesToken.ToObject<List<InvoiceReadDto>>();
                return Ok(invoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPost("create-invoice")]
        public async Task<IActionResult> CreateInvoice([FromBody] InvoiceCreateDto invoice)
        {
            try
            {
                Console.WriteLine("customerid = " + invoice.CustomerId);
                Console.WriteLine("CusotmerXeroId = " + invoice.CustomerXeroId);
                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(invoice.CustomerId, invoice.CustomerXeroId);//sranov stugum enq vor customerId-in hamapatasxani chisht customerXeroID-n,te che exception
                var response = await _xeroApiManager.CreateInvoiceAsync(invoice);
                var createdInvoice = JsonConvert.DeserializeObject<InvoiceReadDto>(response);
                return Ok(createdInvoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
            //var customer = await _customers.GetByIdAsync(invoice.CustomerId);//In Clean Architecture, the controller (Presentation layer) must only talk to the Application layer, never directly to repositories or infrastructure services.dra hamar senc anely kopit xaxtuma.
           
        }

        [HttpPut("update-invoice")]
        public async Task<IActionResult> UpdateInvoice([FromBody] InvoiceCreateDto invoice)
        {
            try
            {
                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(invoice.CustomerId, invoice.CustomerXeroId);
                var response = await _xeroApiManager.UpdateInvoiceAsync(invoice);
                var updatedInvoice = JsonConvert.DeserializeObject<InvoiceReadDto>(response);
                return Ok(updatedInvoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("quotes")]
        public async Task<IActionResult> GetQuotes()
        {
            try
            {
                var response = await _xeroApiManager.GetQuotesAsync();
                var root = JsonConvert.DeserializeObject<JObject>(response);
                var quotes = root["Quotes"]?.ToObject<List<QuoteReadDto>>() ?? new List<QuoteReadDto>();
                return Ok(quotes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPost("create-quote")]
        public async Task<IActionResult> CreateQuote([FromBody] QuoteCreateDto quote)
        {
            try
            {
                Console.WriteLine("customerid = " + quote.CustomerId);
                Console.WriteLine("CusotmerXeroId = " + quote.CustomerXeroId);

                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(quote.CustomerId, quote.CustomerXeroId);

                var response = await _xeroApiManager.CreateQuoteAsync(quote);
                var createdQuote = JsonConvert.DeserializeObject<QuoteReadDto>(response);
                return Ok(createdQuote);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpPut("update-quote")]
        public async Task<IActionResult> UpdateQuote([FromBody] QuoteCreateDto quote)
        {
            try
            {
                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(quote.CustomerId, quote.CustomerXeroId);

                var response = await _xeroApiManager.UpdateQuoteAsync(quote);
                var updatedQuote = JsonConvert.DeserializeObject<QuoteReadDto>(response);
                return Ok(updatedQuote);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }


    }

}
