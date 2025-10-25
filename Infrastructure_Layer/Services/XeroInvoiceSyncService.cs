using Application.DTOs;                // InvoiceCreateDto
using Application_Layer.Interfaces;    // IXeroApiManager, IXeroInvoiceSyncService
using Application_Layer.Interfaces_Repository;
using Application_Layer.Services;
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
            // ✅ 0) Check for existing invoice (prevent duplicates)
            var existing = await _invoices.GetAllAsync();
            var duplicate = existing.FirstOrDefault(i =>
                i.InvoiceNumber == dto.InvoiceNumber &&
                i.CustomerXeroId == dto.CustomerXeroId);

            if (duplicate != null)
            {
                Console.WriteLine($"⚠️ Invoice with number {dto.InvoiceNumber} for customer {dto.CustomerXeroId} already exists locally.");
                throw new Exception($"Invoice {dto.InvoiceNumber} already exists for this customer — Xero will treat it as an update, not a new one.");
            }
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
            Console.WriteLine("datarka che? ->   |" + dto.CustomerXeroId);
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
            invoice.InvoiceNumber = created?["InvoiceNumber"]?.ToString() ?? invoice.InvoiceNumber;//esi navsyaki ete menq swaggeri mej chenq tve value Xero kgeneracni mer poxaren

            if (decimal.TryParse(created?["Total"]?.ToString(), out var totalFromXero))
                invoice.TotalAmount = totalFromXero;

            // 4) Mark synced
            invoice.SyncedToXero = true;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _invoices.UpdateAsync(invoice);

            return invoice;
        }

        public async Task<string> SyncUpdatedInvoiceAsync(InvoiceCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.InvoiceXeroId))
                throw new ArgumentException("InvoiceXeroId is required to update an invoice.");

            // 1️⃣ Find local invoice
            var localInvoice = await _invoices.GetByInvoiceXeroIdAsync(dto.InvoiceXeroId);
            if (localInvoice == null)
                throw new Exception($"Invoice with XeroId {dto.InvoiceXeroId} not found in local DB.");

            // 2️⃣ Keep old values for potential rollback
            var oldValues = new Invoice
            {
                CustomerId = localInvoice.CustomerId,
                CustomerXeroId = localInvoice.CustomerXeroId,
                Description = localInvoice.Description,
                InvoiceNumber = localInvoice.InvoiceNumber,
                TotalAmount = localInvoice.TotalAmount,
                DueDate = localInvoice.DueDate
            };

            // 3️⃣ Update local first
            localInvoice.CustomerId = dto.CustomerId;
            localInvoice.CustomerXeroId = dto.CustomerXeroId;
            localInvoice.Description = dto.Description;
            localInvoice.InvoiceNumber = dto.InvoiceNumber;
            localInvoice.TotalAmount = dto.TotalAmount;
            localInvoice.DueDate = dto.DueDate;
            localInvoice.UpdatedAt = DateTime.UtcNow;
            localInvoice.SyncedToXero = false;

            await _invoices.UpdateAsync(localInvoice);

            // 4️⃣ Try to update in Xero
            try
            {
                var xeroJson = await _xero.UpdateInvoiceAsync(dto);
                var root = JObject.Parse(xeroJson);
                var updated = root["Invoices"]?.FirstOrDefault();

                if (updated == null)
                    throw new Exception("No invoice returned from Xero.");

                // If Xero confirmed → mark synced
                localInvoice.SyncedToXero = true;
                localInvoice.UpdatedAt = DateTime.UtcNow;
                await _invoices.UpdateAsync(localInvoice);

                return xeroJson;
            }
            catch (Exception ex)
            {
                // ❌ Xero failed → rollback local DB to old values
                localInvoice.CustomerId = oldValues.CustomerId;
                localInvoice.CustomerXeroId = oldValues.CustomerXeroId;
                localInvoice.Description = oldValues.Description;
                localInvoice.InvoiceNumber = oldValues.InvoiceNumber;
                localInvoice.TotalAmount = oldValues.TotalAmount;
                localInvoice.DueDate = oldValues.DueDate;
                localInvoice.UpdatedAt = DateTime.UtcNow;
                localInvoice.SyncedToXero = false;

                await _invoices.UpdateAsync(localInvoice);

                throw new Exception("Failed to update invoice in Xero. Local changes rolled back.", ex);
            }
        }


    }
}
