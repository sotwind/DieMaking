-- 数据迁移脚本
-- 从 2026纸箱报价系统 迁移数据到 DieMaking 数据库

-- 使用 OPENQUERY 或链接服务器进行跨库查询
-- 注意：需要在 DieMaking 数据库中执行

-- 迁移 DM_User
SET IDENTITY_INSERT DM_User ON;
INSERT INTO DM_User (UserID, Username, Password, RealName, Permissions, Workstation, IsActive, CreateTime, LastLoginTime)
SELECT UserID, Username, Password, RealName, Permissions, Workstation, IsActive, CreateTime, LastLoginTime
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_User;
SET IDENTITY_INSERT DM_User OFF;

-- 迁移 DM_SystemConfig
SET IDENTITY_INSERT DM_SystemConfig ON;
INSERT INTO DM_SystemConfig (ConfigID, ConfigKey, ConfigValue, Description, CreateTime, UpdateTime)
SELECT ConfigID, ConfigKey, ConfigValue, Description, CreateTime, UpdateTime
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_SystemConfig;
SET IDENTITY_INSERT DM_SystemConfig OFF;

-- 迁移 DM_UserPreference
SET IDENTITY_INSERT DM_UserPreference ON;
INSERT INTO DM_UserPreference (PreferenceID, UserID, Theme, DefaultPageSize, DateFormat, TimeFormat, DefaultPage, UpdateTime)
SELECT PreferenceID, UserID, Theme, DefaultPageSize, DateFormat, TimeFormat, DefaultPage, UpdateTime
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_UserPreference;
SET IDENTITY_INSERT DM_UserPreference OFF;

-- 迁移 DM_DieInfo
SET IDENTITY_INSERT DM_DieInfo ON;
INSERT INTO DM_DieInfo (DieID, DieCode, CustomerName, ProductName, Structure, ModelType, LayoutType, FluteType, Material, 
    ManufactureLength, ManufactureWidth, ManufactureHeight, BlankLength, BlankWidth, ProcessDesc, RequiredProcesses, 
    [Status], AuditStatus, SourceFactory, ExternalOrderID, DeliveryDate, CreateTime, CreateUser, UpdateTime, Remark,
    WorkOrderNo, KnifeLengthM, KnifeMarkLengthM, BoardFeeUnitPrice, BoardFee, ProductionUnitPrice, ProductionFee, 
    DesignUnitPrice, DesignFee)
SELECT DieID, DieCode, CustomerName, ProductName, Structure, ModelType, LayoutType, FluteType, Material, 
    ManufactureLength, ManufactureWidth, ManufactureHeight, BlankLength, BlankWidth, ProcessDesc, RequiredProcesses, 
    [Status], AuditStatus, SourceFactory, ExternalOrderID, DeliveryDate, CreateTime, CreateUser, UpdateTime, Remark,
    WorkOrderNo, KnifeLengthM, KnifeMarkLengthM, BoardFeeUnitPrice, BoardFee, ProductionUnitPrice, ProductionFee, 
    DesignUnitPrice, DesignFee
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_DieInfo;
SET IDENTITY_INSERT DM_DieInfo OFF;

-- 迁移 DM_DieProcess
SET IDENTITY_INSERT DM_DieProcess ON;
INSERT INTO DM_DieProcess (ProcessID, DieID, ProcessName, [Status], StartTime, CompleteTime, OperatorNo, OperatorName,
    BoardLength, BoardWidth, KnifeLength, KnifeTraceLength, Formula, Amount, PrevProcessID, IsPrevCompleted, CreateTime)
SELECT ProcessID, DieID, ProcessName, [Status], StartTime, CompleteTime, OperatorNo, OperatorName,
    BoardLength, BoardWidth, KnifeLength, KnifeTraceLength, Formula, Amount, PrevProcessID, IsPrevCompleted, CreateTime
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_DieProcess;
SET IDENTITY_INSERT DM_DieProcess OFF;

-- 迁移 DM_DieCompletion
SET IDENTITY_INSERT DM_DieCompletion ON;
INSERT INTO DM_DieCompletion (CompletionID, DieID, CompleteTime, TotalAmount, OperatorNo, OperatorName, Remark)
SELECT CompletionID, DieID, CompleteTime, TotalAmount, OperatorNo, OperatorName, Remark
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_DieCompletion;
SET IDENTITY_INSERT DM_DieCompletion OFF;

-- 迁移 DM_StorageLocation
SET IDENTITY_INSERT DM_StorageLocation ON;
INSERT INTO DM_StorageLocation (LocationID, LocationCode, Area, ShelfNo, LayerNo, PositionNo, Description, [Status], CreateTime)
SELECT LocationID, LocationCode, Area, ShelfNo, LayerNo, PositionNo, Description, [Status], CreateTime
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_StorageLocation;
SET IDENTITY_INSERT DM_StorageLocation OFF;

-- 迁移 DM_DieInventory
SET IDENTITY_INSERT DM_DieInventory ON;
INSERT INTO DM_DieInventory (InventoryID, DieID, LocationID, StorageStatus, InStockTime, LastBorrowTime, LastReturnTime, TotalBorrowCount, Remark, UpdateTime)
SELECT InventoryID, DieID, LocationID, StorageStatus, InStockTime, LastBorrowTime, LastReturnTime, TotalBorrowCount, Remark, UpdateTime
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_DieInventory;
SET IDENTITY_INSERT DM_DieInventory OFF;

-- 迁移 DM_DieBorrowRecord
SET IDENTITY_INSERT DM_DieBorrowRecord ON;
INSERT INTO DM_DieBorrowRecord (BorrowID, DieID, InventoryID, BorrowType, BorrowerNo, BorrowerName, BorrowDept, BorrowTime, 
    ExpectedReturnTime, ActualReturnTime, Purpose, [Status], ReturnOperatorNo, ReturnOperatorName, Remark, CreateTime)
SELECT BorrowID, DieID, InventoryID, BorrowType, BorrowerNo, BorrowerName, BorrowDept, BorrowTime, 
    ExpectedReturnTime, ActualReturnTime, Purpose, [Status], ReturnOperatorNo, ReturnOperatorName, Remark, CreateTime
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_DieBorrowRecord;
SET IDENTITY_INSERT DM_DieBorrowRecord OFF;

-- 迁移 DM_DieScrapRecord
SET IDENTITY_INSERT DM_DieScrapRecord ON;
INSERT INTO DM_DieScrapRecord (ScrapID, DieID, InventoryID, ScrapReason, ScrapType, ApplicantNo, ApplicantName, ApplyTime, 
    AuditorNo, AuditorName, AuditTime, AuditStatus, AuditRemark, ScrapTime, CreateTime)
SELECT ScrapID, DieID, InventoryID, ScrapReason, ScrapType, ApplicantNo, ApplicantName, ApplyTime, 
    AuditorNo, AuditorName, AuditTime, AuditStatus, AuditRemark, ScrapTime, CreateTime
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_DieScrapRecord;
SET IDENTITY_INSERT DM_DieScrapRecord OFF;

-- 迁移 DM_OperationLog
SET IDENTITY_INSERT DM_OperationLog ON;
INSERT INTO DM_OperationLog (LogID, UserID, Username, OperationType, OperationDesc, DieID, CreateTime)
SELECT LogID, UserID, Username, OperationType, OperationDesc, DieID, CreateTime
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_OperationLog;
SET IDENTITY_INSERT DM_OperationLog OFF;

-- 迁移 DM_BackupRecord
SET IDENTITY_INSERT DM_BackupRecord ON;
INSERT INTO DM_BackupRecord (BackupID, BackupFileName, BackupPath, BackupSize, BackupType, StartTime, EndTime, [Status], ErrorMessage, CreatedBy, Remark)
SELECT BackupID, BackupFileName, BackupPath, BackupSize, BackupType, StartTime, EndTime, [Status], ErrorMessage, CreatedBy, Remark
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_BackupRecord;
SET IDENTITY_INSERT DM_BackupRecord OFF;

-- 迁移 DM_DatabaseVersion
SET IDENTITY_INSERT DM_DatabaseVersion ON;
INSERT INTO DM_DatabaseVersion (VersionID, VersionNumber, ScriptName, AppliedDate, AppliedBy, Description)
SELECT VersionID, VersionNumber, ScriptName, AppliedDate, AppliedBy, Description
FROM [36.139.89.173].[2026纸箱报价系统].dbo.DM_DatabaseVersion;
SET IDENTITY_INSERT DM_DatabaseVersion OFF;

PRINT '数据迁移完成！';
