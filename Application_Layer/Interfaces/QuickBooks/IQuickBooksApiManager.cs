using Domain_Layer.Models;

namespace Application_Layer.Interfaces.QuickBooks
{
    public interface IQuickBooksApiManager
    {
        // Customers
        Task<Customer> CreateOrUpdateCustomerAsync(Customer customer);
        Task<string> GetCustomerByIdAsync(string quickBooksCustomerId);


        // Invoices
        Task<string> GetInvoiceByIdAsync(string id);
        Task<Invoice> CreateOrUpdateInvoiceAsync(Invoice inv);
        // Quotes (Estimates in QBO)
        Task<string> GetEstimateByIdAsync(string id);
        Task<Quote> CreateOrUpdateQuoteAsync(Quote quote);
    }
}
