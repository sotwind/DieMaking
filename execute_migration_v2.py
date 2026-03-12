#!/usr/bin/env python3
"""
执行 SQL Server 数据库迁移脚本 (适配 DM_ 前缀表名)
"""
import sys
import pyodbc

# 数据库连接信息
SERVER = "36.139.89.173"
DATABASE = "2026纸箱报价系统"
USERNAME = "sa"
PASSWORD = "slbz_888"

CONN_STR = f"DRIVER={{ODBC Driver 17 for SQL Server}};SERVER={SERVER};DATABASE={DATABASE};UID={USERNAME};PWD={PASSWORD};TrustServerCertificate=yes"

def execute_migration():
    """执行数据库迁移脚本"""
    try:
        print(f"正在连接到 SQL Server: {SERVER}...")
        conn = pyodbc.connect(CONN_STR, timeout=30)
        cursor = conn.cursor()
        print("连接成功！")

        # 迁移操作列表
        migrations = []

        # 1. 为 DM_DieInfo 表添加新字段
        new_columns = [
            ('WorkOrderNo', 'NVARCHAR(50) NULL'),
            ('KnifeLengthM', 'DECIMAL(18,4) NULL'),
            ('KnifeMarkLengthM', 'DECIMAL(18,4) NULL'),
            ('BoardFeeUnitPrice', 'DECIMAL(18,2) NOT NULL DEFAULT 90'),
            ('BoardFee', 'DECIMAL(18,2) NULL'),
            ('ProductionUnitPrice', 'DECIMAL(18,2) NOT NULL DEFAULT 8'),
            ('ProductionFee', 'DECIMAL(18,2) NULL'),
            ('DesignUnitPrice', 'DECIMAL(18,2) NOT NULL DEFAULT 70'),
            ('DesignFee', 'DECIMAL(18,2) NULL'),
        ]

        for col_name, col_type in new_columns:
            migrations.append({
                'name': f'添加 DM_DieInfo.{col_name} 字段',
                'check': f"SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('DM_DieInfo') AND name = '{col_name}'",
                'sql': f"ALTER TABLE DM_DieInfo ADD {col_name} {col_type}"
            })

        # 2. 创建改刀记录表
        migrations.append({
            'name': '创建 DM_DieModificationRecord 表',
            'check': "SELECT COUNT(*) FROM sys.tables WHERE name = 'DM_DieModificationRecord'",
            'sql': """
                CREATE TABLE DM_DieModificationRecord (
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
                    CONSTRAINT FK_DieModificationRecord_DieInfo FOREIGN KEY (DieID) REFERENCES DM_DieInfo(DieID)
                )
            """
        })

        # 添加改刀记录表索引
        migrations.append({
            'name': '创建 DM_DieModificationRecord 索引 (DieID)',
            'check': "SELECT COUNT(*) FROM sys.indexes WHERE name = 'IX_DieModificationRecord_DieID'",
            'sql': "CREATE INDEX IX_DieModificationRecord_DieID ON DM_DieModificationRecord(DieID)"
        })

        migrations.append({
            'name': '创建 DM_DieModificationRecord 索引 (ModificationTime)',
            'check': "SELECT COUNT(*) FROM sys.indexes WHERE name = 'IX_DieModificationRecord_ModificationTime'",
            'sql': "CREATE INDEX IX_DieModificationRecord_ModificationTime ON DM_DieModificationRecord(ModificationTime)"
        })

        # 3. 检查 DM_SystemConfig 表是否存在（已存在则跳过创建）
        migrations.append({
            'name': '检查 DM_SystemConfig 表',
            'check': "SELECT COUNT(*) FROM sys.tables WHERE name = 'DM_SystemConfig'",
            'sql': None  # 表已存在，跳过
        })

        # 4. 创建扫码报工记录表
        migrations.append({
            'name': '创建 DM_ScanReportRecord 表',
            'check': "SELECT COUNT(*) FROM sys.tables WHERE name = 'DM_ScanReportRecord'",
            'sql': """
                CREATE TABLE DM_ScanReportRecord (
                    RecordID INT IDENTITY(1,1) PRIMARY KEY,
                    WorkOrderNo NVARCHAR(50) NOT NULL,
                    DieID INT NULL,
                    ProcessID INT NULL,
                    ProcessName NVARCHAR(50) NULL,
                    ScanTime DATETIME NOT NULL DEFAULT GETDATE(),
                    ReportType INT NOT NULL DEFAULT 0,
                    OperatorNo NVARCHAR(50) NULL,
                    OperatorName NVARCHAR(50) NULL,
                    DeviceInfo NVARCHAR(200) NULL,
                    CreateTime DATETIME NOT NULL DEFAULT GETDATE()
                )
            """
        })

        migrations.append({
            'name': '创建 DM_ScanReportRecord 索引 (WorkOrderNo)',
            'check': "SELECT COUNT(*) FROM sys.indexes WHERE name = 'IX_ScanReportRecord_WorkOrderNo'",
            'sql': "CREATE INDEX IX_ScanReportRecord_WorkOrderNo ON DM_ScanReportRecord(WorkOrderNo)"
        })

        migrations.append({
            'name': '创建 DM_ScanReportRecord 索引 (ScanTime)',
            'check': "SELECT COUNT(*) FROM sys.indexes WHERE name = 'IX_ScanReportRecord_ScanTime'",
            'sql': "CREATE INDEX IX_ScanReportRecord_ScanTime ON DM_ScanReportRecord(ScanTime)"
        })

        print("\n开始执行数据库迁移...")
        print("="*60)

        success_count = 0
        skip_count = 0
        error_count = 0

        for migration in migrations:
            name = migration['name']
            check_sql = migration['check']
            exec_sql = migration['sql']

            try:
                # 检查是否已存在
                cursor.execute(check_sql)
                exists = cursor.fetchone()[0] > 0

                if exists:
                    print(f"  ⚠ {name} - 已存在，跳过")
                    skip_count += 1
                    continue

                if exec_sql is None:
                    print(f"  ✓ {name} - 已存在")
                    skip_count += 1
                    continue

                # 执行迁移
                cursor.execute(exec_sql)
                conn.commit()
                print(f"  ✓ {name} - 成功")
                success_count += 1

            except pyodbc.Error as e:
                print(f"  ✗ {name} - 失败: {e}")
                error_count += 1

        print("="*60)
        print(f"\n迁移执行结果:")
        print(f"  成功: {success_count}")
        print(f"  跳过(已存在): {skip_count}")
        print(f"  失败: {error_count}")

        # 验证表结构
        print("\n验证表结构...")
        print("-"*60)

        tables_to_check = [
            'DM_DieInfo',
            'DM_DieModificationRecord',
            'DM_SystemConfig',
            'DM_ScanReportRecord'
        ]

        for table in tables_to_check:
            cursor.execute(f"SELECT COUNT(*) FROM sys.tables WHERE name = '{table}'")
            exists = cursor.fetchone()[0] > 0
            status = "✓ 存在" if exists else "✗ 不存在"
            print(f"  {status}: {table}")

        # 检查 DM_DieInfo 表的新字段
        print("\n验证 DM_DieInfo 表新字段...")
        print("-"*60)

        new_columns_check = [
            'WorkOrderNo', 'KnifeLengthM', 'KnifeMarkLengthM',
            'BoardFeeUnitPrice', 'BoardFee',
            'ProductionUnitPrice', 'ProductionFee',
            'DesignUnitPrice', 'DesignFee'
        ]

        for col in new_columns_check:
            cursor.execute(f"SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('DM_DieInfo') AND name = '{col}'")
            exists = cursor.fetchone()[0] > 0
            status = "✓ 存在" if exists else "✗ 不存在"
            print(f"  {status}: {col}")

        cursor.close()
        conn.close()
        print("\n" + "="*60)
        print("数据库迁移完成！")
        return True

    except Exception as e:
        print(f"\n✗ 数据库迁移失败: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    success = execute_migration()
    sys.exit(0 if success else 1)
