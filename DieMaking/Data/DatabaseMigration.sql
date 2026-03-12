-- =============================================
-- DieMaking 数据库迁移脚本
-- 功能：添加新字段和表结构
-- =============================================

-- 1. 检查并添加 DieInfo 表的新字段
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'WorkOrderNo')
BEGIN
    ALTER TABLE DieInfo ADD WorkOrderNo NVARCHAR(50) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'KnifeLengthM')
BEGIN
    ALTER TABLE DieInfo ADD KnifeLengthM DECIMAL(18,4) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'KnifeMarkLengthM')
BEGIN
    ALTER TABLE DieInfo ADD KnifeMarkLengthM DECIMAL(18,4) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'BoardFeeUnitPrice')
BEGIN
    ALTER TABLE DieInfo ADD BoardFeeUnitPrice DECIMAL(18,2) NOT NULL DEFAULT 90;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'BoardFee')
BEGIN
    ALTER TABLE DieInfo ADD BoardFee DECIMAL(18,2) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'ProductionUnitPrice')
BEGIN
    ALTER TABLE DieInfo ADD ProductionUnitPrice DECIMAL(18,2) NOT NULL DEFAULT 8;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'ProductionFee')
BEGIN
    ALTER TABLE DieInfo ADD ProductionFee DECIMAL(18,2) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'DesignUnitPrice')
BEGIN
    ALTER TABLE DieInfo ADD DesignUnitPrice DECIMAL(18,2) NOT NULL DEFAULT 70;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'DesignFee')
BEGIN
    ALTER TABLE DieInfo ADD DesignFee DECIMAL(18,2) NULL;
END

-- 2. 创建改刀记录表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DieModificationRecord')
BEGIN
    CREATE TABLE DieModificationRecord (
        ModificationID INT IDENTITY(1,1) PRIMARY KEY,
        DieID INT NOT NULL,
        DieCode NVARCHAR(50) NOT NULL,
        CustomerName NVARCHAR(100) NULL,
        ProductName NVARCHAR(100) NULL,
        ModificationAmount DECIMAL(18,2) NOT NULL,
        ModificationTime DATETIME NOT NULL DEFAULT GETDATE(),
        ModifiedBy NVARCHAR(50) NOT NULL,
        Reason NVARCHAR(500) NULL,
        Remark NVARCHAR(500) NULL,
        CreateTime DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_DieModificationRecord_DieInfo FOREIGN KEY (DieID) REFERENCES DieInfo(DieID)
    );
    
    CREATE INDEX IX_DieModificationRecord_DieID ON DieModificationRecord(DieID);
    CREATE INDEX IX_DieModificationRecord_ModificationTime ON DieModificationRecord(ModificationTime);
END

-- 3. 创建系统配置表（用于存储单价默认值）
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemConfig')
BEGIN
    CREATE TABLE SystemConfig (
        ConfigID INT IDENTITY(1,1) PRIMARY KEY,
        ConfigKey NVARCHAR(100) NOT NULL UNIQUE,
        ConfigValue NVARCHAR(500) NOT NULL,
        Description NVARCHAR(200) NULL,
        UpdateTime DATETIME NOT NULL DEFAULT GETDATE(),
        UpdateUser NVARCHAR(50) NULL
    );
    
    -- 插入默认配置
    INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description) VALUES
    ('BoardFeeUnitPrice', '90', '板费单价默认值（元/平方米）'),
    ('ProductionUnitPrice', '8', '制作单价默认值（元/平方米）'),
    ('DesignUnitPrice', '70', '设计单价默认值（元/平方米）');
END

-- 5. 添加自动创建库位相关配置（如果不存在）
IF NOT EXISTS (SELECT * FROM SystemConfig WHERE ConfigKey = 'AutoCreateLocation')
BEGIN
    INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description) VALUES
    ('AutoCreateLocation', 'true', '是否自动创建库位（true/false）');
END

IF NOT EXISTS (SELECT * FROM SystemConfig WHERE ConfigKey = 'DefaultLocationArea')
BEGIN
    INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description) VALUES
    ('DefaultLocationArea', 'A', '默认库位区域');
END

IF NOT EXISTS (SELECT * FROM SystemConfig WHERE ConfigKey = 'DefaultLocationShelf')
BEGIN
    INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description) VALUES
    ('DefaultLocationShelf', '01', '默认库位货架号');
END

IF NOT EXISTS (SELECT * FROM SystemConfig WHERE ConfigKey = 'DefaultLocationLayer')
BEGIN
    INSERT INTO SystemConfig (ConfigKey, ConfigValue, Description) VALUES
    ('DefaultLocationLayer', '01', '默认库位层号');
END

PRINT '数据库迁移完成！'

-- 4. 创建扫码报工记录表
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScanReportRecord')
BEGIN
    CREATE TABLE ScanReportRecord (
        RecordID INT IDENTITY(1,1) PRIMARY KEY,
        WorkOrderNo NVARCHAR(50) NOT NULL,
        DieID INT NULL,
        ProcessID INT NULL,
        ProcessName NVARCHAR(50) NULL,
        ScanTime DATETIME NOT NULL DEFAULT GETDATE(),
        ReportType INT NOT NULL DEFAULT 0, -- 0:完成报产
        OperatorNo NVARCHAR(50) NULL,
        OperatorName NVARCHAR(50) NULL,
        DeviceInfo NVARCHAR(200) NULL,
        CreateTime DATETIME NOT NULL DEFAULT GETDATE()
    );
    
    CREATE INDEX IX_ScanReportRecord_WorkOrderNo ON ScanReportRecord(WorkOrderNo);
    CREATE INDEX IX_ScanReportRecord_ScanTime ON ScanReportRecord(ScanTime);
END

PRINT '数据库迁移完成！'
