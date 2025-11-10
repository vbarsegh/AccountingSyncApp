using Application.DTOs;
using Application_Layer.DTO.Customers;
using RestSharp;
using System.Threading.Tasks;

namespace Application_Layer.Interfaces.Xero
{
    public interface IXeroApiManager
    {
        //knjens verjum
        Task<string> GetValidAccessTokenAsync();
        Task<string> GetConnectionsAsync();
        // Customers
        Task<string> GetCustomerByXeroIdAsync(string xeroId);
        Task<string> GetCustomerByEmailAsync(string email);
        Task<string> GetLatestCustomerAsync();
        Task<string> GetCustomersAsync();
        Task<string> CreateCustomerAsync(CustomerCreateDto customer);
        Task<string> UpdateCustomerAsync(CustomerUpdateDto customer);

        // Invoices
        Task<string> GetInvoiceByXeroIdAsync(string invoiceXeroId);
        Task<string> GetInvoicesAsync();
        Task<string> CreateInvoiceAsync(InvoiceCreateDto invoice);
        Task<string> UpdateInvoiceAsync(InvoiceCreateDto invoice);


        // Quotes
        Task<string> GetQuoteByXeroIdAsync(string quoteXeroId);

        Task<string> GetQuotesAsync();
        Task<string> CreateQuoteAsync(QuoteCreateDto quote);
        Task<string> UpdateQuoteAsync(QuoteCreateDto quote);
    }
}
