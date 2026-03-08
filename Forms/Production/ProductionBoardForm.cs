using DieMaking.Models;
using DieMaking.Services;

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

        _lblStats = new Label
        {
            Location = new Point(960, 20),
            AutoSize = true,
            Font = new Font("微软雅黑", 9, FontStyle.Bold),
            ForeColor = Color.DarkBlue
        };

        panelTop.Controls.AddRange(new Control[]
        {
            lblStartDate, _dtpStartDate, lblEndDate, _dtpEndDate,
            lblCustomer, _txtCustomer, lblDieCode, _txtDieCode,
            btnSearch, btnReset, _lblStats
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
}
