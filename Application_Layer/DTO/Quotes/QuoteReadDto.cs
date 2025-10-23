using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application_Layer.DTO.Quotes
{
    public class QuoteReadDto
    {
        // Used when Xero sends data back to you (via GET or webhook).

        [JsonProperty("QuoteID")]
        public string QuoteXeroId { get; set; } = string.Empty;

        [JsonProperty("QuoteNumber")]
        public string QuoteNumber { get; set; } = string.Empty;

        [JsonProperty("Contact")]
        public ContactDto Contact { get; set; } = new ContactDto();

        [JsonProperty("ExpiryDate")]
        public DateTime ExpiryDate { get; set; }

        [JsonProperty("Total")]
        public decimal TotalAmount { get; set; }

        [JsonProperty("LineItems")]
        public List<LineItemDto> LineItems { get; set; } = new List<LineItemDto>();

        public string Description
        {
            get { return LineItems.FirstOrDefault()?.Description ?? string.Empty; }
        }

        public bool SyncedToXero { get; set; }
    }

    public class ContactDto
    {
        [JsonProperty("ContactID")]
        public string ContactID { get; set; } = string.Empty;

        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;
    }

    public class LineItemDto
    {
        [JsonProperty("Description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("Quantity")]
        public decimal Quantity { get; set; }

        [JsonProperty("UnitAmount")]
        public decimal UnitAmount { get; set; }
    }
}
