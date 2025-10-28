using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application_Layer.Interfaces
{
    public interface IAccountingSyncManager
    {
        //Customers
        Task SyncCustomersFromXeroAsync(string CustomerXeroId);
        //Invoices
        Task SyncInvoicesFromXeroAsync(string InvoiceXeroId);
        Task SyncQuotesFromXeroPeriodicallyAsync();
        //Task SyncQuotesFromXeroAsync(string quoteXeroId);
        Task<bool> CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(int CustomerId, string CustomerXeroId);

    }
}
