using Application.DTOs;
using Application_Layer.DTO.Customers;
using Application_Layer.DTO.Invoices;
using Application_Layer.DTO.Quotes;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces.QuickBooks;
using Domain_Layer.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AccountingSyncApp.Controllers.QuickBooks
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuickBooksController : ControllerBase
    {
        private readonly IQuickBooksApiManager _quickBooksApiManager;
        private readonly IAccountingSyncManager _accountingSyncManager;

        public QuickBooksController(
            IQuickBooksApiManager quickBooksApiManager,
            IAccountingSyncManager accountingSyncManager)
        {
            _quickBooksApiManager = quickBooksApiManager;
            _accountingSyncManager = accountingSyncManager;
        }

        // ---------------- CUSTOMERS ----------------

        [HttpPost("create-customer")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto customerDto)
        {
            try
            {
                if (customerDto == null)
                    return BadRequest("Customer data is required.");

                var customer = new Customer
                {
                    Name = customerDto.Name,
                    Email = customerDto.Email,
                    Phone = customerDto.Phone,
                    Address = customerDto.Address,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _quickBooksApiManager.CreateOrUpdateCustomerAsync(customer);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error while creating QuickBooks customer: {ex.Message}");
            }
        }

        [HttpPut("update-customer")]
        public async Task<IActionResult> UpdateCustomer([FromBody] CustomerCreateDto customerDto)
        {
            try
            {
                if (customerDto == null)
                    return BadRequest("Customer data is required.");

                var customer = new Customer
                {
                    QuickBooksId = customerDto.CustomerQuickBooksId,
                    Name = customerDto.Name,
                    Email = customerDto.Email,
                    Phone = customerDto.Phone,
                    Address = customerDto.Address,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _quickBooksApiManager.CreateOrUpdateCustomerAsync(customer);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error while updating QuickBooks customer: {ex.Message}");
            }
        }

        // ---------------- INVOICES ----------------

        [HttpPost("create-invoice")]
        public async Task<IActionResult> CreateInvoice([FromBody] InvoiceCreateDto invoiceDto)
        {
            try
            {
                if (invoiceDto == null)
                    return BadRequest("Invoice data is required.");

                // ✅ Ensure customer exists locally by QuickBooksId
                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerQuickBooksIDAppropriatingInLocalDbValues(invoiceDto.CustomerId, invoiceDto.CustomerQuickBooksId);
                var invoice = new Invoice
                {
                    CustomerId = invoiceDto.CustomerId,
                    CustomerQuickBooksId = invoiceDto.CustomerQuickBooksId,
                    InvoiceNumber = invoiceDto.InvoiceNumber,
                    Description = invoiceDto.Description,
                    TotalAmount = invoiceDto.TotalAmount,
                    DueDate = invoiceDto.DueDate ?? DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                //quickbooks-i u Xero-i tarberutyuny  ayev ena vor Xero-n stanum er createdto isk quickbooksy stanuma henc domainn=-i hstak modely(invoice, cusotmer)
                var result = await _quickBooksApiManager.CreateOrUpdateInvoiceAsync(invoice);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error while creating invoice in QuickBooks: {ex.Message}");
            }
        }

        //}

        [HttpPut("update-invoice")]
        public async Task<IActionResult> UpdateInvoice([FromBody] InvoiceCreateDto invoiceDto)
        {
            try
            {
                if (invoiceDto == null)
                    return BadRequest("Invoice data is required.");

                await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerQuickBooksIDAppropriatingInLocalDbValues(invoiceDto.CustomerId, invoiceDto.CustomerQuickBooksId);

                var invoice = new Invoice
                {
                    QuickBooksId = invoiceDto.InvoiceQuickBooksId,
                    CustomerId = invoiceDto.CustomerId,
                    CustomerQuickBooksId = invoiceDto.CustomerQuickBooksId,
                    InvoiceNumber = invoiceDto.InvoiceNumber,
                    Description = invoiceDto.Description,
                    TotalAmount = invoiceDto.TotalAmount,
                    DueDate = invoiceDto.DueDate ?? DateTime.UtcNow.AddDays(30),
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _quickBooksApiManager.CreateOrUpdateInvoiceAsync(invoice);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error while updating invoice in QuickBooks: {ex.Message}");
            }
        }


        //// ---------------- QUOTES ----------------

        // ---------------- QUOTES ----------------

        [HttpPost("create-quote")]
        public async Task<IActionResult> CreateQuote([FromBody] QuoteCreateDto quoteDto)
        {
            try
            {
                if (quoteDto == null)
                    return BadRequest("Quote data is required.");

                // ✅ Ensure customer matches DB record
                await _accountingSyncManager
                    .CheckInvoice_QuotesDtoCustomerIdAndCustomerQuickBooksIDAppropriatingInLocalDbValues(
                        quoteDto.CustomerId, quoteDto.CustomerQuickBooksId
                    );

                var quote = new Quote
                {
                    CustomerId = quoteDto.CustomerId,
                    CustomerQuickBooksId = quoteDto.CustomerQuickBooksId,
                    QuoteNumber = quoteDto.QuoteNumber,
                    Description = quoteDto.Description,
                    TotalAmount = quoteDto.TotalAmount,
                    ExpiryDate = quoteDto.ExpiryDate ?? DateTime.UtcNow.AddDays(30),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _quickBooksApiManager.CreateOrUpdateQuoteAsync(quote);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error while creating quote in QuickBooks: {ex.Message}");
            }
        }


        [HttpPut("update-quote")]
        public async Task<IActionResult> UpdateQuote([FromBody] QuoteCreateDto quoteDto)
        {
            try
            {
                if (quoteDto == null)
                    return BadRequest("Quote data is required.");

                await _accountingSyncManager
                    .CheckInvoice_QuotesDtoCustomerIdAndCustomerQuickBooksIDAppropriatingInLocalDbValues(
                        quoteDto.CustomerId, quoteDto.CustomerQuickBooksId
                    );

                var quote = new Quote
                {
                    QuickBooksId = quoteDto.QuoteQuickBooksId,
                    CustomerId = quoteDto.CustomerId,
                    CustomerQuickBooksId = quoteDto.CustomerQuickBooksId,
                    QuoteNumber = quoteDto.QuoteNumber,
                    Description = quoteDto.Description,
                    TotalAmount = quoteDto.TotalAmount,
                    ExpiryDate = quoteDto.ExpiryDate ?? DateTime.UtcNow.AddDays(30),
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _quickBooksApiManager.CreateOrUpdateQuoteAsync(quote);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Error while updating quote in QuickBooks: {ex.Message}");
            }
        }


    }
}
