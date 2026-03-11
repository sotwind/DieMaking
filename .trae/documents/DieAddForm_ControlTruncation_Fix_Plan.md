# 新增刀模页面控制显示截断问题修复计划

## 问题描述

从用户提供的截图可以看出，**新增刀模页面（DieAddForm.cs）**底部的按钮区域（保存、保存草稿、取消）被截断，无法完全显示。

## 问题分析

### DieAddForm.cs 当前布局分析

窗体尺寸：`UIStyleHelper.SizeEditForm` = `800x600`

各区域高度计算：
1. **基本信息区域** (grpBasic): y=10, Size=760x150 → 占用 10-160
2. **尺寸信息区域** (grpSize): y=170, Size=760x100 → 占用 170-270
3. **工序设置区域** (grpProcess): y=280, Size=760x200 → 占用 280-480
4. **备注区域** (grpRemark): y=490, Size=760x60 → 占用 490-550
5. **按钮区域**: y=560, 按钮高度约30px → 占用 560-590

**问题原因**：
- 窗体高度 600px，但控件布局从 y=10 到 y=590，总高度需求约 600px
- 窗体标题栏和边框会占用额外空间（约 30-40px）
- 导致底部按钮被截断

## 修复方案

### 方案1：增加窗体高度（推荐）

将 DieAddForm 的窗体高度从 600 增加到 650 或 700，确保所有控件都能完整显示。

```csharp
// 修改前
this.Size = UIStyleHelper.SizeEditForm;  // 800x600

// 修改后
this.Size = new Size(800, 650);  // 增加高度到650
```

### 方案2：调整控件布局

压缩各区域间距，减少整体高度需求：
- 减小 GroupBox 之间的间距
- 减小行高
- 压缩备注区域高度

### 方案3：使用滚动面板

将内容放入 Panel 中，设置 AutoScroll = true，当内容超出时显示滚动条。

## 检查其他窗体

需要检查以下窗体是否有类似问题：

| 窗体文件 | 使用尺寸 | 状态 |
|---------|---------|------|
| DieAddForm.cs | SizeEditForm (800x600) | **有问题** |
| DieBorrowForm.cs | SizeEditForm (800x600) | 需要检查 |
| DieReturnForm.cs | SizeEditForm (800x600) | 需要检查 |
| ScrapApplyEditForm.cs | 650x450 | 需要检查 |
| ScrapAuditEditForm.cs | 600x500 | 需要检查 |

## 修复步骤

1. **修复 DieAddForm.cs**
   - 将窗体高度从 600 增加到 650
   - 调整按钮区域 Y 坐标从 560 到 600

2. **检查并修复其他使用 SizeEditForm 的窗体**
   - DieBorrowForm.cs
   - DieReturnForm.cs

3. **验证修复效果**
   - 编译项目
   - 运行并打开新增刀模页面
   - 确认所有控件完整显示

## 代码修改详情

### DieAddForm.cs 修改

```csharp
// 第47行：修改窗体大小
// 修改前：
this.Size = UIStyleHelper.SizeEditForm;

// 修改后：
this.Size = new Size(800, 650);

// 第265行：调整按钮区域Y坐标
// 修改前：
int btnY = 560;

// 修改后：
int btnY = 600;
```

### DieBorrowForm.cs 检查

该窗体布局计算：
- y 从 20 开始，每个控件间隔 40px
- 共 9 个输入行 + 按钮区域
- 预计高度需求：20 + 50 + 9*40 + 70 = 480px
- 窗体高度 600px，应该足够

**结论**：DieBorrowForm 高度足够，不需要修改。

### DieReturnForm.cs 检查

该窗体布局计算：
- y 从 20 开始
- 共 7 个区域 + 按钮区域
- 预计高度需求约 400px
- 窗体高度 600px，应该足够

**结论**：DieReturnForm 高度足够，不需要修改。

## 最终修复计划

仅需要修改 **DieAddForm.cs**：

1. 将窗体高度从 600 改为 650
2. 将按钮区域 Y 坐标从 560 改为 600
3. 将备注区域 Y 坐标从 490 改为 530（保持相对位置）

这样可以确保：
- 所有 GroupBox 和控件完整显示
- 底部按钮区域不被截断
- 保持整体布局美观
