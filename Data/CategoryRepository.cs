using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using InventoryManagement.Models;
using InventoryManagement.Exceptions;

namespace InventoryManagement.Data
{
    public class CategoryRepository
    {
        // Get all active categories
        public List<Category> GetAll()
        {
            var list = new List<Category>();
            string sql = "SELECT * FROM Categories WHERE IsActive = 1 ORDER BY CategoryName";

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
                throw new DatabaseException("Error loading categories.", ex);
            }

            return list;
        }

        // Get one category by ID
        public Category GetById(int id)
        {
            string sql = "SELECT * FROM Categories WHERE CategoryId = @Id";

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
                throw new DatabaseException("Error loading category.", ex);
            }

            throw new CategoryNotFoundException(id);
        }

        // Check if a category name already exists
        public bool NameExists(string name)
        {
            string sql = "SELECT COUNT(*) FROM Categories WHERE categoryName = @Name AND IsActive = 1";
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", name);
                    return (int)cmd.ExecuteScalar() > 0;
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error checking category name.", ex);
            }
        }

        // Insert new category, returns new ID
        public int Add(Category category)
        {
            if (NameExists(category.Name!))
                throw new InvalidInputException($"Category '{category.Name}' already exists.");

            string sql = @"INSERT INTO Categories (CategoryName)
                           OUTPUT INSERTED.CategoryId
                           VALUES (@Name)";
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", category.Name);
                    
                    return (int)cmd.ExecuteScalar();
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error adding category.", ex);
            }
        }

        // Update existing category
        public bool Update(Category category)
        {
            string sql = @"UPDATE Categories 
               SET CategoryName = @Name
               WHERE CategoryId = @Id AND IsActive = 1";
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", category.Name);
                    cmd.Parameters.AddWithValue("@Id", category.CategoryId);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (SqlException ex)
            {
                throw new DatabaseException("Error updating category.", ex);
            }
        }

        // Soft delete (marks IsActive = 0, doesn't remove the row)
        public bool Delete(int id)
        {
            string sql = "UPDATE Categories SET IsActive = 0 WHERE CategoryId = @Id";
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
                throw new DatabaseException("Error deleting category.", ex);
            }
        }

        // Map a SQL row to a Category object
        private Category Map(SqlDataReader reader)
        {
            return new Category
            {
                CategoryId  = (int)reader["CategoryId"],
                Name        = reader["CategoryName"].ToString(),
                CreatedAt   = (DateTime)reader["CreatedAt"],
                IsActive    = (bool)reader["IsActive"]
            };
        }
    }
}