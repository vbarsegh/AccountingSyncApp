using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Infrastructure_Layer.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure_Layer.Repositories
{
    public class QuoteRepository : IQuoteRepository
    {
        private readonly AccountingDbContext _context;

        public QuoteRepository(AccountingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Quote>> GetAllAsync()
        {
            return await _context.Quotes.ToListAsync();
        }

        public async Task InsertAsync(Quote quote)
        {
            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Quote quote)
        {
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();
        }

        public async Task<Quote?> GetByXeroIdAsync(int id)
        {
            return await _context.Quotes.FindAsync(id);
        }

        public async Task<IEnumerable<Quote>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Quotes
                .Where(q => q.CustomerId == customerId)
                .ToListAsync();
        }
    }
}
