# 刀模归还菜单报错修复计划

## 问题描述

点击刀模归还菜单时，在 `DieReturnForm.cs` 第205行发生 `System.InvalidCastException` 异常：

```
Unable to cast object of type '<>f__AnonymousType3`2[System.Int32,System.String]' to type 'System.Int32'.
```

## 问题分析

### 根本原因

在 `LoadData()` 方法（第180-188行）中，ComboBox的数据源被设置为一个匿名类型列表：

```csharp
var displayList = _borrowingRecords.Select(r => new
{
    r.BorrowID,
    Display = $"{r.DieCode} - {r.CustomerName} - 领用人：{r.BorrowerName} - 借用时间：{r.BorrowTime:yyyy-MM-dd}"
}).ToList();

cboRecord.DataSource = displayList;
cboRecord.DisplayMember = "Display";
cboRecord.ValueMember = "BorrowID";
```

但在 `CboRecord_SelectedIndexChanged` 事件处理器（第205行）中，代码尝试直接将 `SelectedValue` 转换为 `int`：

```csharp
var borrowId = (int)cboRecord.SelectedValue;
```

**问题**：当 ComboBox 的 `SelectedIndex` 被设置为 0 时（第192行），会触发 `SelectedIndexChanged` 事件，但此时 `SelectedValue` 返回的是整个匿名对象（`<>f__AnonymousType3`），而不是 `BorrowID` 属性的值。

### 涉及位置

1. **DieReturnForm.cs 第205行**: `var borrowId = (int)cboRecord.SelectedValue;`
2. **DieReturnForm.cs 第236行**: `var borrowId = (int)cboRecord.SelectedValue;`
3. **DieReturnForm.cs 第271行**: `var borrowId = (int)cboRecord.SelectedValue;`

## 修复方案

### 方案：使用反射获取 BorrowID 属性值

在类型转换前，先检查 `SelectedValue` 的类型，如果是匿名类型则通过反射获取 `BorrowID` 属性值。

### 具体修改

修改 `DieReturnForm.cs` 文件，创建一个辅助方法来安全地获取 BorrowID：

```csharp
private int GetSelectedBorrowId()
{
    if (cboRecord.SelectedValue == null) return 0;
    
    var selectedValue = cboRecord.SelectedValue;
    
    // 如果已经是 int，直接返回
    if (selectedValue is int intValue)
        return intValue;
    
    // 如果是匿名类型，通过反射获取 BorrowID 属性
    var type = selectedValue.GetType();
    var borrowIdProperty = type.GetProperty("BorrowID");
    if (borrowIdProperty != null)
    {
        var value = borrowIdProperty.GetValue(selectedValue);
        if (value is int borrowId)
            return borrowId;
    }
    
    return 0;
}
```

然后将所有直接转换的地方替换为调用此方法：

1. 第205行：`var borrowId = GetSelectedBorrowId();`
2. 第236行：`var borrowId = GetSelectedBorrowId();`
3. 第271行：`var borrowId = GetSelectedBorrowId();`

## 实施步骤

1. 在 `DieReturnForm.cs` 中添加 `GetSelectedBorrowId()` 辅助方法
2. 替换第205行的类型转换
3. 替换第236行的类型转换
4. 替换第271行的类型转换
5. 编译并测试

## 预期结果

修复后，点击刀模归还菜单时不再报错，ComboBox 选择变更时能正确获取 BorrowID 并显示对应的借用记录信息。
