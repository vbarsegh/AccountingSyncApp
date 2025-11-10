using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application_Layer.DTO.Customers
{
    public class CustomerUpdateDto
    {
        public int Id { get; set; }
        //public string? CompanyName { get; set; }//for quickBooks
        //public decimal? OpenBalance { get; set; }//for quickBooks
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }

    }
}
