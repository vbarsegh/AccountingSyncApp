using Domain_Layer.Models;

namespace Application_Layer.Interfaces.QuickBooks
{
    public interface IQuickBooksApiManager
    {
        // Customers
        Task<Customer> CreateOrUpdateCustomerAsync(Customer customer);

        // Invoices
        Task<Invoice> CreateOrUpdateInvoiceAsync(Invoice invoice);

        // Quotes (Estimates in QBO)
        Task<Quote> CreateOrUpdateQuoteAsync(Quote quote);
    }
}
