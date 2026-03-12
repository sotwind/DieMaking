#!/usr/bin/env python3
"""
Diemaking 数据库迁移脚本
使用方法: python run_migration.py [--connection-string "..."]
"""

import pyodbc
import argparse
import sys
from pathlib import Path

# 默认连接字符串
DEFAULT_CONNECTION_STRING = "DRIVER={ODBC Driver 17 for SQL Server};SERVER=localhost;DATABASE=DieMaking;Trusted_Connection=yes;TrustServerCertificate=yes;"

# 迁移脚本内容
MIGRATION_SQL = """
-- =============================================
-- Diemaking 系统数据库迁移脚本
-- 版本: 2026-03-11
-- =============================================

PRINT '开始执行数据库迁移...';

-- =============================================
-- 1. 扩展 DieInfo 表 - 添加费用相关字段
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'WorkOrderNo')
BEGIN
    ALTER TABLE DieInfo ADD WorkOrderNo NVARCHAR(50) NULL;
    PRINT '已添加 WorkOrderNo 字段';
END
ELSE
BEGIN
    PRINT 'WorkOrderNo 字段已存在，跳过';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'KnifeLengthM')
BEGIN
    ALTER TABLE DieInfo ADD KnifeLengthM DECIMAL(18, 4) NULL;
    PRINT '已添加 KnifeLengthM 字段';
END
ELSE
BEGIN
    PRINT 'KnifeLengthM 字段已存在，跳过';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'KnifeMarkLengthM')
BEGIN
    ALTER TABLE DieInfo ADD KnifeMarkLengthM DECIMAL(18, 4) NULL;
    PRINT '已添加 KnifeMarkLengthM 字段';
END
ELSE
BEGIN
    PRINT 'KnifeMarkLengthM 字段已存在，跳过';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'BoardFeeUnitPrice')
BEGIN
    ALTER TABLE DieInfo ADD BoardFeeUnitPrice DECIMAL(18, 2) NOT NULL DEFAULT(90);
    PRINT '已添加 BoardFeeUnitPrice 字段';
END
ELSE
BEGIN
    PRINT 'BoardFeeUnitPrice 字段已存在，跳过';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'BoardFee')
BEGIN
    ALTER TABLE DieInfo ADD BoardFee DECIMAL(18, 2) NULL;
    PRINT '已添加 BoardFee 字段';
END
ELSE
BEGIN
    PRINT 'BoardFee 字段已存在，跳过';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'ProductionUnitPrice')
BEGIN
    ALTER TABLE DieInfo ADD ProductionUnitPrice DECIMAL(18, 2) NOT NULL DEFAULT(8);
    PRINT '已添加 ProductionUnitPrice 字段';
END
ELSE
BEGIN
    PRINT 'ProductionUnitPrice 字段已存在，跳过';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'ProductionFee')
BEGIN
    ALTER TABLE DieInfo ADD ProductionFee DECIMAL(18, 2) NULL;
    PRINT '已添加 ProductionFee 字段';
END
ELSE
BEGIN
    PRINT 'ProductionFee 字段已存在，跳过';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'DesignUnitPrice')
BEGIN
    ALTER TABLE DieInfo ADD DesignUnitPrice DECIMAL(18, 2) NOT NULL DEFAULT(70);
    PRINT '已添加 DesignUnitPrice 字段';
END
ELSE
BEGIN
    PRINT 'DesignUnitPrice 字段已存在，跳过';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = 'DesignFee')
BEGIN
    ALTER TABLE DieInfo ADD DesignFee DECIMAL(18, 2) NULL;
    PRINT '已添加 DesignFee 字段';
END
ELSE
BEGIN
    PRINT 'DesignFee 字段已存在，跳过';
END

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
    PRINT '已创建 DieModificationRecord 表';
END
ELSE
BEGIN
    PRINT 'DieModificationRecord 表已存在，跳过';
END

-- =============================================
-- 3. 创建系统配置表
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SystemConfig')
BEGIN
    CREATE TABLE SystemConfig (
        ConfigID INT IDENTITY(1,1) PRIMARY KEY,
        ConfigKey NVARCHAR(100) NOT NULL UNIQUE,
        ConfigValue NVARCHAR(500) NOT NULL,
        ConfigType NVARCHAR(50) NULL,
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
    
    PRINT '已创建 SystemConfig 表并插入默认配置';
END
ELSE
BEGIN
    PRINT 'SystemConfig 表已存在，跳过';
END

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
        Status INT NOT NULL DEFAULT(0),
        ErrorMessage NVARCHAR(500) NULL,
        CreateTime DATETIME NOT NULL DEFAULT(GETDATE())
    );
    
    CREATE INDEX IX_ScanReportRecord_WorkOrderNo ON ScanReportRecord(WorkOrderNo);
    CREATE INDEX IX_ScanReportRecord_ScanTime ON ScanReportRecord(ScanTime);
    PRINT '已创建 ScanReportRecord 表';
END
ELSE
BEGIN
    PRINT 'ScanReportRecord 表已存在，跳过';
END

-- =============================================
-- 5. 更新现有数据 - 计算费用字段
-- =============================================
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

DECLARE @updatedRows INT = @@ROWCOUNT;
PRINT '已更新 ' + CAST(@updatedRows AS VARCHAR) + ' 条记录的费用字段';

PRINT '数据库迁移完成！';
"""


def run_migration(connection_string):
    """执行数据库迁移"""
    try:
        print(f"正在连接到数据库...")
        conn = pyodbc.connect(connection_string, timeout=10)
        cursor = conn.cursor()
        
        print("连接成功，开始执行迁移...")
        print("-" * 50)
        
        # 执行迁移脚本
        cursor.execute(MIGRATION_SQL)
        
        # 获取所有消息输出
        while cursor.nextset():
            pass
        
        conn.commit()
        print("-" * 50)
        print("✅ 数据库迁移执行成功！")
        
        # 验证迁移结果
        print("\n正在验证迁移结果...")
        verify_migration(cursor)
        
        conn.close()
        return True
        
    except pyodbc.Error as e:
        print(f"❌ 数据库错误: {e}")
        return False
    except Exception as e:
        print(f"❌ 执行错误: {e}")
        return False


def verify_migration(cursor):
    """验证迁移结果"""
    try:
        # 验证 DieInfo 表字段
        cursor.execute("""
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'DieInfo'
            AND COLUMN_NAME IN ('WorkOrderNo', 'KnifeLengthM', 'KnifeMarkLengthM', 
                                'BoardFeeUnitPrice', 'BoardFee', 'ProductionUnitPrice', 
                                'ProductionFee', 'DesignUnitPrice', 'DesignFee')
        """)
        dieinfo_count = cursor.fetchone()[0]
        print(f"  - DieInfo 表新增字段: {dieinfo_count}/9")
        
        # 验证新表
        cursor.execute("""
            SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME IN ('DieModificationRecord', 'SystemConfig', 'ScanReportRecord')
        """)
        new_tables_count = cursor.fetchone()[0]
        print(f"  - 新创建表: {new_tables_count}/3")
        
        # 验证 SystemConfig 数据
        cursor.execute("SELECT COUNT(*) FROM SystemConfig")
        config_count = cursor.fetchone()[0]
        print(f"  - SystemConfig 配置项: {config_count}")
        
        if dieinfo_count == 9 and new_tables_count == 3 and config_count >= 3:
            print("\n✅ 所有验证通过！")
        else:
            print("\n⚠️ 部分验证未通过，请检查日志")
            
    except Exception as e:
        print(f"验证时出错: {e}")


def main():
    parser = argparse.ArgumentParser(description='Diemaking 数据库迁移工具')
    parser.add_argument('--connection-string', '-c', 
                        default=DEFAULT_CONNECTION_STRING,
                        help='数据库连接字符串')
    parser.add_argument('--dry-run', action='store_true',
                        help='仅显示将要执行的 SQL，不实际执行')
    
    args = parser.parse_args()
    
    if args.dry_run:
        print("将要执行的 SQL 脚本：")
        print("=" * 50)
        print(MIGRATION_SQL)
        print("=" * 50)
        return
    
    success = run_migration(args.connection_string)
    sys.exit(0 if success else 1)


if __name__ == '__main__':
    main()
