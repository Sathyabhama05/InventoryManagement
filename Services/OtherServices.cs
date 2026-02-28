using System;
using System.Collections.Generic;
using InventoryManagement.Data;
using InventoryManagement.Models;
using InventoryManagement.Exceptions;

namespace InventoryManagement.Services
{
    //  CategoryService
    public class CategoryService
    {
        private readonly CategoryRepository _repo;

        public CategoryService()
        {
            _repo = new CategoryRepository();
        }

        public List<Category> GetAll() => _repo.GetAll();

        public Category GetById(int id)
        {
            if (id <= 0)
                throw new InvalidInputException("Category ID must be greater than zero.");
            return _repo.GetById(id);
        }

        public int Add(string name, string description = "")
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidInputException("Category name cannot be empty.");
            if (name.Length > 100)
                throw new InvalidInputException("Category name cannot exceed 100 characters.");

            var category = new Category(name.Trim(), description?.Trim() ?? "");
            return _repo.Add(category);
        }

        public bool Update(int id, string name, string description = "")
        {
            if (id <= 0)
                throw new InvalidInputException("Category ID must be greater than zero.");
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidInputException("Category name cannot be empty.");

            var existing = _repo.GetById(id);
            existing.Name        = name.Trim();
            existing.Description = description?.Trim() ?? "";
            return _repo.Update(existing);
        }

        public bool Delete(int id)
        {
            if (id <= 0)
                throw new InvalidInputException("Category ID must be greater than zero.");
            _repo.GetById(id); // ensure it exists
            return _repo.Delete(id);
        }
    }


    //  InventoryService
    
    public class InventoryService
    {
        private readonly InventoryRepository _inventoryRepo;
        private readonly ProductRepository   _productRepo;

        public InventoryService()
        {
            _inventoryRepo = new InventoryRepository();
            _productRepo   = new ProductRepository();
        }

        public List<InventoryItem> GetAll()       => _inventoryRepo.GetAll();
        public List<InventoryItem> GetLowStock()  => _inventoryRepo.GetLowStock();

        public (int totalProducts, int totalQty, decimal totalValue) GetSummary()
            => _inventoryRepo.GetSummary();

        public void StockIn(int productId, int quantity, string notes = "")
        {
            if (productId <= 0)
                throw new InvalidInputException("Product ID must be greater than zero.");
            if (quantity <= 0)
                throw new InvalidInputException("Quantity must be greater than zero.");

            _productRepo.GetById(productId); // verify product exists
            _inventoryRepo.StockIn(productId, quantity, notes);
        }

        public void StockOut(int productId, int quantity, string notes = "")
        {
            if (productId <= 0)
                throw new InvalidInputException("Product ID must be greater than zero.");
            if (quantity <= 0)
                throw new InvalidInputException("Quantity must be greater than zero.");

            _productRepo.GetById(productId); // verify product exists
            _inventoryRepo.StockOut(productId, quantity, notes);
        }

        public bool UpdateMinStockLevel(int productId, int minLevel)
        {
            if (productId <= 0)
                throw new InvalidInputException("Product ID must be greater than zero.");
            if (minLevel < 0)
                throw new InvalidInputException("Minimum stock level cannot be negative.");

            _productRepo.GetById(productId);
            return _inventoryRepo.UpdateMinStock(productId, minLevel);
        }
    }


    //  TransactionService

    public class TransactionService
    {
        private readonly TransactionRepository _repo;

        public TransactionService()
        {
            _repo = new TransactionRepository();
        }

        public List<Transaction> GetRecent(int limit = 50)
        {
            if (limit <= 0) limit = 50;
            return _repo.GetRecent(limit);
        }

        public List<Transaction> GetByProduct(int productId)
        {
            if (productId <= 0)
                throw new InvalidInputException("Product ID must be greater than zero.");
            return _repo.GetByProduct(productId);
        }

        public List<Transaction> GetByDateRange(DateTime from, DateTime to)
        {
            if (from > to)
                throw new InvalidInputException("Start date cannot be after end date.");
            return _repo.GetByDateRange(from, to);
        }
    }
}