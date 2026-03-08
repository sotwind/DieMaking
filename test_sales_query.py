#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
测试销售员图查询 SQL
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
    
    # 1. 测试销售员图查询 SQL（昨天数据）- 不使用 STATUS 字段
    print(">>> 测试销售员图查询 SQL（昨天数据）...")
    sql = """
        SELECT b.objtyp, t.agntcde, nvl(sum(b.accamt),0) as 金额，
               nvl(sum(t.acreage * t.ordnum),0) as 面积，
               count(*) as 单数 
        FROM ord_bas b 
        JOIN ord_ct t ON b.serial = t.serial 
        WHERE b.isactive='Y'
          AND b.created >= TO_DATE('2026-03-04', 'YYYY-MM-DD') 
          AND b.created < TO_DATE('2026-03-05', 'YYYY-MM-DD')
        GROUP BY t.agntcde, b.objtyp 
        ORDER BY t.agntcde
    """
    cursor.execute(sql)
    rows = cursor.fetchall()
    print(f"查询成功，返回 {len(rows)} 条数据")
    if rows:
        print("前 10 条数据:")
        for row in rows[:10]:
            print(f"  objtyp={row[0]}, agntcde={row[1]}, 金额={row[2]}, 面积={row[3]}, 单数={row[4]}")
    print()
    
    # 2. 测试使用 ptdate 字段
    print(">>> 测试使用 ptdate 字段的查询...")
    sql2 = """
        SELECT b.objtyp, t.agntcde, nvl(sum(b.accamt),0) as 金额，
               nvl(sum(t.acreage * t.ordnum),0) as 面积，
               count(*) as 单数 
        FROM ord_bas b 
        JOIN ord_ct t ON b.serial = t.serial 
        WHERE b.isactive='Y'
          AND b.ptdate >= TO_DATE('2026-03-04', 'YYYY-MM-DD') 
          AND b.ptdate < TO_DATE('2026-03-05', 'YYYY-MM-DD')
        GROUP BY t.agntcde, b.objtyp 
        ORDER BY t.agntcde
    """
    try:
        cursor.execute(sql2)
        rows2 = cursor.fetchall()
        print(f"查询成功，返回 {len(rows2)} 条数据")
        if rows2:
            print("前 10 条数据:")
            for row in rows2[:10]:
                print(f"  objtyp={row[0]}, agntcde={row[1]}, 金额={row[2]}, 面积={row[3]}, 单数={row[4]}")
    except Exception as e:
        print(f"查询失败：{e}")
    print()
    
    # 3. 检查 ORD_BAS 表是否有 PTDATE 字段
    print(">>> 检查 ORD_BAS 表是否有 PTDATE 字段...")
    sql3 = """
        SELECT column_name 
        FROM user_tab_columns 
        WHERE table_name = 'ORD_BAS' 
        AND column_name LIKE '%DATE%'
    """
    cursor.execute(sql3)
    date_cols = cursor.fetchall()
    print(f"ORD_BAS 表中包含 DATE 的字段:")
    for row in date_cols:
        print(f"  {row[0]}")
    print()
    
    # 4. 检查 ORD_CT 表结构
    print(">>> ORD_CT 表字段:")
    sql4 = """
        SELECT column_name, data_type, data_length 
        FROM user_tab_columns 
        WHERE table_name = 'ORD_CT'
        ORDER BY column_id
    """
    cursor.execute(sql4)
    for row in cursor:
        print(f"  {row[0]} - {row[1]}({row[2]})")
    print()
    
    cursor.close()
    connection.close()
    print("查询完成！")

if __name__ == "__main__":
    main()
