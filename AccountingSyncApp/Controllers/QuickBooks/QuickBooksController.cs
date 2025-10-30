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

        //[HttpPost("create-invoice")]
        //public async Task<IActionResult> CreateInvoice([FromBody] InvoiceCreateDto invoiceDto)
        //{
        //    try
        //    {
        //        await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(invoiceDto.CustomerId, invoiceDto.CustomerXeroId);

        //        var invoice = new Invoice
        //        {
        //            InvoiceNumber = invoiceDto.InvoiceNumber,
        //            Description = invoiceDto.Description,
        //            TotalAmount = invoiceDto.TotalAmount,
        //            DueDate = invoiceDto.DueDate ?? DateTime.UtcNow.AddDays(30),
        //            CustomerId = invoiceDto.CustomerId,
        //            CreatedAt = DateTime.UtcNow,
        //            UpdatedAt = DateTime.UtcNow
        //        };

        //        var result = await _quickBooksApiManager.CreateOrUpdateInvoiceAsync(invoice);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"❌ Error while creating QuickBooks invoice: {ex.Message}");
        //    }
        //}

        //[HttpPut("update-invoice")]
        //public async Task<IActionResult> UpdateInvoice([FromBody] InvoiceCreateDto invoiceDto)
        //{
        //    try
        //    {
        //        await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(invoiceDto.CustomerId, invoiceDto.CustomerXeroId);

        //        var invoice = new Invoice
        //        {
        //            QuickBooksId = invoiceDto.InvoiceQuickBooksId,
        //            InvoiceNumber = invoiceDto.InvoiceNumber,
        //            Description = invoiceDto.Description,
        //            TotalAmount = invoiceDto.TotalAmount,
        //            DueDate = invoiceDto.DueDate ?? DateTime.UtcNow.AddDays(30),
        //            CustomerId = invoiceDto.CustomerId,
        //            UpdatedAt = DateTime.UtcNow
        //        };

        //        var result = await _quickBooksApiManager.CreateOrUpdateInvoiceAsync(invoice);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"❌ Error while updating QuickBooks invoice: {ex.Message}");
        //    }
        //}

        //// ---------------- QUOTES ----------------

        //[HttpPost("create-quote")]
        //public async Task<IActionResult> CreateQuote([FromBody] QuoteCreateDto quoteDto)
        //{
        //    try
        //    {
        //        await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(quoteDto.CustomerId, quoteDto.CustomerXeroId);

        //        var quote = new Quote
        //        {
        //            QuoteNumber = quoteDto.QuoteNumber,
        //            Description = quoteDto.Description,
        //            TotalAmount = quoteDto.TotalAmount,
        //            ExpiryDate = quoteDto.ExpiryDate ?? DateTime.UtcNow.AddDays(30),
        //            CustomerId = quoteDto.CustomerId,
        //            CreatedAt = DateTime.UtcNow,
        //            UpdatedAt = DateTime.UtcNow
        //        };

        //        var result = await _quickBooksApiManager.CreateOrUpdateQuoteAsync(quote);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"❌ Error while creating QuickBooks quote: {ex.Message}");
        //    }
        //}

        //[HttpPut("update-quote")]
        //public async Task<IActionResult> UpdateQuote([FromBody] QuoteCreateDto quoteDto)
        //{
        //    try
        //    {
        //        await _accountingSyncManager.CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(quoteDto.CustomerId, quoteDto.CustomerXeroId);

        //        var quote = new Quote
        //        {
        //            QuickBooksId = quoteDto.QuoteQuickBooksId,
        //            QuoteNumber = quoteDto.QuoteNumber,
        //            Description = quoteDto.Description,
        //            TotalAmount = quoteDto.TotalAmount,
        //            ExpiryDate = quoteDto.ExpiryDate ?? DateTime.UtcNow.AddDays(30),
        //            CustomerId = quoteDto.CustomerId,
        //            UpdatedAt = DateTime.UtcNow
        //        };

        //        var result = await _quickBooksApiManager.CreateOrUpdateQuoteAsync(quote);
        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"❌ Error while updating QuickBooks quote: {ex.Message}");
        //    }
        //}
    }
}
