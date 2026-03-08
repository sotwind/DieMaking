#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
检查 ORD_BAS 和 ORD_CT 表结构
"""

import cx_Oracle

# 数据库配置 - 老厂新系统
DB_CONFIG = {
    'user': 'read',
    'password': 'ejsh.read',
    'dsn': cx_Oracle.makedsn('36.138.132.30', 1521, service_name='dbms')
}

def main():
    print(">>> 连接老厂新系统数据库...")
    connection = cx_Oracle.connect(**DB_CONFIG)
    print("✓ 连接成功\n")
    
    cursor = connection.cursor()
    
    # 1. 查询 ORD_BAS 表结构
    print(">>> ORD_BAS 表结构:")
    sql = """
        SELECT column_name, data_type, data_length 
        FROM user_tab_columns 
        WHERE table_name = 'ORD_BAS'
        ORDER BY column_id
    """
    cursor.execute(sql)
    for row in cursor:
        print(f"  {row[0]} - {row[1]}({row[2]})")
    print()
    
    # 2. 查询 ORD_CT 表结构
    print(">>> ORD_CT 表结构:")
    sql = """
        SELECT column_name, data_type, data_length 
        FROM user_tab_columns 
        WHERE table_name = 'ORD_CT'
        ORDER BY column_id
    """
    cursor.execute(sql)
    for row in cursor:
        print(f"  {row[0]} - {row[1]}({row[2]})")
    print()
    
    # 3. 查询示例数据
    print(">>> ORD_BAS 示例数据:")
    sql = """
        SELECT * FROM ord_bas WHERE ROWNUM <= 2
    """
    cursor.execute(sql)
    columns = [desc[0] for desc in cursor.description]
    for row in cursor:
        for i, val in enumerate(row):
            print(f"  {columns[i]}: {val}")
        print()
    
    cursor.close()
    connection.close()
    print("查询完成！")

if __name__ == "__main__":
    main()
