using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Infrastructure_Layer.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure_Layer.Repositories
{
    public class QuickBooksTokenRepository : IQuickBooksTokenRepository
    {
        private readonly AccountingDbContext _context;

        public QuickBooksTokenRepository(AccountingDbContext context)
        {
            _context = context;
        }

        public async Task<QuickBooksTokenResponse> GetLatestAsync()
        {
            return await _context.QuickBooksTokenResponse
                .OrderByDescending(t => t.UpdatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task AddOrUpdateAsync(QuickBooksTokenResponse token)
        {
            var existing = await GetLatestAsync();

            if (existing != null)
            {
                existing.AccessToken = token.AccessToken;
                existing.RefreshToken = token.RefreshToken;
                existing.AccessTokenExpiresAt = token.AccessTokenExpiresAt;
                existing.RefreshTokenExpiresAt = token.RefreshTokenExpiresAt;
                existing.RealmId = token.RealmId;
                existing.UpdatedAt = DateTime.UtcNow;

                _context.QuickBooksTokenResponse.Update(existing);
            }
            else
            {
                await _context.QuickBooksTokenResponse.AddAsync(token);
            }

            await _context.SaveChangesAsync();
        }
    }
}
