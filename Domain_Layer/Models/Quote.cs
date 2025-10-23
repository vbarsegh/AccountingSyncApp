namespace Domain_Layer.Models
{
    public class Quote
    {
        public int Id { get; set; }

        public string? XeroId { get; set; } // QuoteID from Xero
        public string QuoteNumber { get; set; } = string.Empty;

        // Relation to Customer
        public int CustomerId { get; set; } // FK to local DB
        public string? CustomerXeroId { get; set; } // ContactID from Xero

        public string Description { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; } // total amount of quote
        public DateTime ExpiryDate { get; set; } // date until quote is valid

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool SyncedToXero { get; set; }

        // Navigation property
        public Customer? Customer { get; set; }
        // Allows easy access to the related customer object.
    }
}
