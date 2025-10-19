using Application.DTOs;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Infrastructure_Layer.Repositories;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var xeroResponse = await _xero.CreateQuoteAsync(dto);
            var xeroQuote = JsonConvert.DeserializeObject<QuoteReadDto>(xeroResponse);

            var quote = new Quote
            {
                CustomerId = dto.CustomerId,
                TotalAmount = xeroQuote.TotalAmount,
                XeroId = xeroQuote.XeroId,
                QuoteDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _quotes.InsertAsync(quote);
            return quote;
        }

        public async Task<string> SyncUpdatedQuoteAsync(QuoteCreateDto dto)
        {
            var xeroResponse = await _xero.UpdateQuoteAsync(dto);
            var updatedXeroQuote = JsonConvert.DeserializeObject<QuoteReadDto>(xeroResponse);

            var localQuote = await _quotes.GetByXeroIdAsync(int.Parse(updatedXeroQuote.XeroId));
            if (localQuote == null) throw new Exception("Quote not found in local database.");

            localQuote.TotalAmount = updatedXeroQuote.TotalAmount;
            localQuote.UpdatedAt = DateTime.UtcNow;

            await _quotes.UpdateAsync(localQuote);
            return xeroResponse;
        }


        // Create quote in DB, then Xero
        public async Task<Quote> CreateQuoteAndSyncAsync(Quote quote)
        {
            await _quotes.InsertAsync(quote);

            var dto = new QuoteCreateDto
            {
                CustomerName = quote.CustomerName,
                Date = quote.CreatedAt,
                //LineItems = quote.LineItems,
                XeroId = quote.XeroId
            };

            var xeroResponse = await _xero.CreateQuoteAsync(dto);
            var createdXeroQuote = JsonConvert.DeserializeObject<QuoteReadDto>(xeroResponse);
            quote.XeroId = createdXeroQuote.XeroId;

            await _quotes.UpdateAsync(quote);
            return quote;
        }

        // Update quote in DB, then Xero
        public async Task<Quote> UpdateQuoteAndSyncAsync(Quote quote)
        {
            await _quotes.UpdateAsync(quote);

            var dto = new QuoteCreateDto
            {
                CustomerName = quote.CustomerName,
                Date = quote.UpdatedAt,
                //LineItems = quote.LineItems,
                XeroId = quote.XeroId
            };

            await _xero.UpdateQuoteAsync(dto);
            return quote;
        }
    }

}
