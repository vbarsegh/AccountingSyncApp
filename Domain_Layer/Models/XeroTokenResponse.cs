using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain_Layer.Models
{
    [Table("XeroTokenResponse")]//
    public class XeroTokenResponse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("id_token")]
        public string IdToken { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

//[JsonPropertyName("access_token")]
//public string AccessToken { get; set; } //Why it’s needed
//Xero sends JSON like this:
//{
//    "access_token": "eyJ...",
//  "refresh_token": "eyJ...",
//  "expires_in": 3600,
//  "token_type": "Bearer",
//  "id_token": "eyJ..."
//}
//Notice the keys are snake_case (access_token)
//Your C# properties are PascalCase (AccessToken)
//System.Text.Json is case-sensitive by default and looks for JSON keys that exactly match property names.
//What[JsonPropertyName] does
//[JsonPropertyName("access_token")]
//public string AccessToken { get; set; }
//Maps the JSON key "access_token" → C# property AccessToken
//Now deserialization correctly populates all fields
//This is why without [JsonPropertyName], your AccessToken, RefreshToken, and ExpiresIn were all null/0.