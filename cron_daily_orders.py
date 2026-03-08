#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
每日订单统计 - 定时任务
每天早上8点查询各厂区前一天的接单情况，并发送到钉钉群
"""

import cx_Oracle
import json
from datetime import datetime, timedelta
import os
import subprocess

# 各厂区数据库配置
DB_CONFIGS = {
    "集团总部": {
        "host": "36.138.130.91",
        "port": 1521,
        "service_name": "dbms",
        "user": "fgrp",
        "password": "kuke.fgrp"
    },
    "临海厂区": {
        "host": "36.137.213.189",
        "port": 1521,
        "service_name": "dbms",
        "user": "read",
        "password": "ejsh.read"
    },
    "新昌厂区": {
        "host": "36.134.7.141",
        "port": 1521,
        "service_name": "dbms",
        "user": "b0003",
        "password": "kuke.b0003"
    },
    "老厂厂区": {
        "host": "36.138.132.30",
        "port": 1521,
        "service_name": "dbms",
        "user": "read",
        "password": "ejsh.read"
    },
    "文森厂区": {
        "host": "db.05.forestpacking.com",
        "port": 1521,
        "service_name": "dbms",
        "user": "read",
        "password": "ejsh.read"
    }
}

def get_db_connection(config):
    """建立数据库连接"""
    dsn = cx_Oracle.makedsn(
        config["host"],
        config["port"],
        service_name=config["service_name"]
    )
    return cx_Oracle.connect(
        user=config["user"],
        password=config["password"],
        dsn=dsn
    )

def query_yesterday_orders(cursor, yesterday_str, today_str):
    """查询昨天订单数量"""
    sql = """
        SELECT COUNT(*) as cnt 
        FROM ord_bas 
        WHERE created >= TO_DATE(:date_from, 'YYYY-MM-DD') 
          AND created < TO_DATE(:date_to, 'YYYY-MM-DD') 
          AND isactive = 'Y'
    """
    cursor.execute(sql, {
        'date_from': yesterday_str,
        'date_to': today_str
    })
    result = cursor.fetchone()
    return result[0] if result else 0

def query_yesterday_order_details(cursor, yesterday_str, today_str):
    """查询昨天订单详细统计（按部门/销售员分组）"""
    sql = """
        SELECT 
            d.deptcde as dept_code,
            d.deptnme as dept_name,
            COUNT(*) as order_count,
            SUM(NVL(o.totamt, 0)) as total_amount
        FROM ord_bas o
        LEFT JOIN pb_dept d ON o.deptcde = d.deptcde
        WHERE o.created >= TO_DATE(:date_from, 'YYYY-MM-DD') 
          AND o.created < TO_DATE(:date_to, 'YYYY-MM-DD') 
          AND o.isactive = 'Y'
        GROUP BY d.deptcde, d.deptnme
        ORDER BY order_count DESC
    """
    try:
        cursor.execute(sql, {
            'date_from': yesterday_str,
            'date_to': today_str
        })
        rows = []
        for row in cursor:
            rows.append({
                'dept_code': row[0],
                'dept_name': row[1] or '未知部门',
                'order_count': row[2],
                'total_amount': float(row[3]) if row[3] else 0
            })
        return rows
    except Exception as e:
        # 如果表结构不同，返回空列表
        return []

def get_all_plants_orders():
    """获取所有厂区昨天的订单统计"""
    # 计算昨天日期
    today = datetime.now()
    yesterday = today - timedelta(days=1)
    yesterday_str = yesterday.strftime('%Y-%m-%d')
    today_str = today.strftime('%Y-%m-%d')
    
    results = {
        "report_date": yesterday_str,
        "generated_at": today.strftime('%Y-%m-%d %H:%M:%S'),
        "plants": {}
    }
    
    for plant_name, config in DB_CONFIGS.items():
        try:
            conn = get_db_connection(config)
            cursor = conn.cursor()
            
            # 查询订单总数
            order_count = query_yesterday_orders(cursor, yesterday_str, today_str)
            
            # 查询详细统计
            details = query_yesterday_order_details(cursor, yesterday_str, today_str)
            
            results["plants"][plant_name] = {
                "status": "success",
                "order_count": order_count,
                "details": details
            }
            
            cursor.close()
            conn.close()
            
        except Exception as e:
            results["plants"][plant_name] = {
                "status": "error",
                "error": str(e),
                "order_count": 0,
                "details": []
            }
    
    return results

def format_report(data):
    """格式化报告为文本"""
    report_date = data["report_date"]
    lines = []
    lines.append(f"📊 易捷各厂区接单日报 ({report_date})")
    lines.append("=" * 50)
    lines.append("")
    
    total_orders = 0
    
    for plant_name, plant_data in data["plants"].items():
        if plant_data["status"] == "success":
            count = plant_data["order_count"]
            total_orders += count
            lines.append(f"🏭 {plant_name}: {count} 单")
            
            # 显示详细分组（如果有）
            if plant_data.get("details"):
                for detail in plant_data["details"][:5]:  # 只显示前5个部门
                    dept_name = detail.get("dept_name", "未知")
                    dept_count = detail.get("order_count", 0)
                    lines.append(f"   └─ {dept_name}: {dept_count} 单")
        else:
            lines.append(f"🏭 {plant_name}: ❌ 查询失败 ({plant_data.get('error', '未知错误')})")
        lines.append("")
    
    lines.append("=" * 50)
    lines.append(f"📈 合计: {total_orders} 单")
    lines.append(f"⏰ 生成时间: {data['generated_at']}")
    
    return "\n".join(lines)

def send_to_dingtalk(message):
    """发送消息到钉钉群"""
    try:
        # 使用 openclaw message 命令发送消息
        cmd = [
            "openclaw", "message", "send",
            "--channel", "dingtalk",
            "--target", "大龙虾测试群",
            "--message", message
        ]
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=30)
        if result.returncode == 0:
            print("✅ 消息已发送到钉钉群")
            return True
        else:
            print(f"❌ 发送失败: {result.stderr}")
            return False
    except Exception as e:
        print(f"❌ 发送异常: {e}")
        return False

def main():
    """主函数"""
    print("开始执行每日订单统计...")
    
    # 获取数据
    data = get_all_plants_orders()
    
    # 格式化报告
    report = format_report(data)
    
    # 输出报告
    print(report)
    
    # 保存到文件
    output_dir = "/home/admin/.openclaw/workspace/reports"
    os.makedirs(output_dir, exist_ok=True)
    
    filename = f"daily_orders_{data['report_date']}.txt"
    filepath = os.path.join(output_dir, filename)
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(report)
    
    print(f"\n报告已保存: {filepath}")
    
    # 同时输出JSON格式供其他程序使用
    json_filename = f"daily_orders_{data['report_date']}.json"
    json_filepath = os.path.join(output_dir, json_filename)
    
    with open(json_filepath, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    
    print(f"JSON数据已保存: {json_filepath}")
    
    # 发送到钉钉群
    print("\n正在发送消息到钉钉群...")
    send_to_dingtalk(report)
    
    return report

if __name__ == '__main__':
    main()
