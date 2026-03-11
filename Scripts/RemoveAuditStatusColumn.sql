-- 移除审核状态字段
-- 执行此脚本删除 DM_DieInfo 表的 AuditStatus 字段

IF EXISTS (SELECT * FROM sys.columns 
           WHERE Name = N'AuditStatus' 
           AND Object_ID = Object_ID(N'DM_DieInfo'))
BEGIN
    ALTER TABLE DM_DieInfo DROP COLUMN AuditStatus;
    PRINT 'AuditStatus 字段已成功删除';
END
ELSE
BEGIN
    PRINT 'AuditStatus 字段不存在，无需删除';
END
GO
