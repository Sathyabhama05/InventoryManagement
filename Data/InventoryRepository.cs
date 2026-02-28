using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using InventoryManagement.Models;
using InventoryManagement.Exceptions;

namespace InventoryManagement.Data
{
    public class InventoryRepository
    {
        // Get full inventory list
        public List<InventoryItem> GetAll()
        {
            var list = new List<InventoryItem>();
            string sql = @"
                SELECT i.*, p.Name AS ProductName, p.SKU
                FROM Inventory i
                JOIN Products p ON i.ProductId = p.ProductId
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
                throw new DatabaseException("Error loading inventory.", ex);
            }

            return list;
        }

        // Get only items that are at or below their minimum stock level
        public List<InventoryItem> GetLowStock()
        {
            var list = new List<InventoryItem>();
            string sql = @"
                SELECT i.*, p.Name AS ProductName, p.SKU
                FROM Inventory i
                JOIN Products p ON i.ProductId = p.ProductId
                WHERE p.IsActive = 1 AND i.Quantity <= i.MinStockLevel
                ORDER BY i.Quantity ASC";

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
                throw new DatabaseException("Error loading low stock items.", ex);
            }

            return list;
        }

        // Get inventory record for a specific product
        public InventoryItem? GetByProductId(int productId)
        {
            string sql = @"
                SELECT i.*, p.Name AS ProductName, p.SKU
                FROM Inventory i
                JOIN Products p ON i.ProductId = p.ProductId
                WHERE i.ProductId = @ProdId";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ProdId", productId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return Map(reader);
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error loading inventory item.", ex);
            }

            return null;
        }

        // Add stock (Stock In)
        public void StockIn(int productId, int quantity, string notes = "")
        {
            if (quantity <= 0)
                throw new InvalidInputException("Quantity must be greater than zero.");

            string updateSql = @"
                UPDATE Inventory 
                SET Quantity = Quantity + @Qty, LastUpdated = GETDATE()
                WHERE ProductId = @ProdId";

            string logSql = @"
                INSERT INTO Transactions (ProductId, TransactionType, Quantity, Notes)
                VALUES (@ProdId, 'IN', @Qty, @Notes)";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    using (var cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Qty",    quantity);
                        cmd.Parameters.AddWithValue("@ProdId", productId);
                        if (cmd.ExecuteNonQuery() == 0)
                            throw new ProductNotFoundException(productId);
                    }

                    using (var cmd = new SqlCommand(logSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProdId", productId);
                        cmd.Parameters.AddWithValue("@Qty",    quantity);
                        cmd.Parameters.AddWithValue("@Notes",  notes ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error performing stock-in.", ex);
            }
        }

        // Remove stock (Stock Out) — checks available quantity first
        public void StockOut(int productId, int quantity, string notes = "")
        {
            if (quantity <= 0)
                throw new InvalidInputException("Quantity must be greater than zero.");

            var item = GetByProductId(productId);
            if (item == null)
                throw new ProductNotFoundException(productId);
            if (item.Quantity < quantity)
                throw new InsufficientStockException(productId, item.Quantity, quantity);

            string updateSql = @"
                UPDATE Inventory 
                SET Quantity = Quantity - @Qty, LastUpdated = GETDATE()
                WHERE ProductId = @ProdId";

            string logSql = @"
                INSERT INTO Transactions (ProductId, TransactionType, Quantity, Notes)
                VALUES (@ProdId, 'OUT', @Qty, @Notes)";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    using (var cmd = new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Qty",    quantity);
                        cmd.Parameters.AddWithValue("@ProdId", productId);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new SqlCommand(logSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProdId", productId);
                        cmd.Parameters.AddWithValue("@Qty",    quantity);
                        cmd.Parameters.AddWithValue("@Notes",  notes ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error performing stock-out.", ex);
            }
        }

        // Update the minimum stock threshold
        public bool UpdateMinStock(int productId, int minLevel)
        {
            if (minLevel < 0)
                throw new InvalidInputException("Minimum stock level cannot be negative.");

            string sql = "UPDATE Inventory SET MinStockLevel = @Min WHERE ProductId = @ProdId";
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Min",    minLevel);
                    cmd.Parameters.AddWithValue("@ProdId", productId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error updating min stock level.", ex);
            }
        }

        // Get summary numbers for reports
        public (int totalProducts, int totalQty, decimal totalValue) GetSummary()
        {
            string sql = @"
                SELECT COUNT(*)                    AS TotalProducts,
                       SUM(i.Quantity)             AS TotalQty,
                       SUM(i.Quantity * p.Price)   AS TotalValue
                FROM Inventory i
                JOIN Products p ON i.ProductId = p.ProductId
                WHERE p.IsActive = 1";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (
                            totalProducts: (int)reader["TotalProducts"],
                            totalQty:      reader["TotalQty"]    == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalQty"]),
                            totalValue:    reader["TotalValue"]  == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalValue"])
                        );
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error getting summary.", ex);
            }

            return (0, 0, 0);
        }

        private InventoryItem Map(SqlDataReader reader)
        {
            return new InventoryItem
            {
                InventoryId   = (int)reader["InventoryId"],
                ProductId     = (int)reader["ProductId"],
                ProductName   = reader["ProductName"].ToString(),
                SKU           = reader["SKU"].ToString(),
                Quantity      = (int)reader["Quantity"],
                MinStockLevel = (int)reader["MinStockLevel"],
                LastUpdated   = (DateTime)reader["LastUpdated"]
            };
        }
    }
}