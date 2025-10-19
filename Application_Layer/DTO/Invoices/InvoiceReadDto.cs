namespace Application.DTOs
{
    public class InvoiceReadDto
    {
        public string InvoiceID { get; set; }
        public string? XeroId { get; set; }
        public string InvoiceNumber { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
        public DateTime DueDate { get; set; }
        public string ContactName { get; set; }
    }
}
