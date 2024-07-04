namespace Shop.Models
{
    /// <summary>
    /// Represents a sale transaction.
    /// </summary>
    public class Sale
    {
        /// <summary>
        /// Gets or sets the sale ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the product name.
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Gets or sets the amount sold.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the date of the sale.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the price per unit of the product.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the username of the person who made the sale.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets the total revenue from the sale.
        /// </summary>
        public decimal Revenue => Amount * Price;
    }
}
