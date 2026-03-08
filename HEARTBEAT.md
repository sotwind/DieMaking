# HEARTBEAT.md - 定时任务处理

## 系统事件处理

### run_daily_orders_report
当收到此系统事件时，执行以下操作：
1. 运行 /home/admin/.openclaw/workspace/cron_daily_orders.py 脚本
2. 获取各厂区前一天接单情况
3. 将结果发送到钉钉群 "大龙虾测试群"

## 处理逻辑
```
if system_event == "run_daily_orders_report":
    - 执行 Python 脚本查询所有厂区数据库
    - 格式化报告
    - 使用 message 工具发送到钉钉群
```
