using Application.DTOs;
using Domain_Layer.Models;

namespace Application_Layer.Interfaces
{
    public interface IXeroQuoteSyncService
    {
        Task<Quote> SyncCreatedQuoteAsync(QuoteCreateDto dto);
        Task<string> SyncUpdatedQuoteAsync(QuoteCreateDto dto);
        Task<Quote> CreateQuoteAndSyncAsync(Quote quote);
        Task<Quote> UpdateQuoteAndSyncAsync(Quote quote);
    }
}
