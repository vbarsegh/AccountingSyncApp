using Domain_Layer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application_Layer.Interfaces_Repository
{
    public interface IQuoteRepository
    {
        Task<IEnumerable<Quote>> GetAllAsync();
        Task InsertAsync(Quote quote);
        Task UpdateAsync(Quote quote);
        Task<Quote?> GetByXeroIdAsync(int id);
        Task<IEnumerable<Quote>> GetByCustomerIdAsync(int customerId);
    }
}
