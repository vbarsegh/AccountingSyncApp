namespace Application.DTOs
{
    public class QuoteCreateDto
    {
        // Used when you create or update a quote in Xero.
        // Contains only fields required by Xero API or user input.

        public string? QuoteXeroId { get; set; }               // Xero QuoteID
        public int CustomerId { get; set; }                    // Local DB Customer FK
        public string CustomerXeroId { get; set; } = "";       // ContactID from Xero (Customer)
        public string QuoteNumber { get; set; } = "";          // e.g. "QUO-001"
        public string Description { get; set; } = "";          // Line item description
        public decimal TotalAmount { get; set; }               // Total amount
        public DateTime ExpiryDate { get; set; }               // Expiry date
        public bool SyncedToXero { get; set; } = false;
    }
}
