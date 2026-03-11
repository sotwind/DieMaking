# Diemaking 数据库迁移任务执行报告

## 任务概述

执行 Diemaking 项目的数据库迁移，包括：
1. DieInfo 表新增 9 个字段
2. 创建 DieModificationRecord 表
3. 创建 SystemConfig 表
4. 创建 ScanReportRecord 表

## 执行结果

### ❌ 无法直接执行迁移

**原因：**
1. 当前环境没有安装 `sqlcmd` 命令行工具
2. 当前环境没有安装 `dotnet` CLI 工具
3. 无法直接连接到 SQL Server 数据库（连接超时）

**环境信息：**
- 操作系统: Linux 5.10.134-19.2.al8.x86_64
- Python: 可用（pyodbc 已安装）
- sqlcmd: 未安装
- dotnet: 未安装

---

## 生成的迁移文件

已生成以下文件供手动执行：

### 1. SQL 迁移脚本
**文件**: `Migration_Execute_Manually.sql`
**位置**: `/home/admin/.openclaw/workspace/DieMaking/Migration_Execute_Manually.sql`
**说明**: 完整的 T-SQL 迁移脚本，包含所有表结构变更

### 2. Python 迁移脚本
**文件**: `run_migration.py`
**位置**: `/home/admin/.openclaw/workspace/DieMaking/run_migration.py`
**说明**: Python 迁移工具，可在有数据库访问权限的环境中执行

**使用方法**:
```bash
# 使用默认连接字符串
python run_migration.py

# 使用自定义连接字符串
python run_migration.py -c "DRIVER={ODBC Driver 17 for SQL Server};SERVER=your_server;DATABASE=DieMaking;UID=sa;PWD=your_password;"

# 仅查看将要执行的 SQL
python run_migration.py --dry-run
```

### 3. 迁移说明文档
**文件**: `Migration_Instructions.md`
**位置**: `/home/admin/.openclaw/workspace/DieMaking/Migration_Instructions.md`
**说明**: 详细的迁移说明，包含字段列表、执行方式、验证方法等

---

## 迁移内容详情

### 1. DieInfo 表新增字段（9个）

| 序号 | 字段名 | 数据类型 | 默认值 | 说明 |
|------|--------|----------|--------|------|
| 1 | WorkOrderNo | NVARCHAR(50) | NULL | 工单号 |
| 2 | KnifeLengthM | DECIMAL(18,4) | NULL | 刀线长度（米） |
| 3 | KnifeMarkLengthM | DECIMAL(18,4) | NULL | 刀痕长度（米） |
| 4 | BoardFeeUnitPrice | DECIMAL(18,2) | 90 | 板费单价（元/平方米） |
| 5 | BoardFee | DECIMAL(18,2) | NULL | 板费金额 |
| 6 | ProductionUnitPrice | DECIMAL(18,2) | 8 | 制作单价（元/平方米） |
| 7 | ProductionFee | DECIMAL(18,2) | NULL | 制作费金额 |
| 8 | DesignUnitPrice | DECIMAL(18,2) | 70 | 设计单价（元/平方米） |
| 9 | DesignFee | DECIMAL(18,2) | NULL | 设计费金额 |

### 2. DieModificationRecord 表（改刀记录表）

| 字段名 | 数据类型 | 约束 | 说明 |
|--------|----------|------|------|
| ModificationID | INT | IDENTITY PRIMARY KEY | 主键 |
| DieID | INT | NOT NULL, FK | 刀模ID |
| DieCode | NVARCHAR(50) | NOT NULL | 刀模编号 |
| CustomerName | NVARCHAR(100) | NULL | 客户名称 |
| ProductName | NVARCHAR(100) | NULL | 产品名称 |
| ModificationAmount | DECIMAL(18,2) | NOT NULL | 改刀金额 |
| ModificationTime | DATETIME | DEFAULT GETDATE() | 改刀时间 |
| ModifiedBy | NVARCHAR(50) | NOT NULL | 操作人员 |
| Reason | NVARCHAR(500) | NULL | 改刀原因 |
| Remark | NVARCHAR(500) | NULL | 备注 |
| CreateTime | DATETIME | DEFAULT GETDATE() | 创建时间 |

**索引**:
- IX_DieModificationRecord_DieID (DieID)
- IX_DieModificationRecord_ModificationTime (ModificationTime)

### 3. SystemConfig 表（系统配置表）

| 字段名 | 数据类型 | 约束 | 说明 |
|--------|----------|------|------|
| ConfigID | INT | IDENTITY PRIMARY KEY | 主键 |
| ConfigKey | NVARCHAR(100) | NOT NULL UNIQUE | 配置键 |
| ConfigValue | NVARCHAR(500) | NOT NULL | 配置值 |
| ConfigType | NVARCHAR(50) | NULL | 配置类型 |
| Description | NVARCHAR(200) | NULL | 描述 |
| UpdateTime | DATETIME | DEFAULT GETDATE() | 更新时间 |
| UpdateUser | NVARCHAR(50) | NULL | 更新用户 |

**默认配置数据**:
| ConfigKey | ConfigValue | ConfigType | Description |
|-----------|-------------|------------|-------------|
| BoardFeeUnitPrice | 90 | decimal | 板费单价默认值（元/平方米） |
| ProductionUnitPrice | 8 | decimal | 制作单价默认值（元/平方米） |
| DesignUnitPrice | 70 | decimal | 设计单价默认值（元/平方米） |
| DefaultProcesses | 绘图,割板,弯刀,装刀,贴泡沫 | string | 默认工序列表 |

### 4. ScanReportRecord 表（扫码报工记录表）

| 字段名 | 数据类型 | 约束 | 说明 |
|--------|----------|------|------|
| RecordID | INT | IDENTITY PRIMARY KEY | 主键 |
| WorkOrderNo | NVARCHAR(50) | NOT NULL | 工单号 |
| DieID | INT | NULL | 刀模ID |
| ProcessID | INT | NULL | 工序ID |
| ProcessName | NVARCHAR(50) | NULL | 工序名称 |
| ScanTime | DATETIME | DEFAULT GETDATE() | 扫码时间 |
| OperatorNo | NVARCHAR(50) | NULL | 操作员工号 |
| OperatorName | NVARCHAR(50) | NULL | 操作员姓名 |
| DeviceInfo | NVARCHAR(200) | NULL | 设备信息 |
| Status | INT | DEFAULT 0 | 状态（0:成功, 1:失败） |
| ErrorMessage | NVARCHAR(500) | NULL | 错误信息 |
| CreateTime | DATETIME | DEFAULT GETDATE() | 创建时间 |

**索引**:
- IX_ScanReportRecord_WorkOrderNo (WorkOrderNo)
- IX_ScanReportRecord_ScanTime (ScanTime)

---

## 数据库连接信息

根据 `DatabaseConfig.cs` 中的配置：

**默认连接字符串**:
```
Server=localhost;Database=DieMaking;Trusted_Connection=True;TrustServerCertificate=True;
```

**ODBC 连接字符串**:
```
DRIVER={ODBC Driver 17 for SQL Server};SERVER=localhost;DATABASE=DieMaking;Trusted_Connection=yes;TrustServerCertificate=yes;
```

**环境变量**: 可设置 `DIEMAKING_DB_CONNECTION` 覆盖默认配置

---

## 手动执行步骤

### 方式一：使用 SQL Server Management Studio (SSMS)

1. 打开 SSMS 并连接到数据库服务器
2. 打开 `Migration_Execute_Manually.sql` 文件
3. 执行脚本（F5）

### 方式二：使用 sqlcmd

```bash
sqlcmd -S localhost -d DieMaking -i "Migration_Execute_Manually.sql"
```

### 方式三：使用 Python 脚本

在有数据库访问权限的环境中：

```bash
cd /home/admin/.openclaw/workspace/DieMaking
python run_migration.py
```

---

## 迁移验证

执行完成后，请运行以下 SQL 验证迁移结果：

```sql
-- 1. 验证 DieInfo 表字段
SELECT COLUMN_NAME, DATA_TYPE 
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'DieInfo'
AND COLUMN_NAME IN ('WorkOrderNo', 'KnifeLengthM', 'KnifeMarkLengthM', 
                    'BoardFeeUnitPrice', 'BoardFee', 'ProductionUnitPrice', 
                    'ProductionFee', 'DesignUnitPrice', 'DesignFee');

-- 2. 验证新表
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('DieModificationRecord', 'SystemConfig', 'ScanReportRecord');

-- 3. 验证 SystemConfig 数据
SELECT * FROM SystemConfig;

-- 4. 验证费用计算
SELECT TOP 10 DieID, DieCode, BoardFee, ProductionFee, DesignFee
FROM DieInfo WHERE BoardFee IS NOT NULL;
```

---

## 文件清单

| 文件名 | 路径 | 说明 |
|--------|------|------|
| Migration_Execute_Manually.sql | DieMaking/Migration_Execute_Manually.sql | SQL 迁移脚本 |
| run_migration.py | DieMaking/run_migration.py | Python 迁移工具 |
| Migration_Instructions.md | DieMaking/Migration_Instructions.md | 详细说明文档 |
| Migration_Report.md | DieMaking/Migration_Report.md | 本报告 |

---

## 后续建议

1. **在开发环境先测试**: 建议在开发环境先执行迁移脚本，确认无误后再在生产环境执行
2. **备份数据库**: 执行迁移前请备份数据库
3. **检查依赖**: 确保应用程序代码已更新以使用新字段
4. **更新文档**: 迁移完成后更新相关技术文档

---

**报告生成时间**: 2026-03-11
**任务状态**: 迁移脚本已生成，等待手动执行
