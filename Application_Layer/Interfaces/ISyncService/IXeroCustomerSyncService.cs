using Application_Layer.DTO.Customers;
using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;

namespace Application_Layer.Interfaces
{
    public interface IXeroCustomerSyncService
    {
        Task<Customer> SyncCreatedCustomerAsync(ICustomerRepository customerRepository, CustomerCreateDto customerDto, IXeroApiManager xeroApiManager);
        Task<string> SyncUpdatedCustomerAsync(CustomerCreateDto dto);
        Task<Customer> CreateCustomerAndSyncAsync(Customer customer);
        Task<Customer> UpdateCustomerAndSyncAsync(Customer customer);
    }
}
