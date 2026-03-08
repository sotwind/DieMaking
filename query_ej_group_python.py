#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
查询易捷集团数据库
使用 cx_Oracle 连接 Oracle 数据库
"""

import cx_Oracle
import json
from datetime import datetime

# 数据库配置
DB_CONFIG = {
    'user': 'fgrp',
    'password': 'kuke.fgrp',
    'dsn': cx_Oracle.makedsn('36.138.130.91', 1521, service_name='dbms')
}

def print_line(char='=', count=80):
    print(char * count)

def describe_table(cursor, table_name):
    """查询表结构"""
    sql = """
        SELECT column_name, data_type, data_length 
        FROM user_tab_columns 
        WHERE table_name = :table_name
        ORDER BY column_id
    """
    cursor.execute(sql, table_name=table_name)
    columns = []
    for row in cursor:
        columns.append({
            'column_name': row[0],
            'data_type': row[1],
            'data_length': row[2]
        })
    return columns

def execute_query(cursor, sql, params=None):
    """执行查询"""
    try:
        if params:
            cursor.execute(sql, params)
        else:
            cursor.execute(sql)
        # 获取列名
        columns = [desc[0] for desc in cursor.description]
        
        # 获取所有行
        rows = []
        for row in cursor:
            row_dict = {}
            for i, col in enumerate(columns):
                val = row[i]
                # 处理日期类型
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
    print_line()
    print("oders: Oracle 数据库查询工具")
    print("oders: 查询易捷集团数据库")
    print_line()
    
    connection = None
    
    try:
        # 连接数据库
        print("oders: 连接数据库...")
        connection = cx_Oracle.connect(**DB_CONFIG)
        print("oders: ✓ 连接成功\n")
    except cx_Oracle.Error as e:
        print(f"oders: ❌ 连接失败: {e}")
        return
    
    cursor = connection.cursor()
    results = {}
    
    try:
        # 1. 查询 PB_DEPT_MEMBER 表结构
        print("oders: >>> 查询 PB_DEPT_MEMBER 表结构...")
        columns = describe_table(cursor, 'PB_DEPT_MEMBER')
        results['pb_dept_member_structure'] = {
            'table': 'PB_DEPT_MEMBER',
            'success': True,
            'columns': columns
        }
        print("oders: 字段列表:")
        for col in columns:
            print(f"oders:   {col['column_name']} - {col['data_type']}({col['data_length']})")
        print()
        
        # 2. 查询 PB_DEPT 表结构
        print("oders: >>> 查询 PB_DEPT 表结构...")
        columns = describe_table(cursor, 'PB_DEPT')
        results['pb_dept_structure'] = {
            'table': 'PB_DEPT',
            'success': True,
            'columns': columns
        }
        print("oders: 字段列表:")
        for col in columns:
            print(f"oders:   {col['column_name']} - {col['data_type']}({col['data_length']})")
        print()
        
        # 3. 执行修复后的 SQL 查询
        print("oders: >>> 执行修复后的 SQL 查询...")
        # 根据实际表结构调整 SQL 查询
        fixed_sql = """
            SELECT m.EMPCDE, 
                   m.DPTNME, 
                   m.EMPNME
            FROM pb_dept_member m
            WHERE m.ISACTIVE = 'Y'
            ORDER BY m.DPTNME, m.EMPNME
        """
        result = execute_query(cursor, fixed_sql)
        results['fixed_query'] = {
            'sql': fixed_sql.strip(),
            **result
        }
        if result['success']:
            print(f"oders: 查询成功，返回 {len(result['rows'])} 条数据")
            if len(result['rows']) > 0:
                print(f"oders: 列名: {', '.join(result['columns'])}")
                print("oders: 前5条数据:")
                for idx, row in enumerate(result['rows'][:5], 1):
                    print(f"oders:   [{idx}] {json.dumps(row, ensure_ascii=False)}")
        else:
            print(f"oders: 查询失败: {result['error']}")
        print()
        
        # 4. 查询昨天（2026-03-04）到今天（2026-03-05）的订单数量
        print("oders: >>> 查询昨天（2026-03-04）到今天（2026-03-05）的订单数量...")
        sql_orders = """
            SELECT COUNT(*) as cnt 
            FROM ord_bas 
            WHERE created >= TO_DATE(:date_from, 'YYYY-MM-DD') 
              AND created < TO_DATE(:date_to, 'YYYY-MM-DD') 
              AND isactive = 'Y'
        """
        result = execute_query(cursor, sql_orders, {
            'date_from': '2026-03-04',
            'date_to': '2026-03-05'
        })
        results['order_count'] = {
            'sql': sql_orders.strip(),
            'date_from': '2026-03-04',
            'date_to': '2026-03-05',
            **result
        }
        if result['success'] and len(result['rows']) > 0:
            print(f"oders: 昨天（2026-03-04）到今天（2026-03-05）的订单数量: {result['rows'][0]['CNT']}")
        else:
            print(f"oders: 查询失败: {result['error']}")
        print()
        
        # 输出 JSON 结果
        print_line()
        print("oders: 📤 JSON 输出:")
        print_line()
        print(json.dumps(results, ensure_ascii=False, indent=2, default=str))
        
        cursor.close()
        connection.close()
        print("\noders: 🔒 连接已关闭")
        
    except cx_Oracle.Error as e:
        print(f"oders: ❌ Oracle 错误: {e}")
        if cursor:
            try:
                cursor.close()
            except:
                pass
        if connection:
            try:
                connection.close()
                print("oders: 🔒 尝试关闭连接")
            except:
                pass
    except Exception as e:
        print(f"oders: ❌ 执行出错：{e}")

if __name__ == '__main__':
    main()
