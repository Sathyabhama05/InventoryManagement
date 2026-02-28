using System;
using System.Collections.Generic;
using InventoryManagement.Data;
using InventoryManagement.Models;
using InventoryManagement.Exceptions;

namespace InventoryManagement.Services
{
    // ProductService sits between the menu (Program.cs) and the database (ProductRepository)
    // It validates input BEFORE any database call is made
    public class ProductService
    {
        private readonly ProductRepository  _productRepo;
        private readonly CategoryRepository _categoryRepo;

        public ProductService()
        {
            _productRepo  = new ProductRepository();
            _categoryRepo = new CategoryRepository();
        }

        public List<Product> GetAll()
        {
            return _productRepo.GetAll();
        }

        public Product GetById(int id)
        {
            if (id <= 0)
                throw new InvalidInputException("Product ID must be greater than zero.");

            return _productRepo.GetById(id); // throws ProductNotFoundException if not found
        }

        public List<Product> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                throw new InvalidInputException("Search term cannot be empty.");

            return _productRepo.Search(term.Trim());
        }

        public List<Product> GetByCategory(int categoryId)
        {
            _categoryRepo.GetById(categoryId); // validates category exists
            return _productRepo.GetByCategory(categoryId);
        }

        public int Add(string name, string sku, decimal price, int categoryId,
                       string description = "", int minStockLevel = 5)
        {
            Validate(name, sku, price, categoryId);
            _categoryRepo.GetById(categoryId); // ensure category exists

            var product = new Product(name.Trim(), sku.Trim().ToUpper(), price, categoryId, description?.Trim() ?? "")
            {
                MinStockLevel = minStockLevel > 0 ? minStockLevel : 5
            };

            return _productRepo.Add(product);
        }

        public bool Update(int id, string name, string sku, decimal price,
                           int categoryId, string description = "")
        {
            Validate(name, sku, price, categoryId);

            var existing = _productRepo.GetById(id);     // ensure product exists
            _categoryRepo.GetById(categoryId);            // ensure category exists

            existing.Name        = name.Trim();
            existing.SKU         = sku.Trim().ToUpper();
            existing.Price       = price;
            existing.CategoryId  = categoryId;
            existing.Description = description?.Trim() ?? "";

            return _productRepo.Update(existing);
        }

        public bool Delete(int id)
        {
            if (id <= 0)
                throw new InvalidInputException("Product ID must be greater than zero.");

            _productRepo.GetById(id); // ensure exists before deleting
            return _productRepo.Delete(id);
        }

        // Centralised validation used by Add and Update
        private void Validate(string name, string sku, decimal price, int categoryId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidInputException("Product name cannot be empty.");
            if (name.Length > 150)
                throw new InvalidInputException("Product name cannot exceed 150 characters.");
            if (string.IsNullOrWhiteSpace(sku))
                throw new InvalidInputException("SKU cannot be empty.");
            if (sku.Length > 50)
                throw new InvalidInputException("SKU cannot exceed 50 characters.");
            if (price < 0)
                throw new InvalidInputException("Price cannot be negative.");
            if (categoryId <= 0)
                throw new InvalidInputException("Please select a valid category.");
        }
    }
}