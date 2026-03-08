-- =============================================
-- 刀模管理系统 - 数据库索引优化脚本
-- 创建时间: 2026-03-08
-- 说明: 为高频查询字段创建索引，提升查询性能
-- =============================================

-- 检查并创建索引的辅助存储过程
IF OBJECT_ID('sp_CreateIndexIfNotExists', 'P') IS NOT NULL
    DROP PROCEDURE sp_CreateIndexIfNotExists;
GO

CREATE PROCEDURE sp_CreateIndexIfNotExists
    @TableName NVARCHAR(128),
    @IndexName NVARCHAR(128),
    @IndexColumns NVARCHAR(MAX),
    @IncludeColumns NVARCHAR(MAX) = NULL,
    @IsUnique BIT = 0,
    @FillFactor INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = @IndexName AND object_id = OBJECT_ID(@TableName)
    )
    BEGIN
        DECLARE @Sql NVARCHAR(MAX);
        DECLARE @UniqueStr NVARCHAR(10) = CASE WHEN @IsUnique = 1 THEN 'UNIQUE ' ELSE '' END;
        DECLARE @IncludeStr NVARCHAR(MAX) = CASE 
            WHEN @IncludeColumns IS NOT NULL THEN ' INCLUDE (' + @IncludeColumns + ')' 
            ELSE '' 
        END;
        
        SET @Sql = 'CREATE ' + @UniqueStr + 'NONCLUSTERED INDEX ' + @IndexName + 
                   ' ON ' + @TableName + ' (' + @IndexColumns + ')' + 
                   @IncludeStr + ' WITH (FILLFACTOR = ' + CAST(@FillFactor AS NVARCHAR(3)) + ', ONLINE = ON)';
        
        PRINT '创建索引: ' + @IndexName + ' ON ' + @TableName;
        EXEC sp_executesql @Sql;
        PRINT '索引创建成功!';
    END
    ELSE
    BEGIN
        PRINT '索引已存在: ' + @IndexName + ' ON ' + @TableName;
    END
END;
GO

-- =============================================
-- 1. 刀模信息表 (DM_DieInfo) 索引优化
-- =============================================

-- 刀模编号索引（已存在，但确保覆盖查询）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieInfo', 
    'IX_DM_DieInfo_DieCode_Covering', 
    'DieCode', 
    'CustomerName, ProductName, Status, AuditStatus, CreateTime';

-- 客户名称索引（支持模糊查询优化）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieInfo', 
    'IX_DM_DieInfo_CustomerName_Covering', 
    'CustomerName', 
    'DieCode, ProductName, Status, CreateTime';

-- 状态+创建时间复合索引（列表查询优化）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieInfo', 
    'IX_DM_DieInfo_Status_CreateTime_Covering', 
    'Status, CreateTime DESC', 
    'DieCode, CustomerName, ProductName, AuditStatus';

-- 审核状态索引
EXEC sp_CreateIndexIfNotExists 
    'DM_DieInfo', 
    'IX_DM_DieInfo_AuditStatus', 
    'AuditStatus', 
    'DieCode, CustomerName';

-- 交货日期索引（用于预警查询）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieInfo', 
    'IX_DM_DieInfo_DeliveryDate', 
    'DeliveryDate', 
    'DieCode, CustomerName, Status';

-- 创建时间索引（范围查询优化）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieInfo', 
    'IX_DM_DieInfo_CreateTime_Range', 
    'CreateTime DESC', 
    'DieCode, CustomerName, Status, AuditStatus';

-- 来源工厂索引
EXEC sp_CreateIndexIfNotExists 
    'DM_DieInfo', 
    'IX_DM_DieInfo_SourceFactory', 
    'SourceFactory', 
    'DieCode, CustomerName';

-- =============================================
-- 2. 刀模工序表 (DM_DieProcess) 索引优化
-- =============================================

-- 刀模ID+状态复合索引（高频查询）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieProcess', 
    'IX_DM_DieProcess_DieID_Status_Covering', 
    'DieID, Status', 
    'ProcessName, StartTime, CompleteTime, OperatorName, Amount';

-- 工序名称索引（统计查询优化）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieProcess', 
    'IX_DM_DieProcess_ProcessName', 
    'ProcessName', 
    'DieID, Status, Amount';

-- 状态+创建时间复合索引
EXEC sp_CreateIndexIfNotExists 
    'DM_DieProcess', 
    'IX_DM_DieProcess_Status_CreateTime', 
    'Status, CreateTime DESC', 
    'DieID, ProcessName';

-- 操作人索引（绩效统计）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieProcess', 
    'IX_DM_DieProcess_OperatorNo', 
    'OperatorNo', 
    'DieID, ProcessName, Status, Amount, CompleteTime';

-- 完成时间索引（完工统计）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieProcess', 
    'IX_DM_DieProcess_CompleteTime', 
    'CompleteTime DESC', 
    'DieID, ProcessName, OperatorName, Amount';

-- 前道工序ID索引（工序依赖检查）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieProcess', 
    'IX_DM_DieProcess_PrevProcessID', 
    'PrevProcessID', 
    'DieID, Status';

-- =============================================
-- 3. 完工记录表 (DM_DieCompletion) 索引优化
-- =============================================

-- 刀模ID索引
EXEC sp_CreateIndexIfNotExists 
    'DM_DieCompletion', 
    'IX_DM_DieCompletion_DieID', 
    'DieID', 
    'CompleteTime, TotalAmount, OperatorName';

-- 完成时间索引（报表统计）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieCompletion', 
    'IX_DM_DieCompletion_CompleteTime_Covering', 
    'CompleteTime DESC', 
    'DieID, TotalAmount, OperatorName';

-- 操作人索引（绩效统计）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieCompletion', 
    'IX_DM_DieCompletion_OperatorNo', 
    'OperatorNo', 
    'DieID, CompleteTime, TotalAmount';

-- =============================================
-- 4. 库存表 (DM_DieInventory) 索引优化
-- =============================================

-- 刀模ID唯一索引（已存在，确保覆盖）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieInventory', 
    'IX_DM_DieInventory_DieID_Covering', 
    'DieID', 
    'LocationID, StorageStatus, InStockTime, LastBorrowTime';

-- 库位ID索引
EXEC sp_CreateIndexIfNotExists 
    'DM_DieInventory', 
    'IX_DM_DieInventory_LocationID', 
    'LocationID', 
    'DieID, StorageStatus';

-- 存储状态索引（状态筛选）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieInventory', 
    'IX_DM_DieInventory_StorageStatus_Covering', 
    'StorageStatus', 
    'DieID, LocationID, InStockTime';

-- 入库时间索引
EXEC sp_CreateIndexIfNotExists 
    'DM_DieInventory', 
    'IX_DM_DieInventory_InStockTime', 
    'InStockTime DESC', 
    'DieID, LocationID';

-- =============================================
-- 5. 库位表 (DM_StorageLocation) 索引优化
-- =============================================

-- 区域+架号+层号+位号复合索引（排序优化）
EXEC sp_CreateIndexIfNotExists 
    'DM_StorageLocation', 
    'IX_DM_StorageLocation_Area_Shelf_Layer_Pos', 
    'Area, ShelfNo, LayerNo, PositionNo', 
    'LocationCode, Status';

-- 库位编号索引（已存在，确保覆盖）
EXEC sp_CreateIndexIfNotExists 
    'DM_StorageLocation', 
    'IX_DM_StorageLocation_LocationCode_Covering', 
    'LocationCode', 
    'Area, ShelfNo, LayerNo, PositionNo, Status';

-- 状态索引（空闲库位查询）
EXEC sp_CreateIndexIfNotExists 
    'DM_StorageLocation', 
    'IX_DM_StorageLocation_Status', 
    'Status', 
    'LocationCode, Area';

-- =============================================
-- 6. 借用记录表 (DM_DieBorrowRecord) 索引优化
-- =============================================

-- 刀模ID索引
EXEC sp_CreateIndexIfNotExists 
    'DM_DieBorrowRecord', 
    'IX_DM_DieBorrowRecord_DieID', 
    'DieID', 
    'BorrowTime, Status, BorrowerName';

-- 状态+借用时间复合索引（待归还查询）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieBorrowRecord', 
    'IX_DM_DieBorrowRecord_Status_BorrowTime', 
    'Status, BorrowTime DESC', 
    'DieID, BorrowerName, ExpectedReturnTime';

-- 借用人索引（个人记录查询）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieBorrowRecord', 
    'IX_DM_DieBorrowRecord_BorrowerNo', 
    'BorrowerNo', 
    'DieID, BorrowTime, Status';

-- 借用时间索引（报表统计）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieBorrowRecord', 
    'IX_DM_DieBorrowRecord_BorrowTime', 
    'BorrowTime DESC', 
    'DieID, BorrowerName, Status';

-- 预计归还时间索引（逾期预警）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieBorrowRecord', 
    'IX_DM_DieBorrowRecord_ExpectedReturnTime', 
    'ExpectedReturnTime', 
    'DieID, BorrowerName, Status';

-- =============================================
-- 7. 报废记录表 (DM_DieScrapRecord) 索引优化
-- =============================================

-- 刀模ID索引
EXEC sp_CreateIndexIfNotExists 
    'DM_DieScrapRecord', 
    'IX_DM_DieScrapRecord_DieID', 
    'DieID', 
    'ApplyTime, AuditStatus';

-- 审核状态+申请时间复合索引（待审核查询）
EXEC sp_CreateIndexIfNotExists 
    'DM_DieScrapRecord', 
    'IX_DM_DieScrapRecord_AuditStatus_ApplyTime', 
    'AuditStatus, ApplyTime DESC', 
    'DieID, ApplicantName';

-- 申请人索引
EXEC sp_CreateIndexIfNotExists 
    'DM_DieScrapRecord', 
    'IX_DM_DieScrapRecord_ApplicantNo', 
    'ApplicantNo', 
    'DieID, ApplyTime, AuditStatus';

-- =============================================
-- 8. 操作日志表 (DM_OperationLog) 索引优化
-- =============================================

-- 用户ID索引
EXEC sp_CreateIndexIfNotExists 
    'DM_OperationLog', 
    'IX_DM_OperationLog_UserID', 
    'UserID', 
    'OperationType, CreateTime';

-- 创建时间索引（日志查询优化）
EXEC sp_CreateIndexIfNotExists 
    'DM_OperationLog', 
    'IX_DM_OperationLog_CreateTime_Covering', 
    'CreateTime DESC', 
    'UserID, Username, OperationType, OperationDesc';

-- 操作类型索引
EXEC sp_CreateIndexIfNotExists 
    'DM_OperationLog', 
    'IX_DM_OperationLog_OperationType', 
    'OperationType', 
    'UserID, CreateTime';

-- 刀模ID索引（关联查询）
EXEC sp_CreateIndexIfNotExists 
    'DM_OperationLog', 
    'IX_DM_OperationLog_DieID', 
    'DieID', 
    'OperationType, CreateTime';

-- =============================================
-- 9. 用户表 (DM_User) 索引优化
-- =============================================

-- 用户名索引（已存在，确保覆盖）
EXEC sp_CreateIndexIfNotExists 
    'DM_User', 
    'IX_DM_User_Username_Covering', 
    'Username', 
    'RealName, IsActive, LastLoginTime';

-- 状态索引
EXEC sp_CreateIndexIfNotExists 
    'DM_User', 
    'IX_DM_User_IsActive', 
    'IsActive', 
    'Username, RealName';

-- 最后登录时间索引（活跃用户统计）
EXEC sp_CreateIndexIfNotExists 
    'DM_User', 
    'IX_DM_User_LastLoginTime', 
    'LastLoginTime DESC', 
    'Username, RealName';

-- =============================================
-- 10. 系统配置表 (DM_SystemConfig) 索引优化
-- =============================================

-- 配置键索引（已存在，确保覆盖）
EXEC sp_CreateIndexIfNotExists 
    'DM_SystemConfig', 
    'IX_DM_SystemConfig_ConfigKey_Covering', 
    'ConfigKey', 
    'ConfigValue, Description';

-- =============================================
-- 清理临时存储过程
-- =============================================
DROP PROCEDURE sp_CreateIndexIfNotExists;
GO

PRINT '==========================================';
PRINT '数据库索引优化完成!';
PRINT '==========================================';
