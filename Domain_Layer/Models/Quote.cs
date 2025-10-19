using System;

namespace Domain_Layer.Models
{
    public class Quote
    {
        public int Id { get; set; } // Primary key
        public string QuoteNumber { get; set; } = string.Empty;
        public DateTime QuoteDate { get; set; }
        public decimal TotalAmount { get; set; }

        // Optional: Xero/QuickBooks IDs for 2-way sync
        public string? XeroId { get; set; }
        public string? QuickBooksId { get; set; }
        public string CustomerName { get; set; }

        // Foreign key to Customer
        public int CustomerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
