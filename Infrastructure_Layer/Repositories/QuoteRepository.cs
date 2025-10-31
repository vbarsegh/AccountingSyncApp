using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Infrastructure_Layer.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure_Layer.Repositories
{
    public class QuoteRepository : IQuoteRepository
    {
        private readonly AccountingDbContext _context;
        public QuoteRepository(AccountingDbContext context) => _context = context;

        public async Task<Quote> GetByIdAsync(int id) =>
            await _context.Quotes.FindAsync(id) ?? throw new Exception("Quote not found");

        public async Task<Quote> GetByQuoteXeroIdAsync(string quoteXeroId) =>
            await _context.Quotes.FirstOrDefaultAsync(q => q.XeroId == quoteXeroId);

        public async Task<IEnumerable<Quote>> GetAllAsync() =>
            await _context.Quotes.ToListAsync();

        public async Task InsertAsync(Quote quote)
        {
            if (await _context.Quotes.AnyAsync(q => q.QuoteNumber == quote.QuoteNumber))
                throw new Exception($"Duplicate quote number: {quote.QuoteNumber}.");

            quote.CreatedAt = DateTime.UtcNow;
            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Quote quote)
        {
            bool exists = await _context.Quotes
                .AnyAsync(q => q.QuoteNumber == quote.QuoteNumber && q.Id != quote.Id);
            if (exists)
                throw new Exception($"Quote number '{quote.QuoteNumber}' already exists.");

            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var q = await GetByIdAsync(id);
            _context.Quotes.Remove(q);
            await _context.SaveChangesAsync();
        }


        //quickbooks
        public async Task<Quote> GetByQuoteQuickBooksIdAsync(string quickBooksId)
        {
            return await _context.Quotes
                .FirstOrDefaultAsync(q => q.QuickBooksId == quickBooksId);
        }

    }
}
