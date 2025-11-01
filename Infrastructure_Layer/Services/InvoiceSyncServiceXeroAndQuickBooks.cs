using Application.DTOs;                // InvoiceCreateDto
using Application_Layer.Interfaces;    // IXeroApiManager, IXeroInvoiceSyncService
using Application_Layer.Interfaces.QuickBooks;
using Application_Layer.Interfaces.Xero;
using Application_Layer.Interfaces_Repository;
using Application_Layer.Services;
using Domain_Layer.Models;
using Newtonsoft.Json.Linq;

namespace Infrastructure_Layer.Services
{
    public class InvoiceSyncServiceXeroAndQuickBooks : IXeroInvoiceSyncService
    {
        private readonly IXeroApiManager _xero;
        private readonly IQuickBooksApiManager _qb;
        private readonly IInvoiceRepository _invoices;

        public InvoiceSyncServiceXeroAndQuickBooks(IXeroApiManager xero, IInvoiceRepository invoices, IQuickBooksApiManager qb)
        {
            _xero = xero;
            _invoices = invoices;
            _qb = qb;
        }

        /// <summary>
        /// DB -> Xero (create). Create local invoice first (SyncedToXero=false),
        /// call Xero, read InvoiceID, update local (set XeroId + SyncedToXero=true).
        /// Returns the updated local Invoice.
        /// </summary>
        public async Task<Invoice> SyncCreatedInvoiceAsync(InvoiceCreateDto dto)
        {
            // ✅ Prevent duplicates
            var existing = await _invoices.GetAllAsync();
            var duplicate = existing.FirstOrDefault(i =>
                i.InvoiceNumber == dto.InvoiceNumber &&
                i.CustomerXeroId == dto.CustomerXeroId);

            if (duplicate != null)  
                throw new Exception($"Invoice {dto.InvoiceNumber} already exists locally.");

            // ✅ 1. Create locally (pending sync)
            var invoice = new Invoice
            {
                CustomerId = dto.CustomerId,
                CustomerXeroId = dto.CustomerXeroId,
                Description = dto.Description,
                TotalAmount = dto.TotalAmount,
                DueDate = dto.DueDate ?? DateTime.UtcNow.AddDays(30),
                InvoiceNumber = dto.InvoiceNumber ?? "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SyncedToXero = false,
                SyncedToQuickBooks = false
            };

            await _invoices.InsertAsync(invoice);

            // ✅ 2. Push to Xero
            var xeroJson = await _xero.CreateInvoiceAsync(dto);
            var root = JObject.Parse(xeroJson);
            var created = root["Invoices"]?.FirstOrDefault();

            invoice.XeroId = created?["InvoiceID"]?.ToString();
            invoice.InvoiceNumber = created?["InvoiceNumber"]?.ToString() ?? invoice.InvoiceNumber;
            if (decimal.TryParse(created?["Total"]?.ToString(), out var t)) invoice.TotalAmount = t;

            invoice.SyncedToXero = true;
            await _invoices.UpdateAsync(invoice);

            // ✅ 3. Push to QuickBooks
            try
            {
                var qbInvoiceModel = new Invoice
                {
                    Id = invoice.Id,
                    CustomerId = invoice.CustomerId,
                    Description = invoice.Description,
                    TotalAmount = invoice.TotalAmount,
                    InvoiceNumber = invoice.InvoiceNumber,
                    CustomerQuickBooksId = dto.CustomerQuickBooksId
                };

                var qbResult = await _qb.CreateOrUpdateInvoiceAsync(qbInvoiceModel);

                invoice.QuickBooksId = qbResult.QuickBooksId;
                invoice.SyncedToQuickBooks = true;
                await _invoices.UpdateAsync(invoice);
            }
            catch (Exception)
            {
                invoice.SyncedToQuickBooks = false;
                await _invoices.UpdateAsync(invoice);
                throw;
            }

            return invoice;
        }


        public async Task<string> SyncUpdatedInvoiceAsync(InvoiceCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.InvoiceXeroId))
                throw new ArgumentException("InvoiceXeroId required");
            //if (dto.CustomerXeroId != null)
                var local = await _invoices.GetByInvoiceXeroIdAsync(dto.InvoiceXeroId);
            //else if (dto.CustomerQuickBooksId != null)
            //    var local = 
            if (local == null)
                throw new Exception($"Invoice {dto.InvoiceXeroId} not found");

            var backup = new Invoice
            {
                Description = local.Description,
                InvoiceNumber = local.InvoiceNumber,
                TotalAmount = local.TotalAmount,
                DueDate = local.DueDate
            };

            // ✅ Local update
            local.Description = dto.Description;
            local.InvoiceNumber = dto.InvoiceNumber;
            local.TotalAmount = dto.TotalAmount;
            local.DueDate = dto.DueDate ?? DateTime.UtcNow.AddDays(30);
            local.SyncedToXero = false;
            local.SyncedToQuickBooks = false;
            await _invoices.UpdateAsync(local);

            try
            {
                // ✅ Update in Xero
                var xeroJson = await _xero.UpdateInvoiceAsync(dto);
                local.SyncedToXero = true;
                await _invoices.UpdateAsync(local);

                // ✅ Update in QuickBooks
                var qbInvoiceModel = new Invoice
                {
                    Id = local.Id,
                    QuickBooksId = local.QuickBooksId,
                    CustomerId = local.CustomerId,
                    CustomerQuickBooksId = dto.CustomerQuickBooksId,
                    Description = local.Description,
                    InvoiceNumber = local.InvoiceNumber,
                    TotalAmount = local.TotalAmount
                };

                var qb = await _qb.CreateOrUpdateInvoiceAsync(qbInvoiceModel);

                local.QuickBooksId = qb.QuickBooksId;
                local.SyncedToQuickBooks = true;
                await _invoices.UpdateAsync(local);

                return xeroJson;
            }
            catch (Exception ex)
            {
                // ❌ rollback on failure
                local.Description = backup.Description;
                local.InvoiceNumber = backup.InvoiceNumber;
                local.TotalAmount = backup.TotalAmount;
                local.DueDate = backup.DueDate;
                local.SyncedToXero = false;
                await _invoices.UpdateAsync(local);

                throw new Exception("❌ Failed Sync DB→Xero→QB, rollback applied", ex);
            }
        }



    }
}
