namespace Domain_Layer.Models
{
    public class Quote
    {
        public int Id { get; set; }
        public string? XeroId { get; set; }
        public string QuoteNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public string? CustomerXeroId { get; set; }

        public string Description { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime ExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool SyncedToXero { get; set; }

        public Customer? Customer { get; set; }
    }
}
