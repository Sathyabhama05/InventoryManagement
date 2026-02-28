using System;
using System.Collections.Generic;
using InventoryManagement.Data;
using InventoryManagement.Exceptions;
using InventoryManagement.Models;
using InventoryManagement.Services;

namespace InventoryManagement
{
    class Program
    {
        // Create service objects once — reuse throughout the app
        static readonly ProductService     _productService     = new ProductService();
        static readonly CategoryService    _categoryService    = new CategoryService();
        static readonly InventoryService   _inventoryService   = new InventoryService();
        static readonly TransactionService _transactionService = new TransactionService();

        static void Main(string[] args)
        {
            Console.Title = "Inventory Management System";

            // Test DB connection before doing anything
            if (!DatabaseConnection.TestConnection())
            {
                PrintError("Cannot connect to SQL Server.");
                PrintInfo("Check your connection string in Data/DatabaseConnection.cs");
                Console.ReadKey();
                return;
            }

            PrintSuccess("Connected to database!");
            MainMenu();
        }

       
        //  MAIN MENU
     
        static void MainMenu()
        {
            while (true)
            {
                PrintHeader("INVENTORY MANAGEMENT SYSTEM");
                Console.WriteLine("  [1] Product Management");
                Console.WriteLine("  [2] Category Management");
                Console.WriteLine("  [3] Inventory Operations");
                Console.WriteLine("  [4] Reports");
                Console.WriteLine("  [0] Exit");

                int choice = ReadInt("\nChoose an option", 0, 4);

                switch (choice)
                {
                    case 1: ProductMenu();    break;
                    case 2: CategoryMenu();   break;
                    case 3: InventoryMenu();  break;
                    case 4: ReportsMenu();    break;
                    case 0:
                        Console.WriteLine("\n  Goodbye!\n");
                        return;
                }
            }
        }

      
        //  PRODUCT MENU
        
        static void ProductMenu()
        {
            while (true)
            {
                PrintHeader("PRODUCT MANAGEMENT");
                Console.WriteLine("  [1] View All Products");
                Console.WriteLine("  [2] View Product Details");
                Console.WriteLine("  [3] Add Product");
                Console.WriteLine("  [4] Update Product");
                Console.WriteLine("  [5] Delete Product");
                Console.WriteLine("  [6] Search Products");
                Console.WriteLine("  [7] Products by Category");
                Console.WriteLine("  [0] Back");

                int choice = ReadInt("\nChoose an option", 0, 7);
                if (choice == 0) return;

                switch (choice)
                {
                    case 1: ViewAllProducts();        break;
                    case 2: ViewProductDetails();     break;
                    case 3: AddProduct();             break;
                    case 4: UpdateProduct();          break;
                    case 5: DeleteProduct();          break;
                    case 6: SearchProducts();         break;
                    case 7: ProductsByCategory();     break;
                }
            }
        }

        static void ViewAllProducts()
        {
            PrintHeader("ALL PRODUCTS");
            Run(() =>
            {
                var list = _productService.GetAll();
                if (list.Count == 0) { PrintWarning("No products found."); return; }

                PrintTableHeader(("ID",6),("Name",26),("SKU",12),("Price",10),("Category",18),("Stock",8));
                foreach (var p in list)
                {
                    Console.ForegroundColor = p.CurrentStock == 0 ? ConsoleColor.Red
                                            : p.IsLowStock        ? ConsoleColor.Yellow
                                                                   : ConsoleColor.White;
                    PrintRow((p.ProductId.ToString(),6),(p.Name,26),(p.SKU,12),
                             (p.Price.ToString("C"),10),(p.CategoryName,18),(p.CurrentStock.ToString(),8));
                    Console.ResetColor();
                }
                PrintInfo($"Total: {list.Count} product(s)");
            });
            Pause();
        }

        static void ViewProductDetails()
        {
            PrintHeader("PRODUCT DETAILS");
            Run(() =>
            {
                int id = ReadInt("Enter Product ID", 1);
                var p  = _productService.GetById(id);

                Console.WriteLine();
                PrintInfo($"ID          : {p.ProductId}");
                PrintInfo($"Name        : {p.Name}");
                PrintInfo($"SKU         : {p.SKU}");
                PrintInfo($"Price       : {p.Price:C}");
                PrintInfo($"Category    : {p.CategoryName}");
                PrintInfo($"Description : {p.Description}");
                PrintInfo($"Stock       : {p.CurrentStock} units");
                PrintInfo($"Min Stock   : {p.MinStockLevel} units");
                PrintInfo($"Status      : {(p.CurrentStock == 0 ? "OUT OF STOCK" : p.IsLowStock ? "LOW STOCK" : "OK")}");
                PrintInfo($"Created     : {p.CreatedAt:yyyy-MM-dd}");
            });
            Pause();
        }

        static void AddProduct()
        {
            PrintHeader("ADD NEW PRODUCT");
            Run(() =>
            {
                // Show categories first
                var cats = _categoryService.GetAll();
                if (cats.Count == 0) { PrintError("No categories found. Add a category first."); return; }
                Console.WriteLine("\n  Available Categories:");
                foreach (var c in cats) PrintInfo($"  [{c.CategoryId}] {c.Name}");

                string name    = ReadString("Product Name");
                string sku     = ReadString("SKU");
                decimal price  = ReadDecimal("Price");
                int    catId   = ReadInt("Category ID", 1);
                string desc    = ReadString("Description (press Enter to skip)");
                int    minStk  = ReadInt("Minimum Stock Level (default 5)", 0);

                int newId = _productService.Add(name, sku, price, catId, desc, minStk == 0 ? 5 : minStk);
                PrintSuccess($"Product added! ID = {newId}");
            });
            Pause();
        }

        static void UpdateProduct()
        {
            PrintHeader("UPDATE PRODUCT");
            Run(() =>
            {
                int id = ReadInt("Enter Product ID to update", 1);
                var p  = _productService.GetById(id);
                PrintInfo($"Editing: {p.Name} (SKU: {p.SKU})");
                PrintInfo("Press Enter to keep the current value.\n");

                string name  = ReadStringOptional($"Name [{p.Name}]",        p.Name);
                string sku   = ReadStringOptional($"SKU  [{p.SKU}]",         p.SKU);
                string priceStr = ReadStringOptional($"Price [{p.Price:C}]", p.Price.ToString());
                decimal price = decimal.TryParse(priceStr, out decimal parsed) ? parsed : p.Price;

                var cats = _categoryService.GetAll();
                Console.WriteLine("\n  Categories:");
                foreach (var c in cats) PrintInfo($"  [{c.CategoryId}] {c.Name}");

                string catStr = ReadStringOptional($"Category ID [{p.CategoryId}]", p.CategoryId.ToString());
                int catId = int.TryParse(catStr, out int cid) ? cid : p.CategoryId;

                string desc = ReadStringOptional($"Description [{p.Description}]", p.Description);

                if (Confirm("Save changes?"))
                {
                    _productService.Update(id, name, sku, price, catId, desc);
                    PrintSuccess("Product updated successfully.");
                }
                else PrintInfo("Update cancelled.");
            });
            Pause();
        }

        static void DeleteProduct()
        {
            PrintHeader("DELETE PRODUCT");
            Run(() =>
            {
                int id = ReadInt("Enter Product ID to delete", 1);
                var p  = _productService.GetById(id);
                PrintWarning($"You are about to delete: {p.Name} (SKU: {p.SKU})");

                if (Confirm("Are you sure?"))
                {
                    _productService.Delete(id);
                    PrintSuccess("Product deleted.");
                }
                else PrintInfo("Cancelled.");
            });
            Pause();
        }

        static void SearchProducts()
        {
            PrintHeader("SEARCH PRODUCTS");
            Run(() =>
            {
                string term  = ReadString("Search term");
                var results  = _productService.Search(term);
                if (results.Count == 0) { PrintWarning("No products found."); return; }

                PrintTableHeader(("ID",6),("Name",26),("SKU",12),("Price",10),("Category",18),("Stock",8));
                foreach (var p in results)
                    PrintRow((p.ProductId.ToString(),6),(p.Name,26),(p.SKU,12),
                             (p.Price.ToString("C"),10),(p.CategoryName,18),(p.CurrentStock.ToString(),8));
                PrintInfo($"Found: {results.Count} result(s)");
            });
            Pause();
        }

        static void ProductsByCategory()
        {
            PrintHeader("PRODUCTS BY CATEGORY");
            Run(() =>
            {
                var cats = _categoryService.GetAll();
                foreach (var c in cats) PrintInfo($"[{c.CategoryId}] {c.Name}");
                int catId   = ReadInt("Enter Category ID", 1);
                var results = _productService.GetByCategory(catId);
                if (results.Count == 0) { PrintWarning("No products in this category."); return; }

                PrintTableHeader(("ID",6),("Name",30),("SKU",12),("Price",10),("Stock",8));
                foreach (var p in results)
                    PrintRow((p.ProductId.ToString(),6),(p.Name,30),(p.SKU,12),
                             (p.Price.ToString("C"),10),(p.CurrentStock.ToString(),8));
            });
            Pause();
        }

        
        //  CATEGORY MENU
        
        static void CategoryMenu()
        {
            while (true)
            {
                PrintHeader("CATEGORY MANAGEMENT");
                Console.WriteLine("  [1] View All Categories");
                Console.WriteLine("  [2] Add Category");
                Console.WriteLine("  [3] Update Category");
                Console.WriteLine("  [4] Delete Category");
                Console.WriteLine("  [0] Back");

                int choice = ReadInt("\nChoose an option", 0, 4);
                if (choice == 0) return;

                switch (choice)
                {
                    case 1:
                        PrintHeader("ALL CATEGORIES");
                        Run(() =>
                        {
                            var cats = _categoryService.GetAll();
                            if (cats.Count == 0) { PrintWarning("No categories."); return; }
                            PrintTableHeader(("ID",5),("Name",25),("Description",40));
                            foreach (var c in cats)
                                PrintRow((c.CategoryId.ToString(),5),(c.Name,25),(c.Description,40));
                        });
                        Pause();
                        break;

                    case 2:
                        PrintHeader("ADD CATEGORY");
                        Run(() =>
                        {
                            string name = ReadString("Category Name");
                            string desc = ReadString("Description (optional)");
                            int id = _categoryService.Add(name, desc);
                            PrintSuccess($"Category added! ID = {id}");
                        });
                        Pause();
                        break;

                    case 3:
                        PrintHeader("UPDATE CATEGORY");
                        Run(() =>
                        {
                            int id  = ReadInt("Category ID to update", 1);
                            var cat = _categoryService.GetById(id);
                            string name = ReadStringOptional($"Name [{cat.Name}]", cat.Name);
                            string desc = ReadStringOptional($"Desc [{cat.Description}]", cat.Description);
                            _categoryService.Update(id, name, desc);
                            PrintSuccess("Category updated.");
                        });
                        Pause();
                        break;

                    case 4:
                        PrintHeader("DELETE CATEGORY");
                        Run(() =>
                        {
                            int id  = ReadInt("Category ID to delete", 1);
                            var cat = _categoryService.GetById(id);
                            if (Confirm($"Delete '{cat.Name}'?"))
                            {
                                _categoryService.Delete(id);
                                PrintSuccess("Category deleted.");
                            }
                        });
                        Pause();
                        break;
                }
            }
        }

        
        //  INVENTORY MENU
       
        static void InventoryMenu()
        {
            while (true)
            {
                PrintHeader("INVENTORY OPERATIONS");
                Console.WriteLine("  [1] View All Inventory");
                Console.WriteLine("  [2] Stock In");
                Console.WriteLine("  [3] Stock Out");
                Console.WriteLine("  [4] Update Min Stock Level");
                Console.WriteLine("  [5] Low Stock Alerts");
                Console.WriteLine("  [0] Back");

                int choice = ReadInt("\nChoose an option", 0, 5);
                if (choice == 0) return;

                switch (choice)
                {
                    case 1:
                        PrintHeader("FULL INVENTORY");
                        Run(() =>
                        {
                            var items = _inventoryService.GetAll();
                            if (items.Count == 0) { PrintWarning("No inventory records."); return; }
                            PrintTableHeader(("ProdID",7),("Name",28),("SKU",12),("Qty",8),("Min",8),("Status",12));
                            foreach (var item in items)
                            {
                                Console.ForegroundColor = item.IsOutOfStock ? ConsoleColor.Red
                                                        : item.IsLowStock   ? ConsoleColor.Yellow
                                                                             : ConsoleColor.White;
                                PrintRow((item.ProductId.ToString(),7),(item.ProductName,28),(item.SKU,12),
                                         (item.Quantity.ToString(),8),(item.MinStockLevel.ToString(),8),
                                         (item.IsOutOfStock ? "OUT OF STOCK" : item.IsLowStock ? "LOW STOCK" : "OK",12));
                                Console.ResetColor();
                            }
                        });
                        Pause();
                        break;

                    case 2:
                        PrintHeader("STOCK IN");
                        Run(() =>
                        {
                            int prodId = ReadInt("Product ID", 1);
                            int qty    = ReadInt("Quantity to add", 1);
                            string notes = ReadString("Notes (optional)");
                            _inventoryService.StockIn(prodId, qty, notes);
                            PrintSuccess($"Added {qty} units to stock.");
                        });
                        Pause();
                        break;

                    case 3:
                        PrintHeader("STOCK OUT");
                        Run(() =>
                        {
                            int prodId = ReadInt("Product ID", 1);
                            int qty    = ReadInt("Quantity to remove", 1);
                            string notes = ReadString("Notes (optional)");
                            _inventoryService.StockOut(prodId, qty, notes);
                            PrintSuccess($"Removed {qty} units from stock.");
                        });
                        Pause();
                        break;

                    case 4:
                        PrintHeader("UPDATE MIN STOCK LEVEL");
                        Run(() =>
                        {
                            int prodId   = ReadInt("Product ID", 1);
                            int minLevel = ReadInt("New minimum stock level", 0);
                            _inventoryService.UpdateMinStockLevel(prodId, minLevel);
                            PrintSuccess($"Min stock level updated to {minLevel}.");
                        });
                        Pause();
                        break;

                    case 5:
                        PrintHeader("LOW STOCK ALERTS");
                        Run(() =>
                        {
                            var items = _inventoryService.GetLowStock();
                            if (items.Count == 0) { PrintSuccess("All products are well-stocked!"); return; }
                            PrintTableHeader(("ProdID",7),("Name",28),("SKU",12),("Qty",8),("Min",8),("Status",12));
                            foreach (var item in items)
                            {
                                Console.ForegroundColor = item.IsOutOfStock ? ConsoleColor.Red : ConsoleColor.Yellow;
                                PrintRow((item.ProductId.ToString(),7),(item.ProductName,28),(item.SKU,12),
                                         (item.Quantity.ToString(),8),(item.MinStockLevel.ToString(),8),
                                         (item.IsOutOfStock ? "OUT OF STOCK" : "LOW STOCK",12));
                                Console.ResetColor();
                            }
                        });
                        Pause();
                        break;
                }
            }
        }

        
        //  REPORTS MENU
        
        static void ReportsMenu()
        {
            while (true)
            {
                PrintHeader("REPORTS");
                Console.WriteLine("  [1] Inventory Summary");
                Console.WriteLine("  [2] Transaction History");
                Console.WriteLine("  [0] Back");

                int choice = ReadInt("\nChoose an option", 0, 2);
                if (choice == 0) return;

                switch (choice)
                {
                    case 1:
                        PrintHeader("INVENTORY SUMMARY");
                        Run(() =>
                        {
                            var (total, qty, value) = _inventoryService.GetSummary();
                            PrintInfo($"Total Active Products : {total}");
                            PrintInfo($"Total Units in Stock  : {qty:N0}");
                            PrintInfo($"Total Inventory Value : {value:C}");
                            PrintInfo($"Low Stock Alerts      : {_inventoryService.GetLowStock().Count}");
                        });
                        Pause();
                        break;

                    case 2:
                        PrintHeader("TRANSACTION HISTORY");
                        Run(() =>
                        {
                            Console.WriteLine("  [1] Recent 50  [2] By Product  [3] By Date Range");
                            int opt = ReadInt("Choice", 1, 3);
                            List<Transaction> txns;

                            if (opt == 1)
                            {
                                txns = _transactionService.GetRecent(50);
                            }
                            else if (opt == 2)
                            {
                                int pid = ReadInt("Product ID", 1);
                                txns = _transactionService.GetByProduct(pid);
                            }
                            else
                            {
                                DateTime from = ReadDate("Start date (yyyy-MM-dd)");
                                DateTime to   = ReadDate("End date   (yyyy-MM-dd)");
                                txns = _transactionService.GetByDateRange(from, to);
                            }

                            if (txns.Count == 0) { PrintWarning("No transactions found."); return; }

                            PrintTableHeader(("ID",6),("Date",18),("Type",6),("Product",24),("Qty",8),("Notes",20));
                            foreach (var t in txns)
                            {
                                Console.ForegroundColor = t.Type == TransactionType.IN ? ConsoleColor.Green : ConsoleColor.Red;
                                PrintRow((t.TransactionId.ToString(),6),(t.TransactionDate.ToString("yyyy-MM-dd HH:mm"),18),
                                         (t.Type.ToString(),6),(t.ProductName,24),(t.Quantity.ToString(),8),(t.Notes,20));
                                Console.ResetColor();
                            }
                        });
                        Pause();
                        break;
                }
            }
        }

        
        //  CONSOLE HELPERS  (all private/static)
      
        static void PrintHeader(string title)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  ╔══════════════════════════════════════════════════╗");
            Console.WriteLine($"  ║  {title.PadRight(48)}║");
            Console.WriteLine("  ╚══════════════════════════════════════════════════╝\n");
            Console.ResetColor();
        }

        static void PrintSuccess(string msg) { Console.ForegroundColor = ConsoleColor.Green;  Console.WriteLine($"\n  ✔ {msg}"); Console.ResetColor(); }
        static void PrintError(string msg)   { Console.ForegroundColor = ConsoleColor.Red;    Console.WriteLine($"\n  ✖ {msg}"); Console.ResetColor(); }
        static void PrintWarning(string msg) { Console.ForegroundColor = ConsoleColor.Yellow; Console.WriteLine($"\n  ⚠ {msg}"); Console.ResetColor(); }
        static void PrintInfo(string msg)    { Console.ForegroundColor = ConsoleColor.Gray;   Console.WriteLine($"  {msg}");    Console.ResetColor(); }

        static void PrintTableHeader(params (string h, int w)[] cols)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  ");
            foreach (var (h, w) in cols) Console.Write(h.PadRight(w + 2));
            Console.WriteLine();
            Console.Write("  ");
            foreach (var (_, w) in cols) Console.Write(new string('-', w + 2));
            Console.WriteLine();
            Console.ResetColor();
        }

        static void PrintRow(params (string v, int w)[] cols)
        {
            Console.Write("  ");
            foreach (var (v, w) in cols)
            {
                string s = (v ?? "").Length > w ? (v ?? "").Substring(0, w - 1) + "…" : (v ?? "");
                Console.Write(s.PadRight(w + 2));
            }
            Console.WriteLine();
        }

        static void Pause()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("\n  Press any key to continue...");
            Console.ResetColor();
            Console.ReadKey(true);
        }

        static string ReadString(string prompt)
        {
            Console.Write($"  {prompt}: ");
            return Console.ReadLine()?.Trim() ?? "";
        }

        static string ReadStringOptional(string prompt, string fallback)
        {
            Console.Write($"  {prompt}: ");
            string input = Console.ReadLine()?.Trim() ?? "";
            return string.IsNullOrWhiteSpace(input) ? fallback : input;
        }

        static int ReadInt(string prompt, int min = int.MinValue, int max = int.MaxValue)
        {
            while (true)
            {
                Console.Write($"  {prompt}: ");
                if (int.TryParse(Console.ReadLine(), out int v) && v >= min && v <= max) return v;
                PrintError($"Please enter a number between {min} and {max}.");
            }
        }

        static decimal ReadDecimal(string prompt, decimal min = 0)
        {
            while (true)
            {
                Console.Write($"  {prompt}: ");
                if (decimal.TryParse(Console.ReadLine(), out decimal v) && v >= min) return v;
                PrintError($"Please enter a valid number >= {min}.");
            }
        }

        static DateTime ReadDate(string prompt)
        {
            while (true)
            {
                Console.Write($"  {prompt}: ");
                if (DateTime.TryParse(Console.ReadLine(), out DateTime d)) return d;
                PrintError("Invalid date. Use yyyy-MM-dd format.");
            }
        }

        static bool Confirm(string prompt)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"\n  {prompt} (y/n): ");
            Console.ResetColor();
            return (Console.ReadLine()?.Trim().ToLower() ?? "") == "y";
        }

        // Wraps any action and catches all custom exceptions
        static void Run(Action action)
        {
            try { action(); }
            catch (ProductNotFoundException  ex) { PrintError(ex.Message); }
            catch (CategoryNotFoundException ex) { PrintError(ex.Message); }
            catch (InsufficientStockException ex) { PrintError(ex.Message); }
            catch (DuplicateSKUException      ex) { PrintError(ex.Message); }
            catch (InvalidInputException      ex) { PrintError(ex.Message); }
            catch (DatabaseException          ex) { PrintError($"Database error: {ex.Message}"); }
            catch (Exception                  ex) { PrintError($"Unexpected error: {ex.Message}"); }
        }
    }
}