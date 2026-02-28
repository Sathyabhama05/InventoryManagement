using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using InventoryManagement.Models;
using InventoryManagement.Exceptions;

namespace InventoryManagement.Data
{
    public class ProductRepository
    {
        // Get all active products with their stock levels (uses a JOIN)
        public List<Product> GetAll()
        {
            var list = new List<Product>();
            string sql = @"
                SELECT p.*, c.Name AS CategoryName,
                       ISNULL(i.Quantity, 0)      AS CurrentStock,
                       ISNULL(i.MinStockLevel, 5) AS MinStockLevel
                FROM Products p
                LEFT JOIN Categories c  ON p.CategoryId  = c.CategoryId
                LEFT JOIN Inventory i   ON p.ProductId   = i.ProductId
                WHERE p.IsActive = 1
                ORDER BY p.Name";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add(Map(reader));
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error loading products.", ex);
            }

            return list;
        }

        // Get one product by ID
        public Product GetById(int id)
        {
            string sql = @"
                SELECT p.*, c.Name AS CategoryName,
                       ISNULL(i.Quantity, 0)      AS CurrentStock,
                       ISNULL(i.MinStockLevel, 5) AS MinStockLevel
                FROM Products p
                LEFT JOIN Categories c  ON p.CategoryId = c.CategoryId
                LEFT JOIN Inventory i   ON p.ProductId  = i.ProductId
                WHERE p.ProductId = @Id AND p.IsActive = 1";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return Map(reader);
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error loading product.", ex);
            }

            throw new ProductNotFoundException(id);
        }

        // Search by name, SKU, description, or category name
        public List<Product> Search(string term)
        {
            var list = new List<Product>();
            string sql = @"
                SELECT p.*, c.Name AS CategoryName,
                       ISNULL(i.Quantity, 0)      AS CurrentStock,
                       ISNULL(i.MinStockLevel, 5) AS MinStockLevel
                FROM Products p
                LEFT JOIN Categories c  ON p.CategoryId = c.CategoryId
                LEFT JOIN Inventory i   ON p.ProductId  = i.ProductId
                WHERE p.IsActive = 1
                  AND (p.Name LIKE @T OR p.SKU LIKE @T OR p.Description LIKE @T OR c.Name LIKE @T)
                ORDER BY p.Name";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@T", $"%{term}%");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(Map(reader));
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error searching products.", ex);
            }

            return list;
        }

        // Get products that belong to a specific category
        public List<Product> GetByCategory(int categoryId)
        {
            var list = new List<Product>();
            string sql = @"
                SELECT p.*, c.Name AS CategoryName,
                       ISNULL(i.Quantity, 0)      AS CurrentStock,
                       ISNULL(i.MinStockLevel, 5) AS MinStockLevel
                FROM Products p
                LEFT JOIN Categories c  ON p.CategoryId = c.CategoryId
                LEFT JOIN Inventory i   ON p.ProductId  = i.ProductId
                WHERE p.IsActive = 1 AND p.CategoryId = @CatId
                ORDER BY p.Name";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@CatId", categoryId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(Map(reader));
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error loading products by category.", ex);
            }

            return list;
        }

        // Check if a SKU already exists (optionally exclude a product ID when editing)
        public bool SKUExists(string sku, int excludeId = 0)
        {
            string sql = "SELECT COUNT(*) FROM Products WHERE SKU = @SKU AND ProductId != @ExcludeId AND IsActive = 1";
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@SKU", sku);
                    cmd.Parameters.AddWithValue("@ExcludeId", excludeId);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error checking SKU.", ex);
            }
        }

        // Insert a new product and create its inventory record — returns new ProductId
        public int Add(Product product)
        {
            if (SKUExists(product.SKU!))
                throw new DuplicateSKUException(product.SKU!);

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    // Insert product
                    int newId;
                    string insertProduct = @"
                        INSERT INTO Products (Name, SKU, Description, Price, CategoryId)
                        OUTPUT INSERTED.ProductId
                        VALUES (@Name, @SKU, @Desc, @Price, @CatId)";

                    using (var cmd = new SqlCommand(insertProduct, conn))
                    {
                        cmd.Parameters.AddWithValue("@Name",  product.Name);
                        cmd.Parameters.AddWithValue("@SKU",   product.SKU);
                        cmd.Parameters.AddWithValue("@Desc",  product.Description ?? "");
                        cmd.Parameters.AddWithValue("@Price", product.Price);
                        cmd.Parameters.AddWithValue("@CatId", product.CategoryId);
                        newId = (int)cmd.ExecuteScalar();
                    }

                    // Create inventory row with 0 stock
                    string insertInventory = @"
                        INSERT INTO Inventory (ProductId, Quantity, MinStockLevel)
                        VALUES (@ProdId, 0, @MinStock)";

                    using (var cmd = new SqlCommand(insertInventory, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProdId",   newId);
                        cmd.Parameters.AddWithValue("@MinStock", product.MinStockLevel > 0 ? product.MinStockLevel : 5);
                        cmd.ExecuteNonQuery();
                    }

                    return newId;
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error adding product.", ex);
            }
        }

        // Update existing product
        public bool Update(Product product)
        {
            if (SKUExists(product.SKU!, product.ProductId))
                throw new DuplicateSKUException(product.SKU!);

            string sql = @"
                UPDATE Products
                SET Name = @Name, SKU = @SKU, Description = @Desc,
                    Price = @Price, CategoryId = @CatId, UpdatedAt = GETDATE()
                WHERE ProductId = @Id AND IsActive = 1";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name",  product.Name);
                    cmd.Parameters.AddWithValue("@SKU",   product.SKU);
                    cmd.Parameters.AddWithValue("@Desc",  product.Description ?? "");
                    cmd.Parameters.AddWithValue("@Price", product.Price);
                    cmd.Parameters.AddWithValue("@CatId", product.CategoryId);
                    cmd.Parameters.AddWithValue("@Id",    product.ProductId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error updating product.", ex);
            }
        }

        // Soft delete
        public bool Delete(int id)
        {
            string sql = "UPDATE Products SET IsActive = 0 WHERE ProductId = @Id";
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error deleting product.", ex);
            }
        }

        // Map a SQL row to a Product object
        private Product Map(SqlDataReader reader)
        {
            return new Product
            {
                ProductId    = (int)reader["ProductId"],
                Name         = reader["Name"].ToString(),
                SKU          = reader["SKU"].ToString(),
                Description  = reader["Description"].ToString(),
                Price        = (decimal)reader["Price"],
                CategoryId   = reader["CategoryId"] == DBNull.Value ? 0 : (int)reader["CategoryId"],
                CategoryName = reader["CategoryName"] == DBNull.Value ? "None" : reader["CategoryName"].ToString(),
                CreatedAt    = (DateTime)reader["CreatedAt"],
                UpdatedAt    = (DateTime)reader["UpdatedAt"],
                IsActive     = (bool)reader["IsActive"],
                CurrentStock = (int)reader["CurrentStock"],
                MinStockLevel= (int)reader["MinStockLevel"]
            };
        }
    }
}