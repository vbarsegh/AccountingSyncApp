namespace Domain_Layer.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        public string? XeroId { get; set; } // InvoiceID from Xero
        public string? QuickBooksId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;

        // Relation to Customer
        public int CustomerId { get; set; } // FK to local DB
        public string? CustomerXeroId { get; set; } // ContactID from Xero
        public string? CustomerQuickBooksId { get; set; } // FK mapping to QuickBooks customer


        public string Description { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; } // total amount of invoice
        public DateTime DueDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool SyncedToXero { get; set; }
        public bool SyncedToQuickBooks { get; set; }

        // Navigation property
        public Customer? Customer { get; set; }//It allows you to easily access the full customer object from an invoice without manually joining tables.
    }
}
