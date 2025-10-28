namespace Domain_Layer.Models
{
    public class QuickBooksTokenResponse
    {
        public int Id { get; set; }

        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }

        public string RealmId { get; set; }  // from QuickBooks API response
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
