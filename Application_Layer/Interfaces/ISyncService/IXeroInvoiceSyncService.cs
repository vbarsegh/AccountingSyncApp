using Application.DTOs;
using Domain_Layer.Models;

namespace Application_Layer.Interfaces
{
    public interface IXeroInvoiceSyncService
    {
        Task<Invoice> SyncCreatedInvoiceAsync(InvoiceCreateDto dto);
        Task<string> SyncUpdatedInvoiceAsync(InvoiceCreateDto dto);
        Task<Invoice> CreateInvoiceAndSyncAsync(Invoice invoice);
        Task<Invoice> UpdateInvoiceAndSyncAsync(Invoice invoice);
    }
}
