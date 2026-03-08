using DieMaking.Models;
using DieMaking.Services;
using DieMaking.Helpers;

namespace DieMaking.Forms.Report;

/// <summary>
/// 工序统计窗体
/// </summary>
public partial class ProcessStatsForm : Form
{
    private readonly ReportService _reportService;
    private readonly PrintService _printService;
    private DataGridView _dgvSummary = null!;
    private DataGridView _dgvDetail = null!;
    private DateTimePicker _dtpStartDate = null!;
    private DateTimePicker _dtpEndDate = null!;
    private TextBox _txtProcessName = null!;
    private TabControl _tabControl = null!;
    private Label _lblSummaryInfo = null!;

    public ProcessStatsForm()
    {
        _reportService = new ReportService();
        _printService = new PrintService();
        InitializeComponent();
        this.Text = "工序统计";
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1200, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.WindowState = FormWindowState.Maximized;

        // 创建工具栏
        var toolPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            Padding = new Padding(10),
            BorderStyle = BorderStyle.FixedSingle
        };

        // 日期范围标签
        var lblStartDate = new Label
        {
            Text = "开始日期：",
            Location = new Point(10, 15),
            Size = new Size(70, 25)
        };

        _dtpStartDate = new DateTimePicker
        {
            Location = new Point(85, 12),
            Size = new Size(120, 25),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now.AddMonths(-1)
        };

        var lblEndDate = new Label
        {
            Text = "结束日期：",
            Location = new Point(215, 15),
            Size = new Size(70, 25)
        };

        _dtpEndDate = new DateTimePicker
        {
            Location = new Point(290, 12),
            Size = new Size(120, 25),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now
        };

        // 工序名称筛选
        var lblProcessName = new Label
        {
            Text = "工序名称：",
            Location = new Point(420, 15),
            Size = new Size(70, 25)
        };

        _txtProcessName = new TextBox
        {
            Location = new Point(495, 12),
            Size = new Size(150, 25)
        };

        // 查询按钮
        var btnQuery = new Button
        {
            Text = "查询",
            Location = new Point(660, 10),
            Size = new Size(80, 28)
        };
        btnQuery.Click += BtnQuery_Click;

        // 导出按钮
        var btnExport = new Button
        {
            Text = "导出Excel",
            Location = new Point(750, 10),
            Size = new Size(90, 28)
        };
        btnExport.Click += BtnExport_Click;

        // 打印按钮
        var btnPrint = new Button
        {
            Text = "打印",
            Location = new Point(840, 10),
            Size = new Size(80, 28)
        };
        btnPrint.Click += BtnPrint_Click;

        // 统计信息标签
        _lblSummaryInfo = new Label
        {
            Text = "统计说明：统计各工序的生产效率，包括计划数量、完成数量、完成率、平均耗时等",
            Location = new Point(10, 50),
            Size = new Size(800, 25),
            Font = new Font("微软雅黑", 9),
            ForeColor = Color.Gray
        };

        toolPanel.Controls.Add(lblStartDate);
        toolPanel.Controls.Add(_dtpStartDate);
        toolPanel.Controls.Add(lblEndDate);
        toolPanel.Controls.Add(_dtpEndDate);
        toolPanel.Controls.Add(lblProcessName);
        toolPanel.Controls.Add(_txtProcessName);
        toolPanel.Controls.Add(btnQuery);
        toolPanel.Controls.Add(btnExport);
        toolPanel.Controls.Add(btnPrint);
        toolPanel.Controls.Add(_lblSummaryInfo);

        // 创建选项卡控件
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        // 汇总统计选项卡
        var tabSummary = new TabPage("工序汇总统计");
        _dgvSummary = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D
        };
        tabSummary.Controls.Add(_dgvSummary);

        // 明细统计选项卡
        var tabDetail = new TabPage("工序明细");
        _dgvDetail = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D
        };
        tabDetail.Controls.Add(_dgvDetail);

        _tabControl.TabPages.Add(tabSummary);
        _tabControl.TabPages.Add(tabDetail);

        this.Controls.Add(_tabControl);
        this.Controls.Add(toolPanel);

        // 初始加载数据
        LoadData();
    }

    private void BtnQuery_Click(object? sender, EventArgs e)
    {
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var startDate = _dtpStartDate.Value.Date;
            var endDate = _dtpEndDate.Value.Date;
            var processName = _txtProcessName.Text.Trim();

            LoadSummaryData(startDate, endDate, processName);
            LoadDetailData(startDate, endDate, processName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadSummaryData(DateTime startDate, DateTime endDate, string processName)
    {
        var data = _reportService.GetProcessStats(startDate, endDate, 
            string.IsNullOrEmpty(processName) ? null : processName);

        _dgvSummary.Columns.Clear();
        _dgvSummary.Columns.Add("ProcessName", "工序名称");
        _dgvSummary.Columns.Add("TotalCount", "计划数量");
        _dgvSummary.Columns.Add("CompletedCount", "完成数量");
        _dgvSummary.Columns.Add("InProgressCount", "进行中");
        _dgvSummary.Columns.Add("PendingCount", "待生产");
        _dgvSummary.Columns.Add("CompletionRate", "完成率");
        _dgvSummary.Columns.Add("AvgDuration", "平均耗时");
        _dgvSummary.Columns.Add("TotalAmount", "总金额");

        _dgvSummary.Columns["TotalCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvSummary.Columns["CompletedCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvSummary.Columns["InProgressCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvSummary.Columns["PendingCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvSummary.Columns["CompletionRate"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvSummary.Columns["TotalAmount"].DefaultCellStyle.Format = "N2";
        _dgvSummary.Columns["TotalAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        _dgvSummary.Rows.Clear();
        int totalCount = 0;
        int completedCount = 0;
        decimal totalAmount = 0;

        foreach (var item in data)
        {
            var rateText = $"{item.CompletionRate:F1}%";
            var avgDuration = item.AvgDurationMinutes > 0 ? $"{item.AvgDurationMinutes / 60:F1}小时" : "-";

            _dgvSummary.Rows.Add(
                item.ProcessName,
                item.TotalCount,
                item.CompletedCount,
                item.InProgressCount,
                item.PendingCount,
                rateText,
                avgDuration,
                item.TotalAmount
            );

            totalCount += item.TotalCount;
            completedCount += item.CompletedCount;
            totalAmount += item.TotalAmount;
        }

        // 添加汇总行
        if (data.Count > 0)
        {
            var totalRate = totalCount > 0 ? (double)completedCount / totalCount * 100 : 0;
            int summaryRow = _dgvSummary.Rows.Add(
                "【汇总】",
                totalCount,
                completedCount,
                "-",
                "-",
                $"{totalRate:F1}%",
                "-",
                totalAmount
            );
            _dgvSummary.Rows[summaryRow].DefaultCellStyle.Font = new Font(_dgvSummary.Font, FontStyle.Bold);
            _dgvSummary.Rows[summaryRow].DefaultCellStyle.BackColor = Color.LightYellow;
        }
    }

    private void LoadDetailData(DateTime startDate, DateTime endDate, string processName)
    {
        var data = _reportService.GetProcessDetailStats(startDate, endDate,
            string.IsNullOrEmpty(processName) ? null : processName);

        _dgvDetail.Columns.Clear();
        _dgvDetail.Columns.Add("DieCode", "刀模编号");
        _dgvDetail.Columns.Add("CustomerName", "客户名称");
        _dgvDetail.Columns.Add("ProcessName", "工序名称");
        _dgvDetail.Columns.Add("Status", "状态");
        _dgvDetail.Columns.Add("StartTime", "开始时间");
        _dgvDetail.Columns.Add("CompleteTime", "完成时间");
        _dgvDetail.Columns.Add("Duration", "耗时");
        _dgvDetail.Columns.Add("OperatorName", "操作人");
        _dgvDetail.Columns.Add("Amount", "金额");

        _dgvDetail.Columns["StartTime"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        _dgvDetail.Columns["CompleteTime"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        _dgvDetail.Columns["Amount"].DefaultCellStyle.Format = "N2";
        _dgvDetail.Columns["Amount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        _dgvDetail.Rows.Clear();
        foreach (var item in data)
        {
            var duration = item.DurationMinutes.HasValue 
                ? $"{item.DurationMinutes.Value / 60}小时{item.DurationMinutes.Value % 60}分钟" 
                : "-";

            _dgvDetail.Rows.Add(
                item.DieCode,
                item.CustomerName,
                item.ProcessName,
                item.StatusText,
                item.StartTime,
                item.CompleteTime,
                duration,
                item.OperatorName,
                item.Amount
            );
        }
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        try
        {
            var currentGrid = _tabControl.SelectedIndex == 0 ? _dgvSummary : _dgvDetail;
            var sheetName = _tabControl.SelectedIndex == 0 ? "工序汇总统计" : "工序明细";
            
            if (currentGrid.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel文件|*.xlsx|CSV文件|*.csv",
                Title = "导出数据",
                FileName = $"工序统计_{sheetName}_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                var importExportService = new ImportExportService();

                if (saveDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    _printService.ExportToCsv(currentGrid, saveDialog.FileName);
                }
                else
                {
                    var dataTable = importExportService.ConvertDataGridViewToDataTable(currentGrid);
                    importExportService.ExportToExcel(dataTable, sheetName, saveDialog.FileName);
                }

                MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnPrint_Click(object? sender, EventArgs e)
    {
        try
        {
            var currentGrid = _tabControl.SelectedIndex == 0 ? _dgvSummary : _dgvDetail;
            var sheetName = _tabControl.SelectedIndex == 0 ? "工序汇总统计" : "工序明细";

            if (currentGrid.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可打印", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var title = $"{this.Text} - {sheetName}";
            var result = PrintDialogExtensions.ShowPrintOptions(currentGrid, title, _lblSummaryInfo.Text);

            switch (result)
            {
                case DialogResult.OK: // 打印预览
                    _printService.PrintPreview(currentGrid, title, _lblSummaryInfo.Text);
                    break;
                case DialogResult.Yes: // 直接打印
                    _printService.Print(currentGrid, title, _lblSummaryInfo.Text);
                    break;
                case DialogResult.No: // 导出
                    using (var saveDialog = new SaveFileDialog
                    {
                        Filter = "Excel文件|*.xlsx|CSV文件|*.csv",
                        Title = "导出数据",
                        FileName = $"工序统计_{sheetName}_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                    })
                    {
                        if (saveDialog.ShowDialog() == DialogResult.OK)
                        {
                            var importExportService = new ImportExportService();

                            if (saveDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                            {
                                _printService.ExportToCsv(currentGrid, saveDialog.FileName);
                            }
                            else
                            {
                                var dataTable = importExportService.ConvertDataGridViewToDataTable(currentGrid);
                                importExportService.ExportToExcel(dataTable, sheetName, saveDialog.FileName);
                            }

                            MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打印失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
