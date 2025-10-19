using Application_Layer.DTO.Customers;
using Domain_Layer.Models;

namespace Application_Layer.Interfaces
{
    public interface IXeroCustomerSyncService
    {
        Task<Customer> SyncCreatedCustomerAsync(CustomerCreateDto dto);
        Task<string> SyncUpdatedCustomerAsync(CustomerCreateDto dto);
        Task<Customer> CreateCustomerAndSyncAsync(Customer customer);
        Task<Customer> UpdateCustomerAndSyncAsync(Customer customer);
    }
}
