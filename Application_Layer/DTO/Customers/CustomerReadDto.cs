using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application_Layer.DTO.Customers
{
    public class CustomerReadDto
    {
        public string Id { get; set; }

        [JsonProperty("ContactID")]
        public string XeroId { get; set; } = string.Empty;

        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("EmailAddress")]
        public string Email { get; set; } = string.Empty;

        [JsonIgnore]
        public string Phone { get; set; } = string.Empty;

        [JsonIgnore]
        public string Address { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
