using Application.DTOs;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces.QuickBooks;
using Application_Layer.Interfaces.Xero;
using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Newtonsoft.Json.Linq;

namespace Infrastructure_Layer.Services
{
    public class QuoteSyncServiceXeroAndQuickBooks : IXeroQuoteSyncService
    {
        private readonly IXeroApiManager _xero;
        private readonly IQuoteRepository _quotes;
        private readonly IQuickBooksApiManager _qb;

        public QuoteSyncServiceXeroAndQuickBooks (IXeroApiManager xero, IQuoteRepository quotes, IQuickBooksApiManager qb)
        {
            _xero = xero;
            _quotes = quotes;
            _qb = qb;
        }

        // ---------------- CREATE ----------------
        public async Task<Quote> SyncCreatedQuoteAsync(QuoteCreateDto dto)
        {
            // ✅ Check duplicate
            var existing = await _quotes.GetAllAsync();
            if (existing.Any(q => q.QuoteNumber == dto.QuoteNumber && q.CustomerXeroId == dto.CustomerXeroId))
                throw new Exception($"Quote {dto.QuoteNumber} already exists for this customer.");

            // ✅ 1: Save to local DB first
            var quote = new Quote
            {
                CustomerId = dto.CustomerId,
                CustomerXeroId = dto.CustomerXeroId,
                Description = dto.Description,
                TotalAmount = dto.TotalAmount,
                DueDate = dto.DueDate,
                QuoteNumber = dto.QuoteNumber ?? "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SyncedToXero = false,
                SyncedToQuickBooks = false
            };

            await _quotes.InsertAsync(quote);
            // ✅ If NO Xero customer → don't sync to Xero
            if (string.IsNullOrEmpty(dto.CustomerXeroId))
            {
                Console.WriteLine("⚠️ Customer has no XeroId — skipping Xero sync");
            }
            else
            {
                // ✅ Sync to Xero normally
                var xeroJson = await _xero.CreateQuoteAsync(dto);
                var root = JObject.Parse(xeroJson);
                var created = root["Quotes"]?.FirstOrDefault();
                quote.XeroId = created?["QuoteID"]?.ToString();
                quote.SyncedToXero = true;
                quote.UpdatedAt = DateTime.UtcNow;
                await _quotes.UpdateAsync(quote);
            }

            // ✅ 3: Push the same quote to QuickBooks
            // ✅ Always sync to QuickBooks (if QuickBooksId exists)
            if (!string.IsNullOrEmpty(dto.CustomerQuickBooksId))
            {
                try
                {
                    var qbQuote = new Quote
                    {
                        Id = quote.Id,
                        CustomerId = quote.CustomerId,
                        QuoteNumber = quote.QuoteNumber,
                        Description = quote.Description,
                        TotalAmount = quote.TotalAmount,
                        CustomerQuickBooksId = dto.CustomerQuickBooksId // ✅ <- from DTO
                    };

                    var qbResult = await _qb.CreateOrUpdateQuoteAsync(qbQuote);

                    quote.QuickBooksId = qbResult.QuickBooksId;
                    quote.SyncedToQuickBooks = true;
                    await _quotes.UpdateAsync(quote);
                }
                catch
                {
                    quote.SyncedToQuickBooks = false;
                    await _quotes.UpdateAsync(quote);
                    throw;
                }
            }

            return quote;
        }

        // ---------------- UPDATE ----------------
        public async Task<string> SyncUpdatedQuoteAsync(QuoteCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.QuoteXeroId))
                throw new ArgumentException("QuoteXeroId is required to update a quote.");

            // ✅ 1: Find local
            var local = await _quotes.GetByQuoteXeroIdAsync(dto.QuoteXeroId)
                ?? throw new Exception($"Quote {dto.QuoteXeroId} not found.");

            // ✅ Backup for rollback
            var backup = new Quote
            {
                Description = local.Description,
                QuoteNumber = local.QuoteNumber,
                TotalAmount = local.TotalAmount,
                DueDate = local.DueDate
            };

            // ✅ 2: Local update
            local.Description = dto.Description;
            local.QuoteNumber = dto.QuoteNumber;
            local.CustomerXeroId = dto.CustomerXeroId;
            local.TotalAmount = dto.TotalAmount;
            local.DueDate = dto.DueDate;
            local.SyncedToXero = false;
            local.SyncedToQuickBooks = false;
            local.UpdatedAt = DateTime.UtcNow;

            await _quotes.UpdateAsync(local);

            try
            {
                // ✅ 3: Send update to Xero
                var xeroJson = await _xero.UpdateQuoteAsync(dto);
                local.SyncedToXero = true;
                await _quotes.UpdateAsync(local);

                // ✅ 4: Update in QuickBooks
                var qbQuote = new Quote
                {
                    Id = local.Id,
                    QuickBooksId = local.QuickBooksId,
                    CustomerId = local.CustomerId,
                    CustomerQuickBooksId = dto.CustomerQuickBooksId,
                    Description = local.Description,
                    QuoteNumber = local.QuoteNumber,
                    TotalAmount = local.TotalAmount
                };

                var qbResult = await _qb.CreateOrUpdateQuoteAsync(qbQuote);

                local.QuickBooksId = qbResult.QuickBooksId;
                local.SyncedToQuickBooks = true;
                await _quotes.UpdateAsync(local);

                return xeroJson;
            }
            catch (Exception ex)
            {
                // ❌ Rollback
                local.Description = backup.Description;
                local.QuoteNumber = backup.QuoteNumber;
                local.TotalAmount = backup.TotalAmount;
                local.DueDate = backup.DueDate;
                local.SyncedToXero = false;
                local.SyncedToQuickBooks = false;
                await _quotes.UpdateAsync(local);

                throw new Exception("❌ Sync DB→Xero→QB failed. Rolled back.", ex);
            }
        }
    }
}
