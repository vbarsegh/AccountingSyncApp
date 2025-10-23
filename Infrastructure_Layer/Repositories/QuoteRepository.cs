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

        public async Task<Quote> GetByIdAsync(int id) =>
            await _context.Quotes.FindAsync(id) ?? throw new Exception("Quote not found");
        public async Task<Quote> GetByXeroIdAsync(string xeroId)
        {
            return await _context.Quotes.FirstOrDefaultAsync(q => q.XeroId == xeroId);
        }

        public async Task<IEnumerable<Quote>> GetAllAsync() =>
            await _context.Quotes.ToListAsync();

        public async Task InsertAsync(Quote quote)
        {
            quote.CreatedAt = DateTime.UtcNow;
            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Quote quote)
        {
            quote.UpdatedAt = DateTime.UtcNow;
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var quote = await GetByIdAsync(id);
            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();
        }
    }
}
