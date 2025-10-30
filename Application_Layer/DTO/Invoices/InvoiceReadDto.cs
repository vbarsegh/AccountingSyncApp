using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application_Layer.DTO.Invoices
{
    public class InvoiceReadDto
    {
        //Used when Xero sends data back to you (via GET or webhook).
        [JsonProperty("InvoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [JsonProperty("InvoiceID")]
        public string? InvoiceXeroId { get; set; } = string.Empty;

        //
        public string? InvoiceQuickBooksId { get; set; }

        [JsonProperty("Contact")]
        public ContactDto Contact { get; set; } = new ContactDto();

        [JsonProperty("DueDate")]
        public DateTime? DueDate { get; set; }

        [JsonProperty("Total")]
        public decimal TotalAmount { get; set; } // ✅ matches your domain and create DTO

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


/*
 example what return Xero`(JSON format)
{
  "InvoiceID": "98e54f12-10ab-4a1d-95cf-7a94cd58e1c8",
  "InvoiceNumber": "INV-001",
  "Contact": {
    "ContactID": "c317a61d-5a9b-4bc7-9097-9efb02e01d4f",
    "Name": "Armine Ltd"
  },
  "DueDate": "2025-10-30T00:00:00",
  "Total": 1200.00,
  "LineItems": [
    {
      "Description": "Web development services",
      "Quantity": 1,
      "UnitAmount": 1200.00
    }
  ]
}

 */