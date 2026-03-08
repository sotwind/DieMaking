using DieMaking.Models;
using DieMaking.Services;
using DieMaking.Helpers;

namespace DieMaking.Forms.Report;

/// <summary>
/// 库存统计窗体
/// </summary>
public partial class InventoryStatsForm : Form
{
    private readonly ReportService _reportService;
    private readonly PrintService _printService;
    private DataGridView _dgvSummary = null!;
    private DataGridView _dgvLocation = null!;
    private DataGridView _dgvDetail = null!;
    private ComboBox _cmbArea = null!;
    private ComboBox _cmbStatus = null!;
    private TabControl _tabControl = null!;
    private Label _lblSummaryInfo = null!;

    public InventoryStatsForm()
    {
        _reportService = new ReportService();
        _printService = new PrintService();
        InitializeComponent();
        this.Text = "库存统计";
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1200, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.WindowState = FormWindowState.Maximized;

        // 创建顶部面板（包含汇总信息和筛选）
        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 120,
            Padding = new Padding(10)
        };

        // 汇总信息面板
        var summaryPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.LightBlue
        };

        _lblSummaryInfo = new Label
        {
            Text = "正在加载汇总数据...",
            Dock = DockStyle.Fill,
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        summaryPanel.Controls.Add(_lblSummaryInfo);

        // 筛选面板
        var filterPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(10)
        };

        // 区域筛选
        var lblArea = new Label
        {
            Text = "区域：",
            Location = new Point(10, 20),
            Size = new Size(50, 25)
        };

        _cmbArea = new ComboBox
        {
            Location = new Point(65, 17),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbArea.Items.Add("全部");
        _cmbArea.SelectedIndex = 0;

        // 状态筛选
        var lblStatus = new Label
        {
            Text = "状态：",
            Location = new Point(180, 20),
            Size = new Size(50, 25)
        };

        _cmbStatus = new ComboBox
        {
            Location = new Point(235, 17),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbStatus.Items.AddRange(new object[] { "全部", "在库", "借出", "报废", "维修中" });
        _cmbStatus.SelectedIndex = 0;

        // 查询按钮
        var btnQuery = new Button
        {
            Text = "查询",
            Location = new Point(350, 15),
            Size = new Size(80, 28)
        };
        btnQuery.Click += BtnQuery_Click;

        // 刷新按钮
        var btnRefresh = new Button
        {
            Text = "刷新",
            Location = new Point(440, 15),
            Size = new Size(80, 28)
        };
        btnRefresh.Click += BtnRefresh_Click;

        // 导出按钮
        var btnExport = new Button
        {
            Text = "导出Excel",
            Location = new Point(530, 15),
            Size = new Size(90, 28)
        };
        btnExport.Click += BtnExport_Click;

        // 打印按钮
        var btnPrint = new Button
        {
            Text = "打印",
            Location = new Point(620, 15),
            Size = new Size(80, 28)
        };
        btnPrint.Click += BtnPrint_Click;

        filterPanel.Controls.Add(lblArea);
        filterPanel.Controls.Add(_cmbArea);
        filterPanel.Controls.Add(lblStatus);
        filterPanel.Controls.Add(_cmbStatus);
        filterPanel.Controls.Add(btnQuery);
        filterPanel.Controls.Add(btnRefresh);
        filterPanel.Controls.Add(btnExport);
        filterPanel.Controls.Add(btnPrint);

        topPanel.Controls.Add(filterPanel);
        topPanel.Controls.Add(summaryPanel);

        // 创建选项卡控件
        _tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        // 库存汇总选项卡
        var tabSummary = new TabPage("库存状态汇总");
        _dgvSummary = CreateDataGridView();
        tabSummary.Controls.Add(_dgvSummary);

        // 库位分布选项卡
        var tabLocation = new TabPage("库位分布");
        _dgvLocation = CreateDataGridView();
        tabLocation.Controls.Add(_dgvLocation);

        // 库存明细选项卡
        var tabDetail = new TabPage("库存明细");
        _dgvDetail = CreateDataGridView();
        tabDetail.Controls.Add(_dgvDetail);

        _tabControl.TabPages.Add(tabSummary);
        _tabControl.TabPages.Add(tabLocation);
        _tabControl.TabPages.Add(tabDetail);

        this.Controls.Add(_tabControl);
        this.Controls.Add(topPanel);

        // 初始加载数据
        LoadData();
    }

    private DataGridView CreateDataGridView()
    {
        return new DataGridView
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
    }

    private void BtnQuery_Click(object? sender, EventArgs e)
    {
        LoadDetailData();
        _tabControl.SelectedIndex = 2; // 切换到明细选项卡
    }

    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            LoadSummaryData();
            LoadLocationData();
            LoadDetailData();
            LoadAreaFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadSummaryData()
    {
        var stats = _reportService.GetInventorySummaryStats();

        // 更新顶部汇总信息
        _lblSummaryInfo.Text = $"库存总数：{stats.TotalCount} | 在库：{stats.InStockCount} ({stats.InStockRate:F1}%) | " +
                              $"借出：{stats.BorrowedCount} ({stats.BorrowedRate:F1}%) | " +
                              $"报废：{stats.ScrappedCount} | 维修中：{stats.RepairingCount}";

        // 填充汇总表格
        _dgvSummary.Columns.Clear();
        _dgvSummary.Columns.Add("Status", "状态");
        _dgvSummary.Columns.Add("Count", "数量");
        _dgvSummary.Columns.Add("Percentage", "占比");

        _dgvSummary.Columns["Count"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvSummary.Columns["Percentage"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        _dgvSummary.Rows.Clear();
        
        _dgvSummary.Rows.Add("在库", stats.InStockCount, $"{stats.InStockRate:F1}%");
        _dgvSummary.Rows.Add("借出", stats.BorrowedCount, $"{stats.BorrowedRate:F1}%");
        _dgvSummary.Rows.Add("报废", stats.ScrappedCount, $"{(stats.TotalCount > 0 ? (double)stats.ScrappedCount / stats.TotalCount * 100 : 0):F1}%");
        _dgvSummary.Rows.Add("维修中", stats.RepairingCount, $"{(stats.TotalCount > 0 ? (double)stats.RepairingCount / stats.TotalCount * 100 : 0):F1}%");
        
        // 汇总行
        int summaryRow = _dgvSummary.Rows.Add("【合计】", stats.TotalCount, "100.0%");
        _dgvSummary.Rows[summaryRow].DefaultCellStyle.Font = new Font(_dgvSummary.Font, FontStyle.Bold);
        _dgvSummary.Rows[summaryRow].DefaultCellStyle.BackColor = Color.LightYellow;
    }

    private void LoadLocationData()
    {
        var data = _reportService.GetLocationDistributionStats();

        _dgvLocation.Columns.Clear();
        _dgvLocation.Columns.Add("Area", "区域");
        _dgvLocation.Columns.Add("ShelfNo", "货架号");
        _dgvLocation.Columns.Add("DieCount", "刀模总数");
        _dgvLocation.Columns.Add("InStockCount", "在库数量");
        _dgvLocation.Columns.Add("BorrowedCount", "借出数量");

        _dgvLocation.Columns["DieCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvLocation.Columns["InStockCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvLocation.Columns["BorrowedCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        _dgvLocation.Rows.Clear();
        string currentArea = "";
        int areaTotalCount = 0;
        int areaInStockCount = 0;
        int areaBorrowedCount = 0;
        int grandTotalCount = 0;
        int grandInStockCount = 0;
        int grandBorrowedCount = 0;

        foreach (var item in data)
        {
            // 新区域开始时，添加上一区域的汇总行
            if (currentArea != "" && currentArea != item.Area)
            {
                int areaRow = _dgvLocation.Rows.Add(
                    $"【{currentArea} 小计】",
                    "",
                    areaTotalCount,
                    areaInStockCount,
                    areaBorrowedCount
                );
                _dgvLocation.Rows[areaRow].DefaultCellStyle.Font = new Font(_dgvLocation.Font, FontStyle.Bold);
                _dgvLocation.Rows[areaRow].DefaultCellStyle.BackColor = Color.LightCyan;
                
                areaTotalCount = 0;
                areaInStockCount = 0;
                areaBorrowedCount = 0;
            }

            _dgvLocation.Rows.Add(
                item.Area,
                item.ShelfNo,
                item.DieCount,
                item.InStockCount,
                item.BorrowedCount
            );

            areaTotalCount += item.DieCount;
            areaInStockCount += item.InStockCount;
            areaBorrowedCount += item.BorrowedCount;
            grandTotalCount += item.DieCount;
            grandInStockCount += item.InStockCount;
            grandBorrowedCount += item.BorrowedCount;

            currentArea = item.Area;
        }

        // 添加最后一个区域的汇总行
        if (currentArea != "")
        {
            int areaRow = _dgvLocation.Rows.Add(
                $"【{currentArea} 小计】",
                "",
                areaTotalCount,
                areaInStockCount,
                areaBorrowedCount
            );
            _dgvLocation.Rows[areaRow].DefaultCellStyle.Font = new Font(_dgvLocation.Font, FontStyle.Bold);
            _dgvLocation.Rows[areaRow].DefaultCellStyle.BackColor = Color.LightCyan;
        }

        // 添加总计行
        if (grandTotalCount > 0)
        {
            int grandRow = _dgvLocation.Rows.Add(
                "【总计】",
                "",
                grandTotalCount,
                grandInStockCount,
                grandBorrowedCount
            );
            _dgvLocation.Rows[grandRow].DefaultCellStyle.Font = new Font(_dgvLocation.Font, FontStyle.Bold);
            _dgvLocation.Rows[grandRow].DefaultCellStyle.BackColor = Color.LightYellow;
        }
    }

    private void LoadDetailData()
    {
        string? area = _cmbArea.SelectedIndex > 0 ? _cmbArea.SelectedItem?.ToString() : null;
        StorageStatus? status = null;
        
        if (_cmbStatus.SelectedIndex > 0)
        {
            status = _cmbStatus.SelectedIndex switch
            {
                1 => StorageStatus.InStock,
                2 => StorageStatus.Borrowed,
                3 => StorageStatus.Scrapped,
                4 => StorageStatus.Repairing,
                _ => null
            };
        }

        var data = _reportService.GetInventoryDetailStats(area, null, status);

        _dgvDetail.Columns.Clear();
        _dgvDetail.Columns.Add("DieCode", "刀模编号");
        _dgvDetail.Columns.Add("CustomerName", "客户名称");
        _dgvDetail.Columns.Add("ProductName", "产品名称");
        _dgvDetail.Columns.Add("Location", "库位");
        _dgvDetail.Columns.Add("StorageStatus", "库存状态");
        _dgvDetail.Columns.Add("InStockTime", "入库时间");
        _dgvDetail.Columns.Add("LastBorrowTime", "上次借出");
        _dgvDetail.Columns.Add("LastReturnTime", "上次归还");
        _dgvDetail.Columns.Add("TotalBorrowCount", "借用次数");

        _dgvDetail.Columns["InStockTime"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        _dgvDetail.Columns["LastBorrowTime"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        _dgvDetail.Columns["LastReturnTime"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        _dgvDetail.Columns["TotalBorrowCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        _dgvDetail.Rows.Clear();
        foreach (var item in data)
        {
            _dgvDetail.Rows.Add(
                item.DieCode,
                item.CustomerName,
                item.ProductName,
                item.LocationText,
                item.StorageStatusText,
                item.InStockTime,
                item.LastBorrowTime,
                item.LastReturnTime,
                item.TotalBorrowCount
            );
        }
    }

    private void LoadAreaFilter()
    {
        // 从库位分布数据中获取区域列表
        var data = _reportService.GetLocationDistributionStats();
        var areas = data.Select(d => d.Area).Distinct().ToList();

        _cmbArea.Items.Clear();
        _cmbArea.Items.Add("全部");
        foreach (var area in areas)
        {
            _cmbArea.Items.Add(area);
        }
        _cmbArea.SelectedIndex = 0;
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        try
        {
            DataGridView currentGrid;
            string sheetName;
            
            switch (_tabControl.SelectedIndex)
            {
                case 0:
                    currentGrid = _dgvSummary;
                    sheetName = "库存状态汇总";
                    break;
                case 1:
                    currentGrid = _dgvLocation;
                    sheetName = "库位分布";
                    break;
                case 2:
                    currentGrid = _dgvDetail;
                    sheetName = "库存明细";
                    break;
                default:
                    return;
            }
            
            if (currentGrid.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel文件|*.xlsx|CSV文件|*.csv",
                Title = "导出数据",
                FileName = $"库存统计_{sheetName}_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
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
            DataGridView currentGrid;
            string sheetName;

            switch (_tabControl.SelectedIndex)
            {
                case 0:
                    currentGrid = _dgvSummary;
                    sheetName = "库存状态汇总";
                    break;
                case 1:
                    currentGrid = _dgvLocation;
                    sheetName = "库位分布";
                    break;
                case 2:
                    currentGrid = _dgvDetail;
                    sheetName = "库存明细";
                    break;
                default:
                    return;
            }
            
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
                        FileName = $"库存统计_{sheetName}_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
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
