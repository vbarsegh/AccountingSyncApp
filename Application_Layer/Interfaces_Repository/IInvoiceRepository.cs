using Domain_Layer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application_Layer.Interfaces_Repository
{
    public interface IInvoiceRepository
    {
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task InsertAsync(Invoice invoice);
        Task UpdateAsync(Invoice invoice);
        Task<Invoice?> GetByXeroIdAsync(int id);
        Task<IEnumerable<Invoice>> GetByCustomerIdAsync(int customerId);

    }
}
