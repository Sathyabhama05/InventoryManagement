using System;

namespace InventoryManagement.Models
{
    // Enum to represent transaction direction
    public enum TransactionType
    {
        IN,   // Stock coming in
        OUT   // Stock going out
    }

    public class Transaction
    {
        public int TransactionId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public TransactionType Type { get; set; }
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public DateTime TransactionDate { get; set; }

        public Transaction()
        {
            TransactionDate = DateTime.Now;
        }

        public Transaction(int productId, TransactionType type, int quantity, string notes = "") : this()
        {
            ProductId = productId;
            Type = type;
            Quantity = quantity;
            Notes = notes;
        }

        public override string ToString()
        {
            string arrow = Type == TransactionType.IN ? "IN ▲" : "OUT ▼";
            return $"[{TransactionId}] {TransactionDate:yyyy-MM-dd HH:mm} | {arrow} | {ProductName} | Qty: {Quantity}";
        }
    }
}