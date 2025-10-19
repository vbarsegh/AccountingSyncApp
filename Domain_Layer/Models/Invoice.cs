using System;

namespace Domain_Layer.Models
{
    public class Invoice
    {
        public int Id { get; set; } // Primary key
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }

        // Optional: Xero/QuickBooks IDs for 2-way sync
        public string? XeroId { get; set; }
        public string? QuickBooksId { get; set; }

        // Foreign key to Customer
        public int CustomerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        //For a 2-way sync, you often need to know when a record was last updated to avoid overwriting newer changes.
        //UpdatedAt helps determine whether to push updates to Xero/QuickBooks or pull updates from them.
    }
}

