using Domain_Layer.Models;
using Application_Layer.Interfaces_Repository;
using Infrastructure_Layer.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure_Layer.Repositories
{
    public class XeroTokenRepository : IXeroTokenRepository
    {
        private readonly AccountingDbContext _context;

        public XeroTokenRepository(AccountingDbContext context)
        {
            _context = context;
        }

        public async Task<XeroTokenResponse?> GetTokenAsync()
        {
            // Returns the most recently saved token
            return await _context.XeroTokenResponse
                .OrderByDescending(t => t.UpdatedAt)
                .FirstOrDefaultAsync();
        }
        public async Task SaveTokenAsync(XeroTokenResponse token)
        {
            var existing = await _context.XeroTokenResponse.FirstOrDefaultAsync();

            if (existing == null)
            {
                // first-time insert
                _context.XeroTokenResponse.Add(token);
            }
            else
            {
                // update only relevant fields
                existing.AccessToken = token.AccessToken;
                existing.RefreshToken = token.RefreshToken;
                existing.ExpiresIn = token.ExpiresIn;
                existing.TokenType = token.TokenType;
                existing.IdToken = token.IdToken;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

    }
}
