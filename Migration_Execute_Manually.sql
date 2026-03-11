-- =============================================
-- Diemaking 系统数据库迁移脚本
-- 版本: 2026-03-11
-- 说明: 添加刀模费用字段、改刀记录表、系统配置表
-- =============================================

-- =============================================
-- 1. 扩展 DieInfo 表 - 添加费用相关字段
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'WorkOrderNo')
BEGIN
    ALTER TABLE DieInfo ADD WorkOrderNo NVARCHAR(50) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'KnifeLengthM')
BEGIN
    ALTER TABLE DieInfo ADD KnifeLengthM DECIMAL(18, 4) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'KnifeMarkLengthM')
BEGIN
    ALTER TABLE DieInfo ADD KnifeMarkLengthM DECIMAL(18, 4) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'BoardFeeUnitPrice')
BEGIN
    ALTER TABLE DieInfo ADD BoardFeeUnitPrice DECIMAL(18, 2) NOT NULL DEFAULT(90);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'BoardFee')
BEGIN
    ALTER TABLE DieInfo ADD BoardFee DECIMAL(18, 2) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'ProductionUnitPrice')
BEGIN
    ALTER TABLE DieInfo ADD ProductionUnitPrice DECIMAL(18, 2) NOT NULL DEFAULT(8);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'ProductionFee')
BEGIN
    ALTER TABLE DieInfo ADD ProductionFee DECIMAL(18, 2) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'DesignUnitPrice')
BEGIN
    ALTER TABLE DieInfo ADD DesignUnitPrice DECIMAL(18, 2) NOT NULL DEFAULT(70);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'DesignFee')
BEGIN
    ALTER TABLE DieInfo ADD DesignFee DECIMAL(18, 2) NULL;
END
GO

-- =============================================
-- 2. 创建改刀记录表
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DieModificationRecord')
BEGIN
    CREATE TABLE DieModificationRecord (
        ModificationID INT IDENTITY(1,1) PRIMARY KEY,
        DieID INT NOT NULL,
        DieCode NVARCHAR(50) NOT NULL,
        CustomerName NVARCHAR(100) NULL,
        ProductName NVARCHAR(100) NULL,
        ModificationAmount DECIMAL(18, 2) NOT NULL,
        ModificationTime DATETIME NOT NULL DEFAULT(GETDATE()),
        ModifiedBy NVARCHAR(50) NOT NULL,
        Reason NVARCHAR(500) NULL,
        Remark NVARCHAR(500) NULL,
        CreateTime DATETIME NOT NULL DEFAULT(GETDATE()),
        CONSTRAINT FK_DieModificationRecord_DieInfo FOREIGN KEY (DieID) REFERENCES DieInfo(DieID)
    );
    
    CREATE INDEX IX_DieModificationRecord_DieID ON DieModificationRecord(DieID);
    CREATE INDEX IX_DieModificationRecord_ModificationTime ON DieModificationRecord(ModificationTime);
END
GO

-- =============================================
-- 3. 创建系统配置表（用于存储单价预设值）
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemConfig')
BEGIN
    CREATE TABLE SystemConfig (
        ConfigID INT IDENTITY(1,1) PRIMARY KEY,
        ConfigKey NVARCHAR(100) NOT NULL UNIQUE,
        ConfigValue NVARCHAR(500) NOT NULL,
        ConfigType NVARCHAR(50) NULL, -- string, int, decimal, bool, json
        Description NVARCHAR(200) NULL,
        UpdateTime DATETIME NOT NULL DEFAULT(GETDATE()),
        UpdateUser NVARCHAR(50) NULL
    );
    
    -- 插入默认配置
    INSERT INTO SystemConfig (ConfigKey, ConfigValue, ConfigType, Description) VALUES
    ('BoardFeeUnitPrice', '90', 'decimal', '板费单价默认值（元/平方米）'),
    ('ProductionUnitPrice', '8', 'decimal', '制作单价默认值（元/平方米）'),
    ('DesignUnitPrice', '70', 'decimal', '设计单价默认值（元/平方米）'),
    ('DefaultProcesses', '绘图,割板,弯刀,装刀,贴泡沫', 'string', '默认工序列表，逗号分隔');
END
GO

-- =============================================
-- 4. 创建扫码报工记录表
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScanReportRecord')
BEGIN
    CREATE TABLE ScanReportRecord (
        RecordID INT IDENTITY(1,1) PRIMARY KEY,
        WorkOrderNo NVARCHAR(50) NOT NULL,
        DieID INT NULL,
        ProcessID INT NULL,
        ProcessName NVARCHAR(50) NULL,
        ScanTime DATETIME NOT NULL DEFAULT(GETDATE()),
        OperatorNo NVARCHAR(50) NULL,
        OperatorName NVARCHAR(50) NULL,
        DeviceInfo NVARCHAR(200) NULL,
        Status INT NOT NULL DEFAULT(0), -- 0:成功, 1:失败
        ErrorMessage NVARCHAR(500) NULL,
        CreateTime DATETIME NOT NULL DEFAULT(GETDATE())
    );
    
    CREATE INDEX IX_ScanReportRecord_WorkOrderNo ON ScanReportRecord(WorkOrderNo);
    CREATE INDEX IX_ScanReportRecord_ScanTime ON ScanReportRecord(ScanTime);
END
GO

-- =============================================
-- 5. 更新现有数据 - 计算费用字段
-- =============================================
-- 更新现有刀模记录的费用（根据毛坯尺寸计算）
UPDATE DieInfo
SET 
    BoardFee = CASE 
        WHEN BlankLength IS NOT NULL AND BlankWidth IS NOT NULL 
        THEN (BlankLength / 1000.0) * (BlankWidth / 1000.0) * ISNULL(BoardFeeUnitPrice, 90)
        ELSE NULL 
    END,
    ProductionFee = CASE 
        WHEN BlankLength IS NOT NULL AND BlankWidth IS NOT NULL 
        THEN (BlankLength / 1000.0) * (BlankWidth / 1000.0) * ISNULL(ProductionUnitPrice, 8)
        ELSE NULL 
    END,
    DesignFee = CASE 
        WHEN BlankLength IS NOT NULL AND BlankWidth IS NOT NULL 
        THEN (BlankLength / 1000.0) * (BlankWidth / 1000.0) * ISNULL(DesignUnitPrice, 70)
        ELSE NULL 
    END
WHERE BoardFee IS NULL OR ProductionFee IS NULL OR DesignFee IS NULL;
GO

PRINT '数据库迁移完成！';
GO
