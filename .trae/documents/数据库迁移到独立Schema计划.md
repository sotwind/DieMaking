# 数据库迁移到独立 Schema 计划

## 目标
将刀模管理系统的数据表从 `2026纸箱报价系统` 数据库的 `dbo` schema 迁移到独立的 `diemaking` schema 下。

## 当前状态
- **源数据库**: `2026纸箱报价系统`
- **当前 Schema**: `dbo`
- **需要迁移的表**:
  - DieInfo
  - DieProcess
  - DieModificationRecord
  - ScanReportRecord

## 迁移步骤

### 阶段1: 创建 diemaking Schema
在 `2026纸箱报价系统` 数据库中创建新的 schema。

```sql
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'diemaking')
BEGIN
    EXEC('CREATE SCHEMA diemaking');
END
```

### 阶段2: 创建新表结构
在 `diemaking` schema 下创建与源表结构相同的新表。

```sql
-- 创建 DieInfo 表
CREATE TABLE diemaking.DieInfo (
    DieID INT IDENTITY(1,1) PRIMARY KEY,
    DieCode NVARCHAR(50) NOT NULL UNIQUE,
    WorkOrderNo NVARCHAR(50) NULL,
    CustomerName NVARCHAR(100) NOT NULL,
    ProductName NVARCHAR(100) NOT NULL,
    Structure NVARCHAR(200) NULL,
    ModelType NVARCHAR(50) NULL,
    LayoutType NVARCHAR(50) NULL,
    FluteType NVARCHAR(50) NULL,
    Material NVARCHAR(100) NULL,
    ManufactureLength DECIMAL(10,2) NULL,
    ManufactureWidth DECIMAL(10,2) NULL,
    ManufactureHeight DECIMAL(10,2) NULL,
    BlankLength DECIMAL(10,2) NULL,
    BlankWidth DECIMAL(10,2) NULL,
    KnifeLengthM DECIMAL(10,3) NULL,
    KnifeMarkLengthM DECIMAL(10,3) NULL,
    BoardFeeUnitPrice DECIMAL(10,2) DEFAULT 90.00,
    BoardFee DECIMAL(10,2) NULL,
    ProductionUnitPrice DECIMAL(10,2) DEFAULT 8.00,
    ProductionFee DECIMAL(10,2) NULL,
    DesignUnitPrice DECIMAL(10,2) DEFAULT 70.00,
    DesignFee DECIMAL(10,2) NULL,
    ProcessDesc NVARCHAR(500) NULL,
    RequiredProcesses NVARCHAR(200) NULL,
    [Status] INT DEFAULT 0,
    AuditStatus INT DEFAULT 0,
    SourceFactory NVARCHAR(50) NULL,
    ExternalOrderID INT NULL,
    ExternalOrderNo NVARCHAR(50) NULL,
    DeliveryDate DATETIME NULL,
    CreateTime DATETIME DEFAULT GETDATE(),
    CreateUser NVARCHAR(50) NULL,
    UpdateTime DATETIME DEFAULT GETDATE(),
    UpdateUser NVARCHAR(50) NULL,
    Remark NVARCHAR(500) NULL
);

-- 创建 DieProcess 表
CREATE TABLE diemaking.DieProcess (
    ProcessID INT IDENTITY(1,1) PRIMARY KEY,
    DieID INT NOT NULL,
    ProcessName NVARCHAR(50) NOT NULL,
    ProcessOrder INT DEFAULT 0,
    [Status] INT DEFAULT 0,
    StartTime DATETIME NULL,
    CompleteTime DATETIME NULL,
    OperatorNo NVARCHAR(50) NULL,
    OperatorName NVARCHAR(50) NULL,
    Amount DECIMAL(10,2) NULL,
    PrevProcessID INT NULL,
    CreateTime DATETIME DEFAULT GETDATE(),
    Remark NVARCHAR(500) NULL,
    FOREIGN KEY (DieID) REFERENCES diemaking.DieInfo(DieID) ON DELETE CASCADE
);

-- 创建 DieModificationRecord 表
CREATE TABLE diemaking.DieModificationRecord (
    ModificationID INT IDENTITY(1,1) PRIMARY KEY,
    DieID INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    ModificationTime DATETIME DEFAULT GETDATE(),
    ModifierNo NVARCHAR(50) NULL,
    ModifierName NVARCHAR(50) NULL,
    Reason NVARCHAR(500) NULL,
    CreateTime DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (DieID) REFERENCES diemaking.DieInfo(DieID) ON DELETE CASCADE
);

-- 创建 ScanReportRecord 表
CREATE TABLE diemaking.ScanReportRecord (
    RecordID INT IDENTITY(1,1) PRIMARY KEY,
    WorkOrderNo NVARCHAR(50) NOT NULL,
    DieID INT NULL,
    ProcessName NVARCHAR(50) NULL,
    ScanTime DATETIME DEFAULT GETDATE(),
    OperatorNo NVARCHAR(50) NULL,
    OperatorName NVARCHAR(50) NULL,
    [Status] INT DEFAULT 0,
    ErrorMessage NVARCHAR(500) NULL,
    CreateTime DATETIME DEFAULT GETDATE()
);
```

### 阶段3: 迁移数据
将数据从 `dbo` schema 迁移到 `diemaking` schema。

```sql
-- 迁移 DieInfo 数据
SET IDENTITY_INSERT diemaking.DieInfo ON;
INSERT INTO diemaking.DieInfo (
    DieID, DieCode, WorkOrderNo, CustomerName, ProductName, Structure, ModelType, 
    LayoutType, FluteType, Material, ManufactureLength, ManufactureWidth, ManufactureHeight,
    BlankLength, BlankWidth, KnifeLengthM, KnifeMarkLengthM, BoardFeeUnitPrice, BoardFee,
    ProductionUnitPrice, ProductionFee, DesignUnitPrice, DesignFee, ProcessDesc, 
    RequiredProcesses, [Status], AuditStatus, SourceFactory, ExternalOrderID, 
    ExternalOrderNo, DeliveryDate, CreateTime, CreateUser, UpdateTime, UpdateUser, Remark
)
SELECT * FROM dbo.DieInfo;
SET IDENTITY_INSERT diemaking.DieInfo OFF;

-- 迁移 DieProcess 数据
SET IDENTITY_INSERT diemaking.DieProcess ON;
INSERT INTO diemaking.DieProcess (
    ProcessID, DieID, ProcessName, ProcessOrder, [Status], StartTime, CompleteTime,
    OperatorNo, OperatorName, Amount, PrevProcessID, CreateTime, Remark
)
SELECT * FROM dbo.DieProcess;
SET IDENTITY_INSERT diemaking.DieProcess OFF;

-- 迁移 DieModificationRecord 数据
SET IDENTITY_INSERT diemaking.DieModificationRecord ON;
INSERT INTO diemaking.DieModificationRecord (
    ModificationID, DieID, Amount, ModificationTime, ModifierNo, ModifierName, Reason, CreateTime
)
SELECT * FROM dbo.DieModificationRecord;
SET IDENTITY_INSERT diemaking.DieModificationRecord OFF;

-- 迁移 ScanReportRecord 数据
SET IDENTITY_INSERT diemaking.ScanReportRecord ON;
INSERT INTO diemaking.ScanReportRecord (
    RecordID, WorkOrderNo, DieID, ProcessName, ScanTime, OperatorNo, OperatorName,
    [Status], ErrorMessage, CreateTime
)
SELECT * FROM dbo.ScanReportRecord;
SET IDENTITY_INSERT diemaking.ScanReportRecord OFF;
```

### 阶段4: 创建索引
```sql
CREATE INDEX IX_DieInfo_WorkOrderNo ON diemaking.DieInfo(WorkOrderNo);
CREATE INDEX IX_DieInfo_CustomerName ON diemaking.DieInfo(CustomerName);
CREATE INDEX IX_DieInfo_Status ON diemaking.DieInfo([Status]);
CREATE INDEX IX_DieInfo_CreateTime ON diemaking.DieInfo(CreateTime);
CREATE INDEX IX_DieProcess_DieID ON diemaking.DieProcess(DieID);
CREATE INDEX IX_DieModificationRecord_DieID ON diemaking.DieModificationRecord(DieID);
CREATE INDEX IX_ScanReportRecord_WorkOrderNo ON diemaking.ScanReportRecord(WorkOrderNo);
```

### 阶段5: 验证数据完整性
```sql
-- 验证数据迁移是否完整
SELECT 
    'DieInfo' as TableName, 
    (SELECT COUNT(*) FROM dbo.DieInfo) as SourceCount,
    (SELECT COUNT(*) FROM diemaking.DieInfo) as TargetCount
UNION ALL
SELECT 
    'DieProcess', 
    (SELECT COUNT(*) FROM dbo.DieProcess),
    (SELECT COUNT(*) FROM diemaking.DieProcess)
UNION ALL
SELECT 
    'DieModificationRecord', 
    (SELECT COUNT(*) FROM dbo.DieModificationRecord),
    (SELECT COUNT(*) FROM diemaking.DieModificationRecord)
UNION ALL
SELECT 
    'ScanReportRecord', 
    (SELECT COUNT(*) FROM dbo.ScanReportRecord),
    (SELECT COUNT(*) FROM diemaking.ScanReportRecord);
```

### 阶段6: 更新应用程序配置
修改 `App.config` 中的连接字符串，添加 schema 配置或修改代码使用新的 schema。

**选项A: 修改连接字符串使用默认 schema**
```xml
<connectionStrings>
    <add name="DieMakingDB" connectionString="server=36.139.89.173;user id=sa;password=slbz_888;database=2026纸箱报价系统;TrustServerCertificate=True;" providerName="Microsoft.Data.SqlClient" />
</connectionStrings>
```
然后在代码中指定 schema：
```csharp
// 在 SQL 查询中使用 diemaking.DieInfo 而不是 DieInfo
```

**选项B: 修改 DatabaseConfig 类**
在 `DatabaseConfig.cs` 中添加默认 schema 设置。

### 阶段7: 测试验证
1. 编译项目
2. 运行应用程序
3. 验证所有功能正常
4. 确认数据读取正确

### 阶段8: 清理（可选）
确认迁移成功后，可以删除 dbo schema 下的旧表：
```sql
-- 仅在确认迁移成功后执行
-- DROP TABLE dbo.ScanReportRecord;
-- DROP TABLE dbo.DieModificationRecord;
-- DROP TABLE dbo.DieProcess;
-- DROP TABLE dbo.DieInfo;
```

## 注意事项

1. **备份数据**: 迁移前务必备份数据库
2. **事务处理**: 所有迁移操作应在事务中进行，确保数据一致性
3. **外键约束**: 注意表之间的外键关系，按正确顺序迁移数据
4. **IDENTITY 列**: 使用 SET IDENTITY_INSERT 保持原有 ID 值
5. **测试环境**: 建议先在测试环境验证迁移脚本

## 回滚方案
如果迁移失败，可以：
1. 删除 diemaking schema 下的表
2. 继续使用 dbo schema 下的表
3. 修改应用程序配置回退到 dbo schema
