using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application_Layer.DTO.Customers
{
    public class CustomerCreateDto
    {
        public string? XeroId { get; set; }  // the invoice ID in Xero
        public string? CustomerQuickBooksId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public bool SyncedToXero { get; set; } = false;
    }
}
