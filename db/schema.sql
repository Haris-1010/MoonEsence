-- EcommerceClean - normalized schema for SQL Server
-- Execute in order: schema.sql then seed.sql

SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.OrderItems','U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders','U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Products','U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Categories','U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.SubCategories','U') IS NOT NULL DROP TABLE dbo.SubCategories;
IF OBJECT_ID('dbo.Users','U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Users
(
    Id            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username      NVARCHAR(50)  NOT NULL,
    Email         NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    PasswordSalt NVARCHAR(255) NOT NULL,
    Role          INT NOT NULL, -- 0=Admin, 1=Client (see Ecommerce.Domain.Enums.UserRole)
    CreatedAt     DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.Categories
(
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive    BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
    CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Categories_Name UNIQUE (Name)
);
GO

CREATE TABLE dbo.SubCategories
(
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SubCategories PRIMARY KEY,
    CategoryId  INT NOT NULL CONSTRAINT FK_SubCategories_Categories FOREIGN KEY REFERENCES dbo.Categories(Id),
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsActive    BIT NOT NULL CONSTRAINT DF_SubCategories_IsActive DEFAULT (1),
    CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_SubCategories_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_SubCategories_Category_Name UNIQUE (CategoryId, Name)
);
GO

CREATE INDEX IX_SubCategories_CategoryId ON dbo.SubCategories(CategoryId);
GO

CREATE TABLE dbo.Products
(
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Name        NVARCHAR(120) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Price       DECIMAL(18,2) NOT NULL,
    SubCategoryId INT NOT NULL CONSTRAINT FK_Products_SubCategories FOREIGN KEY REFERENCES dbo.SubCategories(Id),
    ImageUrl    NVARCHAR(500) NOT NULL CONSTRAINT DF_Products_ImageUrl DEFAULT (''),
    IsActive    BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
    CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2 NOT NULL CONSTRAINT DF_Products_UpdatedAt DEFAULT SYSUTCDATETIME()
);
GO

CREATE INDEX IX_Products_SubCategoryId ON dbo.Products(SubCategoryId);
CREATE INDEX IX_Products_IsActive ON dbo.Products(IsActive);
GO

CREATE TABLE dbo.Orders
(
    Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    UserId      INT NULL CONSTRAINT FK_Orders_Users FOREIGN KEY REFERENCES dbo.Users(Id),
    Status      INT NOT NULL, -- see Ecommerce.Domain.Enums.OrderStatus
    PaymentMethod INT NOT NULL, -- see Ecommerce.Domain.Enums.PaymentMethod (COD only for now)
    CreatedAt   DATETIME2 NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   DATETIME2 NOT NULL CONSTRAINT DF_Orders_UpdatedAt DEFAULT SYSUTCDATETIME(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    FullName    NVARCHAR(120) NOT NULL,
    Phone       NVARCHAR(30) NOT NULL,
    Email       NVARCHAR(100) NULL,
    City        NVARCHAR(80) NOT NULL,
    AddressLine1 NVARCHAR(200) NOT NULL,
    AddressLine2 NVARCHAR(200) NULL,
    ZipCode     NVARCHAR(20) NOT NULL,
    AccessCode  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Orders_AccessCode DEFAULT NEWID()
);
GO

CREATE INDEX IX_Orders_UserId_CreatedAt ON dbo.Orders(UserId, CreatedAt DESC);
CREATE INDEX IX_Orders_Status_CreatedAt ON dbo.Orders(Status, CreatedAt DESC);
CREATE INDEX IX_Orders_AccessCode ON dbo.Orders(AccessCode);
GO

CREATE TABLE dbo.OrderItems
(
    Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
    OrderId      BIGINT NOT NULL CONSTRAINT FK_OrderItems_Orders FOREIGN KEY REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    ProductId    INT NOT NULL CONSTRAINT FK_OrderItems_Products FOREIGN KEY REFERENCES dbo.Products(Id),
    Quantity     INT NOT NULL,
    UnitPrice    DECIMAL(18,2) NOT NULL,
    LineTotal    DECIMAL(18,2) NOT NULL,
    CONSTRAINT CK_OrderItems_Quantity_Positive CHECK (Quantity > 0)
);
GO

CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
GO

