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
        Task<IEnumerable<Customer>> GetAllAsync();
        Task InsertAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task<Customer> GetByXeroIdAsync(string xeroId);
    }
}
