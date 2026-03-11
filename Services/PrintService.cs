using System.Drawing.Printing;
using System.Text;

namespace DieMaking.Services;

/// <summary>
/// 打印服务类
/// </summary>
public class PrintService
{
    private DataGridView? _dataGridView;
    private string _title = "";
    private string _subtitle = "";
    private int _currentRowIndex = 0;
    private int _currentPage = 0;
    private int _totalPages = 0;
    private List<float> _columnWidths = new();
    private Font _headerFont = new("微软雅黑", 12, FontStyle.Bold);
    private Font _subtitleFont = new("微软雅黑", 10, FontStyle.Regular);
    private Font _cellFont = new("微软雅黑", 9, FontStyle.Regular);
    private Font _footerFont = new("微软雅黑", 8, FontStyle.Regular);
    private Brush _headerBrush = Brushes.Black;
    private Brush _cellBrush = Brushes.Black;
    private Pen _gridPen = new Pen(Color.Black, 0.5f);
    private float _rowHeight = 25f;
    private float _headerHeight = 40f;
    private float _footerHeight = 30f;
    private float _leftMargin = 50f;
    private float _topMargin = 60f;

    /// <summary>
    /// 打印预览
    /// </summary>
    public void PrintPreview(DataGridView dgv, string title, string? subtitle = null)
    {
        _dataGridView = dgv;
        _title = title;
        _subtitle = subtitle ?? $"打印时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

        using var printDoc = new PrintDocument();
        printDoc.PrintPage += PrintDocument_PrintPage;

        using var previewDialog = new PrintPreviewDialog
        {
            Document = printDoc,
            WindowState = FormWindowState.Maximized,
            StartPosition = FormStartPosition.CenterScreen
        };

        previewDialog.ShowDialog();
    }

    /// <summary>
    /// 直接打印
    /// </summary>
    public void Print(DataGridView dgv, string title, string? subtitle = null)
    {
        _dataGridView = dgv;
        _title = title;
        _subtitle = subtitle ?? $"打印时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

        using var printDoc = new PrintDocument();
        printDoc.PrintPage += PrintDocument_PrintPage;

        using var printDialog = new PrintDialog
        {
            Document = printDoc,
            AllowSomePages = false
        };

        if (printDialog.ShowDialog() == DialogResult.OK)
        {
            _currentRowIndex = 0;
            _currentPage = 0;
            printDoc.Print();
        }
    }

    /// <summary>
    /// 导出为PDF（通过打印到PDF虚拟打印机）
    /// </summary>
    public void ExportToPdf(DataGridView dgv, string title, string filePath, string? subtitle = null)
    {
        _dataGridView = dgv;
        _title = title;
        _subtitle = subtitle ?? $"导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

        using var printDoc = new PrintDocument();
        printDoc.PrintPage += PrintDocument_PrintPage;
        printDoc.PrinterSettings.PrintToFile = true;
        printDoc.PrinterSettings.PrintFileName = filePath;

        // 尝试找到PDF虚拟打印机
        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            if (printer.ToLower().Contains("pdf") || printer.ToLower().Contains("microsoft print to pdf"))
            {
                printDoc.PrinterSettings.PrinterName = printer;
                break;
            }
        }

        _currentRowIndex = 0;
        _currentPage = 0;
        printDoc.Print();
    }

    /// <summary>
    /// 导出为CSV
    /// </summary>
    public void ExportToCsv(DataGridView dgv, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        // 写入表头
        var headers = new List<string>();
        foreach (DataGridViewColumn col in dgv.Columns)
        {
            headers.Add(col.HeaderText);
        }
        writer.WriteLine(string.Join(",", headers));

        // 写入数据
        foreach (DataGridViewRow row in dgv.Rows)
        {
            var values = new List<string>();
            foreach (DataGridViewCell cell in row.Cells)
            {
                var value = cell.Value?.ToString() ?? "";
                // 处理包含逗号或换行符的情况
                if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                {
                    value = "\"" + value.Replace("\"", "\"\"") + "\"";
                }
                values.Add(value);
            }
            writer.WriteLine(string.Join(",", values));
        }
    }

    /// <summary>
    /// 导出为TXT（制表符分隔）
    /// </summary>
    public void ExportToTxt(DataGridView dgv, string filePath)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        // 写入表头
        var headers = new List<string>();
        foreach (DataGridViewColumn col in dgv.Columns)
        {
            headers.Add(col.HeaderText);
        }
        writer.WriteLine(string.Join("\t", headers));

        // 写入数据
        foreach (DataGridViewRow row in dgv.Rows)
        {
            var values = new List<string>();
            foreach (DataGridViewCell cell in row.Cells)
            {
                values.Add(cell.Value?.ToString() ?? "");
            }
            writer.WriteLine(string.Join("\t", values));
        }
    }

    private void PrintDocument_PrintPage(object? sender, PrintPageEventArgs e)
    {
        if (_dataGridView == null || e.Graphics == null) return;

        var g = e.Graphics;
        var pageBounds = e.MarginBounds;
        _leftMargin = pageBounds.Left;
        _topMargin = pageBounds.Top;
        var printableWidth = pageBounds.Width;
        var printableHeight = pageBounds.Height;

        _currentPage++;

        // 计算总页数（仅在第一页计算）
        if (_currentPage == 1)
        {
            CalculateTotalPages(g, printableWidth, printableHeight);
        }

        // 绘制标题
        DrawTitle(g, printableWidth);

        // 绘制表头
        var tableTop = _topMargin + _headerHeight + 10;
        DrawTableHeader(g, tableTop, printableWidth);

        // 绘制数据行
        var dataTop = tableTop + _rowHeight;
        var availableHeight = printableHeight - _footerHeight - 10;
        var rowsDrawn = DrawTableRows(g, dataTop, availableHeight, printableWidth);

        // 绘制页脚
        DrawFooter(g, printableWidth, printableHeight);

        // 判断是否还有更多页
        e.HasMorePages = _currentRowIndex < _dataGridView.Rows.Count;

        if (!e.HasMorePages)
        {
            _currentRowIndex = 0;
            _currentPage = 0;
        }
    }

    private void CalculateTotalPages(Graphics g, float printableWidth, float printableHeight)
    {
        var availableHeight = printableHeight - _headerHeight - _rowHeight - _footerHeight - 20;
        var rowsPerPage = (int)(availableHeight / _rowHeight);
        var totalRows = _dataGridView!.Rows.Count;
        _totalPages = (int)Math.Ceiling((double)totalRows / rowsPerPage);
        if (_totalPages == 0) _totalPages = 1;
    }

    private void DrawTitle(Graphics g, float printableWidth)
    {
        // 主标题
        var titleSize = g.MeasureString(_title, _headerFont);
        var titleX = _leftMargin + (printableWidth - titleSize.Width) / 2;
        g.DrawString(_title, _headerFont, _headerBrush, titleX, _topMargin);

        // 副标题
        var subtitleSize = g.MeasureString(_subtitle, _subtitleFont);
        var subtitleX = _leftMargin + (printableWidth - subtitleSize.Width) / 2;
        g.DrawString(_subtitle, _subtitleFont, _cellBrush, subtitleX, _topMargin + 25);
    }

    private void DrawTableHeader(Graphics g, float top, float printableWidth)
    {
        CalculateColumnWidths(g, printableWidth);

        var x = _leftMargin;
        for (int i = 0; i < _dataGridView!.Columns.Count; i++)
        {
            var col = _dataGridView.Columns[i];
            var width = _columnWidths[i];

            // 绘制单元格背景
            g.FillRectangle(Brushes.LightGray, x, top, width, _rowHeight);
            // 绘制边框
            g.DrawRectangle(_gridPen, x, top, width, _rowHeight);
            // 绘制文字
            var text = col.HeaderText;
            var textSize = g.MeasureString(text, _cellFont);
            var textX = x + (width - textSize.Width) / 2;
            var textY = top + (_rowHeight - textSize.Height) / 2;
            g.DrawString(text, _cellFont, _headerBrush, textX, textY);

            x += width;
        }
    }

    private int DrawTableRows(Graphics g, float top, float availableHeight, float printableWidth)
    {
        int rowsDrawn = 0;
        var y = top;

        while (_currentRowIndex < _dataGridView!.Rows.Count && y + _rowHeight <= top + availableHeight)
        {
            var row = _dataGridView.Rows[_currentRowIndex];
            var x = _leftMargin;

            for (int i = 0; i < _dataGridView.Columns.Count; i++)
            {
                var col = _dataGridView.Columns[i];
                var cell = row.Cells[i];
                var width = _columnWidths[i];

                // 绘制单元格背景（交替行颜色）
                var backBrush = _currentRowIndex % 2 == 0 ? Brushes.White : Brushes.WhiteSmoke;
                g.FillRectangle(backBrush, x, y, width, _rowHeight);
                // 绘制边框
                g.DrawRectangle(_gridPen, x, y, width, _rowHeight);

                // 绘制文字
                var value = cell.Value?.ToString() ?? "";
                var textSize = g.MeasureString(value, _cellFont);
                var textX = x + 3;

                // 数字右对齐
                if (cell.Value is int || cell.Value is decimal || cell.Value is double || cell.Value is float)
                {
                    textX = x + width - textSize.Width - 3;
                }

                var textY = y + (_rowHeight - textSize.Height) / 2;
                g.DrawString(value, _cellFont, _cellBrush, textX, textY);

                x += width;
            }

            y += _rowHeight;
            _currentRowIndex++;
            rowsDrawn++;
        }

        return rowsDrawn;
    }

    private void DrawFooter(Graphics g, float printableWidth, float printableHeight)
    {
        var footerY = _topMargin + printableHeight - _footerHeight + 10;
        var footerText = $"第 {_currentPage} 页 / 共 {_totalPages} 页";
        var textSize = g.MeasureString(footerText, _footerFont);
        var textX = _leftMargin + (printableWidth - textSize.Width) / 2;
        g.DrawString(footerText, _footerFont, _cellBrush, textX, footerY);
    }

    private void CalculateColumnWidths(Graphics g, float printableWidth)
    {
        _columnWidths.Clear();
        var totalWidth = 0f;
        var colCount = _dataGridView!.Columns.Count;

        // 首先尝试根据内容计算宽度
        for (int i = 0; i < colCount; i++)
        {
            var col = _dataGridView.Columns[i];
            var headerSize = g.MeasureString(col.HeaderText, _cellFont);
            float maxWidth = headerSize.Width + 10;

            // 检查前20行的数据宽度
            for (int j = 0; j < Math.Min(20, _dataGridView.Rows.Count); j++)
            {
                var cellValue = _dataGridView.Rows[j].Cells[i].Value?.ToString() ?? "";
                var cellSize = g.MeasureString(cellValue, _cellFont);
                maxWidth = Math.Max(maxWidth, cellSize.Width + 10);
        }

            // 限制最大宽度
            maxWidth = Math.Min(maxWidth, 200);
            // 最小宽度
            maxWidth = Math.Max(maxWidth, 50);

            _columnWidths.Add(maxWidth);
            totalWidth += maxWidth;
        }

        // 如果总宽度小于可打印宽度，按比例放大
        if (totalWidth < printableWidth && totalWidth > 0)
        {
            var scale = printableWidth / totalWidth;
            for (int i = 0; i < _columnWidths.Count; i++)
            {
                _columnWidths[i] *= scale;
            }
        }
        // 如果总宽度大于可打印宽度，按比例缩小
        else if (totalWidth > printableWidth && totalWidth > 0)
        {
            var scale = printableWidth / totalWidth;
            for (int i = 0; i < _columnWidths.Count; i++)
            {
                _columnWidths[i] *= scale;
            }
        }
    }
}

/// <summary>
/// 打印对话框扩展
/// </summary>
public static class PrintDialogExtensions
{
    /// <summary>
    /// 显示打印选项对话框
    /// </summary>
    public static DialogResult ShowPrintOptions(DataGridView dgv, string title, string? subtitle = null)
    {
        using var dialog = new Form
        {
            Text = "打印选项",
            Size = new Size(400, 250),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblTitle = new Label
        {
            Text = $"报表标题：{title}",
            Location = new Point(20, 20),
            Size = new Size(350, 25),
            Font = new Font("微软雅黑", 10, FontStyle.Bold)
        };

        var lblInfo = new Label
        {
            Text = $"数据行数：{dgv.Rows.Count} 行",
            Location = new Point(20, 50),
            Size = new Size(350, 25)
        };

        var btnPrint = new Button
        {
            Text = "直接打印",
            Location = new Point(40, 100),
            Size = new Size(100, 35)
        };
        btnPrint.Click += (s, e) =>
        {
            dialog.DialogResult = DialogResult.Yes;
            dialog.Close();
        };

        var btnPreview = new Button
        {
            Text = "打印预览",
            Location = new Point(150, 100),
            Size = new Size(100, 35)
        };
        btnPreview.Click += (s, e) =>
        {
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };

        var btnExport = new Button
        {
            Text = "导出CSV",
            Location = new Point(260, 100),
            Size = new Size(100, 35)
        };
        btnExport.Click += (s, e) =>
        {
            dialog.DialogResult = DialogResult.No;
            dialog.Close();
        };

        var btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(150, 160),
            Size = new Size(100, 35),
            DialogResult = DialogResult.Cancel
        };

        dialog.Controls.Add(lblTitle);
        dialog.Controls.Add(lblInfo);
        dialog.Controls.Add(btnPrint);
        dialog.Controls.Add(btnPreview);
        dialog.Controls.Add(btnExport);
        dialog.Controls.Add(btnCancel);

        return dialog.ShowDialog();
    }
}
