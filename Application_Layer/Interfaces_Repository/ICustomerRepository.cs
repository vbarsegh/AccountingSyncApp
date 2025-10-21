using Application_Layer.DTO.Customers;
using Domain_Layer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application_Layer.Interfaces_Repository
{
    public interface ICustomerRepository
    {
        Task<Customer> GetByIdAsync(int id);
        Task UpdateSyncedToXeroAsync(int id);
        Task<IEnumerable<Customer>> GetAllAsync();
        Task InsertAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task<Customer> GetByXeroIdAsync(string xeroId);
        Task<Customer> GetByXeroIdAndSyncedToXeroAsync(CustomerReadDto dto);
        Task DeleteAsync(int id);
    }
}
