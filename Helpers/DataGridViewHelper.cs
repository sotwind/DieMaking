using System.ComponentModel;

namespace DieMaking.Helpers;

/// <summary>
/// DataGridView 性能优化帮助类
/// </summary>
public static class DataGridViewHelper
{
    /// <summary>
    /// 大数据量阈值
    /// </summary>
    private const int LargeDataThreshold = 1000;

    /// <summary>
    /// 虚拟模式阈值
    /// </summary>
    private const int VirtualModeThreshold = 5000;

    /// <summary>
    /// 配置 DataGridView 以优化性能
    /// </summary>
    /// <param name="dataGridView">要配置的 DataGridView</param>
    /// <param name="enableVirtualMode">是否启用虚拟模式</param>
    /// <param name="expectedRowCount">预期数据行数</param>
    public static void ConfigureForPerformance(this DataGridView dataGridView, bool enableVirtualMode = false, int expectedRowCount = 0)
    {
        // 基础性能设置
        dataGridView.AutoGenerateColumns = false;
        dataGridView.AllowUserToAddRows = false;
        dataGridView.AllowUserToDeleteRows = false;
        dataGridView.AllowUserToResizeRows = true;
        dataGridView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        
        // 启用双缓冲以减少闪烁
        EnableDoubleBuffering(dataGridView);

        // 大数据量优化
        if (expectedRowCount > LargeDataThreshold || enableVirtualMode)
        {
            ConfigureForLargeData(dataGridView);
        }

        // 虚拟模式（超大数据量）
        if (expectedRowCount > VirtualModeThreshold || enableVirtualMode)
        {
            ConfigureVirtualMode(dataGridView);
        }
    }

    /// <summary>
    /// 启用双缓冲
    /// </summary>
    private static void EnableDoubleBuffering(DataGridView dataGridView)
    {
        // 使用反射设置 DoubleBuffered 属性
        var property = typeof(DataGridView).GetProperty("DoubleBuffered", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        property?.SetValue(dataGridView, true, null);
    }

    /// <summary>
    /// 配置大数据量优化
    /// </summary>
    private static void ConfigureForLargeData(DataGridView dataGridView)
    {
        // 禁用自动调整列宽
        dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        
        // 设置行高固定
        dataGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        
        // 禁用行标题
        dataGridView.RowHeadersVisible = false;
        
        // 设置默认单元格样式
        dataGridView.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        
        // 设置选择模式为整行选择
        dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        
        // 禁用编辑
        dataGridView.ReadOnly = true;
        
        // 设置背景色交替以提高可读性
        dataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.LightGray;
    }

    /// <summary>
    /// 配置虚拟模式
    /// </summary>
    private static void ConfigureVirtualMode(DataGridView dataGridView)
    {
        dataGridView.VirtualMode = true;
        
        // 设置缓存大小
        dataGridView.RowCount = 0; // 初始化为0，数据加载时再设置
        
        // 处理 CellValueNeeded 事件
        dataGridView.CellValueNeeded += (sender, e) =>
        {
            // 这里需要外部提供数据
            // 使用 VirtualModeDataSource 属性来获取数据
            var dataSource = dataGridView.Tag as IBindingList;
            if (dataSource != null && e.RowIndex >= 0 && e.RowIndex < dataSource.Count)
            {
                var item = dataSource[e.RowIndex];
                var property = item.GetType().GetProperty(dataGridView.Columns[e.ColumnIndex].DataPropertyName);
                if (property != null)
                {
                    e.Value = property.GetValue(item);
                }
            }
        };
    }

    /// <summary>
    /// 高效加载数据到 DataGridView
    /// </summary>
    public static void LoadDataEfficiently<T>(this DataGridView dataGridView, List<T> data) where T : class
    {
        // 暂停绘制和布局
        dataGridView.SuspendLayout();
        
        try
        {
            // 清除现有数据
            dataGridView.Rows.Clear();
            
            if (data == null || data.Count == 0)
            {
                return;
            }

            // 大数据量使用虚拟模式
            if (data.Count > VirtualModeThreshold && dataGridView.VirtualMode)
            {
                // 存储数据引用
                dataGridView.Tag = new BindingList<T>(data);
                dataGridView.RowCount = data.Count;
            }
            else
            {
                // 批量添加行
                var rows = new DataGridViewRow[data.Count];
                for (int i = 0; i < data.Count; i++)
                {
                    rows[i] = new DataGridViewRow();
                    rows[i].CreateCells(dataGridView);
                    
                    // 填充单元格数据
                    for (int j = 0; j < dataGridView.Columns.Count; j++)
                    {
                        var column = dataGridView.Columns[j];
                        if (!string.IsNullOrEmpty(column.DataPropertyName))
                        {
                            var property = typeof(T).GetProperty(column.DataPropertyName);
                            if (property != null)
                            {
                                rows[i].Cells[j].Value = property.GetValue(data[i]);
                            }
                        }
                    }
                    
                    // 存储数据对象
                    rows[i].Tag = data[i];
                }
                
                dataGridView.Rows.AddRange(rows);
            }
        }
        finally
        {
            // 恢复绘制和布局
            dataGridView.ResumeLayout(true);
        }
    }

    /// <summary>
    /// 高效更新 DataGridView 数据
    /// </summary>
    public static void UpdateDataEfficiently<T>(this DataGridView dataGridView, List<T> data) where T : class
    {
        dataGridView.SuspendLayout();
        
        try
        {
            // 保存当前选择
            var selectedIndex = dataGridView.SelectedRows.Count > 0 
                ? dataGridView.SelectedRows[0].Index 
                : -1;
            
            // 保存滚动位置
            var firstDisplayedRow = dataGridView.FirstDisplayedScrollingRowIndex;
            
            // 重新加载数据
            dataGridView.LoadDataEfficiently(data);
            
            // 恢复选择
            if (selectedIndex >= 0 && selectedIndex < dataGridView.Rows.Count)
            {
                dataGridView.Rows[selectedIndex].Selected = true;
            }
            
            // 恢复滚动位置
            if (firstDisplayedRow >= 0 && firstDisplayedRow < dataGridView.Rows.Count)
            {
                dataGridView.FirstDisplayedScrollingRowIndex = firstDisplayedRow;
            }
        }
        finally
        {
            dataGridView.ResumeLayout(true);
        }
    }

    /// <summary>
    /// 设置列的默认格式
    /// </summary>
    public static void SetDefaultColumnStyles(this DataGridView dataGridView)
    {
        foreach (DataGridViewColumn column in dataGridView.Columns)
        {
            // 设置列标题样式
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            column.HeaderCell.Style.Font = new Font(dataGridView.Font, FontStyle.Bold);
            
            // 根据列名设置默认格式
            if (column.DataPropertyName.Contains("Date") || column.DataPropertyName.Contains("Time"))
            {
                column.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            else if (column.DataPropertyName.Contains("Amount") || column.DataPropertyName.Contains("Price") || column.DataPropertyName.Contains("Length") || column.DataPropertyName.Contains("Width") || column.DataPropertyName.Contains("Height"))
            {
                column.DefaultCellStyle.Format = "N2";
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            else if (column.DataPropertyName.Contains("Count") || column.DataPropertyName.Contains("ID"))
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
            else
            {
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            }
        }
    }

    /// <summary>
    /// 自动调整列宽（高效版本）
    /// </summary>
    public static void AutoResizeColumnsEfficiently(this DataGridView dataGridView, int maxRowsToMeasure = 100)
    {
        dataGridView.SuspendLayout();
        
        try
        {
            // 只对可见行和最多 maxRowsToMeasure 行进行测量
            var rowCount = Math.Min(dataGridView.Rows.Count, maxRowsToMeasure);
            
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                if (column.Visible)
                {
                    // 计算标题宽度
                    var headerWidth = TextRenderer.MeasureText(column.HeaderText, dataGridView.Font).Width + 20;
                    
                    // 计算数据宽度（只测量部分行）
                    var maxWidth = headerWidth;
                    for (int i = 0; i < rowCount; i++)
                    {
                        if (dataGridView.Rows[i].Cells[column.Index].Value != null)
                        {
                            var cellText = dataGridView.Rows[i].Cells[column.Index].Value.ToString() ?? "";
                            var cellWidth = TextRenderer.MeasureText(cellText, dataGridView.Font).Width + 10;
                            maxWidth = Math.Max(maxWidth, cellWidth);
                        }
                    }
                    
                    // 设置列宽（限制最大宽度）
                    column.Width = Math.Min(maxWidth, 300);
                }
            }
        }
        finally
        {
            dataGridView.ResumeLayout(true);
        }
    }

    /// <summary>
    /// 获取选中的数据对象
    /// </summary>
    public static T? GetSelectedRowData<T>(this DataGridView dataGridView) where T : class
    {
        if (dataGridView.SelectedRows.Count > 0)
        {
            return dataGridView.SelectedRows[0].Tag as T;
        }
        return null;
    }

    /// <summary>
    /// 获取所有选中的数据对象
    /// </summary>
    public static List<T> GetSelectedRowsData<T>(this DataGridView dataGridView) where T : class
    {
        var result = new List<T>();
        foreach (DataGridViewRow row in dataGridView.SelectedRows)
        {
            if (row.Tag is T data)
            {
                result.Add(data);
            }
        }
        return result;
    }
}
