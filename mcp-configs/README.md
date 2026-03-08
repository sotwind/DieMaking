# 易捷 Oracle MCP Server 配置

## 配置文件列表

已为您生成以下 5 个独立的 MCP 配置文件，分别对应易捷的五个数据库服务器：

| 文件名 | 数据库 | 主机 | 用户名 |
|--------|--------|------|--------|
| `yijie-xinchang.json` | 新厂新系统 | 36.134.7.141 | b0003 |
| `yijie-laochang.json` | 老厂新系统 | 36.138.132.30 | read |
| `yijie-linhai.json` | 临海老系统 | 36.137.213.189 | read |
| `yijie-wensen.json` | 温森新系统 | db.05.forestpacking.com | read |
| `yijie-group.json` | 易捷集团 | 36.138.130.91 | fgrp |

以及一个合并配置文件：
- `trae-yijie-all.json` - 包含所有五个服务器的配置

## Trae AI 使用方法

### 1. 打开 Trae AI 设置

在 Trae AI 中，进入 **Settings → MCP → Custom Configuration**

### 2. 添加配置

将 `trae-yijie-all.json` 的内容复制到 Trae AI 的 MCP 配置中。

### 3. 重启 Trae AI

保存配置后，重启 Trae AI 以加载 MCP Servers。

## 可用工具

连接成功后，您可以在 Trae AI 中使用以下工具：

- **get_table_schema** - 获取指定表的详细结构
- **get_tables_schema** - 批量获取多个表的结构
- **search_tables_schema** - 按名称模式搜索表
- **search_columns** - 搜索包含特定列的表
- **run_sql_query** - 执行 SQL 查询（只读模式）
- **get_related_tables** - 获取与指定表相关的外键关系表
- **get_table_constraints** - 获取表的约束信息
- **get_table_indexes** - 获取表的索引信息
- **rebuild_schema_cache** - 重建 schema 缓存
- **get_database_vendor_info** - 获取数据库版本信息
- **get_pl_sql_objects** - 获取 PL/SQL 对象信息
- **get_object_source** - 获取 PL/SQL 对象源代码

## 安全说明

- 所有配置默认启用 **只读模式** (`READ_ONLY_MODE: "1"`)
- 只允许 SELECT 查询，禁止 INSERT/UPDATE/DELETE/DDL 操作
- 如需写入操作，请将 `READ_ONLY_MODE` 改为 `"0"`

## Docker 镜像

使用的 Docker 镜像：`dmeppiel/oracle-mcp-server:latest`

镜像已预装：
- Oracle Instant Client v23.7
- Python 3.12+
- 支持 Oracle 19c 到 23ai
- 支持 linux/arm64 和 linux/amd64

## 测试连接

可以使用以下命令测试单个 MCP Server：

```bash
# 测试老厂新系统
docker run -i --rm \
  -e ORACLE_CONNECTION_STRING="read/ejsh.read@36.138.132.30:1521/dbms" \
  -e READ_ONLY_MODE="1" \
  dmeppiel/oracle-mcp-server
```

## 文件位置

所有配置文件位于：`/home/admin/.openclaw/workspace/mcp-configs/`
