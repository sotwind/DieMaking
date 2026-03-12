#!/usr/bin/env python3
"""
执行 SQL Server 数据库迁移脚本
"""
import sys

try:
    import pyodbc
except ImportError:
    print("正在安装 pyodbc...")
    import subprocess
    subprocess.run([sys.executable, "-m", "pip", "install", "pyodbc", "-q"])
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

        # 读取迁移脚本
        with open('DieMaking/Data/DatabaseMigration.sql', 'r', encoding='utf-8') as f:
            migration_script = f.read()

        print("\n正在执行数据库迁移脚本...")

        # 分割并执行每个 SQL 语句
        statements = migration_script.split('\nGO')
        success_count = 0
        skip_count = 0
        error_count = 0

        for i, stmt in enumerate(statements):
            stmt = stmt.strip()
            if not stmt or stmt.startswith('--') or stmt.startswith('PRINT'):
                continue

            # 移除 PRINT 语句
            lines = stmt.split('\n')
            filtered_lines = [line for line in lines if not line.strip().startswith('PRINT')]
            stmt = '\n'.join(filtered_lines).strip()

            if not stmt:
                continue

            try:
                cursor.execute(stmt)
                conn.commit()
                success_count += 1
                print(f"  ✓ 语句 {i+1} 执行成功")
            except pyodbc.Error as e:
                error_msg = str(e)
                if "already exists" in error_msg.lower() or "已存在" in error_msg:
                    print(f"  ⚠ 语句 {i+1}: 对象已存在，跳过")
                    skip_count += 1
                else:
                    print(f"  ✗ 语句 {i+1} 执行失败: {error_msg}")
                    error_count += 1

        print(f"\n{'='*50}")
        print("迁移执行结果:")
        print(f"  成功: {success_count}")
        print(f"  跳过(已存在): {skip_count}")
        print(f"  失败: {error_count}")
        print(f"{'='*50}")

        # 验证表结构
        print("\n验证表结构...")
        tables_to_check = [
            'DieInfo',
            'DieModificationRecord',
            'SystemConfig',
            'ScanReportRecord'
        ]

        for table in tables_to_check:
            try:
                cursor.execute(f"SELECT COUNT(*) FROM sys.tables WHERE name = '{table}'")
                exists = cursor.fetchone()[0] > 0
                if exists:
                    print(f"  ✓ 表 {table} 存在")
                else:
                    print(f"  ✗ 表 {table} 不存在")
            except Exception as e:
                print(f"  ✗ 检查表 {table} 时出错: {e}")

        # 检查 DieInfo 表的新字段
        print("\n验证 DieInfo 表新字段...")
        new_columns = [
            'WorkOrderNo',
            'KnifeLengthM',
            'KnifeMarkLengthM',
            'BoardFeeUnitPrice',
            'BoardFee',
            'ProductionUnitPrice',
            'ProductionFee',
            'DesignUnitPrice',
            'DesignFee'
        ]

        for col in new_columns:
            try:
                cursor.execute(f"SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('DieInfo') AND name = '{col}'")
                exists = cursor.fetchone()[0] > 0
                if exists:
                    print(f"  ✓ 字段 {col} 存在")
                else:
                    print(f"  ✗ 字段 {col} 不存在")
            except Exception as e:
                print(f"  ✗ 检查字段 {col} 时出错: {e}")

        cursor.close()
        conn.close()
        print("\n数据库迁移完成！")
        return True

    except Exception as e:
        print(f"\n✗ 数据库迁移失败: {e}")
        return False

if __name__ == "__main__":
    success = execute_migration()
    sys.exit(0 if success else 1)
