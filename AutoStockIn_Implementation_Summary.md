# 刀模自动入库功能实现总结

## 功能概述
当刀模的所有工序（绘图、割板、弯刀、装刀、贴泡沫）都完成报工后，系统自动将该刀模入库。

---

## 修改的文件清单

### 1. ProcessService.cs
**路径**: `/home/admin/.openclaw/workspace/DieMaking/DieMaking/Services/ProcessService.cs`

**修改内容**:
- 在 `CompleteProcessByScan` 方法中，工序报产成功后添加自动入库检查
- 新增 `CheckAndAutoStockIn` 私有方法，用于检查所有工序是否完成并触发自动入库

**关键代码**:
```csharp
// 工序报产成功后，检查是否所有工序都已完成
var autoStockInResult = CheckAndAutoStockIn(die.DieID, operatorNo, operatorName);
if (autoStockInResult.Success)
{
    return (true, $"工序 {processName} 报产成功，{autoStockInResult.Message}");
}
```

### 2. DieService.cs
**路径**: `/home/admin/.openclaw/workspace/DieMaking/DieMaking/Services/DieService.cs`

**修改内容**:
- 新增 `AutoStockIn` 方法：实现自动入库逻辑
- 新增 `AreAllProcessesCompleted` 方法：检查刀模是否所有工序都已完成
- 新增 `GetDieStockStatus` 方法：获取刀模入库状态

**关键代码**:
```csharp
/// <summary>
/// 自动入库 - 当刀模所有工序完成时自动入库
/// </summary>
public (bool Success, string Message) AutoStockIn(int dieId, string operatorNo, string operatorName)
{
    // 1. 检查刀模是否存在
    // 2. 检查刀模是否已入库（避免重复入库）
    // 3. 查找空闲库位
    // 4. 创建入库记录（DM_DieInventory）
    // 5. 更新库位状态为占用
    // 6. 更新刀模状态为已完成（DieStatus.Completed）
}
```

### 3. AutoStockIn_Migration.sql (新增)
**路径**: `/home/admin/.openclaw/workspace/DieMaking/Scripts/AutoStockIn_Migration.sql`

**内容**:
- 创建 DM_DieInventory 表（如果不存在）
- 创建 DM_StorageLocation 表（如果不存在）
- 创建 DM_DieBorrowRecord 表（借用记录）
- 创建 DM_DieScrapRecord 表（报废记录）
- 插入示例库位数据

---

## 执行逻辑说明

### 自动入库触发流程

```
扫码报工完成工序
       ↓
更新工序状态为 Completed
       ↓
调用 CheckAndAutoStockIn 方法
       ↓
获取刀模所有工序列表
       ↓
检查是否所有工序状态都是 Completed
       ↓
    ├─ 否：返回进度信息（已完成 X/5 道工序）
    └─ 是：调用 AutoStockIn 方法
              ↓
         检查刀模是否已入库
              ↓
         查找空闲库位（DM_StorageLocation）
              ↓
         创建入库记录（DM_DieInventory）
              ↓
         更新库位状态为占用
              ↓
         更新刀模状态为 Completed
              ↓
         返回入库成功消息
```

### 工序完成判断逻辑

系统默认的5道工序：
1. 绘图
2. 割板
3. 弯刀
4. 装刀
5. 贴泡沫

当这5道工序的状态全部为 `ProcessStatus.Completed` 时，触发自动入库。

### 自动入库操作详情

1. **检查重复入库**：如果刀模已在库中，返回"已入库"提示
2. **分配库位**：自动查找状态为"空闲"的库位（按区域、架号、层号、位号排序）
3. **创建入库记录**：
   - 表：DM_DieInventory
   - 字段：DieID, LocationID, StorageStatus=InStock, InStockTime, Remark
   - Remark 格式：`自动入库 - 所有工序完成 - 操作人：{姓名}({工号})`
4. **更新库位状态**：将库位状态更新为"占用"
5. **更新刀模状态**：将 DieInfo.Status 更新为 `DieStatus.Completed`

---

## 数据库表结构

### DM_DieInventory（刀模库存表）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| InventoryID | INT | 主键，自增 |
| DieID | INT | 刀模ID，外键 |
| LocationID | INT | 库位ID，外键，可为空 |
| StorageStatus | INT | 存储状态：0=在库, 1=借出, 2=报废, 3=维修中 |
| InStockTime | DATETIME | 入库时间 |
| LastBorrowTime | DATETIME | 最后借出时间 |
| LastReturnTime | DATETIME | 最后归还时间 |
| TotalBorrowCount | INT | 借用次数 |
| Remark | NVARCHAR(500) | 备注 |
| UpdateTime | DATETIME | 更新时间 |

### DM_StorageLocation（库位表）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| LocationID | INT | 主键，自增 |
| LocationCode | NVARCHAR(50) | 库位编号，唯一 |
| Area | NVARCHAR(50) | 区域 |
| ShelfNo | NVARCHAR(20) | 货架号 |
| LayerNo | NVARCHAR(20) | 层号 |
| PositionNo | NVARCHAR(20) | 位置号 |
| Description | NVARCHAR(200) | 描述 |
| Status | INT | 状态：0=空闲, 1=占用, 2=禁用 |
| CreateTime | DATETIME | 创建时间 |

---

## 返回消息示例

### 工序报产成功但未全部完成
```
工序 弯刀 报产成功
```
或
```
工序 弯刀 报产成功，已完成 3/5 道工序
```

### 工序报产成功并触发自动入库
```
工序 贴泡沫 报产成功，刀模自动入库成功，库位：A-01-01-01
```

### 刀模已入库（重复入库保护）
```
工序 贴泡沫 报产成功，刀模已入库，无需重复入库
```

---

## 遇到的问题和解决方案

### 问题1：数据库表前缀不一致
**现象**: 代码中使用 `DM_DieInventory` 和 `DM_StorageLocation`，但需要确认实际表名

**解决方案**: 
- 迁移脚本使用 `DM_` 前缀（与 WarehouseService.cs 保持一致）
- 同时兼容不带前缀的表名（DieInventory）
- 脚本中使用 `IF NOT EXISTS` 检查，避免重复创建

### 问题2：库位自动分配策略
**现象**: 需要决定如何自动分配库位

**解决方案**:
- 优先选择状态为"空闲"的库位
- 按 Area → ShelfNo → LayerNo → PositionNo 排序
- 如果没有空闲库位，仍然可以入库（LocationID 为 NULL），但会提示"未分配库位"

### 问题3：重复入库保护
**现象**: 防止同一刀模重复入库

**解决方案**:
- 入库前检查 DM_DieInventory 表中是否已存在该刀模的在库记录
- 如果已存在，返回提示信息但不报错

### 问题4：事务一致性
**现象**: 入库操作涉及多个表更新

**解决方案**:
- 使用数据库事务（Transaction）包裹所有操作
- 包括：创建入库记录、更新库位状态、更新刀模状态
- 任何步骤失败则回滚事务

---

## 后续优化建议

1. **配置化工序列表**：将默认工序（绘图、割板、弯刀、装刀、贴泡沫）配置化，支持自定义
2. **库位分配策略优化**：支持按刀模尺寸、类型智能分配库位
3. **入库通知**：添加入库成功后的消息通知（邮件/短信/钉钉）
4. **入库审核**：支持入库前审核流程
5. **库位可视化**：提供仓库可视化界面，直观显示库位占用情况

---

## 测试建议

1. **正常流程测试**：
   - 创建刀模 → 完成所有工序 → 验证自动入库

2. **边界情况测试**：
   - 重复完成同一工序（应提示"无需重复报产"）
   - 所有工序完成后再次扫码（应提示"已入库"）
   - 无空闲库位时的入库行为

3. **异常处理测试**：
   - 数据库连接失败
   - 事务回滚验证

---

## 部署步骤

1. **执行数据库迁移脚本**:
   ```sql
   -- 在 SQL Server Management Studio 中执行
   \DieMaking\Scripts\AutoStockIn_Migration.sql
   ```

2. **编译项目**:
   ```bash
   cd /home/admin/.openclaw/workspace/DieMaking
   dotnet build
   ```

3. **验证功能**:
   - 创建一个测试刀模
   - 依次完成5道工序的扫码报工
   - 验证第5道工序完成后自动入库

---

*文档生成时间: 2026-03-11*
*版本: 1.0*
