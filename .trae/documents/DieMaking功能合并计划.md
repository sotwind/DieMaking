# DieMaking 功能合并计划

## 问题分析

另一个软件将功能修改保存在了 `DieMaking/` 子目录中，但主项目根目录下的文件没有被修改。由于 `.csproj` 排除了子目录文件，导致运行时使用的是旧代码。

## 已确认子目录中包含的修改

### 1. ✅ 刀模模型 (DieInfo.cs) - 已包含新字段
- 工单号 (WorkOrderNo)
- 刀长 (KnifeLengthM)
- 刀痕长 (KnifeMarkLengthM)
- 板费单价 (BoardFeeUnitPrice, 默认90)
- 板费 (BoardFee)
- 制作单价 (ProductionUnitPrice, 默认8)
- 制作费 (ProductionFee)
- 设计单价 (DesignUnitPrice, 默认70)
- 设计费 (DesignFee)
- 计算面积方法 (CalculateArea)
- 计算费用方法 (CalculateFees)

### 2. ✅ 改刀功能
- DieModificationRecord.cs - 改刀记录实体
- DieService.cs - 包含 AddModificationRecord, GetModificationRecords, GetTotalModificationAmount 方法

### 3. ✅ 工序自动生成
- DieService.cs - GenerateDefaultProcesses 方法生成5个默认工序：绘图、割板、弯刀、装刀、贴泡沫

### 4. ✅ 易捷导入服务
- YijieImportService.cs - 从易捷系统导入工单
- YijieWorkOrder.cs - 易捷工单模型

### 5. ✅ 权限常量
- PermissionKeys.cs - 包含 SystemAdmin 等权限定义

### 6. ❓ 需要检查根目录中缺失的功能
- 系统设置-系统管理菜单权限检查
- 刀模列表界面按钮（易捷导入、改刀）
- 改刀记录查询菜单
- 工序报产手机端功能

## 合并步骤

### 阶段1: 合并模型和服务文件
1. 将 `DieMaking/Models/DieInfo.cs` 复制到 `Models/DieInfo.cs`
2. 将 `DieMaking/Models/DieModificationRecord.cs` 复制到 `Models/DieModificationRecord.cs`
3. 将 `DieMaking/Models/YijieWorkOrder.cs` 复制到 `Models/YijieWorkOrder.cs`
4. 将 `DieMaking/Services/DieService.cs` 复制到 `Services/DieService.cs`
5. 将 `DieMaking/Services/YijieImportService.cs` 复制到 `Services/YijieImportService.cs`

### 阶段2: 合并枚举文件
1. 对比 `DieMaking/Enums/StatusEnums.cs` 和 `Enums/StatusEnums.cs`，合并差异
2. 对比 `DieMaking/Enums/PermissionKeys.cs` 和 `Enums/PermissionKeys.cs`，合并差异

### 阶段3: 检查并修复权限问题
1. 检查主窗体中系统管理菜单的权限检查逻辑
2. 确保系统管理员有权限访问系统设置

### 阶段4: 检查UI界面
1. 检查刀模列表界面是否有易捷导入按钮
2. 检查刀模列表界面是否有改刀按钮
3. 检查是否有改刀记录查询菜单
4. 检查工序报产界面是否简化为两个状态

### 阶段5: 编译验证
1. 执行 dotnet build 验证编译
2. 解决可能的命名空间冲突

## 文件映射关系

| 子目录文件 | 根目录目标文件 | 状态 |
|-----------|--------------|------|
| DieMaking/Models/DieInfo.cs | Models/DieInfo.cs | 待合并 |
| DieMaking/Models/DieModificationRecord.cs | Models/DieModificationRecord.cs | 待复制 |
| DieMaking/Models/YijieWorkOrder.cs | Models/YijieWorkOrder.cs | 待复制 |
| DieMaking/Services/DieService.cs | Services/DieService.cs | 待合并 |
| DieMaking/Services/YijieImportService.cs | Services/YijieImportService.cs | 待复制 |
| DieMaking/Enums/StatusEnums.cs | Enums/StatusEnums.cs | 待对比合并 |
| DieMaking/Enums/PermissionKeys.cs | Enums/PermissionKeys.cs | 待对比合并 |
| DieMaking/Data/DatabaseConfig.cs | - | 需检查是否包含新配置 |
| DieMaking/Data/SystemConfigService.cs | - | 需检查是否包含新配置 |

## 注意事项

1. 子目录中的 DieService.cs 使用的是 `DieMaking.Data` 命名空间，而根目录可能使用不同的命名空间
2. 需要确保数据库表结构已更新（DieModificationRecord 表等）
3. 需要检查根目录的 Forms 界面是否需要更新
