-- =============================================
-- DieMaking 自动入库功能数据库迁移脚本
-- 功能：支持刀模所有工序完成后自动入库
-- 创建时间: 2026-03-11
-- =============================================

-- =============================================
-- 1. 检查并创建 DM_DieInventory 表（如果不存在）
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DM_DieInventory')
BEGIN
    CREATE TABLE DM_DieInventory (
        InventoryID INT IDENTITY(1,1) PRIMARY KEY,
        DieID INT NOT NULL,
        LocationID INT NULL,
        StorageStatus INT NOT NULL DEFAULT 0, -- 0:在库, 1:借出, 2:报废, 3:维修中
        InStockTime DATETIME NULL,
        LastBorrowTime DATETIME NULL,
        LastReturnTime DATETIME NULL,
        TotalBorrowCount INT NOT NULL DEFAULT 0,
        Remark NVARCHAR(500) NULL,
        UpdateTime DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_DM_DieInventory_DieID FOREIGN KEY (DieID) REFERENCES DieInfo(DieID),
        CONSTRAINT FK_DM_DieInventory_LocationID FOREIGN KEY (LocationID) REFERENCES DM_StorageLocation(LocationID)
    );
    
    -- 创建唯一索引：一个刀模只能有一条在库记录
    CREATE UNIQUE INDEX IX_DM_DieInventory_DieID_Unique ON DM_DieInventory(DieID) WHERE StorageStatus = 0;
    
    -- 创建其他索引
    CREATE INDEX IX_DM_DieInventory_StorageStatus ON DM_DieInventory(StorageStatus);
    CREATE INDEX IX_DM_DieInventory_InStockTime ON DM_DieInventory(InStockTime DESC);
    
    PRINT 'DM_DieInventory 表创建成功！';
END
ELSE
BEGIN
    PRINT 'DM_DieInventory 表已存在，跳过创建。';
END

-- =============================================
-- 2. 检查并创建 DM_StorageLocation 表（如果不存在）
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DM_StorageLocation')
BEGIN
    CREATE TABLE DM_StorageLocation (
        LocationID INT IDENTITY(1,1) PRIMARY KEY,
        LocationCode NVARCHAR(50) NOT NULL UNIQUE,
        Area NVARCHAR(50) NOT NULL,
        ShelfNo NVARCHAR(20) NOT NULL,
        LayerNo NVARCHAR(20) NOT NULL,
        PositionNo NVARCHAR(20) NOT NULL,
        Description NVARCHAR(200) NULL,
        Status INT NOT NULL DEFAULT 0, -- 0:空闲, 1:占用, 2:禁用
        CreateTime DATETIME NOT NULL DEFAULT GETDATE()
    );
    
    -- 创建索引
    CREATE INDEX IX_DM_StorageLocation_Status ON DM_StorageLocation(Status);
    CREATE INDEX IX_DM_StorageLocation_Area ON DM_StorageLocation(Area);
    
    PRINT 'DM_StorageLocation 表创建成功！';
    
    -- 插入示例库位数据
    INSERT INTO DM_StorageLocation (LocationCode, Area, ShelfNo, LayerNo, PositionNo, Description, Status)
    VALUES 
        ('A-01-01-01', 'A区', '01', '01', '01', 'A区1架1层1位', 0),
        ('A-01-01-02', 'A区', '01', '01', '02', 'A区1架1层2位', 0),
        ('A-01-01-03', 'A区', '01', '01', '03', 'A区1架1层3位', 0),
        ('A-01-02-01', 'A区', '01', '02', '01', 'A区1架2层1位', 0),
        ('A-01-02-02', 'A区', '01', '02', '02', 'A区1架2层2位', 0),
        ('B-01-01-01', 'B区', '01', '01', '01', 'B区1架1层1位', 0),
        ('B-01-01-02', 'B区', '01', '01', '02', 'B区1架1层2位', 0),
        ('B-02-01-01', 'B区', '02', '01', '01', 'B区2架1层1位', 0);
    
    PRINT '示例库位数据插入成功！';
END
ELSE
BEGIN
    PRINT 'DM_StorageLocation 表已存在，跳过创建。';
END

-- =============================================
-- 3. 检查 DieInfo 表的 Status 字段
-- =============================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'Status')
BEGIN
    PRINT 'DieInfo.Status 字段已存在。';
    
    -- 检查 Status 字段的默认值
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        IS_NULLABLE,
        COLUMN_DEFAULT
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'DieInfo' AND COLUMN_NAME = 'Status';
END
ELSE
BEGIN
    PRINT '警告：DieInfo.Status 字段不存在，请检查数据库结构！';
END

-- =============================================
-- 4. 检查并创建 DM_DieBorrowRecord 表（借用记录）
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DM_DieBorrowRecord')
BEGIN
    CREATE TABLE DM_DieBorrowRecord (
        BorrowID INT IDENTITY(1,1) PRIMARY KEY,
        DieID INT NOT NULL,
        InventoryID INT NOT NULL,
        BorrowType INT NOT NULL DEFAULT 0, -- 0:内部领用, 1:生产领用, 2:外借, 3:调拨
        BorrowerNo NVARCHAR(50) NOT NULL,
        BorrowerName NVARCHAR(50) NOT NULL,
        BorrowDept NVARCHAR(50) NULL,
        BorrowTime DATETIME NOT NULL DEFAULT GETDATE(),
        ExpectedReturnTime DATETIME NULL,
        ActualReturnTime DATETIME NULL,
        Purpose NVARCHAR(200) NULL,
        Status INT NOT NULL DEFAULT 0, -- 0:借用中, 1:已归还, 2:逾期
        ReturnOperatorNo NVARCHAR(50) NULL,
        ReturnOperatorName NVARCHAR(50) NULL,
        Remark NVARCHAR(500) NULL,
        CreateTime DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_DM_DieBorrowRecord_DieID FOREIGN KEY (DieID) REFERENCES DieInfo(DieID),
        CONSTRAINT FK_DM_DieBorrowRecord_InventoryID FOREIGN KEY (InventoryID) REFERENCES DM_DieInventory(InventoryID)
    );
    
    CREATE INDEX IX_DM_DieBorrowRecord_DieID ON DM_DieBorrowRecord(DieID);
    CREATE INDEX IX_DM_DieBorrowRecord_Status ON DM_DieBorrowRecord(Status);
    CREATE INDEX IX_DM_DieBorrowRecord_BorrowTime ON DM_DieBorrowRecord(BorrowTime DESC);
    
    PRINT 'DM_DieBorrowRecord 表创建成功！';
END
ELSE
BEGIN
    PRINT 'DM_DieBorrowRecord 表已存在，跳过创建。';
END

-- =============================================
-- 5. 检查并创建 DM_DieScrapRecord 表（报废记录）
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DM_DieScrapRecord')
BEGIN
    CREATE TABLE DM_DieScrapRecord (
        ScrapID INT IDENTITY(1,1) PRIMARY KEY,
        DieID INT NOT NULL,
        InventoryID INT NOT NULL,
        ScrapReason NVARCHAR(500) NOT NULL,
        ScrapType NVARCHAR(50) NULL,
        ApplicantNo NVARCHAR(50) NOT NULL,
        ApplicantName NVARCHAR(50) NOT NULL,
        ApplyTime DATETIME NOT NULL DEFAULT GETDATE(),
        AuditorNo NVARCHAR(50) NULL,
        AuditorName NVARCHAR(50) NULL,
        AuditTime DATETIME NULL,
        AuditStatus INT NOT NULL DEFAULT 0, -- 0:待审核, 1:已通过, 2:已拒绝
        AuditRemark NVARCHAR(500) NULL,
        ScrapTime DATETIME NULL,
        CreateTime DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_DM_DieScrapRecord_DieID FOREIGN KEY (DieID) REFERENCES DieInfo(DieID),
        CONSTRAINT FK_DM_DieScrapRecord_InventoryID FOREIGN KEY (InventoryID) REFERENCES DM_DieInventory(InventoryID)
    );
    
    CREATE INDEX IX_DM_DieScrapRecord_DieID ON DM_DieScrapRecord(DieID);
    CREATE INDEX IX_DM_DieScrapRecord_AuditStatus ON DM_DieScrapRecord(AuditStatus);
    CREATE INDEX IX_DM_DieScrapRecord_ApplyTime ON DM_DieScrapRecord(ApplyTime DESC);
    
    PRINT 'DM_DieScrapRecord 表创建成功！';
END
ELSE
BEGIN
    PRINT 'DM_DieScrapRecord 表已存在，跳过创建。';
END

PRINT '';
PRINT '==========================================';
PRINT '自动入库功能数据库迁移完成！';
PRINT '==========================================';
PRINT '';
PRINT '说明：';
PRINT '1. DM_DieInventory - 刀模库存表，记录刀模入库信息';
PRINT '2. DM_StorageLocation - 库位表，管理仓库库位';
PRINT '3. DM_DieBorrowRecord - 借用记录表';
PRINT '4. DM_DieScrapRecord - 报废记录表';
PRINT '';
PRINT '自动入库逻辑：';
PRINT '当刀模的所有工序（绘图、割板、弯刀、装刀、贴泡沫）都完成报工后，';
PRINT '系统自动将该刀模入库，并更新刀模状态为"已完成"。';
PRINT '==========================================';
