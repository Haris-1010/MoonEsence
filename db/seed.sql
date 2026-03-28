-- EcommerceClean seed data
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

-- Admin user
-- Username: admin
-- Password: Admin123!
INSERT INTO dbo.Users (Username, Email, PasswordHash, PasswordSalt, Role, CreatedAt)
VALUES
(
  'admin',
  'admin@local',
  'FM7NUtqDv1KMJhYFzEc/KMjxhw2Trq1pNGijcTHXa/k=',
  'jar2Qz1S6qURUCitAYn6sQ==',
  0, -- UserRole.Admin
  SYSUTCDATETIME()
);

-- Categories
INSERT INTO dbo.Categories (Name, Description, IsActive, CreatedAt)
VALUES
('Electronics', 'Gadgets and devices', 1, SYSUTCDATETIME()),
('Books', 'Readable and educational', 1, SYSUTCDATETIME()),
('Clothing', 'Apparel and accessories', 1, SYSUTCDATETIME());

-- SubCategories
INSERT INTO dbo.SubCategories (CategoryId, Name, Description, IsActive, CreatedAt)
SELECT c.Id, sc.Name, sc.Description, 1, SYSUTCDATETIME()
FROM dbo.Categories c
CROSS APPLY (VALUES
    ('Electronics', 'Mobiles', 'Phones and handheld devices'),
    ('Electronics', 'Audio', 'Headphones and speakers'),
    ('Books', 'Programming', 'Software development and code'),
    ('Clothing', 'T-Shirts', 'Men and women t-shirts')
) AS sc(CategoryName, Name, Description)
WHERE c.Name = sc.CategoryName;

-- Sample products (no real images yet; imageUrl can be updated via Admin UI)
INSERT INTO dbo.Products (Name, Description, Price, SubCategoryId, ImageUrl, IsActive, CreatedAt, UpdatedAt)
VALUES
('Smartphone', 'A modern smartphone with great performance.', 599.99, (SELECT TOP 1 Id FROM dbo.SubCategories WHERE Name='Mobiles' AND CategoryId=(SELECT TOP 1 Id FROM dbo.Categories WHERE Name='Electronics')), '/uploads/products/placeholder.svg', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
('Wireless Headphones', 'Comfortable sound for everyday listening.', 129.50, (SELECT TOP 1 Id FROM dbo.SubCategories WHERE Name='Audio' AND CategoryId=(SELECT TOP 1 Id FROM dbo.Categories WHERE Name='Electronics')), '/uploads/products/placeholder.svg', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
('Programming Book', 'Learn fundamentals and build real projects.', 39.00, (SELECT TOP 1 Id FROM dbo.SubCategories WHERE Name='Programming' AND CategoryId=(SELECT TOP 1 Id FROM dbo.Categories WHERE Name='Books')), '/uploads/products/placeholder.svg', 1, SYSUTCDATETIME(), SYSUTCDATETIME()),
('T-Shirt', 'Soft cotton t-shirt.', 19.99, (SELECT TOP 1 Id FROM dbo.SubCategories WHERE Name='T-Shirts' AND CategoryId=(SELECT TOP 1 Id FROM dbo.Categories WHERE Name='Clothing')), '/uploads/products/placeholder.svg', 1, SYSUTCDATETIME(), SYSUTCDATETIME());

COMMIT;
GO

