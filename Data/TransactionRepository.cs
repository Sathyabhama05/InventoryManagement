using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using InventoryManagement.Models;
using InventoryManagement.Exceptions;

namespace InventoryManagement.Data
{
    public class TransactionRepository
    {
        // Get recent transactions (default: last 50)
        public List<Transaction> GetRecent(int limit = 50)
        {
            var list = new List<Transaction>();
            string sql = @"
                SELECT TOP (@Limit) t.*, p.Name AS ProductName
                FROM Transactions t
                JOIN Products p ON t.ProductId = p.ProductId
                ORDER BY t.TransactionDate DESC";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Limit", limit);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(Map(reader));
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error loading transactions.", ex);
            }

            return list;
        }

        // Get all transactions for one product
        public List<Transaction> GetByProduct(int productId)
        {
            var list = new List<Transaction>();
            string sql = @"
                SELECT t.*, p.Name AS ProductName
                FROM Transactions t
                JOIN Products p ON t.ProductId = p.ProductId
                WHERE t.ProductId = @ProdId
                ORDER BY t.TransactionDate DESC";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@ProdId", productId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(Map(reader));
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error loading product transactions.", ex);
            }

            return list;
        }

        // Get transactions between two dates
        public List<Transaction> GetByDateRange(DateTime from, DateTime to)
        {
            var list = new List<Transaction>();
            string sql = @"
                SELECT t.*, p.Name AS ProductName
                FROM Transactions t
                JOIN Products p ON t.ProductId = p.ProductId
                WHERE t.TransactionDate BETWEEN @From AND @To
                ORDER BY t.TransactionDate DESC";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@From", from);
                    cmd.Parameters.AddWithValue("@To",   to.Date.AddDays(1).AddSeconds(-1));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                            list.Add(Map(reader));
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error loading transactions by date.", ex);
            }

            return list;
        }

        private Transaction Map(SqlDataReader reader)
        {
            return new Transaction
            {
                TransactionId   = (int)reader["TransactionId"],
                ProductId       = (int)reader["ProductId"],
                ProductName     = reader["ProductName"].ToString(),
                Type            = reader["TransactionType"].ToString() == "IN" ? TransactionType.IN : TransactionType.OUT,
                Quantity        = (int)reader["Quantity"],
                Notes           = reader["Notes"].ToString(),
                TransactionDate = (DateTime)reader["TransactionDate"]
            };
        }
    }
}