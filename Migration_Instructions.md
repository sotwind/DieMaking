# Diemaking 数据库迁移执行说明

## 迁移内容概览

本次数据库迁移包含以下内容：

### 1. DieInfo 表新增字段
| 字段名 | 数据类型 | 说明 |
|--------|----------|------|
| WorkOrderNo | NVARCHAR(50) NULL | 工单号 |
| KnifeLengthM | DECIMAL(18,4) NULL | 刀线长度（米） |
| KnifeMarkLengthM | DECIMAL(18,4) NULL | 刀痕长度（米） |
| BoardFeeUnitPrice | DECIMAL(18,2) NOT NULL DEFAULT 90 | 板费单价（元/平方米） |
| BoardFee | DECIMAL(18,2) NULL | 板费金额 |
| ProductionUnitPrice | DECIMAL(18,2) NOT NULL DEFAULT 8 | 制作单价（元/平方米） |
| ProductionFee | DECIMAL(18,2) NULL | 制作费金额 |
| DesignUnitPrice | DECIMAL(18,2) NOT NULL DEFAULT 70 | 设计单价（元/平方米） |
| DesignFee | DECIMAL(18,2) NULL | 设计费金额 |

### 2. DieModificationRecord 表（改刀记录表）
| 字段名 | 数据类型 | 说明 |
|--------|----------|------|
| ModificationID | INT IDENTITY PRIMARY KEY | 主键 |
| DieID | INT NOT NULL | 刀模ID |
| DieCode | NVARCHAR(50) NOT NULL | 刀模编号 |
| CustomerName | NVARCHAR(100) NULL | 客户名称 |
| ProductName | NVARCHAR(100) NULL | 产品名称 |
| ModificationAmount | DECIMAL(18,2) NOT NULL | 改刀金额 |
| ModificationTime | DATETIME NOT NULL | 改刀时间 |
| ModifiedBy | NVARCHAR(50) NOT NULL | 操作人员 |
| Reason | NVARCHAR(500) NULL | 改刀原因 |
| Remark | NVARCHAR(500) NULL | 备注 |
| CreateTime | DATETIME NOT NULL | 创建时间 |

### 3. SystemConfig 表（系统配置表）
| 字段名 | 数据类型 | 说明 |
|--------|----------|------|
| ConfigID | INT IDENTITY PRIMARY KEY | 主键 |
| ConfigKey | NVARCHAR(100) NOT NULL UNIQUE | 配置键 |
| ConfigValue | NVARCHAR(500) NOT NULL | 配置值 |
| ConfigType | NVARCHAR(50) NULL | 配置类型 |
| Description | NVARCHAR(200) NULL | 描述 |
| UpdateTime | DATETIME NOT NULL | 更新时间 |
| UpdateUser | NVARCHAR(50) NULL | 更新用户 |

**默认配置：**
- BoardFeeUnitPrice: 90（板费单价默认值）
- ProductionUnitPrice: 8（制作单价默认值）
- DesignUnitPrice: 70（设计单价默认值）
- DefaultProcesses: 绘图,割板,弯刀,装刀,贴泡沫

### 4. ScanReportRecord 表（扫码报工记录表）
| 字段名 | 数据类型 | 说明 |
|--------|----------|------|
| RecordID | INT IDENTITY PRIMARY KEY | 主键 |
| WorkOrderNo | NVARCHAR(50) NOT NULL | 工单号 |
| DieID | INT NULL | 刀模ID |
| ProcessID | INT NULL | 工序ID |
| ProcessName | NVARCHAR(50) NULL | 工序名称 |
| ScanTime | DATETIME NOT NULL | 扫码时间 |
| OperatorNo | NVARCHAR(50) NULL | 操作员工号 |
| OperatorName | NVARCHAR(50) NULL | 操作员姓名 |
| DeviceInfo | NVARCHAR(200) NULL | 设备信息 |
| Status | INT NOT NULL DEFAULT 0 | 状态（0:成功, 1:失败） |
| ErrorMessage | NVARCHAR(500) NULL | 错误信息 |
| CreateTime | DATETIME NOT NULL | 创建时间 |

---

## 执行方式

### 方式一：使用 SQL Server Management Studio (SSMS)

1. 打开 SSMS
2. 连接到目标数据库服务器
3. 打开 `Migration_Execute_Manually.sql` 文件
4. 执行脚本（F5 或点击"执行"按钮）

### 方式二：使用 sqlcmd 命令行工具

```bash
# Windows 命令行
sqlcmd -S localhost -d DieMaking -i "Migration_Execute_Manually.sql"

# 如果需要指定用户名密码
sqlcmd -S localhost -U sa -P your_password -d DieMaking -i "Migration_Execute_Manually.sql"
```

### 方式三：使用 PowerShell

```powershell
Invoke-Sqlcmd -ServerInstance "localhost" -Database "DieMaking" -InputFile "Migration_Execute_Manually.sql"
```

---

## 数据库连接信息

根据 `DatabaseConfig.cs` 中的配置：

- **默认连接字符串**: `Server=localhost;Database=DieMaking;Trusted_Connection=True;TrustServerCertificate=True;`
- **环境变量**: 可设置 `DIEMAKING_DB_CONNECTION` 覆盖默认配置

---

## 迁移验证

执行完成后，请验证以下内容：

### 1. 检查 DieInfo 表字段
```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'DieInfo'
AND COLUMN_NAME IN ('WorkOrderNo', 'KnifeLengthM', 'KnifeMarkLengthM', 
                    'BoardFeeUnitPrice', 'BoardFee', 'ProductionUnitPrice', 
                    'ProductionFee', 'DesignUnitPrice', 'DesignFee')
ORDER BY ORDINAL_POSITION;
```

### 2. 检查新表是否创建
```sql
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('DieModificationRecord', 'SystemConfig', 'ScanReportRecord');
```

### 3. 检查 SystemConfig 默认数据
```sql
SELECT * FROM SystemConfig;
```

### 4. 检查费用计算是否正确
```sql
SELECT DieID, DieCode, BlankLength, BlankWidth, 
       BoardFee, ProductionFee, DesignFee
FROM DieInfo
WHERE BoardFee IS NOT NULL
LIMIT 10;
```

---

## 回滚脚本（如需撤销迁移）

**注意：回滚会删除新表和相关字段，请谨慎操作！**

```sql
-- 删除新表
DROP TABLE IF EXISTS ScanReportRecord;
DROP TABLE IF EXISTS SystemConfig;
DROP TABLE IF EXISTS DieModificationRecord;

-- 删除 DieInfo 新增字段
ALTER TABLE DieInfo DROP COLUMN IF EXISTS WorkOrderNo;
ALTER TABLE DieInfo DROP COLUMN IF EXISTS KnifeLengthM;
ALTER TABLE DieInfo DROP COLUMN IF EXISTS KnifeMarkLengthM;
ALTER TABLE DieInfo DROP COLUMN IF EXISTS BoardFeeUnitPrice;
ALTER TABLE DieInfo DROP COLUMN IF EXISTS BoardFee;
ALTER TABLE DieInfo DROP COLUMN IF EXISTS ProductionUnitPrice;
ALTER TABLE DieInfo DROP COLUMN IF EXISTS ProductionFee;
ALTER TABLE DieInfo DROP COLUMN IF EXISTS DesignUnitPrice;
ALTER TABLE DieInfo DROP COLUMN IF EXISTS DesignFee;
```

---

## 迁移状态

- [x] 迁移脚本已生成
- [ ] 已执行迁移
- [ ] 已验证迁移结果
- [ ] 已备份数据库（建议执行前备份）

---

**生成时间**: 2026-03-11
**脚本位置**: `/home/admin/.openclaw/workspace/DieMaking/Migration_Execute_Manually.sql`
