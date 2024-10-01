﻿namespace Application.Dtos
{
    public class ProductDto
    {
        public string Band { get; set; }
        public string CategoryCode { get; set; }
        public string Manufacturer { get; set; }
        public string PartSKU { get; set; }
        public string ItemDescription { get; set; }
        public decimal ListPrice { get; set; }
        public decimal MinDiscount { get; set; }
        public decimal DiscountPrice { get; set; }
    }
}

