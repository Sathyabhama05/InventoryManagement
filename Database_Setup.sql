CREATE DATABASE InventoryDB;
GO

USE InventoryDB;
GO

-- 1. Categories
CREATE TABLE Categories (
    CategoryId  INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255) DEFAULT '',
    CreatedAt   DATETIME      DEFAULT GETDATE(),
    IsActive    BIT           DEFAULT 1
);

-- 2. Products
CREATE TABLE Products (
    ProductId   INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(150) NOT NULL,
    SKU         NVARCHAR(50)  NOT NULL UNIQUE,
    Description NVARCHAR(500) DEFAULT '',
    Price       DECIMAL(18,2) NOT NULL,
    CategoryId  INT FOREIGN KEY REFERENCES Categories(CategoryId),
    CreatedAt   DATETIME      DEFAULT GETDATE(),
    UpdatedAt   DATETIME      DEFAULT GETDATE(),
    IsActive    BIT           DEFAULT 1
);

-- 3. Inventory (one row per product)
CREATE TABLE Inventory (
    InventoryId   INT IDENTITY(1,1) PRIMARY KEY,
    ProductId     INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId),
    Quantity      INT NOT NULL DEFAULT 0 CHECK (Quantity >= 0),
    MinStockLevel INT NOT NULL DEFAULT 5,
    LastUpdated   DATETIME     DEFAULT GETDATE()
);

-- 4. Transactions (log of every stock-in / stock-out)
CREATE TABLE Transactions (
    TransactionId   INT IDENTITY(1,1) PRIMARY KEY,
    ProductId       INT NOT NULL FOREIGN KEY REFERENCES Products(ProductId),
    TransactionType NVARCHAR(3)  NOT NULL CHECK (TransactionType IN ('IN','OUT')),
    Quantity        INT NOT NULL CHECK (Quantity > 0),
    Notes           NVARCHAR(500) DEFAULT '',
    TransactionDate DATETIME DEFAULT GETDATE()
);
GO

-- Seed 5 starter categories
INSERT INTO Categories (Name, Description) VALUES
('Electronics',      'Electronic devices and accessories'),
('Clothing',         'Apparel and fashion items'),
('Food & Beverages', 'Consumable food products'),
('Office Supplies',  'Office and stationery items'),
('Tools & Hardware', 'Tools and hardware equipment');
GO

PRINT 'Database setup complete!';