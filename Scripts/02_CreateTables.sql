-- WMS Tables Creation Script
USE WMS_Database;
GO

-- AspNet Identity Tables
CREATE TABLE [dbo].[AspNetRoles] (
    [Id] NVARCHAR(128) NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(256) NOT NULL
);

CREATE TABLE [dbo].[AspNetUsers] (
    [Id] NVARCHAR(128) NOT NULL PRIMARY KEY,
    [Email] NVARCHAR(256),
    [EmailConfirmed] BIT NOT NULL,
    [PasswordHash] NVARCHAR(MAX),
    [SecurityStamp] NVARCHAR(MAX),
    [PhoneNumber] NVARCHAR(MAX),
    [PhoneNumberConfirmed] BIT NOT NULL,
    [TwoFactorEnabled] BIT NOT NULL,
    [LockoutEndDateUtc] DATETIME,
    [LockoutEnabled] BIT NOT NULL,
    [AccessFailedCount] INT NOT NULL,
    [UserName] NVARCHAR(256) NOT NULL,
    [FullName] NVARCHAR(100),
    [DeptId] NVARCHAR(50),
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[AspNetUserRoles] (
    [UserId] NVARCHAR(128) NOT NULL,
    [RoleId] NVARCHAR(128) NOT NULL,
    PRIMARY KEY ([UserId], [RoleId]),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles]([Id]) ON DELETE CASCADE
);

-- Department Tables
CREATE TABLE [dbo].[Departments] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [DeptId] NVARCHAR(50) NOT NULL UNIQUE,
    [Name] NVARCHAR(100) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    [IsActive] BIT DEFAULT 1
);

-- Warehouse Tables
CREATE TABLE [dbo].[Warehouses] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Code] NVARCHAR(20) NOT NULL UNIQUE,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500),
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(128),
    [UpdatedAt] DATETIME2,
    [UpdatedBy] NVARCHAR(128),
    [IsActive] BIT DEFAULT 1
);

CREATE TABLE [dbo].[Locations] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Code] NVARCHAR(50) NOT NULL UNIQUE,
    [WarehouseId] INT NOT NULL,
    [Zone] NVARCHAR(10),
    [Aisle] NVARCHAR(10),
    [Rack] NVARCHAR(10),
    [Bin] NVARCHAR(10),
    [LocationType] NVARCHAR(20) DEFAULT 'Storage',
    [MaxWeight] DECIMAL(10,2),
    [MaxVolume] DECIMAL(10,2),
    [IsLocked] BIT DEFAULT 0,
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(128),
    [IsActive] BIT DEFAULT 1,
    FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses]([Id])
);

-- Item Tables
CREATE TABLE [dbo].[Items] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Code] NVARCHAR(50) NOT NULL UNIQUE,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(500),
    [UoM] NVARCHAR(10) NOT NULL DEFAULT 'EA',
    [IsConsumable] BIT DEFAULT 1,
    [Category] NVARCHAR(50),
    [ImageUrl] NVARCHAR(500),
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(128),
    [UpdatedAt] DATETIME2,
    [UpdatedBy] NVARCHAR(128),
    [IsActive] BIT DEFAULT 1
);

-- Stock Tables
CREATE TABLE [dbo].[Stock] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [ItemId] INT NOT NULL,
    [LocationId] INT NOT NULL,
    [QtyOnHand] DECIMAL(10,2) NOT NULL DEFAULT 0,
    [QtyAllocated] DECIMAL(10,2) NOT NULL DEFAULT 0,
    [QtyAvailable] AS ([QtyOnHand] - [QtyAllocated]),
    [Lot] NVARCHAR(50),
    [Serial] NVARCHAR(50),
    [ExpiryDate] DATE,
    [ReceivedDate] DATETIME2,
    [Lpn] NVARCHAR(50),
    [LastUpdated] DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY ([ItemId]) REFERENCES [Items]([Id]),
    FOREIGN KEY ([LocationId]) REFERENCES [Locations]([Id])
);

-- Transaction Tables
CREATE TABLE [dbo].[Transactions] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [TransactionDate] DATETIME2 DEFAULT GETDATE(),
    [Type] NVARCHAR(20) NOT NULL, -- Receive, Issue, Move, CountAdjust
    [ItemId] INT NOT NULL,
    [FromLocationId] INT,
    [ToLocationId] INT,
    [Quantity] DECIMAL(10,2) NOT NULL,
    [UoM] NVARCHAR(10) NOT NULL,
    [Lot] NVARCHAR(50),
    [Serial] NVARCHAR(50),
    [ExpiryDate] DATE,
    [RefNo] NVARCHAR(50),
    [Lpn] NVARCHAR(50),
    [Notes] NVARCHAR(500),
    [CreatedBy] NVARCHAR(128) NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY ([ItemId]) REFERENCES [Items]([Id]),
    FOREIGN KEY ([FromLocationId]) REFERENCES [Locations]([Id]),
    FOREIGN KEY ([ToLocationId]) REFERENCES [Locations]([Id])
);

-- Purchase Order Tables
CREATE TABLE [dbo].[PoHeaders] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [PoNo] NVARCHAR(50) NOT NULL UNIQUE,
    [PoDate] DATE NOT NULL,
    [Supplier] NVARCHAR(200),
    [Status] NVARCHAR(20) DEFAULT 'Open',
    [TotalAmount] DECIMAL(15,2),
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(128)
);

CREATE TABLE [dbo].[PoLines] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [PoHeaderId] INT NOT NULL,
    [LineNo] INT NOT NULL,
    [ItemId] INT NOT NULL,
    [QtyOrdered] DECIMAL(10,2) NOT NULL,
    [QtyReceived] DECIMAL(10,2) DEFAULT 0,
    [QtyRemaining] AS ([QtyOrdered] - [QtyReceived]),
    [UnitPrice] DECIMAL(10,2),
    [LineAmount] AS ([QtyOrdered] * [UnitPrice]),
    [DueDate] DATE,
    FOREIGN KEY ([PoHeaderId]) REFERENCES [PoHeaders]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([ItemId]) REFERENCES [Items]([Id])
);

-- LPN Tables
CREATE TABLE [dbo].[Lpns] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [LpnCode] NVARCHAR(50) NOT NULL UNIQUE,
    [LocationId] INT,
    [Status] NVARCHAR(20) DEFAULT 'Open', -- Open, Closed, Shipped
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(128),
    [ClosedAt] DATETIME2,
    [ClosedBy] NVARCHAR(128),
    [Notes] NVARCHAR(500),
    FOREIGN KEY ([LocationId]) REFERENCES [Locations]([Id])
);

CREATE TABLE [dbo].[LpnItems] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [LpnId] INT NOT NULL,
    [ItemId] INT NOT NULL,
    [Quantity] DECIMAL(10,2) NOT NULL,
    [UoM] NVARCHAR(10) NOT NULL,
    [Lot] NVARCHAR(50),
    [Serial] NVARCHAR(50),
    [ExpiryDate] DATE,
    [PoNo] NVARCHAR(50),
    [PoLineId] INT,
    FOREIGN KEY ([LpnId]) REFERENCES [Lpns]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([ItemId]) REFERENCES [Items]([Id]),
    FOREIGN KEY ([PoLineId]) REFERENCES [PoLines]([Id])
);

-- Request Tables
CREATE TABLE [dbo].[Requests] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [RequestNo] NVARCHAR(50) NOT NULL UNIQUE,
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    [DeptId] NVARCHAR(50) NOT NULL,
    [RequesterName] NVARCHAR(100) NOT NULL,
    [RequesterEmail] NVARCHAR(200),
    [ItemId] INT NOT NULL,
    [QtyRequested] DECIMAL(10,2) NOT NULL,
    [QtyIssued] DECIMAL(10,2) DEFAULT 0,
    [UoM] NVARCHAR(10) NOT NULL,
    [Priority] NVARCHAR(10) DEFAULT 'Normal', -- Low, Normal, High, Urgent
    [Status] NVARCHAR(20) DEFAULT 'New', -- New, InProgress, Completed, Rejected
    [Notes] NVARCHAR(500),
    [ProcessedBy] NVARCHAR(128),
    [ProcessedAt] DATETIME2,
    [CompletedAt] DATETIME2,
    FOREIGN KEY ([ItemId]) REFERENCES [Items]([Id])
);

-- Replenishment Rules
CREATE TABLE [dbo].[ReplenishmentRules] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [ItemId] INT NOT NULL,
    [WarehouseId] INT NOT NULL,
    [SafetyStock] DECIMAL(10,2) NOT NULL DEFAULT 0,
    [MaxStock] DECIMAL(10,2) NOT NULL DEFAULT 0,
    [MOQ] DECIMAL(10,2) NOT NULL DEFAULT 1,
    [LeadTimeDays] INT NOT NULL DEFAULT 7,
    [IsActive] BIT DEFAULT 1,
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2,
    FOREIGN KEY ([ItemId]) REFERENCES [Items]([Id]),
    FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses]([Id]),
    UNIQUE ([ItemId], [WarehouseId])
);

-- Department Warehouse Mapping
CREATE TABLE [dbo].[DepartmentWarehouses] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [DeptId] NVARCHAR(50) NOT NULL,
    [WarehouseId] INT NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(128),
    FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses]([Id]),
    UNIQUE ([DeptId], [WarehouseId])
);

-- User Warehouse Access
CREATE TABLE [dbo].[UserWarehouses] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [UserId] NVARCHAR(128) NOT NULL,
    [WarehouseId] INT NOT NULL,
    [CreatedAt] DATETIME2 DEFAULT GETDATE(),
    [CreatedBy] NVARCHAR(128),
    FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]) ON DELETE CASCADE,
    FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses]([Id]),
    UNIQUE ([UserId], [WarehouseId])
);

-- System Audit Log
CREATE TABLE [dbo].[SystemLogs] (
    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Timestamp] DATETIME2 DEFAULT GETDATE(),
    [UserName] NVARCHAR(256),
    [IpAddress] NVARCHAR(45),
    [Controller] NVARCHAR(100),
    [Action] NVARCHAR(100),
    [HttpMethod] NVARCHAR(10),
    [Url] NVARCHAR(500),
    [Parameters] NVARCHAR(MAX),
    [Result] NVARCHAR(20),
    [DurationMs] INT,
    [Exception] NVARCHAR(MAX)
);

-- Create Indexes for Performance
CREATE INDEX IX_Stock_ItemId_LocationId ON [Stock] ([ItemId], [LocationId]);
CREATE INDEX IX_Stock_LocationId ON [Stock] ([LocationId]);
CREATE INDEX IX_Transactions_ItemId ON [Transactions] ([ItemId]);
CREATE INDEX IX_Transactions_Date ON [Transactions] ([TransactionDate]);
CREATE INDEX IX_Transactions_Type ON [Transactions] ([Type]);
CREATE INDEX IX_Locations_WarehouseId ON [Locations] ([WarehouseId]);
CREATE INDEX IX_PoLines_PoHeaderId ON [PoLines] ([PoHeaderId]);
CREATE INDEX IX_LpnItems_LpnId ON [LpnItems] ([LpnId]);
CREATE INDEX IX_Requests_Status ON [Requests] ([Status]);
CREATE INDEX IX_Requests_DeptId ON [Requests] ([DeptId]);
CREATE INDEX IX_SystemLogs_Timestamp ON [SystemLogs] ([Timestamp]);
CREATE INDEX IX_SystemLogs_UserName ON [SystemLogs] ([UserName]);

PRINT 'Database tables created successfully!';
GO
