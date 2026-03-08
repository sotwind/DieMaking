#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
检查 ORD_BAS 和 ORD_CT 表结构
"""

import cx_Oracle
import json

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
    
    # 3. 测试简单的 JOIN 查询
    print(">>> 测试简单的 JOIN 查询:")
    sql = """
        SELECT b.serial, b.objtyp, t.agntcde, b.accamt, t.acreage, t.ordnum
        FROM ord_bas b 
        JOIN ord_ct t ON b.serial = t.serial 
        WHERE b.status='Y' and b.isactive='Y'
          AND b.created >= TO_DATE('2026-03-04', 'YYYY-MM-DD') 
          AND b.created < TO_DATE('2026-03-05', 'YYYY-MM-DD')
        AND ROWNUM <= 5
    """
    cursor.execute(sql)
    print("查询结果:")
    for row in cursor:
        print(f"  {row}")
    print()
    
    # 4. 检查 objtyp 字段值
    print(">>> 检查 objtyp 字段值:")
    sql = """
        SELECT DISTINCT objtyp 
        FROM ord_bas 
        WHERE isactive = 'Y'
        AND created >= TO_DATE('2026-03-04', 'YYYY-MM-DD')
    """
    cursor.execute(sql)
    for row in cursor:
        print(f"  objtyp: {row[0]}")
    print()
    
    # 5. 检查 agntcde 字段值
    print(">>> 检查 agntcde 字段值:")
    sql = """
        SELECT DISTINCT agntcde 
        FROM ord_ct 
        WHERE agntcde IS NOT NULL
        AND ROWNUM <= 10
    """
    cursor.execute(sql)
    for row in cursor:
        print(f"  agntcde: {row[0]}")
    print()
    
    cursor.close()
    connection.close()
    print("查询完成！")

if __name__ == "__main__":
    main()
