namespace Application.DTOs
{
    public class QuoteCreateDto
    {
        public int CustomerId { get; set; } // For local DB
        public string CustomerName { get; set; } = string.Empty; // For Xero
        public DateTime Date { get; set; }
        public List<LineItemDto> LineItems { get; set; } = new List<LineItemDto>();
        public string? XeroId { get; set; } // For updates
    }
    public class LineItemDto
    {
        public string Description { get; set; } = string.Empty;
        public decimal UnitAmount { get; set; }
        public int Quantity { get; set; }
    }
}
