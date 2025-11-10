using Application.DTOs;
using Application_Layer.DTO.Customers;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces_Repository;
using Application_Layer.Services;
using Domain_Layer.Models;
using Infrastructure_Layer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AccountingSyncApp.Controllers.Local
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocalDbController : ControllerBase
    {
        //private readonly ICustomerRepository _customerRepository;
        private readonly IXeroCustomerSyncService _xeroCustomerSync;
        private readonly IXeroInvoiceSyncService _xeroInvoiceSync;
        private readonly IXeroQuoteSyncService _xeroQuoteSync;

        private readonly IAccountingSyncManager _accountingSyncManager;

        //private readonly IXeroApiManager _xeroApiManager;
        private readonly ILogger<LocalDbController> _logger;

        public LocalDbController(
            IXeroCustomerSyncService xeroCustomerSync,
            IXeroInvoiceSyncService xeroInvoiceSync,
            IXeroQuoteSyncService xeroQuoteSync,
            IAccountingSyncManager accountingSyncManager,
            ILogger<LocalDbController> logger)
        {
            _xeroCustomerSync = xeroCustomerSync;
            _xeroInvoiceSync = xeroInvoiceSync;
            _xeroQuoteSync = xeroQuoteSync;
            _accountingSyncManager = accountingSyncManager;
            _logger = logger;
        }

        // CUSTOMER ENDPOINTS
        // POST: api/customers/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto customerDto)
        {
            try
            {
                if (customerDto == null)
                    return BadRequest("Customer data is required.");
                _logger.LogInformation("📥 Creating new customer locally: {Name}", customerDto.Name);
                await _xeroCustomerSync.SyncCreatedCustomerAsync(customerDto);
                return Ok(new
                {
                    message = "Customer created successfully in local DB."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while creating local customer.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // ✅ PUT: api/localdb/update
        [HttpPut("update")]
        public async Task<IActionResult> UpdateCustomer([FromBody] CustomerUpdateDto customerDto)
        {
            try
            {
                if (customerDto == null)
                    return BadRequest("Customer data is required.");

                if (customerDto.Id <= 0)
                    return BadRequest("Id is required to update a customer.");//chem karcum ,pti esi poxvi XeroId-ic sovorakan Id-ii!!!

                _logger.LogInformation("✏️ Updating customer in Xero and local DB: {Name}", customerDto.Name);
                var result = await _xeroCustomerSync.SyncUpdatedCustomerAsync(customerDto);

                return Ok(new
                {
                    message = "Customer updated successfully in Xero and local DB.",
                    xeroResponse = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while updating customer.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //// ✅ GET: api/customers
        //[HttpGet]
        //public async Task<IActionResult> GetAllCustomers()
        //{
        //    var customers = await  _customerRepository.GetAllAsync();
        //    return Ok(customers);
        //}

        //INVOICE ENDPOINTS
        [HttpPost("invoice/create")]
        public async Task<IActionResult> CreateInvoice([FromBody] InvoiceCreateDto invoiceDto)
        {
            try
            {
                if (invoiceDto == null)
                    return BadRequest("Invoice data is required.");

                _logger.LogInformation("🧾 Creating new invoice locally for CustomerXeroId={CustomerXeroId}", invoiceDto.CustomerXeroId);

                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(invoiceDto.CustomerId, invoiceDto.CustomerXeroId);
                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerQuickBooksIDAppropriatingInLocalDbValues(invoiceDto.CustomerId, invoiceDto.CustomerQuickBooksId);
                var createdInvoice = await _xeroInvoiceSync.SyncCreatedInvoiceAsync(invoiceDto);

                return Ok(new
                {
                    message = "Invoice created successfully in DB and Xero.",
                    localInvoiceId = createdInvoice.Id,
                    xeroId = createdInvoice.XeroId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while creating invoice.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("invoice/update")]
        public async Task<IActionResult> UpdateInvoice([FromBody] InvoiceCreateDto invoiceDto)
        {
            try
            {
                if (invoiceDto == null)
                    return BadRequest("Invoice data is required.");

                if (string.IsNullOrWhiteSpace(invoiceDto.InvoiceXeroId))
                    return BadRequest("InvoiceXeroId is required to update an invoice.");

                _logger.LogInformation("✏️ Updating invoice in Xero and local DB: {InvoiceNumber}", invoiceDto.InvoiceNumber);

                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(invoiceDto.CustomerId, invoiceDto.CustomerXeroId);
                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerQuickBooksIDAppropriatingInLocalDbValues(invoiceDto.CustomerId, invoiceDto.CustomerQuickBooksId);
                var result = await _xeroInvoiceSync.SyncUpdatedInvoiceAsync(invoiceDto);

                return Ok(new
                {
                    message = "Invoice updated successfully in Xero and local DB.",
                    xeroResponse = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while updating invoice.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // ✅ QUOTES ENDPOINTS
        [HttpPost("quote/create")]
        public async Task<IActionResult> CreateQuote([FromBody] QuoteCreateDto quoteDto)
        {
            try
            {
                if (quoteDto == null)
                    return BadRequest("Quote data is required.");

                _logger.LogInformation("🧾 Creating new quote locally for CustomerXeroId={CustomerXeroId}", quoteDto.CustomerXeroId);

                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(
                    quoteDto.CustomerId, quoteDto.CustomerXeroId);

                var createdQuote = await _xeroQuoteSync.SyncCreatedQuoteAsync(quoteDto);

                return Ok(new
                {
                    message = "Quote created successfully in DB and Xero.",
                    localQuoteId = createdQuote.Id,
                    xeroId = createdQuote.XeroId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while creating quote.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("quote/update")]
        public async Task<IActionResult> UpdateQuote([FromBody] QuoteCreateDto quoteDto)
        {
            try
            {
                if (quoteDto == null)
                    return BadRequest("Quote data is required.");

                if (string.IsNullOrWhiteSpace(quoteDto.QuoteXeroId))
                    return BadRequest("QuoteXeroId is required to update a quote.");

                _logger.LogInformation("✏️ Updating quote in Xero and local DB: {QuoteNumber}", quoteDto.QuoteNumber);

                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(
                    quoteDto.CustomerId, quoteDto.CustomerXeroId);

                var result = await _xeroQuoteSync.SyncUpdatedQuoteAsync(quoteDto);

                return Ok(new
                {
                    message = "Quote updated successfully in Xero and local DB.",
                    xeroResponse = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while updating quote.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}
