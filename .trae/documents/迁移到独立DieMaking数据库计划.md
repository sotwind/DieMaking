# 迁移到独立 DieMaking 数据库计划

## 目标
将所有本项目相关的数据表从 `2026纸箱报价系统` 数据库迁移到独立的 `DieMaking` 数据库中。

## 源数据库信息
- **服务器**: 36.139.89.173
- **数据库**: 2026纸箱报价系统
- **用户名**: sa

## 目标数据库信息
- **服务器**: 36.139.89.173 (相同服务器)
- **数据库**: DieMaking
- **用户名**: sa

## 需要迁移的表清单

根据代码分析，本项目使用的表包括：

### 核心刀模管理表
1. **DieInfo** / **DM_DieInfo** - 刀模信息表
2. **DieProcess** / **DM_DieProcess** - 刀模工序表
3. **DieModificationRecord** / **DM_DieModificationRecord** - 改刀记录表
4. **ScanReportRecord** / **DM_ScanReportRecord** - 扫码报产记录表

### 仓库管理表
5. **DM_StorageLocation** - 库位信息表
6. **DM_DieInventory** - 刀模库存表
7. **DM_DieBorrowRecord** - 借用记录表
8. **DM_DieScrapRecord** - 报废记录表

### 生产管理表
9. **DM_DieCompletion** - 完工记录表

### 系统管理表
10. **DM_User** - 用户表
11. **DM_SystemConfig** - 系统配置表
12. **DM_UserPreference** - 用户偏好设置表
13. **DM_OperationLog** - 操作日志表

### 其他表
14. **DM_BackupRecord** - 备份记录表
15. **DM_DatabaseVersion** - 数据库版本表
16. **DM_SlowQueryLog** - 慢查询日志表

## 迁移步骤

### 阶段1: 在目标数据库创建表结构
使用源数据库的表结构在 DieMaking 数据库中创建相同的表。

### 阶段2: 迁移数据
将每个表的数据从源数据库复制到目标数据库。

### 阶段3: 创建约束和索引
在目标数据库中创建主键、外键、索引等约束。

### 阶段4: 验证数据完整性
对比源数据库和目标数据库的记录数，确保数据完整迁移。

### 阶段5: 更新应用程序配置
修改 `App.config` 中的连接字符串，指向新的 DieMaking 数据库。

### 阶段6: 更新代码中的表名引用
将所有 SQL 查询中的表名统一（移除 diemaking. 前缀，或统一使用 DM_ 前缀）。

### 阶段7: 测试验证
编译并运行应用程序，验证所有功能正常。

## 注意事项

1. **表名不一致问题**: 代码中同时存在 `DieInfo` 和 `DM_DieInfo` 两种表名引用，需要统一
2. **外键约束**: 注意表之间的外键关系，按正确顺序迁移数据
3. **IDENTITY 列**: 使用 SET IDENTITY_INSERT 保持原有 ID 值
4. **事务处理**: 每个表的迁移应在事务中进行

## 回滚方案
如果迁移失败：
1. 清空 DieMaking 数据库中的表
2. 恢复应用程序配置指向 2026纸箱报价系统
3. 继续使用源数据库
