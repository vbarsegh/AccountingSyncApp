namespace Application.DTOs
{
    public class InvoiceCreateDto
    {
        public int CustomerId { get; set; } // Local DB reference
        public string? XeroId { get; set; } // Used only for Update
        public string ContactId { get; set; }          // Xero Contact ID
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime DueDate { get; set; }
    }
}
