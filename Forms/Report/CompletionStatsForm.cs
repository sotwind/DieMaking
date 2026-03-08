using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Report;

/// <summary>
/// 完工统计窗体
/// </summary>
public partial class CompletionStatsForm : BaseListForm
{
    private readonly ReportService _reportService;
    private readonly PrintService _printService;
    private DataGridView _dgvData = null!;
    private DateTimePicker _dtpStartDate = null!;
    private DateTimePicker _dtpEndDate = null!;
    private TextBox _txtDieCode = null!;
    private TextBox _txtCustomerName = null!;
    private ComboBox _cmbGroupBy = null!;
    private Label _lblSummary = null!;

    public CompletionStatsForm()
    {
        _reportService = new ReportService();
        _printService = new PrintService();
        InitializeComponent();
        this.Text = "完工统计";
    }

    private void InitializeComponent()
    {
        this.Size = UIStyleHelper.SizeListForm;
        this.StartPosition = FormStartPosition.CenterParent;

        // 创建工具栏
        var toolPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            Padding = new Padding(10),
            BorderStyle = BorderStyle.FixedSingle
        };

        // 日期范围标签
        var lblStartDate = UIStyleHelper.CreateLabel("开始日期：", new Point(10, 15), new Size(70, 25));

        _dtpStartDate = new DateTimePicker
        {
            Location = new Point(85, 12),
            Size = new Size(120, 25),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now.AddMonths(-1),
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        var lblEndDate = UIStyleHelper.CreateLabel("结束日期：", new Point(215, 15), new Size(70, 25));

        _dtpEndDate = new DateTimePicker
        {
            Location = new Point(290, 12),
            Size = new Size(120, 25),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        // 刀模编号筛选
        var lblDieCode = UIStyleHelper.CreateLabel("刀模编号：", new Point(420, 15), new Size(70, 25));

        _txtDieCode = UIStyleHelper.CreateTextBox(new Point(495, 12), new Size(120, 25), "输入刀模编号");

        // 客户名称筛选
        var lblCustomerName = UIStyleHelper.CreateLabel("客户名称：", new Point(625, 15), new Size(70, 25));

        _txtCustomerName = UIStyleHelper.CreateTextBox(new Point(700, 12), new Size(120, 25), "输入客户名称");

        // 分组方式
        var lblGroupBy = UIStyleHelper.CreateLabel("统计维度：", new Point(830, 15), new Size(70, 25));

        _cmbGroupBy = new ComboBox
        {
            Location = new Point(905, 12),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        _cmbGroupBy.Items.AddRange(new object[] { "按刀模", "按客户", "按日期" });
        _cmbGroupBy.SelectedIndex = 0;

        // 查询按钮
        var btnQuery = UIStyleHelper.CreateSearchButton();
        btnQuery.Location = new Point(10, 45);
        btnQuery.Click += BtnQuery_Click;

        // 导出按钮
        var btnExport = UIStyleHelper.CreateExportButton("导出CSV");
        btnExport.Location = new Point(120, 45);
        btnExport.Click += BtnExport_Click;

        // 打印按钮
        var btnPrint = UIStyleHelper.CreatePrintButton();
        btnPrint.Location = new Point(230, 45);
        btnPrint.Click += BtnPrint_Click;

        // 汇总信息标签
        _lblSummary = new Label
        {
            Text = "",
            Location = new Point(340, 50),
            Size = new Size(600, 25),
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Bold, GraphicsUnit.Point, 134),
            ForeColor = UIStyleHelper.ColorInfo
        };

        toolPanel.Controls.Add(lblStartDate);
        toolPanel.Controls.Add(_dtpStartDate);
        toolPanel.Controls.Add(lblEndDate);
        toolPanel.Controls.Add(_dtpEndDate);
        toolPanel.Controls.Add(lblDieCode);
        toolPanel.Controls.Add(_txtDieCode);
        toolPanel.Controls.Add(lblCustomerName);
        toolPanel.Controls.Add(_txtCustomerName);
        toolPanel.Controls.Add(lblGroupBy);
        toolPanel.Controls.Add(_cmbGroupBy);
        toolPanel.Controls.Add(btnQuery);
        toolPanel.Controls.Add(btnExport);
        toolPanel.Controls.Add(btnPrint);
        toolPanel.Controls.Add(_lblSummary);

        // 创建数据表格
        _dgvData = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };
        ApplyDataGridViewStyle(_dgvData);

        // 状态栏
        var statusStrip = CreateStatusBar();

        this.Controls.Add(_dgvData);
        this.Controls.Add(toolPanel);
        this.Controls.Add(statusStrip);

        // 初始加载数据
        LoadData();
    }

    private void BtnQuery_Click(object? sender, EventArgs e)
    {
        LoadData();
    }

    protected override void LoadData()
    {
        Form? loadingForm = null;
        try
        {
            loadingForm = UIStyleHelper.ShowLoading(this, "正在加载统计数据...");

            var startDate = _dtpStartDate.Value.Date;
            var endDate = _dtpEndDate.Value.Date;
            var dieCode = _txtDieCode.Text.Trim();
            var customerName = _txtCustomerName.Text.Trim();

            // 检查是否为placeholder
            if (dieCode == (string?)_txtDieCode.Tag) dieCode = "";
            if (customerName == (string?)_txtCustomerName.Tag) customerName = "";

            switch (_cmbGroupBy.SelectedIndex)
            {
                case 0: // 按刀模
                    LoadDataByDie(startDate, endDate, dieCode, customerName);
                    break;
                case 1: // 按客户
                    LoadDataByCustomer(startDate, endDate);
                    break;
                case 2: // 按日期
                    LoadDataByDate(startDate, endDate);
                    break;
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载数据失败：{ex.Message}");
        }
        finally
        {
            loadingForm?.Close();
        }
    }

    private void LoadDataByDie(DateTime startDate, DateTime endDate, string dieCode, string customerName)
    {
        var data = _reportService.GetCompletionStatsByDie(startDate, endDate,
            string.IsNullOrEmpty(dieCode) ? null : dieCode,
            string.IsNullOrEmpty(customerName) ? null : customerName);

        _dgvData.Columns.Clear();
        _dgvData.Columns.Add("DieCode", "刀模编号");
        _dgvData.Columns.Add("CustomerName", "客户名称");
        _dgvData.Columns.Add("ProductName", "产品名称");
        _dgvData.Columns.Add("RequiredProcesses", "所需工序");
        _dgvData.Columns.Add("CompleteTime", "完工时间");
        _dgvData.Columns.Add("TotalAmount", "总金额");
        _dgvData.Columns.Add("OperatorName", "操作人");
        _dgvData.Columns.Add("Remark", "备注");

        _dgvData.Columns["CompleteTime"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Format = "N2";
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        _dgvData.Rows.Clear();
        decimal totalAmount = 0;
        foreach (var item in data)
        {
            _dgvData.Rows.Add(
                item.DieCode,
                item.CustomerName,
                item.ProductName,
                item.RequiredProcesses,
                item.CompleteTime,
                item.TotalAmount,
                item.OperatorName,
                item.Remark
            );
            totalAmount += item.TotalAmount;
        }

        _lblSummary.Text = $"共 {data.Count} 条记录，总金额：{totalAmount:N2} 元";

        if (StatusUserLabel != null)
        {
            StatusUserLabel.Text = _lblSummary.Text;
        }
    }

    private void LoadDataByCustomer(DateTime startDate, DateTime endDate)
    {
        var data = _reportService.GetCompletionStatsByCustomer(startDate, endDate);

        _dgvData.Columns.Clear();
        _dgvData.Columns.Add("CustomerName", "客户名称");
        _dgvData.Columns.Add("CompletionCount", "完工数量");
        _dgvData.Columns.Add("TotalAmount", "总金额");
        _dgvData.Columns.Add("FirstCompleteTime", "首次完工时间");
        _dgvData.Columns.Add("LastCompleteTime", "末次完工时间");

        _dgvData.Columns["CompletionCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Format = "N2";
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _dgvData.Columns["FirstCompleteTime"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
        _dgvData.Columns["LastCompleteTime"].DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";

        _dgvData.Rows.Clear();
        int totalCount = 0;
        decimal totalAmount = 0;
        foreach (var item in data)
        {
            _dgvData.Rows.Add(
                item.CustomerName,
                item.CompletionCount,
                item.TotalAmount,
                item.FirstCompleteTime,
                item.LastCompleteTime
            );
            totalCount += item.CompletionCount;
            totalAmount += item.TotalAmount;
        }

        _lblSummary.Text = $"共 {data.Count} 个客户，完工总数：{totalCount}，总金额：{totalAmount:N2} 元";

        if (StatusUserLabel != null)
        {
            StatusUserLabel.Text = _lblSummary.Text;
        }
    }

    private void LoadDataByDate(DateTime startDate, DateTime endDate)
    {
        var data = _reportService.GetCompletionStatsByDate(startDate, endDate);

        _dgvData.Columns.Clear();
        _dgvData.Columns.Add("CompleteDate", "完工日期");
        _dgvData.Columns.Add("CompletionCount", "完工数量");
        _dgvData.Columns.Add("TotalAmount", "总金额");

        _dgvData.Columns["CompleteDate"].DefaultCellStyle.Format = "yyyy-MM-dd";
        _dgvData.Columns["CompletionCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Format = "N2";
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        _dgvData.Rows.Clear();
        int totalCount = 0;
        decimal totalAmount = 0;
        foreach (var item in data)
        {
            _dgvData.Rows.Add(
                item.CompleteDate,
                item.CompletionCount,
                item.TotalAmount
            );
            totalCount += item.CompletionCount;
            totalAmount += item.TotalAmount;
        }

        _lblSummary.Text = $"共 {data.Count} 天，完工总数：{totalCount}，总金额：{totalAmount:N2} 元";

        if (StatusUserLabel != null)
        {
            StatusUserLabel.Text = _lblSummary.Text;
        }
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_dgvData.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "CSV文件|*.csv",
                Title = "导出数据",
                FileName = $"完工统计_{DateTime.Now:yyyyMMddHHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                _printService.ExportToCsv(_dgvData, saveDialog.FileName);
                ShowSuccess("导出成功！");
            }
        }
        catch (Exception ex)
        {
            ShowError($"导出失败：{ex.Message}");
        }
    }

    private void BtnPrint_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_dgvData.Rows.Count == 0)
            {
                MessageBox.Show("没有数据可打印", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = PrintDialogExtensions.ShowPrintOptions(_dgvData, this.Text, _lblSummary.Text);

            switch (result)
            {
                case DialogResult.OK: // 打印预览
                    _printService.PrintPreview(_dgvData, this.Text, _lblSummary.Text);
                    break;
                case DialogResult.Yes: // 直接打印
                    _printService.Print(_dgvData, this.Text, _lblSummary.Text);
                    break;
                case DialogResult.No: // 导出CSV
                    using (var saveDialog = new SaveFileDialog
                    {
                        Filter = "CSV文件|*.csv",
                        Title = "导出数据",
                        FileName = $"完工统计_{DateTime.Now:yyyyMMddHHmmss}.csv"
                    })
                    {
                        if (saveDialog.ShowDialog() == DialogResult.OK)
                        {
                            _printService.ExportToCsv(_dgvData, saveDialog.FileName);
                            ShowSuccess("导出成功！");
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            ShowError($"打印失败：{ex.Message}");
        }
    }
}
