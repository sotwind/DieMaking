#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
查询新系统数据库的订单数据
"""

import cx_Oracle
import json
from datetime import datetime

# 数据库配置 - 老厂新系统
DB_CONFIG = {
    'user': 'read',
    'password': 'ejsh.read',
    'dsn': cx_Oracle.makedsn('36.138.132.30', 1521, service_name='dbms')
}

def execute_query(cursor, sql, params=None):
    """执行查询"""
    try:
        if params:
            cursor.execute(sql, params)
        else:
            cursor.execute(sql)
        
        columns = [desc[0] for desc in cursor.description]
        rows = []
        for row in cursor:
            row_dict = {}
            for i, col in enumerate(columns):
                val = row[i]
                if isinstance(val, datetime):
                    val = val.strftime('%Y-%m-%d %H:%M:%S')
                row_dict[col] = val
            rows.append(row_dict)
        
        return {
            'success': True,
            'columns': columns,
            'rows': rows
        }
    except Exception as e:
        return {
            'success': False,
            'error': str(e)
        }

def main():
    print(">>> 连接老厂新系统数据库...")
    connection = cx_Oracle.connect(**DB_CONFIG)
    print("✓ 连接成功\n")
    
    cursor = connection.cursor()
    
    # 1. 查询昨天的订单数量
    print(">>> 查询昨天（2026-03-04）的订单数量...")
    sql1 = """
        SELECT COUNT(*) as cnt 
        FROM ord_bas 
        WHERE created >= TO_DATE('2026-03-04', 'YYYY-MM-DD') 
          AND created < TO_DATE('2026-03-05', 'YYYY-MM-DD') 
          AND isactive = 'Y'
    """
    result1 = execute_query(cursor, sql1)
    print(f"昨天订单数：{result1['rows'][0]['CNT'] if result1['success'] and result1['rows'] else 0}\n")
    
    # 2. 查询今天到现在的订单数量
    print(">>> 查询今天（2026-03-05）的订单数量...")
    sql2 = """
        SELECT COUNT(*) as cnt 
        FROM ord_bas 
        WHERE created >= TO_DATE('2026-03-05', 'YYYY-MM-DD') 
          AND created < TO_DATE('2026-03-06', 'YYYY-MM-DD') 
          AND isactive = 'Y'
    """
    result2 = execute_query(cursor, sql2)
    print(f"今天订单数：{result2['rows'][0]['CNT'] if result2['success'] and result2['rows'] else 0}\n")
    
    # 3. 查询最近 3 天的订单数量
    print(">>> 查询最近 3 天的订单数量...")
    sql3 = """
        SELECT COUNT(*) as cnt 
        FROM ord_bas 
        WHERE created >= TO_DATE('2026-03-03', 'YYYY-MM-DD') 
          AND created < TO_DATE('2026-03-06', 'YYYY-MM-DD') 
          AND isactive = 'Y'
    """
    result3 = execute_query(cursor, sql3)
    print(f"最近 3 天订单数：{result3['rows'][0]['CNT'] if result3['success'] and result3['rows'] else 0}\n")
    
    # 4. 查询 ORD_BAS 表结构
    print(">>> 查询 ORD_BAS 表结构...")
    sql4 = """
        SELECT column_name, data_type, data_length 
        FROM user_tab_columns 
        WHERE table_name = 'ORD_BAS'
        ORDER BY column_id
    """
    result4 = execute_query(cursor, sql4)
    print("ORD_BAS 表字段:")
    if result4['success']:
        for row in result4['rows']:
            print(f"  {row['COLUMN_NAME']} - {row['DATA_TYPE']}({row['DATA_LENGTH']})")
    print()
    
    # 5. 查询 ORD_CT 表结构
    print(">>> 查询 ORD_CT 表结构...")
    sql5 = """
        SELECT column_name, data_type, data_length 
        FROM user_tab_columns 
        WHERE table_name = 'ORD_CT'
        ORDER BY column_id
    """
    result5 = execute_query(cursor, sql5)
    print("ORD_CT 表字段:")
    if result5['success']:
        for row in result5['rows']:
            print(f"  {row['COLUMN_NAME']} - {row['DATA_TYPE']}({row['DATA_LENGTH']})")
    print()
    
    # 6. 查询销售员图 SQL 测试（昨天数据）
    print(">>> 测试销售员图查询 SQL（昨天数据）...")
    sql6 = """
        SELECT b.objtyp, t.agntcde, nvl(sum(b.accamt),0) as 金额，
               nvl(sum(t.acreage * t.ordnum),0) as 面积，
               count(*) as 单数 
        FROM ord_bas b 
        JOIN ord_ct t ON b.serial = t.serial 
        WHERE b.status='Y' and b.isactive='Y'
          AND b.created >= TO_DATE('2026-03-04', 'YYYY-MM-DD') 
          AND b.created < TO_DATE('2026-03-05', 'YYYY-MM-DD')
        GROUP BY t.agntcde, b.objtyp 
        ORDER BY t.agntcde
    """
    result6 = execute_query(cursor, sql6)
    print(f"查询成功，返回 {len(result6['rows']) if result6['success'] else 0} 条数据")
    if result6['success'] and result6['rows']:
        print("前 5 条数据:")
        for row in result6['rows'][:5]:
            print(f"  {row}")
    print()
    
    cursor.close()
    connection.close()
    print("查询完成！")

if __name__ == "__main__":
    main()
