#!/usr/bin/env python3
"""
检查 SQL Server 数据库中的表
"""
import pyodbc

SERVER = "36.139.89.173"
DATABASE = "2026纸箱报价系统"
USERNAME = "sa"
PASSWORD = "slbz_888"

CONN_STR = f"DRIVER={{ODBC Driver 17 for SQL Server}};SERVER={SERVER};DATABASE={DATABASE};UID={USERNAME};PWD={PASSWORD};TrustServerCertificate=yes"

try:
    conn = pyodbc.connect(CONN_STR, timeout=30)
    cursor = conn.cursor()
    print(f"已连接到数据库: {DATABASE}")
    print("\n数据库中的表:")
    print("="*50)

    cursor.execute("SELECT name FROM sys.tables ORDER BY name")
    tables = cursor.fetchall()

    if tables:
        for i, table in enumerate(tables, 1):
            print(f"  {i}. {table[0]}")
    else:
        print("  (数据库中没有表)")

    print(f"\n总计: {len(tables)} 个表")

    cursor.close()
    conn.close()

except Exception as e:
    print(f"错误: {e}")
