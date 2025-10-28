using Application.DTOs;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces.Xero;
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

        public async Task<Quote> SyncCreatedQuoteAsync(QuoteCreateDto dto)
        {
            var existing = await _quotes.GetAllAsync();
            if (existing.Any(q => q.QuoteNumber == dto.QuoteNumber && q.CustomerXeroId == dto.CustomerXeroId))
                throw new Exception($"Quote {dto.QuoteNumber} already exists for this customer.");

            var quote = new Quote
            {
                CustomerId = dto.CustomerId,
                CustomerXeroId = dto.CustomerXeroId,
                Description = dto.Description,
                TotalAmount = dto.TotalAmount,
                DueDate = dto.DueDate,
                QuoteNumber = dto.QuoteNumber ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SyncedToXero = false
            };
            await _quotes.InsertAsync(quote);

            var xeroJson = await _xero.CreateQuoteAsync(dto);
            var root = JObject.Parse(xeroJson);
            var created = root["Quotes"]?.FirstOrDefault();
            var xeroId = created?["QuoteID"]?.ToString();

            if (!string.IsNullOrWhiteSpace(xeroId))
                quote.XeroId = xeroId;

            quote.SyncedToXero = true;
            quote.UpdatedAt = DateTime.UtcNow;
            await _quotes.UpdateAsync(quote);

            return quote;
        }

        public async Task<string> SyncUpdatedQuoteAsync(QuoteCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.QuoteXeroId))
                throw new ArgumentException("QuoteXeroId is required to update a quote.");

            var local = await _quotes.GetByQuoteXeroIdAsync(dto.QuoteXeroId)
                ?? throw new Exception($"Quote with XeroId {dto.QuoteXeroId} not found.");

            local.CustomerId = dto.CustomerId;
            local.CustomerXeroId = dto.CustomerXeroId;
            local.Description = dto.Description;
            local.QuoteNumber = dto.QuoteNumber;
            local.TotalAmount = dto.TotalAmount;
            local.DueDate = dto.DueDate;
            local.SyncedToXero = false;

            await _quotes.UpdateAsync(local);

            var xeroJson = await _xero.UpdateQuoteAsync(dto);
            local.SyncedToXero = true;
            local.UpdatedAt = DateTime.UtcNow;
            await _quotes.UpdateAsync(local);

            return xeroJson;
        }
    }
}
