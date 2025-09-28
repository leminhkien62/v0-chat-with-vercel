-- WMS Database Creation Script
-- SQL Server 2016+ Required

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'WMS_Database')
BEGIN
    CREATE DATABASE WMS_Database
    COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

USE WMS_Database;
GO

-- Enable snapshot isolation for better concurrency
ALTER DATABASE WMS_Database SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE WMS_Database SET READ_COMMITTED_SNAPSHOT ON;
GO
