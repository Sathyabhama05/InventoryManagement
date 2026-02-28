using System;

namespace InventoryManagement.Exceptions
{
    // Thrown when a product cannot be found
    public class ProductNotFoundException : Exception
    {
        public int ProductId { get; }

        public ProductNotFoundException(int productId)
            : base($"Product with ID {productId} was not found.")
        {
            ProductId = productId;
        }

        public ProductNotFoundException(string message) : base(message) { }
    }

    // Thrown when a category cannot be found
    public class CategoryNotFoundException : Exception
    {
        public int CategoryId { get; }

        public CategoryNotFoundException(int categoryId)
            : base($"Category with ID {categoryId} was not found.")
        {
            CategoryId = categoryId;
        }

        public CategoryNotFoundException(string message) : base(message) { }
    }

    // Thrown when trying to remove more stock than available
    public class InsufficientStockException : Exception
    {
        public int ProductId { get; }
        public int AvailableStock { get; }
        public int RequestedQuantity { get; }

        public InsufficientStockException(int productId, int available, int requested)
            : base($"Not enough stock for Product ID {productId}. Available: {available}, Requested: {requested}.")
        {
            ProductId = productId;
            AvailableStock = available;
            RequestedQuantity = requested;
        }
    }

    // Thrown when adding a product with a SKU that already exists
    public class DuplicateSKUException : Exception
    {
        public string SKU { get; }

        public DuplicateSKUException(string sku)
            : base($"A product with SKU '{sku}' already exists.")
        {
            SKU = sku;
        }
    }

    // Thrown for bad user input
    public class InvalidInputException : Exception
    {
        public InvalidInputException(string message) : base(message) { }
    }

    // Thrown when something goes wrong with the database
    public class DatabaseException : Exception
    {
        public DatabaseException(string message) : base(message) { }

        public DatabaseException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}