using System;

namespace Shop.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public decimal Price { get; set; } 
        public decimal Revenue => Amount * Price;
    }
}
