#!/bin/bash
# 每日订单统计定时任务脚本
# 每天早上8点执行

cd /home/admin/.openclaw/workspace

# 运行Python脚本并捕获输出
python3 cron_daily_orders.py > /tmp/daily_orders_output.txt 2>&1

# 读取报告内容
REPORT=$(cat /tmp/daily_orders_output.txt)

# 发送到钉钉群（通过OpenClaw message工具）
echo "$REPORT"
