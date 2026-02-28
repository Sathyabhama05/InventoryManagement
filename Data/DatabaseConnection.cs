using System;
using System.Data;
using Microsoft.Data.SqlClient;
using InventoryManagement.Exceptions;

namespace InventoryManagement.Data
{
    public class DatabaseConnection
    {
       
        private static string _connectionString =
    @"Server=localhost,1433;Database=InventoryDB;User Id=sa;Password=Sathya@123;TrustServerCertificate=True;";

        public static string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = value;
        }

        // Returns an open connection — caller must close it using 'using'
        public static SqlConnection GetConnection()
        {
            try
            {
                var connection = new SqlConnection(_connectionString);
                connection.Open();
                return connection;
            }
            catch (SqlException ex)
            {
                throw new DatabaseException($"Could not connect to database: {ex.Message}", ex);
            }
        }

        // Quick test to see if DB is reachable
        public static bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    return conn.State == ConnectionState.Open;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}