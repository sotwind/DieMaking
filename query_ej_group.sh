#!/bin/bash
# Oracle Database Query Script for 易捷集团
# 使用外部设置的 LD_LIBRARY_PATH 运行 Python 脚本

export LD_LIBRARY_PATH=/home/admin/oracle_instantclient/instantclient_21_1

# 运行 Python 脚本
exec python3 /home/admin/.openclaw/workspace/query_ej_group_python.py
