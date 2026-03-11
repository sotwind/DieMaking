# 修复刀模工序数据网格显示问题

## 问题描述

在新增刀模界面（DieAddForm.cs）中，添加工序后，工序信息没有正确显示在数据网格（DataGridView）中。

## 问题分析

### 根本原因

问题出在 `RefreshProcessGrid` 方法（第474-478行）：

```csharp
private void RefreshProcessGrid()
{
    dgvProcesses.DataSource = null;
    dgvProcesses.DataSource = _processes;
}
```

**问题1：列定义与数据源不匹配**
- DataGridView 的列是通过 `Columns.Add` 手动添加的（第204-218行）
- 当使用 `DataSource = _processes` 绑定到对象列表时，DataGridView 会自动根据 `DieProcess` 类的属性创建新的列
- 这导致手动定义的列和自动生成的列混合，数据显示混乱

**问题2：列的 DataPropertyName 设置不完整**
- 手动添加的列中，只有 `Status` 列设置了 `DataPropertyName`
- 其他列（如 ProcessName、OperatorName 等）没有设置 `DataPropertyName`，无法正确绑定数据

### 具体代码问题

```csharp
// 第204-218行：列定义
dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProcessID", HeaderText = "ID", Visible = false });
dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProcessName", HeaderText = "工序名称", Width = 120 });
dgvProcesses.Columns.Add(new DataGridViewComboBoxColumn
{
    Name = "Status",
    HeaderText = "状态",
    Width = 80,
    DataSource = Enum.GetValues(typeof(ProcessStatus)).Cast<ProcessStatus>().Select(s => new { Value = s, Text = s.GetDisplayName() }).ToList(),
    DisplayMember = "Text",
    ValueMember = "Value",
    DataPropertyName = "Status"  // 只有这一列设置了 DataPropertyName
});
dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "OperatorName", HeaderText = "操作员", Width = 80 });
dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "Formula", HeaderText = "计算公式", Width = 150 });
dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "金额", Width = 80 });
```

## 修复方案

### 方案选择

有两种修复方案：

**方案A：使用数据绑定（推荐）**
- 为所有手动添加的列设置正确的 `DataPropertyName`
- 设置 `AutoGenerateColumns = false` 防止自动生成列
- 保持使用 `DataSource` 绑定

**方案B：手动填充行数据**
- 不使用 `DataSource` 绑定
- 手动添加行并填充单元格数据

选择 **方案A**，因为它更符合 WinForms 数据绑定的最佳实践。

## 修复步骤

### 1. 修复 DieAddForm.cs 的 RefreshProcessGrid 方法

修改 `InitializeComponent` 中的列定义：
- 设置 `dgvProcesses.AutoGenerateColumns = false`
- 为所有列添加 `DataPropertyName` 属性

修改 `RefreshProcessGrid` 方法：
- 确保列绑定正确

### 2. 检查其他窗体

需要检查以下窗体是否存在类似问题：
- DieListForm.cs - 使用 DataSource 绑定
- DieListForm_Fixed.cs - 使用 DataSource 绑定
- ScrapApplyForm.cs - 使用 DataSource 绑定（ComboBox）
- DieReturnForm.cs - 使用 DataSource 绑定（ComboBox）
- DieBorrowForm.cs - 使用 DataSource 绑定（ComboBox）

## 实施计划

1. **修复 DieAddForm.cs**
   - 在 InitializeComponent 中设置 `AutoGenerateColumns = false`
   - 为所有手动添加的列设置 `DataPropertyName`

2. **验证修复**
   - 测试添加工序后是否正确显示
   - 测试编辑工序后是否正确更新
   - 测试删除工序后是否正确刷新

## 预期结果

修复后，添加工序时数据应该正确显示在数据网格中，包括：
- 工序名称
- 状态（下拉框显示）
- 操作员姓名
- 计算公式
- 金额
