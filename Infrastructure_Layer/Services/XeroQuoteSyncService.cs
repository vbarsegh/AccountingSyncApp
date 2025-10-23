using Application.DTOs;                // QuoteCreateDto
using Application_Layer.Interfaces;    // IXeroApiManager, IXeroQuoteSyncService
using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Newtonsoft.Json.Linq;

namespace Infrastructure_Layer.Services
{
    public class XeroQuoteSyncService : IXeroQuoteSyncService
    {
        private readonly IXeroApiManager _xero;
        private readonly IQuoteRepository _quotes;

        public XeroQuoteSyncService(IXeroApiManager xero, IQuoteRepository quotes)
        {
            _xero = xero;
            _quotes = quotes;
        }

        /// <summary>
        /// DB -> Xero (create). Create local quote first (SyncedToXero=false),
        /// call Xero, read QuoteID, update local (set XeroId + SyncedToXero=true).
        /// Returns the updated local Quote.
        /// </summary>
        public async Task<Quote> SyncCreatedQuoteAsync(QuoteCreateDto dto)
        {
            // 1) Save locally first (pending sync)
            var quote = new Quote
            {
                CustomerId     = dto.CustomerId,
                CustomerXeroId = dto.CustomerXeroId,
                Description    = dto.Description,
                TotalAmount    = dto.TotalAmount,
                ExpiryDate     = dto.ExpiryDate,
                QuoteNumber    = dto.QuoteNumber ?? string.Empty,
                CreatedAt      = DateTime.UtcNow,
                UpdatedAt      = DateTime.UtcNow,
                SyncedToXero   = false
            };

            await _quotes.InsertAsync(quote);

            // 2) Create in Xero
            var xeroJson = await _xero.CreateQuoteAsync(dto);

            // 3) Parse Xero response to get Xero QuoteID and canonical fields
            var root = JObject.Parse(xeroJson);
            var created = root["Quotes"]?.FirstOrDefault();
            var xeroId = created?["QuoteID"]?.ToString();

            if (!string.IsNullOrWhiteSpace(xeroId))
                quote.XeroId = xeroId;

            quote.QuoteNumber = created?["QuoteNumber"]?.ToString() ?? quote.QuoteNumber;

            if (decimal.TryParse(created?["Total"]?.ToString(), out var totalFromXero))
                quote.TotalAmount = totalFromXero;

            if (DateTime.TryParse(created?["ExpiryDate"]?.ToString(), out var exp))
                quote.ExpiryDate = exp;

            // 4) Mark synced
            quote.SyncedToXero = true;
            quote.UpdatedAt = DateTime.UtcNow;
            await _quotes.UpdateAsync(quote);

            return quote;
        }

        /// <summary>
        /// DB/Xero -> update. Sends update to Xero, then refreshes local record
        /// from the Xero response and keeps SyncedToXero=true.
        /// Returns raw Xero JSON if you want to bubble it up.
        /// </summary>
        public async Task<string> SyncUpdatedQuoteAsync(QuoteCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.QuoteXeroId))
                throw new ArgumentException("XeroId is required to update a quote.");

            // 1) Send update to Xero
            var xeroJson = await _xero.UpdateQuoteAsync(dto);

            // 2) Parse response
            var root = JObject.Parse(xeroJson);
            var updated = root["Quotes"]?.FirstOrDefault();

            var xeroId = updated?["QuoteID"]?.ToString() ?? dto.QuoteXeroId;

            // 3) Find local by XeroId
            var local = await _quotes.GetByXeroIdAsync(xeroId);
            if (local == null)
                throw new Exception("Local quote not found by XeroId.");

            // 4) Update local from DTO or canonical Xero response
            local.Description = updated?["LineItems"]?.FirstOrDefault()?["Description"]?.ToString() ?? dto.Description;

            local.QuoteNumber = updated?["QuoteNumber"]?.ToString() ?? local.QuoteNumber;

            if (decimal.TryParse(updated?["Total"]?.ToString(), out var total))
                local.TotalAmount = total;
            else
                local.TotalAmount = dto.TotalAmount;

            if (DateTime.TryParse(updated?["ExpiryDate"]?.ToString(), out var exp))
                local.ExpiryDate = exp;
            else
                local.ExpiryDate = dto.ExpiryDate;

            local.SyncedToXero = true;
            local.UpdatedAt = DateTime.UtcNow;

            await _quotes.UpdateAsync(local);
            return xeroJson;
        }
    }
}
