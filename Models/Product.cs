using System;

namespace InventoryManagement.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public string? SKU { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        // Stock info (loaded via JOIN from Inventory table)
        public int CurrentStock { get; set; }
        public int MinStockLevel { get; set; }

        // Computed property - no DB column needed
        public bool IsLowStock => CurrentStock <= MinStockLevel;

        public Product()
        {
            IsActive = true;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            MinStockLevel = 5;
        }

        public Product(string name, string sku, decimal price, int categoryId, string description = "") : this()
        {
            Name = name;
            SKU = sku;
            Price = price;
            CategoryId = categoryId;
            Description = description;
        }

        public override string ToString()
        {
            return $"[{ProductId}] {Name} | SKU: {SKU} | Price: {Price:C} | Stock: {CurrentStock}";
        }
    }
}