using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Band { get; set; }
        public string CategoryCode { get; set; }
        public string Manufacturer { get; set; }
        public string PartSKU { get; set; } // Unique
        public string ItemDescription { get; set; }
        public decimal ListPrice { get; set; }
        public decimal MinDiscount { get; set; }
        public decimal DiscountPrice { get; set; }
    }
}
