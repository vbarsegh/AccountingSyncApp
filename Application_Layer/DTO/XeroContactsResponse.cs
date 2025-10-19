using System.Collections.Generic;

namespace Application_Layer.DTO.Customers
{
    public class XeroContactsResponse
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public List<CustomerReadDto> Contacts { get; set; }
    }
}
