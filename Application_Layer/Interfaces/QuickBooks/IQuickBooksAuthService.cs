//namespace Application_Layer.Interfaces.QuickBooks
//{
//    public interface IQuickBooksAuthService
//    {
//        Task<string> GetAccessTokenAsync();
//        Task EnsureFreshTokenAsync();
//    }
//}

using System.Threading.Tasks;

namespace Application_Layer.Interfaces
{
    public interface IQuickBooksAuthService
    {
        /// Returns a valid access token (refreshing if needed).
        Task<string> GetAccessTokenAsync();

        /// Called from OAuth callback to exchange "code" for tokens and store them.
        Task HandleAuthCallbackAsync(string code, string realmId);

        /// Force refresh if token is near/after expiry.
        Task EnsureFreshTokenAsync();
    }
}
