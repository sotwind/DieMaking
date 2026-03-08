using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Production;

public partial class CompletionQueryForm : Form
{
    private readonly ProductionService _productionService;
    private DataGridView _dgvCompletions = null!;
    private DateTimePicker _dtpStartDate = null!;
    private DateTimePicker _dtpEndDate = null!;
    private TextBox _txtDieCode = null!;
    private TextBox _txtProcessName = null!;
    private Label _lblCount = null!;

    public CompletionQueryForm()
    {
        _productionService = new ProductionService();
        InitializeComponent();
        this.Text = "完工查询";
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1100, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.WindowState = FormWindowState.Maximized;

        // 顶部筛选区域
        var panelTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            Padding = new Padding(10)
        };

        var lblStartDate = new Label
        {
            Text = "完工开始日期：",
            Location = new Point(10, 25),
            AutoSize = true
        };

        _dtpStartDate = new DateTimePicker
        {
            Location = new Point(100, 21),
            Width = 120,
            Format = DateTimePickerFormat.Short
        };

        var lblEndDate = new Label
        {
            Text = "完工结束日期：",
            Location = new Point(230, 25),
            AutoSize = true
        };

        _dtpEndDate = new DateTimePicker
        {
            Location = new Point(320, 21),
            Width = 120,
            Format = DateTimePickerFormat.Short
        };

        var lblDieCode = new Label
        {
            Text = "刀模编号：",
            Location = new Point(450, 25),
            AutoSize = true
        };

        _txtDieCode = new TextBox
        {
            Location = new Point(515, 21),
            Width = 120
        };

        var lblProcessName = new Label
        {
            Text = "工序：",
            Location = new Point(645, 25),
            AutoSize = true
        };

        _txtProcessName = new TextBox
        {
            Location = new Point(690, 21),
            Width = 100
        };

        var btnSearch = new Button
        {
            Text = "查询",
            Location = new Point(800, 20),
            Size = new Size(80, 28)
        };
        btnSearch.Click += BtnSearch_Click;

        var btnReset = new Button
        {
            Text = "重置",
            Location = new Point(890, 20),
            Size = new Size(80, 28)
        };
        btnReset.Click += BtnReset_Click;

        var btnExport = new Button
        {
            Text = "导出",
            Location = new Point(980, 20),
            Size = new Size(80, 28)
        };
        btnExport.Click += BtnExport_Click;

        _lblCount = new Label
        {
            Location = new Point(1070, 25),
            AutoSize = true,
            Font = new Font("微软雅黑", 9, FontStyle.Bold),
            ForeColor = Color.DarkBlue
        };

        panelTop.Controls.AddRange(new Control[]
        {
            lblStartDate, _dtpStartDate, lblEndDate, _dtpEndDate,
            lblDieCode, _txtDieCode, lblProcessName, _txtProcessName,
            btnSearch, btnReset, btnExport, _lblCount
        });

        // 数据表格区域
        _dgvCompletions = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            RowHeadersVisible = false,
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.LightGray
            }
        };

        _dgvCompletions.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CompletionID",
            HeaderText = "ID",
            Width = 50,
            Visible = false
        });

        _dgvCompletions.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "DieCode",
            HeaderText = "刀模编号",
            Width = 150
        });

        _dgvCompletions.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CustomerName",
            HeaderText = "客户名称",
            Width = 150
        });

        _dgvCompletions.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ProductName",
            HeaderText = "产品名称",
            Width = 150
        });

        _dgvCompletions.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CompleteTime",
            HeaderText = "完工时间",
            Width = 140
        });

        _dgvCompletions.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TotalAmount",
            HeaderText = "总金额",
            Width = 100,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight,
                Format = "N2"
            }
        });

        _dgvCompletions.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "OperatorName",
            HeaderText = "操作员",
            Width = 100
        });

        _dgvCompletions.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Remark",
            HeaderText = "备注",
            Width = 200
        });

        this.Controls.Add(_dgvCompletions);
        this.Controls.Add(panelTop);

        // 设置默认值
        _dtpStartDate.Value = DateTime.Now.AddMonths(-1);
        _dtpEndDate.Value = DateTime.Now;
    }

    private void LoadData()
    {
        try
        {
            var records = _productionService.QueryCompletions(
                _dtpStartDate.Value.Date,
                _dtpEndDate.Value.Date.AddDays(1).AddSeconds(-1),
                string.IsNullOrEmpty(_txtDieCode.Text) ? null : _txtDieCode.Text.Trim(),
                string.IsNullOrEmpty(_txtProcessName.Text) ? null : _txtProcessName.Text.Trim()
            );

            _dgvCompletions.Rows.Clear();

            foreach (var record in records)
            {
                var rowIndex = _dgvCompletions.Rows.Add();
                var row = _dgvCompletions.Rows[rowIndex];

                row.Cells["CompletionID"].Value = record.CompletionID;
                row.Cells["DieCode"].Value = record.DieCode;
                row.Cells["CustomerName"].Value = record.CustomerName;
                row.Cells["ProductName"].Value = record.ProductName;
                row.Cells["CompleteTime"].Value = record.CompleteTime.ToString("yyyy-MM-dd HH:mm");
                row.Cells["TotalAmount"].Value = record.TotalAmount;
                row.Cells["OperatorName"].Value = record.OperatorName;
                row.Cells["Remark"].Value = record.Remark;

                row.Tag = record;
            }

            _lblCount.Text = $"共 {records.Count} 条记录";
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
        _txtDieCode.Clear();
        _txtProcessName.Clear();
        LoadData();
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        if (_dgvCompletions.Rows.Count == 0)
        {
            MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "CSV文件|*.csv",
                FileName = $"完工查询_{DateTime.Now:yyyyMMddHHmmss}.csv"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                using var writer = new StreamWriter(saveDialog.FileName, false, System.Text.Encoding.UTF8);

                // 写入表头
                var headers = new[] { "刀模编号", "客户名称", "产品名称", "完工时间", "总金额", "操作员", "备注" };
                writer.WriteLine(string.Join(",", headers));

                // 写入数据
                foreach (DataGridViewRow row in _dgvCompletions.Rows)
                {
                    var values = new[]
                    {
                        row.Cells["DieCode"].Value?.ToString() ?? "",
                        row.Cells["CustomerName"].Value?.ToString() ?? "",
                        row.Cells["ProductName"].Value?.ToString() ?? "",
                        row.Cells["CompleteTime"].Value?.ToString() ?? "",
                        row.Cells["TotalAmount"].Value?.ToString() ?? "",
                        row.Cells["OperatorName"].Value?.ToString() ?? "",
                        row.Cells["Remark"].Value?.ToString() ?? ""
                    };
                    writer.WriteLine(string.Join(",", values.Select(v => $"\"{v}\"")));
                }

                MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
