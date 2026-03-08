using DieMaking.Services;
using DieMaking.Helpers;
using Microsoft.Data.SqlClient;

namespace DieMaking.Forms.Report;

/// <summary>
/// 员工绩效报表窗体
/// </summary>
public partial class EmployeePerformanceForm : Form
{
    private readonly ReportService _reportService;
    private readonly PrintService _printService;
    private DataGridView _dgvData = null!;
    private DateTimePicker _dtpStartDate = null!;
    private DateTimePicker _dtpEndDate = null!;
    private ComboBox _cmbEmployee = null!;
    private ComboBox _cmbGroupBy = null!;
    private Label _lblSummary = null!;

    public EmployeePerformanceForm()
    {
        _reportService = new ReportService();
        _printService = new PrintService();
        InitializeComponent();
        this.Text = "员工绩效报表";
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

        // 员工筛选
        var lblEmployee = new Label
        {
            Text = "员工：",
            Location = new Point(420, 15),
            Size = new Size(50, 25)
        };

        _cmbEmployee = new ComboBox
        {
            Location = new Point(475, 12),
            Size = new Size(120, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        // 分组方式
        var lblGroupBy = new Label
        {
            Text = "统计维度：",
            Location = new Point(605, 15),
            Size = new Size(70, 25)
        };

        _cmbGroupBy = new ComboBox
        {
            Location = new Point(680, 12),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbGroupBy.Items.AddRange(new object[] { "按员工汇总", "按日期汇总", "按员工日期明细" });
        _cmbGroupBy.SelectedIndex = 0;
        _cmbGroupBy.SelectedIndexChanged += (s, e) => LoadData();

        // 查询按钮
        var btnQuery = new Button
        {
            Text = "查询",
            Location = new Point(10, 45),
            Size = new Size(80, 28)
        };
        btnQuery.Click += BtnQuery_Click;

        // 导出按钮
        var btnExport = new Button
        {
            Text = "导出Excel",
            Location = new Point(100, 45),
            Size = new Size(90, 28)
        };
        btnExport.Click += BtnExport_Click;

        // 打印按钮
        var btnPrint = new Button
        {
            Text = "打印",
            Location = new Point(190, 45),
            Size = new Size(80, 28)
        };
        btnPrint.Click += BtnPrint_Click;

        // 汇总信息标签
        _lblSummary = new Label
        {
            Text = "",
            Location = new Point(280, 50),
            Size = new Size(600, 25),
            Font = new Font("微软雅黑", 9, FontStyle.Bold),
            ForeColor = Color.Blue
        };

        toolPanel.Controls.Add(lblStartDate);
        toolPanel.Controls.Add(_dtpStartDate);
        toolPanel.Controls.Add(lblEndDate);
        toolPanel.Controls.Add(_dtpEndDate);
        toolPanel.Controls.Add(lblEmployee);
        toolPanel.Controls.Add(_cmbEmployee);
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
            BorderStyle = BorderStyle.Fixed3D
        };

        this.Controls.Add(_dgvData);
        this.Controls.Add(toolPanel);

        // 加载员工列表
        LoadEmployeeList();

        // 初始加载数据
        LoadData();
    }

    private void LoadEmployeeList()
    {
        _cmbEmployee.Items.Clear();
        _cmbEmployee.Items.Add("全部员工");

        try
        {
            var sql = "SELECT DISTINCT OperatorName FROM DM_DieProcess WHERE OperatorName IS NOT NULL AND OperatorName != '' ORDER BY OperatorName";
            var employees = DbHelper.ExecuteQuery(sql, reader => reader["OperatorName"].ToString() ?? "");

            foreach (var emp in employees)
            {
                if (!string.IsNullOrEmpty(emp))
                {
                    _cmbEmployee.Items.Add(emp);
                }
            }
        }
        catch
        {
            // 如果查询失败，使用完工记录中的操作人
            try
            {
                var sql = "SELECT DISTINCT OperatorName FROM DM_DieCompletion WHERE OperatorName IS NOT NULL AND OperatorName != '' ORDER BY OperatorName";
                var employees = DbHelper.ExecuteQuery(sql, reader => reader["OperatorName"].ToString() ?? "");

                foreach (var emp in employees)
                {
                    if (!string.IsNullOrEmpty(emp))
                    {
                        _cmbEmployee.Items.Add(emp);
                    }
                }
            }
            catch { }
        }

        _cmbEmployee.SelectedIndex = 0;
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
            var employeeName = _cmbEmployee.SelectedIndex > 0 ? _cmbEmployee.SelectedItem?.ToString() : null;

            switch (_cmbGroupBy.SelectedIndex)
            {
                case 0: // 按员工汇总
                    LoadDataByEmployee(startDate, endDate, employeeName);
                    break;
                case 1: // 按日期汇总
                    LoadDataByDate(startDate, endDate, employeeName);
                    break;
                case 2: // 按员工日期明细
                    LoadDataByEmployeeDateDetail(startDate, endDate, employeeName);
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadDataByEmployee(DateTime startDate, DateTime endDate, string? employeeName)
    {
        var sql = @"
            SELECT 
                ISNULL(dp.OperatorName, '未分配') as EmployeeName,
                COUNT(DISTINCT dp.DieID) as CompletedDieCount,
                COUNT(*) as CompletedProcessCount,
                SUM(CASE WHEN dp.CompleteTime IS NOT NULL AND dp.StartTime IS NOT NULL 
                    THEN DATEDIFF(MINUTE, dp.StartTime, dp.CompleteTime) ELSE 0 END) as TotalWorkMinutes,
                SUM(dp.Amount) as TotalAmount,
                AVG(CASE WHEN dp.CompleteTime IS NOT NULL AND dp.StartTime IS NOT NULL 
                    THEN DATEDIFF(MINUTE, dp.StartTime, dp.CompleteTime) ELSE NULL END) as AvgProcessMinutes
            FROM DM_DieProcess dp
            WHERE dp.Status = 2
                AND dp.CompleteTime >= @StartDate
                AND dp.CompleteTime <= @EndDate";

        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@StartDate", startDate),
            new SqlParameter("@EndDate", endDate.AddDays(1).AddSeconds(-1))
        };

        if (!string.IsNullOrEmpty(employeeName))
        {
            sql += " AND dp.OperatorName = @EmployeeName";
            parameters.Add(new SqlParameter("@EmployeeName", employeeName));
        }

        sql += @"
            GROUP BY dp.OperatorName
            ORDER BY CompletedProcessCount DESC";

        var data = DbHelper.ExecuteQuery(sql, reader => new EmployeePerformance
        {
            EmployeeName = reader["EmployeeName"].ToString() ?? "未分配",
            CompletedDieCount = Convert.ToInt32(reader["CompletedDieCount"]),
            CompletedProcessCount = Convert.ToInt32(reader["CompletedProcessCount"]),
            TotalWorkMinutes = reader["TotalWorkMinutes"] != DBNull.Value ? Convert.ToInt32(reader["TotalWorkMinutes"]) : 0,
            TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0,
            AvgProcessMinutes = reader["AvgProcessMinutes"] != DBNull.Value ? Convert.ToDouble(reader["AvgProcessMinutes"]) : 0
        }, parameters.ToArray());

        _dgvData.Columns.Clear();
        _dgvData.Columns.Add("Rank", "排名");
        _dgvData.Columns.Add("EmployeeName", "员工姓名");
        _dgvData.Columns.Add("CompletedDieCount", "完工刀模数");
        _dgvData.Columns.Add("CompletedProcessCount", "完成工序数");
        _dgvData.Columns.Add("TotalWorkHours", "工作时长");
        _dgvData.Columns.Add("AvgProcessTime", "平均工序耗时");
        _dgvData.Columns.Add("TotalAmount", "金额合计");
        _dgvData.Columns.Add("PerformanceScore", "绩效评分");

        _dgvData.Columns["Rank"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["Rank"].Width = 60;
        _dgvData.Columns["CompletedDieCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["CompletedProcessCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["TotalWorkHours"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["AvgProcessTime"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Format = "N2";
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _dgvData.Columns["PerformanceScore"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

        _dgvData.Rows.Clear();
        int totalDieCount = 0;
        int totalProcessCount = 0;
        decimal totalAmount = 0;
        int rank = 1;

        foreach (var item in data)
        {
            var workHours = item.TotalWorkMinutes / 60.0;
            var avgMinutes = item.AvgProcessMinutes;
            var performanceScore = CalculatePerformanceScore(item.CompletedProcessCount, workHours, totalAmount);

            var rowIndex = _dgvData.Rows.Add(
                rank,
                item.EmployeeName,
                item.CompletedDieCount,
                item.CompletedProcessCount,
                $"{workHours:F1}小时",
                avgMinutes > 0 ? $"{avgMinutes:F0}分钟" : "-",
                item.TotalAmount,
                $"{performanceScore:F0}"
            );

            // 前三名高亮显示
            if (rank <= 3)
            {
                _dgvData.Rows[rowIndex].DefaultCellStyle.BackColor = rank switch
                {
                    1 => Color.Gold,
                    2 => Color.Silver,
                    3 => Color.FromArgb(205, 127, 50), // 铜色
                    _ => Color.White
                };
                _dgvData.Rows[rowIndex].DefaultCellStyle.Font = new Font(_dgvData.Font, FontStyle.Bold);
            }

            totalDieCount += item.CompletedDieCount;
            totalProcessCount += item.CompletedProcessCount;
            totalAmount += item.TotalAmount;
            rank++;
        }

        // 添加汇总行
        if (data.Count > 0)
        {
            int summaryRow = _dgvData.Rows.Add(
                "-",
                "【汇总】",
                totalDieCount,
                totalProcessCount,
                "-",
                "-",
                totalAmount,
                "-"
            );
            _dgvData.Rows[summaryRow].DefaultCellStyle.Font = new Font(_dgvData.Font, FontStyle.Bold);
            _dgvData.Rows[summaryRow].DefaultCellStyle.BackColor = Color.LightYellow;
        }

        _lblSummary.Text = $"共 {data.Count} 名员工，完工刀模总数：{totalDieCount}，完成工序总数：{totalProcessCount}，总金额：{totalAmount:N2} 元";
    }

    private void LoadDataByDate(DateTime startDate, DateTime endDate, string? employeeName)
    {
        var sql = @"
            SELECT 
                CAST(dp.CompleteTime AS DATE) as WorkDate,
                COUNT(DISTINCT dp.DieID) as CompletedDieCount,
                COUNT(*) as CompletedProcessCount,
                SUM(CASE WHEN dp.CompleteTime IS NOT NULL AND dp.StartTime IS NOT NULL 
                    THEN DATEDIFF(MINUTE, dp.StartTime, dp.CompleteTime) ELSE 0 END) as TotalWorkMinutes,
                SUM(dp.Amount) as TotalAmount
            FROM DM_DieProcess dp
            WHERE dp.Status = 2
                AND dp.CompleteTime >= @StartDate
                AND dp.CompleteTime <= @EndDate";

        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@StartDate", startDate),
            new SqlParameter("@EndDate", endDate.AddDays(1).AddSeconds(-1))
        };

        if (!string.IsNullOrEmpty(employeeName))
        {
            sql += " AND dp.OperatorName = @EmployeeName";
            parameters.Add(new SqlParameter("@EmployeeName", employeeName));
        }

        sql += @"
            GROUP BY CAST(dp.CompleteTime AS DATE)
            ORDER BY WorkDate DESC";

        var data = DbHelper.ExecuteQuery(sql, reader => new EmployeePerformanceByDate
        {
            WorkDate = Convert.ToDateTime(reader["WorkDate"]),
            CompletedDieCount = Convert.ToInt32(reader["CompletedDieCount"]),
            CompletedProcessCount = Convert.ToInt32(reader["CompletedProcessCount"]),
            TotalWorkMinutes = reader["TotalWorkMinutes"] != DBNull.Value ? Convert.ToInt32(reader["TotalWorkMinutes"]) : 0,
            TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0
        }, parameters.ToArray());

        _dgvData.Columns.Clear();
        _dgvData.Columns.Add("WorkDate", "工作日期");
        _dgvData.Columns.Add("CompletedDieCount", "完工刀模数");
        _dgvData.Columns.Add("CompletedProcessCount", "完成工序数");
        _dgvData.Columns.Add("TotalWorkHours", "工作时长");
        _dgvData.Columns.Add("TotalAmount", "金额合计");
        _dgvData.Columns.Add("AvgAmountPerDie", "单均金额");

        _dgvData.Columns["WorkDate"].DefaultCellStyle.Format = "yyyy-MM-dd";
        _dgvData.Columns["CompletedDieCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["CompletedProcessCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["TotalWorkHours"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Format = "N2";
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _dgvData.Columns["AvgAmountPerDie"].DefaultCellStyle.Format = "N2";
        _dgvData.Columns["AvgAmountPerDie"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        _dgvData.Rows.Clear();
        int totalDieCount = 0;
        int totalProcessCount = 0;
        decimal totalAmount = 0;

        foreach (var item in data)
        {
            var workHours = item.TotalWorkMinutes / 60.0;
            var avgAmount = item.CompletedDieCount > 0 ? item.TotalAmount / item.CompletedDieCount : 0;

            _dgvData.Rows.Add(
                item.WorkDate,
                item.CompletedDieCount,
                item.CompletedProcessCount,
                $"{workHours:F1}小时",
                item.TotalAmount,
                avgAmount
            );

            totalDieCount += item.CompletedDieCount;
            totalProcessCount += item.CompletedProcessCount;
            totalAmount += item.TotalAmount;
        }

        // 添加汇总行
        if (data.Count > 0)
        {
            var totalAvgAmount = totalDieCount > 0 ? totalAmount / totalDieCount : 0;
            int summaryRow = _dgvData.Rows.Add(
                "【汇总】",
                totalDieCount,
                totalProcessCount,
                "-",
                totalAmount,
                totalAvgAmount
            );
            _dgvData.Rows[summaryRow].DefaultCellStyle.Font = new Font(_dgvData.Font, FontStyle.Bold);
            _dgvData.Rows[summaryRow].DefaultCellStyle.BackColor = Color.LightYellow;
        }

        _lblSummary.Text = $"共 {data.Count} 天，完工刀模总数：{totalDieCount}，完成工序总数：{totalProcessCount}，总金额：{totalAmount:N2} 元";
    }

    private void LoadDataByEmployeeDateDetail(DateTime startDate, DateTime endDate, string? employeeName)
    {
        var sql = @"
            SELECT 
                ISNULL(dp.OperatorName, '未分配') as EmployeeName,
                CAST(dp.CompleteTime AS DATE) as WorkDate,
                COUNT(DISTINCT dp.DieID) as CompletedDieCount,
                COUNT(*) as CompletedProcessCount,
                SUM(CASE WHEN dp.CompleteTime IS NOT NULL AND dp.StartTime IS NOT NULL 
                    THEN DATEDIFF(MINUTE, dp.StartTime, dp.CompleteTime) ELSE 0 END) as TotalWorkMinutes,
                SUM(dp.Amount) as TotalAmount
            FROM DM_DieProcess dp
            WHERE dp.Status = 2
                AND dp.CompleteTime >= @StartDate
                AND dp.CompleteTime <= @EndDate";

        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@StartDate", startDate),
            new SqlParameter("@EndDate", endDate.AddDays(1).AddSeconds(-1))
        };

        if (!string.IsNullOrEmpty(employeeName))
        {
            sql += " AND dp.OperatorName = @EmployeeName";
            parameters.Add(new SqlParameter("@EmployeeName", employeeName));
        }

        sql += @"
            GROUP BY dp.OperatorName, CAST(dp.CompleteTime AS DATE)
            ORDER BY EmployeeName, WorkDate DESC";

        var data = DbHelper.ExecuteQuery(sql, reader => new EmployeePerformanceDetail
        {
            EmployeeName = reader["EmployeeName"].ToString() ?? "未分配",
            WorkDate = Convert.ToDateTime(reader["WorkDate"]),
            CompletedDieCount = Convert.ToInt32(reader["CompletedDieCount"]),
            CompletedProcessCount = Convert.ToInt32(reader["CompletedProcessCount"]),
            TotalWorkMinutes = reader["TotalWorkMinutes"] != DBNull.Value ? Convert.ToInt32(reader["TotalWorkMinutes"]) : 0,
            TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0
        }, parameters.ToArray());

        _dgvData.Columns.Clear();
        _dgvData.Columns.Add("EmployeeName", "员工姓名");
        _dgvData.Columns.Add("WorkDate", "工作日期");
        _dgvData.Columns.Add("CompletedDieCount", "完工刀模数");
        _dgvData.Columns.Add("CompletedProcessCount", "完成工序数");
        _dgvData.Columns.Add("TotalWorkHours", "工作时长");
        _dgvData.Columns.Add("TotalAmount", "金额合计");

        _dgvData.Columns["WorkDate"].DefaultCellStyle.Format = "yyyy-MM-dd";
        _dgvData.Columns["CompletedDieCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["CompletedProcessCount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["TotalWorkHours"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Format = "N2";
        _dgvData.Columns["TotalAmount"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        _dgvData.Rows.Clear();
        string currentEmployee = "";
        int empDieCount = 0;
        int empProcessCount = 0;
        decimal empAmount = 0;
        int totalDieCount = 0;
        int totalProcessCount = 0;
        decimal totalAmount = 0;

        foreach (var item in data)
        {
            // 新员工开始时，添加小计行
            if (currentEmployee != "" && currentEmployee != item.EmployeeName)
            {
                int subtotalRow = _dgvData.Rows.Add(
                    $"【{currentEmployee} 小计】",
                    "",
                    empDieCount,
                    empProcessCount,
                    "",
                    empAmount
                );
                _dgvData.Rows[subtotalRow].DefaultCellStyle.Font = new Font(_dgvData.Font, FontStyle.Bold);
                _dgvData.Rows[subtotalRow].DefaultCellStyle.BackColor = Color.LightCyan;

                empDieCount = 0;
                empProcessCount = 0;
                empAmount = 0;
            }

            var workHours = item.TotalWorkMinutes / 60.0;

            _dgvData.Rows.Add(
                item.EmployeeName,
                item.WorkDate,
                item.CompletedDieCount,
                item.CompletedProcessCount,
                $"{workHours:F1}小时",
                item.TotalAmount
            );

            empDieCount += item.CompletedDieCount;
            empProcessCount += item.CompletedProcessCount;
            empAmount += item.TotalAmount;
            totalDieCount += item.CompletedDieCount;
            totalProcessCount += item.CompletedProcessCount;
            totalAmount += item.TotalAmount;

            currentEmployee = item.EmployeeName;
        }

        // 添加最后一名员工的小计行
        if (currentEmployee != "")
        {
            int subtotalRow = _dgvData.Rows.Add(
                $"【{currentEmployee} 小计】",
                "",
                empDieCount,
                empProcessCount,
                "",
                empAmount
            );
            _dgvData.Rows[subtotalRow].DefaultCellStyle.Font = new Font(_dgvData.Font, FontStyle.Bold);
            _dgvData.Rows[subtotalRow].DefaultCellStyle.BackColor = Color.LightCyan;
        }

        // 添加总计行
        if (totalDieCount > 0)
        {
            int grandRow = _dgvData.Rows.Add(
                "【总计】",
                "",
                totalDieCount,
                totalProcessCount,
                "",
                totalAmount
            );
            _dgvData.Rows[grandRow].DefaultCellStyle.Font = new Font(_dgvData.Font, FontStyle.Bold);
            _dgvData.Rows[grandRow].DefaultCellStyle.BackColor = Color.LightYellow;
        }

        _lblSummary.Text = $"共 {data.Count} 条记录，完工刀模总数：{totalDieCount}，完成工序总数：{totalProcessCount}，总金额：{totalAmount:N2} 元";
    }

    private double CalculatePerformanceScore(int processCount, double workHours, decimal totalAmount)
    {
        // 简单的绩效评分算法：工序数量 * 0.5 + 工作时长 * 2 + 金额 / 100
        var score = processCount * 0.5 + workHours * 2 + (double)(totalAmount / 100);
        return Math.Min(score, 100); // 最高100分
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
                FileName = $"员工绩效报表_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
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
                    importExportService.ExportToExcel(dataTable, "员工绩效报表", saveDialog.FileName);
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
                case DialogResult.No: // 导出
                    using (var saveDialog = new SaveFileDialog
                    {
                        Filter = "Excel文件|*.xlsx|CSV文件|*.csv",
                        Title = "导出数据",
                        FileName = $"员工绩效报表_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
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
                                importExportService.ExportToExcel(dataTable, "员工绩效报表", saveDialog.FileName);
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
/// 员工绩效数据模型
/// </summary>
public class EmployeePerformance
{
    public string EmployeeName { get; set; } = string.Empty;
    public int CompletedDieCount { get; set; }
    public int CompletedProcessCount { get; set; }
    public int TotalWorkMinutes { get; set; }
    public decimal TotalAmount { get; set; }
    public double AvgProcessMinutes { get; set; }
}

public class EmployeePerformanceByDate
{
    public DateTime WorkDate { get; set; }
    public int CompletedDieCount { get; set; }
    public int CompletedProcessCount { get; set; }
    public int TotalWorkMinutes { get; set; }
    public decimal TotalAmount { get; set; }
}

public class EmployeePerformanceDetail
{
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public int CompletedDieCount { get; set; }
    public int CompletedProcessCount { get; set; }
    public int TotalWorkMinutes { get; set; }
    public decimal TotalAmount { get; set; }
}
