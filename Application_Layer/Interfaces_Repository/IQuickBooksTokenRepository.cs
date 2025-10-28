using Domain_Layer.Models;

namespace Application_Layer.Interfaces_Repository
{
    public interface IQuickBooksTokenRepository
    {
        Task<QuickBooksTokenResponse> GetLatestAsync();
        Task AddOrUpdateAsync(QuickBooksTokenResponse token);
    }
}
