using Application.DTOs;                // InvoiceCreateDto
using Application_Layer.Interfaces;    // IXeroApiManager, IXeroInvoiceSyncService
using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Newtonsoft.Json.Linq;

namespace Infrastructure_Layer.Services
{
    public class XeroInvoiceSyncService : IXeroInvoiceSyncService
    {
        private readonly IXeroApiManager _xero;
        private readonly IInvoiceRepository _invoices;

        public XeroInvoiceSyncService(IXeroApiManager xero, IInvoiceRepository invoices)
        {
            _xero = xero;
            _invoices = invoices;
        }

        /// <summary>
        /// DB -> Xero (create). Create local invoice first (SyncedToXero=false),
        /// call Xero, read InvoiceID, update local (set XeroId + SyncedToXero=true).
        /// Returns the updated local Invoice.
        /// </summary>
        public async Task<Invoice> SyncCreatedInvoiceAsync(InvoiceCreateDto dto)
        {
            // 1) Save locally first (pending sync)
            var invoice = new Invoice
            {
                CustomerId     = dto.CustomerId,
                CustomerXeroId = dto.CustomerXeroId, // ContactID for Xero calls (optional to store)
                Description    = dto.Description,
                TotalAmount    = dto.TotalAmount,
                DueDate        = dto.DueDate,
                InvoiceNumber  = dto.InvoiceNumber ?? string.Empty,
                CreatedAt      = DateTime.UtcNow,
                UpdatedAt      = DateTime.UtcNow,
                SyncedToXero   = false
            };

            await _invoices.InsertAsync(invoice);

            // 2) Create in Xero
            var xeroJson = await _xero.CreateInvoiceAsync(dto);

            // 3) Parse Xero response to get Xero InvoiceID and any canonical values
            var root = JObject.Parse(xeroJson);
            var created = root["Invoices"]?.FirstOrDefault();
            var xeroId = created?["InvoiceID"]?.ToString();

            if (!string.IsNullOrWhiteSpace(xeroId))
                invoice.XeroId = xeroId;

            // (Optional) normalize number/amount from Xero if present
            invoice.InvoiceNumber = created?["InvoiceNumber"]?.ToString() ?? invoice.InvoiceNumber;

            if (decimal.TryParse(created?["Total"]?.ToString(), out var totalFromXero))
                invoice.TotalAmount = totalFromXero;

            // 4) Mark synced
            invoice.SyncedToXero = true;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _invoices.UpdateAsync(invoice);

            return invoice;
        }

        /// <summary>
        /// DB/Xero -> update. Sends update to Xero, then refreshes local record
        /// from the Xero response and keeps SyncedToXero=true.
        /// Returns raw Xero JSON if you want to bubble it up.
        /// </summary>
        public async Task<string> SyncUpdatedInvoiceAsync(InvoiceCreateDto dto)
        {
            // Require XeroId to update
            if (string.IsNullOrWhiteSpace(dto.InvoiceXeroId))
                throw new ArgumentException("XeroId is required to update an invoice.");

            // 1) Send update to Xero
            var xeroJson = await _xero.UpdateInvoiceAsync(dto);

            // 2) Parse response
            var root = JObject.Parse(xeroJson);
            var updated = root["Invoices"]?.FirstOrDefault();

            var xeroId = updated?["InvoiceID"]?.ToString() ?? dto.InvoiceXeroId;

            // 3) Find local by XeroId
            var local = await _invoices.GetByInvoiceXeroIdAsync(xeroId);
            if (local == null)
                throw new Exception("Local invoice not found by XeroId.");

            // 4) Update local from either DTO or canonical Xero response
            local.Description   = updated?["LineItems"]?.FirstOrDefault()?["Description"]?.ToString() ?? dto.Description;
            local.InvoiceNumber = updated?["InvoiceNumber"]?.ToString() ?? local.InvoiceNumber;

            if (decimal.TryParse(updated?["Total"]?.ToString(), out var total))
                local.TotalAmount = total;
            else
                local.TotalAmount = dto.TotalAmount;

            if (DateTime.TryParse(updated?["DueDate"]?.ToString(), out var due))
                local.DueDate = due;
            else
                local.DueDate = dto.DueDate;

            local.SyncedToXero = true;
            local.UpdatedAt = DateTime.UtcNow;

            await _invoices.UpdateAsync(local);
            return xeroJson;
        }
    }
}
