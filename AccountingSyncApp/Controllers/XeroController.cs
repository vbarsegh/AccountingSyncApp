using Application.DTOs;
using Application_Layer.DTO.Customers;
using Application_Layer.Interfaces;
using Application_Layer.Services;
using Azure;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
namespace AccountingSyncApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class XeroController : ControllerBase
    {
        private readonly IXeroApiManager _xero;

        public XeroController(IXeroApiManager xero)
        {
            _xero = xero;
        }
        //kjnjenq verjum
        [HttpGet("connections")]
        public async Task<IActionResult> GetConnections()
        {
            /////KAREVOR//////
            //es action methody mi angam enq kanchelu enqan vor reposnei body-ic vercnenq tenantId-n,vortev yuraqanchyur Xero Api call-i 
            //hamar pti ogtagorcenq tenantId-n` request.AddHeader("xero-tenant-id", _config["XeroSettings:TenantId"]);
            //bayc depqer karar linen
            var result = await _xero.GetConnectionsAsync();
            return Ok(result);
        }
        // GET api/xero/customers
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            var json = await _xero.GetCustomersAsync();

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

            var response = await _xero.CreateCustomerAsync(customerDto);
            // Deserialize the created customer from Xero response
            var createdCustomer = JsonConvert.DeserializeObject<CustomerReadDto>(response);

            return Ok(createdCustomer);
        }

        [HttpPut("update-customer")]
        public async Task<IActionResult> UpdateCustomer([FromBody] CustomerCreateDto customerDto)
        {
            if (customerDto == null)
                return BadRequest("Customer data is required.");

            var response = await _xero.UpdateCustomerAsync(customerDto);
            var updatedCustomer = JsonConvert.DeserializeObject<CustomerReadDto>(response);
            return Ok(updatedCustomer);
        }
        // GET api/xero/invoices
        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices()
        {
            var response = await _xero.GetInvoicesAsync();
            var invoices = JsonConvert.DeserializeObject<List<InvoiceReadDto>>(response);
            return Ok(invoices);
        }
        [HttpPost("create-invoice")]
        public async Task<IActionResult> CreateInvoice([FromBody] InvoiceCreateDto dto)
        {
            var response = await _xero.CreateInvoiceAsync(dto);
            var createdInvoice = JsonConvert.DeserializeObject<InvoiceReadDto>(response);
            return Ok(createdInvoice);
        }
        [HttpPut("update-invoice")]
        public async Task<IActionResult> UpdateInvoice([FromBody] InvoiceCreateDto dto)
        {
            var response = await _xero.UpdateInvoiceAsync(dto);
            var updatedInvoice = JsonConvert.DeserializeObject<InvoiceReadDto>(response);
            return Ok(updatedInvoice);
        }
        [HttpGet("quotes")]
        public async Task<IActionResult> GetQuotes()
        {
            var response = await _xero.GetQuotesAsync();
            var quotes = JsonConvert.DeserializeObject<List<QuoteReadDto>>(response);
            return Ok(quotes);
        }

        [HttpPost("create-quote")]
        public async Task<IActionResult> CreateQuote([FromBody] QuoteCreateDto dto)
        {
            var response = await _xero.CreateQuoteAsync(dto);
            var createdQuote = JsonConvert.DeserializeObject<QuoteReadDto>(response);
            return Ok(createdQuote);
        }

        [HttpPut("update-quote")]
        public async Task<IActionResult> UpdateQuote([FromBody] QuoteCreateDto dto)
        {
            var response = await _xero.UpdateQuoteAsync(dto);
            var updatedQuote = JsonConvert.DeserializeObject<QuoteReadDto>(response);
            return Ok(updatedQuote);
        }

    }

}
