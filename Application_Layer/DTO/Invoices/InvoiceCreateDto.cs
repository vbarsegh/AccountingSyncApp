using System.ComponentModel;

namespace Application.DTOs
{
    public class InvoiceCreateDto
    {
        //Used when you create a new invoice in Xero.
        //Contains only fields required by Xero API or user input.
        public string InvoiceNumber { get; set; } = "";   // e.g. "INV-001"
        public string? InvoiceXeroId { get; set; }               // Xero InvoiceID
        public int CustomerId { get; set; }  // Local DB Customer FK
        public string CustomerXeroId { get; set; } = "";  // ContactID from Xero (Customer)
        public string Description { get; set; } = "";     // Line item description
        public decimal TotalAmount { get; set; }          // Total amount
        public DateTime DueDate { get; set; }             // Due date
        public bool SyncedToXero { get; set; } = false;
    }
}
