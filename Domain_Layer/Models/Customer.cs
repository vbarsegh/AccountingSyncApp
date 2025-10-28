namespace Domain_Layer.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string XeroId { get; set; }
        public string QuickBooksId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool SyncedToXero { get; set; } = false;
        public bool SyncedToQuickBooks { get; set; } = false;

    }
}
