namespace Application.DTOs
{
    public class QuoteReadDto
    {
        public string QuoteNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? XeroId { get; set; }
    }
}
