using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Forms.System;

public partial class OperationLogForm : Form
{
    private BindingSource _bindingSource = new();
    private List<OperationLogViewModel> _logs = new();

    public OperationLogForm()
    {
        InitializeComponent();
        this.Text = "操作日志";
        LoadLogs();
        LoadUsers();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1100, 650);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // 标题标签
        var lblTitle = new Label
        {
            Text = "操作日志",
            Font = new Font("微软雅黑", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        // 筛选区域
        int filterY = 55;

        // 开始日期
        var lblStartDate = new Label
        {
            Text = "开始日期：",
            Location = new Point(20, filterY),
            Size = new Size(70, 25)
        };

        dtpStartDate = new DateTimePicker
        {
            Location = new Point(95, filterY - 2),
            Size = new Size(120, 25),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now.AddDays(-7) // 默认显示最近7天
        };

        // 结束日期
        var lblEndDate = new Label
        {
            Text = "结束日期：",
            Location = new Point(225, filterY),
            Size = new Size(70, 25)
        };

        dtpEndDate = new DateTimePicker
        {
            Location = new Point(300, filterY - 2),
            Size = new Size(120, 25),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now
        };

        // 用户筛选
        var lblUser = new Label
        {
            Text = "用户：",
            Location = new Point(430, filterY),
            Size = new Size(45, 25)
        };

        cmbUser = new ComboBox
        {
            Location = new Point(480, filterY - 2),
            Size = new Size(120, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        // 操作类型筛选
        var lblOperationType = new Label
        {
            Text = "操作类型：",
            Location = new Point(610, filterY),
            Size = new Size(70, 25)
        };

        cmbOperationType = new ComboBox
        {
            Location = new Point(685, filterY - 2),
            Size = new Size(120, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbOperationType.Items.Add("全部");
        cmbOperationType.Items.Add("登录");
        cmbOperationType.Items.Add("登出");
        cmbOperationType.Items.Add("新增");
        cmbOperationType.Items.Add("修改");
        cmbOperationType.Items.Add("删除");
        cmbOperationType.Items.Add("重置密码");
        cmbOperationType.Items.Add("启用用户");
        cmbOperationType.Items.Add("禁用用户");
        cmbOperationType.Items.Add("入库");
        cmbOperationType.Items.Add("出库");
        cmbOperationType.Items.Add("借用");
        cmbOperationType.Items.Add("归还");
        cmbOperationType.Items.Add("报废申请");
        cmbOperationType.Items.Add("报废审核");
        cmbOperationType.SelectedIndex = 0;

        // 查询按钮
        btnSearch = new Button
        {
            Text = "查询",
            Location = new Point(820, filterY - 4),
            Size = new Size(80, 30)
        };
        btnSearch.Click += BtnSearch_Click;

        // 重置按钮
        btnReset = new Button
        {
            Text = "重置",
            Location = new Point(910, filterY - 4),
            Size = new Size(80, 30)
        };
        btnReset.Click += BtnReset_Click;

        // 导出按钮
        btnExport = new Button
        {
            Text = "导出",
            Location = new Point(1000, filterY - 4),
            Size = new Size(80, 30)
        };
        btnExport.Click += BtnExport_Click;

        // 数据表格
        dgvLogs = new DataGridView
        {
            Location = new Point(20, 95),
            Size = new Size(1040, 480),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        // 添加列
        dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "LogID",
            HeaderText = "日志ID",
            DataPropertyName = "LogID",
            Width = 70
        });
        dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CreateTime",
            HeaderText = "操作时间",
            DataPropertyName = "CreateTime",
            Width = 140
        });
        dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Username",
            HeaderText = "操作用户",
            DataPropertyName = "Username",
            Width = 100
        });
        dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "OperationType",
            HeaderText = "操作类型",
            DataPropertyName = "OperationType",
            Width = 100
        });
        dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "OperationDesc",
            HeaderText = "操作内容",
            DataPropertyName = "OperationDesc",
            Width = 350
        });
        dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "DieCode",
            HeaderText = "刀模编号",
            DataPropertyName = "DieCode",
            Width = 120
        });
        dgvLogs.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "IPAddress",
            HeaderText = "IP地址",
            DataPropertyName = "IPAddress",
            Width = 120
        });

        // 统计信息
        lblStats = new Label
        {
            Text = "",
            Location = new Point(20, 585),
            Size = new Size(500, 25),
            ForeColor = Color.Gray
        };

        // 添加控件
        this.Controls.Add(lblTitle);
        this.Controls.Add(lblStartDate);
        this.Controls.Add(dtpStartDate);
        this.Controls.Add(lblEndDate);
        this.Controls.Add(dtpEndDate);
        this.Controls.Add(lblUser);
        this.Controls.Add(cmbUser);
        this.Controls.Add(lblOperationType);
        this.Controls.Add(cmbOperationType);
        this.Controls.Add(btnSearch);
        this.Controls.Add(btnReset);
        this.Controls.Add(btnExport);
        this.Controls.Add(dgvLogs);
        this.Controls.Add(lblStats);
    }

    private void LoadUsers()
    {
        try
        {
            cmbUser.Items.Clear();
            cmbUser.Items.Add("全部用户");
            
            var sql = "SELECT DISTINCT Username FROM DM_OperationLog WHERE Username IS NOT NULL AND Username != '' ORDER BY Username";
            var usernames = DbHelper.ExecuteQuery(sql, reader => reader["Username"].ToString() ?? "");
            
            foreach (var username in usernames)
            {
                cmbUser.Items.Add(username);
            }
            
            cmbUser.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载用户列表失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadLogs()
    {
        try
        {
            var sql = @"SELECT 
                            l.LogID,
                            l.UserID,
                            l.Username,
                            l.OperationType,
                            l.OperationDesc,
                            l.DieID,
                            d.DieCode,
                            l.IPAddress,
                            l.CreateTime
                        FROM DM_OperationLog l
                        LEFT JOIN DM_Die d ON l.DieID = d.DieID
                        WHERE l.CreateTime BETWEEN @StartDate AND @EndDate
                        ORDER BY l.CreateTime DESC";

            var parameters = new[]
            {
                new SqlParameter("@StartDate", dtpStartDate.Value.Date),
                new SqlParameter("@EndDate", dtpEndDate.Value.Date.AddDays(1).AddSeconds(-1))
            };

            _logs = DbHelper.ExecuteQuery(sql, MapToLogViewModel, parameters);
            FilterLogs();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载日志数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private OperationLogViewModel MapToLogViewModel(SqlDataReader reader)
    {
        return new OperationLogViewModel
        {
            LogID = Convert.ToInt32(reader["LogID"]),
            UserID = reader["UserID"] != DBNull.Value ? Convert.ToInt32(reader["UserID"]) : null,
            Username = reader["Username"].ToString() ?? "",
            OperationType = reader["OperationType"].ToString() ?? "",
            OperationDesc = reader["OperationDesc"].ToString() ?? "",
            DieID = reader["DieID"] != DBNull.Value ? Convert.ToInt32(reader["DieID"]) : null,
            DieCode = reader["DieCode"].ToString() ?? "",
            IPAddress = reader["IPAddress"].ToString() ?? "",
            CreateTime = Convert.ToDateTime(reader["CreateTime"])
        };
    }

    private void FilterLogs()
    {
        var filteredLogs = _logs.AsEnumerable();

        // 用户筛选
        if (cmbUser.SelectedIndex > 0)
        {
            var selectedUser = cmbUser.SelectedItem?.ToString();
            filteredLogs = filteredLogs.Where(l => l.Username == selectedUser);
        }

        // 操作类型筛选
        if (cmbOperationType.SelectedIndex > 0)
        {
            var selectedType = cmbOperationType.SelectedItem?.ToString();
            filteredLogs = filteredLogs.Where(l => l.OperationType.Contains(selectedType ?? ""));
        }

        var result = filteredLogs.ToList();
        _bindingSource.DataSource = result;
        dgvLogs.DataSource = _bindingSource;

        // 更新统计信息
        lblStats.Text = $"共 {result.Count} 条记录";
    }

    private void BtnSearch_Click(object? sender, EventArgs e)
    {
        LoadLogs();
    }

    private void BtnReset_Click(object? sender, EventArgs e)
    {
        dtpStartDate.Value = DateTime.Now.AddDays(-7);
        dtpEndDate.Value = DateTime.Now;
        cmbUser.SelectedIndex = 0;
        cmbOperationType.SelectedIndex = 0;
        LoadLogs();
    }

    private void BtnExport_Click(object? sender, EventArgs e)
    {
        try
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV文件|*.csv",
                FileName = $"操作日志_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveDialog.ShowDialog(this) == DialogResult.OK)
            {
                var logs = (List<OperationLogViewModel>)_bindingSource.DataSource;
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("日志ID,操作时间,操作用户,操作类型,操作内容,刀模编号,IP地址");

                foreach (var log in logs)
                {
                    csv.AppendLine($"{log.LogID},{log.CreateTime:yyyy-MM-dd HH:mm:ss},{EscapeCsv(log.Username)},{EscapeCsv(log.OperationType)},{EscapeCsv(log.OperationDesc)},{EscapeCsv(log.DieCode)},{log.IPAddress}");
                }

                File.WriteAllText(saveDialog.FileName, csv.ToString(), System.Text.Encoding.UTF8);
                MessageBox.Show("导出成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }

    private DateTimePicker dtpStartDate = null!;
    private DateTimePicker dtpEndDate = null!;
    private ComboBox cmbUser = null!;
    private ComboBox cmbOperationType = null!;
    private Button btnSearch = null!;
    private Button btnReset = null!;
    private Button btnExport = null!;
    private DataGridView dgvLogs = null!;
    private Label lblStats = null!;
}

/// <summary>
/// 操作日志视图模型
/// </summary>
public class OperationLogViewModel
{
    public int LogID { get; set; }
    public int? UserID { get; set; }
    public string Username { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string OperationDesc { get; set; } = string.Empty;
    public int? DieID { get; set; }
    public string DieCode { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
}
