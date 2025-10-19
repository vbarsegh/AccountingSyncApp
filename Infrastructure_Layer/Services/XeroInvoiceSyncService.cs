using Application.DTOs;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Infrastructure_Layer.Repositories;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<Invoice> SyncCreatedInvoiceAsync(InvoiceCreateDto dto)
        {
            var xeroResponse = await _xero.CreateInvoiceAsync(dto);
            var xeroInvoice = JsonConvert.DeserializeObject<InvoiceReadDto>(xeroResponse);

            var invoice = new Invoice
            {
                CustomerId = dto.CustomerId,
                TotalAmount = xeroInvoice.Total,
                XeroId = xeroInvoice.InvoiceID,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _invoices.InsertAsync(invoice);
            return invoice;
        }

        public async Task<string> SyncUpdatedInvoiceAsync(InvoiceCreateDto dto)
        {
            var xeroResponse = await _xero.UpdateInvoiceAsync(dto);
            var updatedXeroInvoice = JsonConvert.DeserializeObject<InvoiceReadDto>(xeroResponse);

            var localInvoice = await _invoices.GetByXeroIdAsync(int.Parse(updatedXeroInvoice.InvoiceID));
            if (localInvoice == null) throw new Exception("Invoice not found in local database.");

            localInvoice.TotalAmount = updatedXeroInvoice.Total;
            localInvoice.UpdatedAt = DateTime.UtcNow;

            await _invoices.UpdateAsync(localInvoice);
            return xeroResponse;
        }



        // Create invoice in DB, then Xero
        public async Task<Invoice> CreateInvoiceAndSyncAsync(Invoice invoice)
        {
            // 1️⃣ Save in local DB
            await _invoices.InsertAsync(invoice);

            // 2️⃣ Prepare DTO for Xero
            var dto = new InvoiceCreateDto
            {
                CustomerId = invoice.CustomerId,
                DueDate = invoice.CreatedAt,
                //LineItems = invoice.LineItems, // assuming compatible
                XeroId = invoice.XeroId
            };

            // 3️⃣ Create in Xero
            var xeroResponse = await _xero.CreateInvoiceAsync(dto);

            // 4️⃣ Optional: update local invoice with XeroId returned
            var createdXeroInvoice = JsonConvert.DeserializeObject<InvoiceReadDto>(xeroResponse);
            invoice.XeroId = createdXeroInvoice.XeroId;
            await _invoices.UpdateAsync(invoice);

            return invoice;
        }

        // Update invoice in DB, then Xero
        public async Task<Invoice> UpdateInvoiceAndSyncAsync(Invoice invoice)
        {
            // 1️⃣ Update local DB
            await _invoices.UpdateAsync(invoice);

            // 2️⃣ Prepare DTO
            var dto = new InvoiceCreateDto
            {
                CustomerId = invoice.CustomerId,
                DueDate = invoice.UpdatedAt,
                //LineItems = invoice.LineItems,
                XeroId = invoice.XeroId
            };

            // 3️⃣ Update in Xero
            await _xero.UpdateInvoiceAsync(dto);

            return invoice;
        }
    }

}
