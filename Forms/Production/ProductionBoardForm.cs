using DieMaking.Models;
using DieMaking.Services;
using DieMaking.Helpers;

namespace DieMaking.Forms.Production;

public partial class ProductionBoardForm : Form
{
    private readonly ProductionService _productionService;
    private ListView _lvPending = null!;
    private ListView _lvInProgress = null!;
    private ListView _lvCompleted = null!;
    private DateTimePicker _dtpStartDate = null!;
    private DateTimePicker _dtpEndDate = null!;
    private TextBox _txtCustomer = null!;
    private TextBox _txtDieCode = null!;
    private Label _lblStats = null!;

    public ProductionBoardForm()
    {
        _productionService = new ProductionService();
        InitializeComponent();
        this.Text = "生产看板";
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1200, 800);
        this.StartPosition = FormStartPosition.CenterParent;
        this.WindowState = FormWindowState.Maximized;

        // 顶部筛选区域
        var panelTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(10)
        };

        var lblStartDate = new Label
        {
            Text = "开始日期：",
            Location = new Point(10, 20),
            AutoSize = true
        };

        _dtpStartDate = new DateTimePicker
        {
            Location = new Point(80, 16),
            Width = 120,
            Format = DateTimePickerFormat.Short
        };

        var lblEndDate = new Label
        {
            Text = "结束日期：",
            Location = new Point(210, 20),
            AutoSize = true
        };

        _dtpEndDate = new DateTimePicker
        {
            Location = new Point(280, 16),
            Width = 120,
            Format = DateTimePickerFormat.Short
        };

        var lblCustomer = new Label
        {
            Text = "客户：",
            Location = new Point(410, 20),
            AutoSize = true
        };

        _txtCustomer = new TextBox
        {
            Location = new Point(455, 16),
            Width = 120
        };

        var lblDieCode = new Label
        {
            Text = "刀模编号：",
            Location = new Point(585, 20),
            AutoSize = true
        };

        _txtDieCode = new TextBox
        {
            Location = new Point(650, 16),
            Width = 120
        };

        var btnSearch = new Button
        {
            Text = "查询",
            Location = new Point(780, 15),
            Size = new Size(80, 28)
        };
        btnSearch.Click += BtnSearch_Click;

        var btnReset = new Button
        {
            Text = "重置",
            Location = new Point(870, 15),
            Size = new Size(80, 28)
        };
        btnReset.Click += BtnReset_Click;

        var btnExport = new Button
        {
            Text = "导出Excel",
            Location = new Point(960, 15),
            Size = new Size(90, 28)
        };
        btnExport.Click += BtnExport_Click;

        var btnPrint = new Button
        {
            Text = "打印",
            Location = new Point(1060, 15),
            Size = new Size(80, 28)
        };
        btnPrint.Click += BtnPrint_Click;

        _lblStats = new Label
        {
            Location = new Point(1150, 20),
            AutoSize = true,
            Font = new Font("微软雅黑", 9, FontStyle.Bold),
            ForeColor = Color.DarkBlue
        };

        panelTop.Controls.AddRange(new Control[]
        {
            lblStartDate, _dtpStartDate, lblEndDate, _dtpEndDate,
            lblCustomer, _txtCustomer, lblDieCode, _txtDieCode,
            btnSearch, btnReset, btnExport, btnPrint, _lblStats
        });

        // 看板主区域 - 三列布局
        var panelBoard = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        // 待生产列
        var panelPending = CreateBoardColumn("待生产", Color.Orange, out _lvPending);
        panelPending.Dock = DockStyle.Left;
        panelPending.Width = 380;

        // 生产中列
        var panelInProgress = CreateBoardColumn("生产中", Color.DodgerBlue, out _lvInProgress);
        panelInProgress.Dock = DockStyle.Left;
        panelInProgress.Width = 380;

        // 已完成列
        var panelCompleted = CreateBoardColumn("已完成", Color.Green, out _lvCompleted);
        panelCompleted.Dock = DockStyle.Fill;

        panelBoard.Controls.Add(panelCompleted);
        panelBoard.Controls.Add(panelInProgress);
        panelBoard.Controls.Add(panelPending);

        this.Controls.Add(panelBoard);
        this.Controls.Add(panelTop);

        // 设置默认值
        _dtpStartDate.Value = DateTime.Now.AddMonths(-1);
        _dtpEndDate.Value = DateTime.Now;
    }

    private Panel CreateBoardColumn(string title, Color headerColor, out ListView listView)
    {
        var panel = new Panel
        {
            Padding = new Padding(5),
            Margin = new Padding(5)
        };

        // 标题栏
        var lblTitle = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 35,
            BackColor = headerColor,
            ForeColor = Color.White,
            Font = new Font("微软雅黑", 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ListView
        listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = false
        };

        listView.Columns.Add("刀模编号", 100);
        listView.Columns.Add("客户", 80);
        listView.Columns.Add("产品", 80);
        listView.Columns.Add("进度", 60);
        listView.Columns.Add("交期", 80);

        panel.Controls.Add(listView);
        panel.Controls.Add(lblTitle);

        return panel;
    }

    private void LoadData()
    {
        try
        {
            var data = _productionService.GetProductionBoardData(
                _dtpStartDate.Value.Date,
                _dtpEndDate.Value.Date.AddDays(1).AddSeconds(-1),
                string.IsNullOrEmpty(_txtCustomer.Text) ? null : _txtCustomer.Text.Trim(),
                string.IsNullOrEmpty(_txtDieCode.Text) ? null : _txtDieCode.Text.Trim()
            );

            // 更新统计信息
            _lblStats.Text = $"统计：待生产 {data.Statistics.PendingCount} | 生产中 {data.Statistics.InProgressCount} | 已完成 {data.Statistics.CompletedCount} | 总计 {data.Statistics.TotalCount}";

            // 填充待生产列表
            _lvPending.Items.Clear();
            foreach (var item in data.PendingList)
            {
                var listItem = new ListViewItem(item.DieCode);
                listItem.SubItems.Add(item.CustomerName);
                listItem.SubItems.Add(item.ProductName);
                listItem.SubItems.Add(item.ProgressText);
                listItem.SubItems.Add(item.DeliveryDate?.ToString("MM-dd") ?? "-");
                listItem.Tag = item;
                _lvPending.Items.Add(listItem);
            }

            // 填充生产中列表
            _lvInProgress.Items.Clear();
            foreach (var item in data.InProgressList)
            {
                var listItem = new ListViewItem(item.DieCode);
                listItem.SubItems.Add(item.CustomerName);
                listItem.SubItems.Add(item.ProductName);
                listItem.SubItems.Add(item.ProgressText);
                listItem.SubItems.Add(item.DeliveryDate?.ToString("MM-dd") ?? "-");
                listItem.Tag = item;
                _lvInProgress.Items.Add(listItem);
            }

            // 填充已完成列表
            _lvCompleted.Items.Clear();
            foreach (var item in data.CompletedList)
            {
                var listItem = new ListViewItem(item.DieCode);
                listItem.SubItems.Add(item.CustomerName);
                listItem.SubItems.Add(item.ProductName);
                listItem.SubItems.Add(item.ProgressText);
                listItem.SubItems.Add(item.DeliveryDate?.ToString("MM-dd") ?? "-");
                listItem.Tag = item;
                _lvCompleted.Items.Add(listItem);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnSearch_Click(object? sender, EventArgs e)
    {
        LoadData();
    }

    private void BtnReset_Click(object? sender, EventArgs e)
    {
        _dtpStartDate.Value = DateTime.Now.AddMonths(-1);
        _dtpEndDate.Value = DateTime.Now;
        _txtCustomer.Clear();
        _txtDieCode.Clear();
        LoadData();
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        try
        {
            var data = _productionService.GetProductionBoardData(
                _dtpStartDate.Value.Date,
                _dtpEndDate.Value.Date.AddDays(1).AddSeconds(-1),
                string.IsNullOrEmpty(_txtCustomer.Text) ? null : _txtCustomer.Text.Trim(),
                string.IsNullOrEmpty(_txtDieCode.Text) ? null : _txtDieCode.Text.Trim()
            );

            if (data.Statistics.TotalCount == 0)
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel文件|*.xlsx|CSV文件|*.csv",
                FileName = $"生产看板_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                var importExportService = new ImportExportService();

                var dataTable = new global::System.Data.DataTable();
                dataTable.Columns.Add("刀模编号", typeof(string));
                dataTable.Columns.Add("客户名称", typeof(string));
                dataTable.Columns.Add("产品名称", typeof(string));
                dataTable.Columns.Add("状态", typeof(string));
                dataTable.Columns.Add("进度", typeof(string));
                dataTable.Columns.Add("交期", typeof(string));

                // 添加待生产数据
                foreach (ListViewItem item in _lvPending.Items)
                {
                    dataTable.Rows.Add(item.SubItems[0].Text, item.SubItems[1].Text, item.SubItems[2].Text, "待生产", item.SubItems[3].Text, item.SubItems[4].Text);
                }
                // 添加生产中数据
                foreach (ListViewItem item in _lvInProgress.Items)
                {
                    dataTable.Rows.Add(item.SubItems[0].Text, item.SubItems[1].Text, item.SubItems[2].Text, "生产中", item.SubItems[3].Text, item.SubItems[4].Text);
                }
                // 添加已完成数据
                foreach (ListViewItem item in _lvCompleted.Items)
                {
                    dataTable.Rows.Add(item.SubItems[0].Text, item.SubItems[1].Text, item.SubItems[2].Text, "已完成", item.SubItems[3].Text, item.SubItems[4].Text);
                }

                if (saveDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    importExportService.ExportToCsv(dataTable, saveDialog.FileName);
                }
                else
                {
                    importExportService.ExportToExcel(dataTable, "生产看板", saveDialog.FileName);
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
        // 创建打印用的DataGridView
        var dgvPrint = new DataGridView();
        dgvPrint.Columns.Add("DieCode", "刀模编号");
        dgvPrint.Columns.Add("CustomerName", "客户");
        dgvPrint.Columns.Add("ProductName", "产品");
        dgvPrint.Columns.Add("Status", "状态");
        dgvPrint.Columns.Add("Progress", "进度");
        dgvPrint.Columns.Add("DeliveryDate", "交期");

        // 添加待生产数据
        foreach (ListViewItem item in _lvPending.Items)
        {
            dgvPrint.Rows.Add(item.SubItems[0].Text, item.SubItems[1].Text, item.SubItems[2].Text, "待生产", item.SubItems[3].Text, item.SubItems[4].Text);
        }
        // 添加生产中数据
        foreach (ListViewItem item in _lvInProgress.Items)
        {
            dgvPrint.Rows.Add(item.SubItems[0].Text, item.SubItems[1].Text, item.SubItems[2].Text, "生产中", item.SubItems[3].Text, item.SubItems[4].Text);
        }
        // 添加已完成数据
        foreach (ListViewItem item in _lvCompleted.Items)
        {
            dgvPrint.Rows.Add(item.SubItems[0].Text, item.SubItems[1].Text, item.SubItems[2].Text, "已完成", item.SubItems[3].Text, item.SubItems[4].Text);
        }

        if (dgvPrint.Rows.Count == 0)
        {
            MessageBox.Show("没有数据可打印", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var printService = new PrintService();
        var subtitle = $"打印时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}  操作员：{CurrentUser.User?.RealName ?? CurrentUser.User?.Username ?? "未知"}  {_lblStats.Text}";
        printService.PrintPreview(dgvPrint, "刀模管理系统 - 生产看板", subtitle);
    }
}
