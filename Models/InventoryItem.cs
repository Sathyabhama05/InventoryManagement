using System;

namespace InventoryManagement.Models
{
    public class InventoryItem
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? SKU { get; set; }
        public int Quantity { get; set; }
        public int MinStockLevel { get; set; }
        public DateTime LastUpdated { get; set; }

        // Computed properties
        public bool IsLowStock => Quantity <= MinStockLevel;
        public bool IsOutOfStock => Quantity == 0;

        public InventoryItem()
        {
            LastUpdated = DateTime.Now;
            MinStockLevel = 5;
        }

        public override string ToString()
        {
            string status = IsOutOfStock ? "OUT OF STOCK" : IsLowStock ? "LOW STOCK" : "OK";
            return $"{ProductName} | Qty: {Quantity} | Min: {MinStockLevel} | Status: {status}";
        }
    }
}