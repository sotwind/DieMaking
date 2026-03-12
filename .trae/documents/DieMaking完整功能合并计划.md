# DieMaking 完整功能合并计划

## 问题分析

另一个软件将大量功能修改保存在了 `DieMaking/` 子目录中，但主项目根目录下的文件仍然是旧版本。由于 `.csproj` 排除了子目录文件，导致运行时使用的是旧代码，所有新功能都无法使用。

## 子目录中包含的新功能（已确认）

### 1. 刀模模型扩展 (DieInfo.cs)
**新增字段：**
- 工单号 (WorkOrderNo)
- 刀长 (KnifeLengthM)
- 刀痕长 (KnifeMarkLengthM)
- 板费单价 (BoardFeeUnitPrice, 默认90)
- 板费 (BoardFee)
- 制作单价 (ProductionUnitPrice, 默认8)
- 制作费 (ProductionFee)
- 设计单价 (DesignUnitPrice, 默认70)
- 设计费 (DesignFee)
- 审核状态 (AuditStatus)
- 外部订单号 (ExternalOrderNo)

**新增方法：**
- CalculateArea() - 计算面积（毛坯尺寸转换为平方米）
- CalculateFees() - 计算费用（面积*单价）

### 2. 改刀功能
**新增文件：**
- DieModificationRecord.cs - 改刀记录实体（改刀ID、刀模ID、金额、时间、改刀人、原因等）

**DieService 新增方法：**
- AddModificationRecord() - 添加改刀记录
- GetModificationRecords() - 获取改刀记录列表（支持筛选）
- GetTotalModificationAmount() - 获取刀模改刀总金额

### 3. 工序自动生成
**DieService 新增：**
- GenerateDefaultProcesses() 方法生成5个默认工序：绘图、割板、弯刀、装刀、贴泡沫
- 创建刀模时自动添加工序

### 4. 易捷系统导入功能
**新增文件：**
- YijieWorkOrder.cs - 易捷工单模型
- YijieImportService.cs - 易捷导入服务

**功能：**
- 查询多个易捷数据库（新厂、老厂、临海、温森）
- 筛选新刀订单
- 排除已存在的工单
- 自动导入刀模信息

### 5. 手机扫码报产功能
**新增文件：**
- ProcessReportService.cs - 工序报产服务

**功能：**
- 扫描易捷工单号查找刀模
- 直接完成工序（简化流程：只有"待生产"和"已完成"两种状态）
- 保存扫码记录
- 自动更新刀模状态

### 6. 数据库配置扩展
**DatabaseConfig 新增：**
- YijieDatabaseConfig - 易捷数据库配置
- YijieDatabaseInfo - 易捷数据库连接信息
- 支持4个易捷数据库连接

### 7. 系统配置服务
**SystemConfigService 新增：**
- GetPriceConfigs() - 获取单价配置（板费、制作费、设计费）
- GetDecimalConfig() - 获取decimal类型配置

### 8. 枚举扩展
**StatusEnums 新增：**
- ProcessType 枚举（绘图、割板、弯刀、装刀、贴泡沫）
- GetAllProcessTypes() 方法

**Warehouse 模型：**
- LocationStatus、StorageStatus、BorrowType、BorrowStatus 枚举
- StorageLocation、DieInventory、DieBorrowRecord 实体类

**Statistics 模型：**
- InventorySummaryStats、BorrowStats 统计类

### 9. 权限常量
**PermissionKeys 包含：**
- SystemAdmin - 系统管理员
- UserManage - 用户管理
- SystemConfig - 系统配置
- 以及其他权限常量

## 文件差异对比

| 文件 | 根目录状态 | 子目录状态 | 操作 |
|------|-----------|-----------|------|
| Models/DieInfo.cs | 旧版本，缺少新字段 | ✅ 完整新版本 | 替换 |
| Models/DieProcess.cs | 与DieInfo.cs在同一文件 | ✅ 独立且完整 | 替换 |
| Models/DieModificationRecord.cs | ❌ 不存在 | ✅ 存在 | 复制 |
| Models/YijieWorkOrder.cs | ❌ 不存在 | ✅ 存在 | 复制 |
| Models/Warehouse.cs | ❌ 不存在 | ✅ 存在 | 复制 |
| Models/Statistics.cs | ❌ 不存在 | ✅ 存在 | 复制 |
| Services/DieService.cs | 旧版本 | ✅ 完整新版本 | 替换 |
| Services/YijieImportService.cs | ❌ 不存在 | ✅ 存在 | 复制 |
| Services/ProcessReportService.cs | ❌ 不存在 | ✅ 存在 | 复制 |
| Services/SystemConfigService.cs | ❌ 不存在 | ✅ 存在 | 复制 |
| Data/DatabaseConfig.cs | 旧版本 | ✅ 包含易捷配置 | 替换 |
| Enums/StatusEnums.cs | 已复制到根目录 | ✅ 完整 | 对比合并 |
| Enums/PermissionKeys.cs | 已复制到根目录 | ✅ 完整 | 对比合并 |

## 合并步骤

### 阶段1: 复制/替换 Models 文件
1. 备份根目录 Models/DieInfo.cs
2. 复制 DieMaking/Models/DieInfo.cs 到 Models/DieInfo.cs
3. 复制 DieMaking/Models/DieModificationRecord.cs 到 Models/DieModificationRecord.cs
4. 复制 DieMaking/Models/YijieWorkOrder.cs 到 Models/YijieWorkOrder.cs
5. 复制 DieMaking/Models/Warehouse.cs 到 Models/Warehouse.cs
6. 复制 DieMaking/Models/Statistics.cs 到 Models/Statistics.cs

### 阶段2: 复制/替换 Services 文件
1. 备份根目录 Services/DieService.cs
2. 复制 DieMaking/Services/DieService.cs 到 Services/DieService.cs
3. 复制 DieMaking/Services/YijieImportService.cs 到 Services/YijieImportService.cs
4. 复制 DieMaking/Services/ProcessReportService.cs 到 Services/ProcessReportService.cs
5. 复制 DieMaking/Services/SystemConfigService.cs 到 Services/SystemConfigService.cs

### 阶段3: 复制/替换 Data 文件
1. 备份根目录 Helpers/DbHelper.cs 或 Data 相关文件
2. 复制 DieMaking/Data/DatabaseConfig.cs 到 Helpers/DatabaseConfig.cs（或创建 Data 目录）

### 阶段4: 合并 Enums 文件
1. 对比 Enums/StatusEnums.cs 和 DieMaking/Enums/StatusEnums.cs
2. 合并缺失的枚举类型和方法
3. 对比 Enums/PermissionKeys.cs 和 DieMaking/Enums/PermissionKeys.cs
4. 合并缺失的权限常量

### 阶段5: 检查并修复命名空间
子目录文件使用 `DieMaking.Data` 命名空间，根目录可能使用 `DieMaking.Helpers`：
- 检查并统一命名空间
- 更新 using 语句

### 阶段6: 编译验证
1. 执行 dotnet build
2. 解决编译错误
3. 检查命名空间冲突

### 阶段7: 检查 UI 界面（如果需要）
检查以下界面是否需要更新：
- 刀模列表界面（添加易捷导入按钮、改刀按钮）
- 改刀记录查询界面
- 工序报产界面（手机端）
- 系统设置菜单权限

## 注意事项

1. **数据库表结构**：确保数据库已更新，包含以下新表/字段：
   - DieModificationRecord 表
   - ScanReportRecord 表
   - DieInfo 表的新字段（WorkOrderNo, KnifeLengthM, KnifeMarkLengthM, BoardFeeUnitPrice等）

2. **命名空间冲突**：
   - 子目录使用 `DieMaking.Data`
   - 根目录可能使用 `DieMaking.Helpers`
   - 需要统一或添加兼容代码

3. **配置文件**：
   - 检查是否需要添加易捷数据库连接配置
   - 检查系统配置表是否有默认单价配置

4. **权限检查**：
   - 确保系统管理员有权限访问系统设置菜单
   - 检查权限验证逻辑
