-- 刀模管理系统数据库表创建脚本
-- 创建 DieInfo 表及相关表结构

-- 检查并删除旧表（如果存在）
IF OBJECT_ID('DieModificationRecord', 'U') IS NOT NULL DROP TABLE DieModificationRecord;
IF OBJECT_ID('ScanReportRecord', 'U') IS NOT NULL DROP TABLE ScanReportRecord;
IF OBJECT_ID('DieProcess', 'U') IS NOT NULL DROP TABLE DieProcess;
IF OBJECT_ID('DieInfo', 'U') IS NOT NULL DROP TABLE DieInfo;
GO

-- 创建刀模信息表
CREATE TABLE DieInfo (
    DieID INT IDENTITY(1,1) PRIMARY KEY,
    DieCode NVARCHAR(50) NOT NULL UNIQUE,
    WorkOrderNo NVARCHAR(50) NULL,
    CustomerName NVARCHAR(100) NOT NULL,
    ProductName NVARCHAR(100) NOT NULL,
    Structure NVARCHAR(200) NULL,
    ModelType NVARCHAR(50) NULL,
    LayoutType NVARCHAR(50) NULL,
    FluteType NVARCHAR(50) NULL,
    Material NVARCHAR(100) NULL,
    ManufactureLength DECIMAL(10,2) NULL,
    ManufactureWidth DECIMAL(10,2) NULL,
    ManufactureHeight DECIMAL(10,2) NULL,
    BlankLength DECIMAL(10,2) NULL,
    BlankWidth DECIMAL(10,2) NULL,
    KnifeLengthM DECIMAL(10,3) NULL,
    KnifeMarkLengthM DECIMAL(10,3) NULL,
    BoardFeeUnitPrice DECIMAL(10,2) DEFAULT 90.00,
    BoardFee DECIMAL(10,2) NULL,
    ProductionUnitPrice DECIMAL(10,2) DEFAULT 8.00,
    ProductionFee DECIMAL(10,2) NULL,
    DesignUnitPrice DECIMAL(10,2) DEFAULT 70.00,
    DesignFee DECIMAL(10,2) NULL,
    ProcessDesc NVARCHAR(500) NULL,
    RequiredProcesses NVARCHAR(200) NULL,
    [Status] INT DEFAULT 0,
    AuditStatus INT DEFAULT 0,
    SourceFactory NVARCHAR(50) NULL,
    ExternalOrderID INT NULL,
    ExternalOrderNo NVARCHAR(50) NULL,
    DeliveryDate DATETIME NULL,
    CreateTime DATETIME DEFAULT GETDATE(),
    CreateUser NVARCHAR(50) NULL,
    UpdateTime DATETIME DEFAULT GETDATE(),
    UpdateUser NVARCHAR(50) NULL,
    Remark NVARCHAR(500) NULL
);
GO

-- 创建刀模工序表
CREATE TABLE DieProcess (
    ProcessID INT IDENTITY(1,1) PRIMARY KEY,
    DieID INT NOT NULL,
    ProcessName NVARCHAR(50) NOT NULL,
    ProcessOrder INT DEFAULT 0,
    [Status] INT DEFAULT 0,
    StartTime DATETIME NULL,
    CompleteTime DATETIME NULL,
    OperatorNo NVARCHAR(50) NULL,
    OperatorName NVARCHAR(50) NULL,
    Amount DECIMAL(10,2) NULL,
    PrevProcessID INT NULL,
    CreateTime DATETIME DEFAULT GETDATE(),
    Remark NVARCHAR(500) NULL,
    FOREIGN KEY (DieID) REFERENCES DieInfo(DieID) ON DELETE CASCADE
);
GO

-- 创建改刀记录表
CREATE TABLE DieModificationRecord (
    ModificationID INT IDENTITY(1,1) PRIMARY KEY,
    DieID INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    ModificationTime DATETIME DEFAULT GETDATE(),
    ModifierNo NVARCHAR(50) NULL,
    ModifierName NVARCHAR(50) NULL,
    Reason NVARCHAR(500) NULL,
    CreateTime DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (DieID) REFERENCES DieInfo(DieID) ON DELETE CASCADE
);
GO

-- 创建扫码报产记录表
CREATE TABLE ScanReportRecord (
    RecordID INT IDENTITY(1,1) PRIMARY KEY,
    WorkOrderNo NVARCHAR(50) NOT NULL,
    DieID INT NULL,
    ProcessName NVARCHAR(50) NULL,
    ScanTime DATETIME DEFAULT GETDATE(),
    OperatorNo NVARCHAR(50) NULL,
    OperatorName NVARCHAR(50) NULL,
    [Status] INT DEFAULT 0,
    ErrorMessage NVARCHAR(500) NULL,
    CreateTime DATETIME DEFAULT GETDATE()
);
GO

-- 创建索引
CREATE INDEX IX_DieInfo_WorkOrderNo ON DieInfo(WorkOrderNo);
CREATE INDEX IX_DieInfo_CustomerName ON DieInfo(CustomerName);
CREATE INDEX IX_DieInfo_Status ON DieInfo([Status]);
CREATE INDEX IX_DieInfo_CreateTime ON DieInfo(CreateTime);
CREATE INDEX IX_DieProcess_DieID ON DieProcess(DieID);
CREATE INDEX IX_DieModificationRecord_DieID ON DieModificationRecord(DieID);
CREATE INDEX IX_ScanReportRecord_WorkOrderNo ON ScanReportRecord(WorkOrderNo);
GO

PRINT '刀模管理系统表结构创建完成！';
