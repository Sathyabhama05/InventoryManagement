using System;

namespace InventoryManagement.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        public Category()
        {
            IsActive = true;
            CreatedAt = DateTime.Now;
        }

        public Category(string name, string description = "") : this()
        {
            Name = name;
            Description = description;
        }

        public override string ToString()
        {
            return $"[{CategoryId}] {Name} - {Description}";
        }
    }
}