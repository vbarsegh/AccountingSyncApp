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
        Task<Invoice> GetByIdAsync(int id);

        Task<Invoice> GetByXeroIdAsync(string xeroId);
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task InsertAsync(Invoice invoice);
        Task UpdateAsync(Invoice invoice);
        Task DeleteAsync(int id);

    }
}
