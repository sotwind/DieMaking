using DieMaking.Models;
using DieMaking.Services;
using DieMaking.Helpers;

namespace DieMaking.Forms.Warehouse;

public partial class BorrowRecordForm : Form
{
    private readonly WarehouseService _warehouseService;
    private BindingSource _bindingSource = new();
    private List<DieBorrowRecord> _records = new();

    public BorrowRecordForm()
    {
        InitializeComponent();
        _warehouseService = new WarehouseService();
        LoadRecords();
    }

    private void InitializeComponent()
    {
        this.Text = "借用记录";
        this.Size = new Size(1200, 700);
        this.StartPosition = FormStartPosition.CenterParent;

        // 创建工具栏
        var toolStrip = new ToolStrip();
        
        var btnRefresh = new ToolStripButton("刷新") { Image = SystemIcons.Question.ToBitmap() };
        btnRefresh.Click += (s, e) => LoadRecords();
        
        var btnExport = new ToolStripButton("导出Excel") { Image = SystemIcons.Question.ToBitmap() };
        btnExport.Click += (s, e) => ExportData();

        toolStrip.Items.AddRange(new ToolStripItem[] { btnRefresh, new ToolStripSeparator(), btnExport });

        // 搜索区域
        var panelSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 80,
            Padding = new Padding(10, 5, 10, 5)
        };

        int x = 10;
        int y = 8;

        // 刀模编号
        var lblDieCode = new Label { Text = "刀模编号：", Location = new Point(x, y + 3), AutoSize = true };
        x += 70;
        txtDieCode = new TextBox { Location = new Point(x, y), Size = new Size(120, 25) };
        x += 130;

        // 领用人
        var lblBorrower = new Label { Text = "领用人：", Location = new Point(x, y + 3), AutoSize = true };
        x += 60;
        txtBorrower = new TextBox { Location = new Point(x, y), Size = new Size(100, 25) };
        x += 110;

        // 状态
        var lblFilterStatus = new Label { Text = "状态：", Location = new Point(x, y + 3), AutoSize = true };
        x += 50;
        cboStatus = new ComboBox 
        { 
            Location = new Point(x, y), 
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboStatus.Items.Add("全部");
        cboStatus.Items.Add("借用中");
        cboStatus.Items.Add("已归还");
        cboStatus.Items.Add("逾期");
        cboStatus.SelectedIndex = 0;
        x += 110;

        // 开始日期
        var lblStartDate = new Label { Text = "开始日期：", Location = new Point(x, y + 3), AutoSize = true };
        x += 70;
        dtpStartDate = new DateTimePicker 
        { 
            Location = new Point(x, y), 
            Size = new Size(120, 25),
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Checked = false
        };
        x += 130;

        // 结束日期
        var lblEndDate = new Label { Text = "结束日期：", Location = new Point(x, y + 3), AutoSize = true };
        x += 70;
        dtpEndDate = new DateTimePicker 
        { 
            Location = new Point(x, y), 
            Size = new Size(120, 25),
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Checked = false
        };
        x += 130;

        // 查询按钮
        var btnSearch = new Button { Text = "查询", Location = new Point(x, y), Size = new Size(80, 28) };
        btnSearch.Click += (s, e) => SearchRecords();
        x += 90;

        // 清空按钮
        var btnClear = new Button { Text = "清空", Location = new Point(x, y), Size = new Size(80, 28) };
        btnClear.Click += (s, e) => ClearFilters();

        panelSearch.Controls.AddRange(new Control[] {
            lblDieCode, txtDieCode, lblBorrower, txtBorrower, lblFilterStatus, cboStatus,
            lblStartDate, dtpStartDate, lblEndDate, dtpEndDate, btnSearch, btnClear
        });

        // 数据表格
        dgvRecords = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            RowHeadersVisible = false,
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.AliceBlue }
        };

        // 添加列
        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "BorrowID",
            HeaderText = "ID",
            Width = 60,
            Visible = false
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "DieCode",
            HeaderText = "刀模编号",
            Width = 120
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "CustomerName",
            HeaderText = "客户名称",
            Width = 150
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ProductName",
            HeaderText = "产品名称",
            Width = 150
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "BorrowTypeText",
            HeaderText = "借用类型",
            Width = 80
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "BorrowerName",
            HeaderText = "领用人",
            Width = 80
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "BorrowDept",
            HeaderText = "部门",
            Width = 100
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "BorrowTime",
            HeaderText = "借用时间",
            Width = 130,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" }
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ExpectedReturnTime",
            HeaderText = "预计归还",
            Width = 130,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" }
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ActualReturnTime",
            HeaderText = "实际归还",
            Width = 130,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" }
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "StatusText",
            HeaderText = "状态",
            Width = 70
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Purpose",
            HeaderText = "用途",
            Width = 200
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ReturnOperatorName",
            HeaderText = "归还操作人",
            Width = 90
        });

        // 状态栏
        var statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel("就绪");
        statusStrip.Items.Add(lblStatus);

        // 布局
        var panelContent = new Panel { Dock = DockStyle.Fill };
        panelContent.Controls.Add(dgvRecords);

        this.Controls.Add(panelContent);
        this.Controls.Add(panelSearch);
        this.Controls.Add(toolStrip);
        this.Controls.Add(statusStrip);
    }

    private void LoadRecords()
    {
        try
        {
            _records = _warehouseService.GetAllBorrowRecords();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SearchRecords()
    {
        try
        {
            DateTime? startDate = dtpStartDate.Checked ? dtpStartDate.Value : null;
            DateTime? endDate = dtpEndDate.Checked ? dtpEndDate.Value : null;

            _records = _warehouseService.SearchBorrowRecords(
                txtDieCode.Text.Trim(),
                txtBorrower.Text.Trim(),
                startDate,
                endDate
            );

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"搜索失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyFilters()
    {
        var filteredRecords = _records.AsEnumerable();

        // 状态筛选
        if (cboStatus.SelectedIndex > 0)
        {
            var status = cboStatus.SelectedIndex switch
            {
                1 => BorrowStatus.Borrowing,
                2 => BorrowStatus.Returned,
                3 => BorrowStatus.Overdue,
                _ => (BorrowStatus?)null
            };

            if (status.HasValue)
            {
                filteredRecords = filteredRecords.Where(r => r.Status == status.Value);
            }
        }

        var result = filteredRecords.ToList();
        _bindingSource.DataSource = result;
        dgvRecords.DataSource = _bindingSource;
        lblStatus.Text = $"共 {result.Count} 条记录";
    }

    private void ClearFilters()
    {
        txtDieCode.Clear();
        txtBorrower.Clear();
        cboStatus.SelectedIndex = 0;
        dtpStartDate.Checked = false;
        dtpEndDate.Checked = false;
        LoadRecords();
    }

    private void ExportData()
    {
        try
        {
            if (_records.Count == 0)
            {
                MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel文件|*.xlsx|CSV文件|*.csv",
                FileName = $"借用记录_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                var importExportService = new ImportExportService();

                var dataTable = new global::System.Data.DataTable();
                dataTable.Columns.Add("刀模编号", typeof(string));
                dataTable.Columns.Add("客户名称", typeof(string));
                dataTable.Columns.Add("产品名称", typeof(string));
                dataTable.Columns.Add("借用类型", typeof(string));
                dataTable.Columns.Add("领用人", typeof(string));
                dataTable.Columns.Add("部门", typeof(string));
                dataTable.Columns.Add("借用时间", typeof(string));
                dataTable.Columns.Add("预计归还", typeof(string));
                dataTable.Columns.Add("实际归还", typeof(string));
                dataTable.Columns.Add("状态", typeof(string));
                dataTable.Columns.Add("用途", typeof(string));
                dataTable.Columns.Add("归还操作人", typeof(string));

                foreach (var record in _records)
                {
                    dataTable.Rows.Add(
                        record.DieCode,
                        record.CustomerName,
                        record.ProductName,
                        record.BorrowTypeText,
                        record.BorrowerName,
                        record.BorrowDept,
                        record.BorrowTime.ToString("yyyy-MM-dd HH:mm"),
                        record.ExpectedReturnTime?.ToString("yyyy-MM-dd HH:mm") ?? "",
                        record.ActualReturnTime?.ToString("yyyy-MM-dd HH:mm") ?? "",
                        record.StatusText,
                        record.Purpose,
                        record.ReturnOperatorName
                    );
                }

                if (saveDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    importExportService.ExportToCsv(dataTable, saveDialog.FileName);
                }
                else
                {
                    importExportService.ExportToExcel(dataTable, "借用记录", saveDialog.FileName);
                }

                MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private DataGridView dgvRecords = null!;
    private TextBox txtDieCode = null!;
    private TextBox txtBorrower = null!;
    private ComboBox cboStatus = null!;
    private DateTimePicker dtpStartDate = null!;
    private DateTimePicker dtpEndDate = null!;
    private ToolStripStatusLabel lblStatus = null!;
}
