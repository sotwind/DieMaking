using DieMaking.Services;
using DieMaking.Helpers;
using Microsoft.Data.SqlClient;

namespace DieMaking.Forms.Report;

/// <summary>
/// 交期预警报表窗体
/// </summary>
public partial class DeliveryWarningForm : Form
{
    private readonly ReportService _reportService;
    private readonly PrintService _printService;
    private DataGridView _dgvData = null!;
    private ComboBox _cmbWarningLevel = null!;
    private ComboBox _cmbCustomer = null!;
    private ComboBox _cmbPriority = null!;
    private DateTimePicker _dtpDeadlineFrom = null!;
    private DateTimePicker _dtpDeadlineTo = null!;
    private Label _lblSummary = null!;
    private CheckBox _chkShowCompleted = null!;

    public DeliveryWarningForm()
    {
        _reportService = new ReportService();
        _printService = new PrintService();
        InitializeComponent();
        this.Text = "交期预警报表";
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
            Height = 110,
            Padding = new Padding(10),
            BorderStyle = BorderStyle.FixedSingle
        };

        // 预警级别筛选
        var lblWarningLevel = new Label
        {
            Text = "预警级别：",
            Location = new Point(10, 15),
            Size = new Size(70, 25)
        };

        _cmbWarningLevel = new ComboBox
        {
            Location = new Point(85, 12),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbWarningLevel.Items.AddRange(new object[] { "全部", "正常", "即将到期", "已逾期" });
        _cmbWarningLevel.SelectedIndex = 0;

        // 客户筛选
        var lblCustomer = new Label
        {
            Text = "客户：",
            Location = new Point(195, 15),
            Size = new Size(50, 25)
        };

        _cmbCustomer = new ComboBox
        {
            Location = new Point(250, 12),
            Size = new Size(120, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        // 优先级筛选
        var lblPriority = new Label
        {
            Text = "优先级：",
            Location = new Point(380, 15),
            Size = new Size(60, 25)
        };

        _cmbPriority = new ComboBox
        {
            Location = new Point(445, 12),
            Size = new Size(80, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbPriority.Items.AddRange(new object[] { "全部", "高", "中", "低" });
        _cmbPriority.SelectedIndex = 0;

        // 交期范围
        var lblDeadlineFrom = new Label
        {
            Text = "交期从：",
            Location = new Point(535, 15),
            Size = new Size(60, 25)
        };

        _dtpDeadlineFrom = new DateTimePicker
        {
            Location = new Point(600, 12),
            Size = new Size(110, 25),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now.AddDays(-7)
        };

        var lblDeadlineTo = new Label
        {
            Text = "到：",
            Location = new Point(715, 15),
            Size = new Size(30, 25)
        };

        _dtpDeadlineTo = new DateTimePicker
        {
            Location = new Point(750, 12),
            Size = new Size(110, 25),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now.AddDays(30)
        };

        // 显示已完成订单
        _chkShowCompleted = new CheckBox
        {
            Text = "显示已完成订单",
            Location = new Point(870, 14),
            Size = new Size(120, 25),
            Checked = false
        };

        // 查询按钮
        var btnQuery = new Button
        {
            Text = "查询",
            Location = new Point(10, 50),
            Size = new Size(80, 28)
        };
        btnQuery.Click += BtnQuery_Click;

        // 导出按钮
        var btnExport = new Button
        {
            Text = "导出Excel",
            Location = new Point(100, 50),
            Size = new Size(90, 28)
        };
        btnExport.Click += BtnExport_Click;

        // 打印按钮
        var btnPrint = new Button
        {
            Text = "打印",
            Location = new Point(190, 50),
            Size = new Size(80, 28)
        };
        btnPrint.Click += BtnPrint_Click;

        // 图例说明
        var legendPanel = new Panel
        {
            Location = new Point(280, 45),
            Size = new Size(400, 35),
            BorderStyle = BorderStyle.None
        };

        var lblLegend = new Label
        {
            Text = "图例：",
            Location = new Point(0, 8),
            Size = new Size(40, 20)
        };

        var pnlGreen = new Panel
        {
            Location = new Point(45, 5),
            Size = new Size(20, 20),
            BackColor = Color.LightGreen,
            BorderStyle = BorderStyle.FixedSingle
        };
        var lblGreen = new Label
        {
            Text = "正常",
            Location = new Point(68, 8),
            Size = new Size(35, 20)
        };

        var pnlYellow = new Panel
        {
            Location = new Point(110, 5),
            Size = new Size(20, 20),
            BackColor = Color.LightYellow,
            BorderStyle = BorderStyle.FixedSingle
        };
        var lblYellow = new Label
        {
            Text = "即将到期",
            Location = new Point(133, 8),
            Size = new Size(60, 20)
        };

        var pnlRed = new Panel
        {
            Location = new Point(200, 5),
            Size = new Size(20, 20),
            BackColor = Color.LightCoral,
            BorderStyle = BorderStyle.FixedSingle
        };
        var lblRed = new Label
        {
            Text = "已逾期",
            Location = new Point(223, 8),
            Size = new Size(50, 20)
        };

        legendPanel.Controls.Add(lblLegend);
        legendPanel.Controls.Add(pnlGreen);
        legendPanel.Controls.Add(lblGreen);
        legendPanel.Controls.Add(pnlYellow);
        legendPanel.Controls.Add(lblYellow);
        legendPanel.Controls.Add(pnlRed);
        legendPanel.Controls.Add(lblRed);

        // 汇总信息标签
        _lblSummary = new Label
        {
            Text = "",
            Location = new Point(10, 85),
            Size = new Size(800, 25),
            Font = new Font("微软雅黑", 9, FontStyle.Bold),
            ForeColor = Color.Blue
        };

        toolPanel.Controls.Add(lblWarningLevel);
        toolPanel.Controls.Add(_cmbWarningLevel);
        toolPanel.Controls.Add(lblCustomer);
        toolPanel.Controls.Add(_cmbCustomer);
        toolPanel.Controls.Add(lblPriority);
        toolPanel.Controls.Add(_cmbPriority);
        toolPanel.Controls.Add(lblDeadlineFrom);
        toolPanel.Controls.Add(_dtpDeadlineFrom);
        toolPanel.Controls.Add(lblDeadlineTo);
        toolPanel.Controls.Add(_dtpDeadlineTo);
        toolPanel.Controls.Add(_chkShowCompleted);
        toolPanel.Controls.Add(btnQuery);
        toolPanel.Controls.Add(btnExport);
        toolPanel.Controls.Add(btnPrint);
        toolPanel.Controls.Add(legendPanel);
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
            BorderStyle = BorderStyle.Fixed3D
        };

        this.Controls.Add(_dgvData);
        this.Controls.Add(toolPanel);

        // 加载客户列表
        LoadCustomerList();

        // 初始加载数据
        LoadData();
    }

    private void LoadCustomerList()
    {
        _cmbCustomer.Items.Clear();
        _cmbCustomer.Items.Add("全部客户");

        try
        {
            var sql = "SELECT DISTINCT CustomerName FROM DM_DieInfo WHERE CustomerName IS NOT NULL AND CustomerName != '' ORDER BY CustomerName";
            var customers = DbHelper.ExecuteQuery(sql, reader => reader["CustomerName"].ToString() ?? "");

            foreach (var customer in customers)
            {
                if (!string.IsNullOrEmpty(customer))
                {
                    _cmbCustomer.Items.Add(customer);
                }
            }
        }
        catch { }

        _cmbCustomer.SelectedIndex = 0;
    }

    private void BtnQuery_Click(object? sender, EventArgs e)
    {
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var deadlineFrom = _dtpDeadlineFrom.Value.Date;
            var deadlineTo = _dtpDeadlineTo.Value.Date;
            var warningLevel = _cmbWarningLevel.SelectedIndex;
            var customerName = _cmbCustomer.SelectedIndex > 0 ? _cmbCustomer.SelectedItem?.ToString() : null;
            var priority = _cmbPriority.SelectedIndex;
            var showCompleted = _chkShowCompleted.Checked;

            var sql = @"
                SELECT 
                    d.DieID,
                    d.DieCode,
                    d.CustomerName,
                    d.ProductName,
                    d.Priority,
                    d.Deadline,
                    d.Status as DieStatus,
                    ISNULL(dc.CompleteTime, NULL) as ActualCompleteTime,
                    CASE 
                        WHEN dc.CompleteTime IS NOT NULL THEN '已完成'
                        WHEN d.Deadline < CAST(GETDATE() AS DATE) THEN '已逾期'
                        WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), d.Deadline) <= 3 THEN '即将到期'
                        ELSE '正常'
                    END as WarningLevel,
                    DATEDIFF(DAY, CAST(GETDATE() AS DATE), d.Deadline) as RemainingDays,
                    ISNULL((SELECT COUNT(*) FROM DM_DieProcess WHERE DieID = d.DieID), 0) as TotalProcesses,
                    ISNULL((SELECT COUNT(*) FROM DM_DieProcess WHERE DieID = d.DieID AND Status = 2), 0) as CompletedProcesses,
                    ISNULL((SELECT SUM(Amount) FROM DM_DieProcess WHERE DieID = d.DieID), 0) as TotalAmount
                FROM DM_DieInfo d
                LEFT JOIN DM_DieCompletion dc ON d.DieID = dc.DieID
                WHERE d.Deadline >= @DeadlineFrom AND d.Deadline <= @DeadlineTo";

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@DeadlineFrom", deadlineFrom),
                new SqlParameter("@DeadlineTo", deadlineTo.AddDays(1).AddSeconds(-1))
            };

            if (!showCompleted)
            {
                sql += " AND dc.CompleteTime IS NULL";
            }

            if (!string.IsNullOrEmpty(customerName))
            {
                sql += " AND d.CustomerName = @CustomerName";
                parameters.Add(new SqlParameter("@CustomerName", customerName));
            }

            if (priority > 0)
            {
                sql += " AND d.Priority = @Priority";
                parameters.Add(new SqlParameter("@Priority", priority - 1));
            }

            // 根据预警级别筛选
            if (warningLevel == 1) // 正常
            {
                sql += " AND (d.Deadline >= DATEADD(DAY, 4, CAST(GETDATE() AS DATE)) OR dc.CompleteTime IS NOT NULL)";
            }
            else if (warningLevel == 2) // 即将到期
            {
                sql += " AND d.Deadline >= CAST(GETDATE() AS DATE) AND d.Deadline < DATEADD(DAY, 4, CAST(GETDATE() AS DATE)) AND dc.CompleteTime IS NULL";
            }
            else if (warningLevel == 3) // 已逾期
            {
                sql += " AND d.Deadline < CAST(GETDATE() AS DATE) AND dc.CompleteTime IS NULL";
            }

            sql += " ORDER BY 
                CASE 
                    WHEN dc.CompleteTime IS NOT NULL THEN 3
                    WHEN d.Deadline < CAST(GETDATE() AS DATE) THEN 0
                    WHEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), d.Deadline) <= 3 THEN 1
                    ELSE 2
                END,
                d.Deadline ASC,
                d.Priority ASC";

            var data = DbHelper.ExecuteQuery(sql, reader => new DeliveryWarningItem
            {
                DieID = Convert.ToInt32(reader["DieID"]),
                DieCode = reader["DieCode"].ToString() ?? "",
                CustomerName = reader["CustomerName"].ToString() ?? "",
                ProductName = reader["ProductName"].ToString() ?? "",
                Priority = Convert.ToInt32(reader["Priority"]),
                Deadline = Convert.ToDateTime(reader["Deadline"]),
                DieStatus = Convert.ToInt32(reader["DieStatus"]),
                ActualCompleteTime = reader["ActualCompleteTime"] != DBNull.Value ? Convert.ToDateTime(reader["ActualCompleteTime"]) : null,
                WarningLevel = reader["WarningLevel"].ToString() ?? "正常",
                RemainingDays = Convert.ToInt32(reader["RemainingDays"]),
                TotalProcesses = Convert.ToInt32(reader["TotalProcesses"]),
                CompletedProcesses = Convert.ToInt32(reader["CompletedProcesses"]),
                TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0
            }, parameters.ToArray());

            _dgvData.Columns.Clear();
            _dgvData.Columns.Add("WarningLevel", "预警级别");
            _dgvData.Columns.Add("DieCode", "刀模编号");
            _dgvData.Columns.Add("CustomerName", "客户名称");
            _dgvData.Columns.Add("ProductName", "产品名称");
            _dgvData.Columns.Add("Priority", "优先级");
            _dgvData.Columns.Add("Deadline", "交期");
            _dgvData.Columns.Add("RemainingDays", "剩余天数");
            _dgvData.Columns.Add("Progress", "完成进度");
            _dgvData.Columns.Add("TotalAmount", "金额");
            _dgvData.Columns.Add("Status", "状态");

            _dgvData.Columns["WarningLevel"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvData.Columns["Priority"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvData.Columns["Deadline"].DefaultCellStyle.Format = "yyyy-MM-dd";
            _dgvData.Columns["RemainingDays"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvData.Columns["Progress"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _dgvData.Columns["TotalAmount"].DefaultCellStyle.Format = "N2";
            _dgvData.Columns["TotalAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _dgvData.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            _dgvData.Rows.Clear();
            int normalCount = 0;
            int warningCount = 0;
            int overdueCount = 0;
            int completedCount = 0;
            decimal totalAmount = 0;

            foreach (var item in data)
            {
                var progressRate = item.TotalProcesses > 0 ? (double)item.CompletedProcesses / item.TotalProcesses * 100 : 0;
                var progressText = $"{item.CompletedProcesses}/{item.TotalProcesses} ({progressRate:F0}%)";
                var priorityText = item.Priority switch
                {
                    0 => "高",
                    1 => "中",
                    2 => "低",
                    _ => "中"
                };

                string remainingDaysText;
                if (item.ActualCompleteTime.HasValue)
                {
                    remainingDaysText = "已完成";
                }
                else if (item.RemainingDays < 0)
                {
                    remainingDaysText = $"逾期 {Math.Abs(item.RemainingDays)} 天";
                }
                else if (item.RemainingDays == 0)
                {
                    remainingDaysText = "今天到期";
                }
                else
                {
                    remainingDaysText = $"剩余 {item.RemainingDays} 天";
                }

                var statusText = item.ActualCompleteTime.HasValue ? "已完成" : "生产中";

                var rowIndex = _dgvData.Rows.Add(
                    item.WarningLevel,
                    item.DieCode,
                    item.CustomerName,
                    item.ProductName,
                    priorityText,
                    item.Deadline,
                    remainingDaysText,
                    progressText,
                    item.TotalAmount,
                    statusText
                );

                // 根据预警级别设置行颜色
                if (item.ActualCompleteTime.HasValue)
                {
                    _dgvData.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                    completedCount++;
                }
                else if (item.WarningLevel == "已逾期")
                {
                    _dgvData.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightCoral;
                    _dgvData.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.DarkRed;
                    _dgvData.Rows[rowIndex].DefaultCellStyle.Font = new Font(_dgvData.Font, FontStyle.Bold);
                    overdueCount++;
                }
                else if (item.WarningLevel == "即将到期")
                {
                    _dgvData.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightYellow;
                    _dgvData.Rows[rowIndex].DefaultCellStyle.ForeColor = Color.DarkOrange;
                    warningCount++;
                }
                else
                {
                    normalCount++;
                }

                if (!item.ActualCompleteTime.HasValue)
                {
                    totalAmount += item.TotalAmount;
                }
            }

            _lblSummary.Text = $"总计：{data.Count} 条 | 正常：{normalCount} | 即将到期：{warningCount} | 已逾期：{overdueCount} | 已完成：{completedCount} | 未完成金额：{totalAmount:N2} 元";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                Filter = "Excel文件|*.xlsx|CSV文件|*.csv",
                Title = "导出数据",
                FileName = $"交期预警报表_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                var importExportService = new ImportExportService();

                if (saveDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    _printService.ExportToCsv(_dgvData, saveDialog.FileName);
                }
                else
                {
                    var dataTable = importExportService.ConvertDataGridViewToDataTable(_dgvData);
                    importExportService.ExportToExcel(dataTable, "交期预警报表", saveDialog.FileName);
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
                        Filter = "Excel文件|*.xlsx|CSV文件|*.csv",
                        Title = "导出数据",
                        FileName = $"交期预警报表_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
                    })
                    {
                        if (saveDialog.ShowDialog() == DialogResult.OK)
                        {
                            var importExportService = new ImportExportService();

                            if (saveDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                            {
                                _printService.ExportToCsv(_dgvData, saveDialog.FileName);
                            }
                            else
                            {
                                var dataTable = importExportService.ConvertDataGridViewToDataTable(_dgvData);
                                importExportService.ExportToExcel(dataTable, "交期预警报表", saveDialog.FileName);
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

/// <summary>
/// 交期预警数据模型
/// </summary>
public class DeliveryWarningItem
{
    public int DieID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime Deadline { get; set; }
    public int DieStatus { get; set; }
    public DateTime? ActualCompleteTime { get; set; }
    public string WarningLevel { get; set; } = string.Empty;
    public int RemainingDays { get; set; }
    public int TotalProcesses { get; set; }
    public int CompletedProcesses { get; set; }
    public decimal TotalAmount { get; set; }

    public string PriorityText => Priority switch
    {
        0 => "高",
        1 => "中",
        2 => "低",
        _ => "中"
    };

    public double CompletionRate => TotalProcesses > 0 ? (double)CompletedProcesses / TotalProcesses * 100 : 0;
}
