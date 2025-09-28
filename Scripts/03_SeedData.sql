-- WMS Seed Data Script
USE WMS_Database;
GO

-- Insert Default Roles
INSERT INTO [AspNetRoles] ([Id], [Name]) VALUES 
('1', 'Admin'),
('2', 'Store'),
('3', 'Manager'),
('4', 'Viewer');

-- Insert Default Admin User (Password: Admin@123)
INSERT INTO [AspNetUsers] ([Id], [UserName], [Email], [EmailConfirmed], [PasswordHash], [SecurityStamp], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount], [FullName], [DeptId], [IsActive])
VALUES ('admin-user-id', 'admin@wms.com', 'admin@wms.com', 1, 'AQAAAAEAACcQAAAAEJ7QQaXQQaXQQaXQQaXQQaXQQaXQQaXQQaXQQaXQQaXQQaXQQaXQQaXQQaXQQaXQ==', 'SECURITY-STAMP-HERE', 0, 0, 1, 0, 'System Administrator', 'IT', 1);

-- Assign Admin Role
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId]) VALUES ('admin-user-id', '1');

-- Insert Sample Departments
INSERT INTO [Departments] ([DeptId], [Name]) VALUES 
('IT', 'Information Technology'),
('HR', 'Human Resources'),
('FIN', 'Finance'),
('OPS', 'Operations'),
('MFG', 'Manufacturing'),
('QA', 'Quality Assurance');

-- Insert Sample Warehouses
INSERT INTO [Warehouses] ([Code], [Name], [Description], [CreatedBy]) VALUES 
('WH01', 'Main Warehouse', 'Primary storage facility', 'admin-user-id'),
('WH02', 'Raw Materials', 'Raw materials storage', 'admin-user-id'),
('WH03', 'Finished Goods', 'Finished products storage', 'admin-user-id'),
('WH04', 'Consumables', 'Office and maintenance supplies', 'admin-user-id');

-- Insert Sample Locations
DECLARE @WH01_Id INT = (SELECT Id FROM Warehouses WHERE Code = 'WH01');
DECLARE @WH02_Id INT = (SELECT Id FROM Warehouses WHERE Code = 'WH02');
DECLARE @WH03_Id INT = (SELECT Id FROM Warehouses WHERE Code = 'WH03');
DECLARE @WH04_Id INT = (SELECT Id FROM Warehouses WHERE Code = 'WH04');

-- WH01 Locations
INSERT INTO [Locations] ([Code], [WarehouseId], [Zone], [Aisle], [Rack], [Bin], [CreatedBy]) VALUES 
('WH01-A-01-01-01', @WH01_Id, 'A', '01', '01', '01', 'admin-user-id'),
('WH01-A-01-01-02', @WH01_Id, 'A', '01', '01', '02', 'admin-user-id'),
('WH01-A-01-02-01', @WH01_Id, 'A', '01', '02', '01', 'admin-user-id'),
('WH01-A-01-02-02', @WH01_Id, 'A', '01', '02', '02', 'admin-user-id'),
('WH01-B-01-01-01', @WH01_Id, 'B', '01', '01', '01', 'admin-user-id'),
('WH01-B-01-01-02', @WH01_Id, 'B', '01', '01', '02', 'admin-user-id'),
('WH01-STAGING', @WH01_Id, 'STG', '00', '00', '00', 'admin-user-id'),
('WH01-SHIPPING', @WH01_Id, 'SHP', '00', '00', '00', 'admin-user-id');

-- WH02 Locations (Raw Materials)
INSERT INTO [Locations] ([Code], [WarehouseId], [Zone], [Aisle], [Rack], [Bin], [CreatedBy]) VALUES 
('WH02-A-01-01-01', @WH02_Id, 'A', '01', '01', '01', 'admin-user-id'),
('WH02-A-01-01-02', @WH02_Id, 'A', '01', '01', '02', 'admin-user-id'),
('WH02-B-01-01-01', @WH02_Id, 'B', '01', '01', '01', 'admin-user-id'),
('WH02-STAGING', @WH02_Id, 'STG', '00', '00', '00', 'admin-user-id');

-- Insert Sample Items
INSERT INTO [Items] ([Code], [Name], [Description], [UoM], [IsConsumable], [Category], [CreatedBy]) VALUES 
('ITM001', 'Office Paper A4', '80gsm white office paper', 'REAM', 1, 'Office Supplies', 'admin-user-id'),
('ITM002', 'Ballpoint Pen Blue', 'Blue ink ballpoint pen', 'EA', 1, 'Office Supplies', 'admin-user-id'),
('ITM003', 'Laptop Battery', 'Replacement laptop battery', 'EA', 0, 'IT Equipment', 'admin-user-id'),
('ITM004', 'Safety Helmet', 'Industrial safety helmet', 'EA', 0, 'Safety Equipment', 'admin-user-id'),
('ITM005', 'Steel Rod 10mm', '10mm diameter steel rod', 'M', 0, 'Raw Materials', 'admin-user-id'),
('ITM006', 'Cleaning Spray', 'Multi-purpose cleaning spray', 'BTL', 1, 'Cleaning Supplies', 'admin-user-id'),
('ITM007', 'USB Cable Type-C', 'USB Type-C charging cable', 'EA', 0, 'IT Equipment', 'admin-user-id'),
('ITM008', 'Work Gloves', 'Industrial work gloves', 'PAIR', 1, 'Safety Equipment', 'admin-user-id');

-- Insert Sample Stock
DECLARE @ITM001_Id INT = (SELECT Id FROM Items WHERE Code = 'ITM001');
DECLARE @ITM002_Id INT = (SELECT Id FROM Items WHERE Code = 'ITM002');
DECLARE @ITM003_Id INT = (SELECT Id FROM Items WHERE Code = 'ITM003');
DECLARE @LOC001_Id INT = (SELECT Id FROM Locations WHERE Code = 'WH01-A-01-01-01');
DECLARE @LOC002_Id INT = (SELECT Id FROM Locations WHERE Code = 'WH01-A-01-01-02');

INSERT INTO [Stock] ([ItemId], [LocationId], [QtyOnHand], [ReceivedDate]) VALUES 
(@ITM001_Id, @LOC001_Id, 50.00, GETDATE()-30),
(@ITM002_Id, @LOC001_Id, 200.00, GETDATE()-25),
(@ITM003_Id, @LOC002_Id, 15.00, GETDATE()-20);

-- Insert Sample Replenishment Rules
INSERT INTO [ReplenishmentRules] ([ItemId], [WarehouseId], [SafetyStock], [MaxStock], [MOQ]) VALUES 
(@ITM001_Id, @WH01_Id, 20.00, 100.00, 10.00),
(@ITM002_Id, @WH01_Id, 50.00, 500.00, 50.00),
(@ITM003_Id, @WH01_Id, 5.00, 25.00, 5.00);

-- Insert Department Warehouse Mappings
INSERT INTO [DepartmentWarehouses] ([DeptId], [WarehouseId], [CreatedBy]) VALUES 
('IT', @WH01_Id, 'admin-user-id'),
('IT', @WH04_Id, 'admin-user-id'),
('OPS', @WH01_Id, 'admin-user-id'),
('OPS', @WH02_Id, 'admin-user-id'),
('OPS', @WH03_Id, 'admin-user-id'),
('MFG', @WH02_Id, 'admin-user-id'),
('MFG', @WH03_Id, 'admin-user-id');

-- Insert Sample Purchase Orders
INSERT INTO [PoHeaders] ([PoNo], [PoDate], [Supplier], [CreatedBy]) VALUES 
('PO2024001', '2024-01-15', 'Office Supplies Co.', 'admin-user-id'),
('PO2024002', '2024-01-20', 'Tech Equipment Ltd.', 'admin-user-id');

DECLARE @PO1_Id INT = (SELECT Id FROM PoHeaders WHERE PoNo = 'PO2024001');
DECLARE @PO2_Id INT = (SELECT Id FROM PoHeaders WHERE PoNo = 'PO2024002');

INSERT INTO [PoLines] ([PoHeaderId], [LineNo], [ItemId], [QtyOrdered], [UnitPrice]) VALUES 
(@PO1_Id, 1, @ITM001_Id, 100.00, 5.50),
(@PO1_Id, 2, @ITM002_Id, 500.00, 0.75),
(@PO2_Id, 1, @ITM003_Id, 20.00, 45.00);

PRINT 'Seed data inserted successfully!';
GO
