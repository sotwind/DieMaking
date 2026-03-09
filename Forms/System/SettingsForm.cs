using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;
using Microsoft.Data.SqlClient;

namespace DieMaking.Forms.System;

/// <summary>
/// 系统设置窗体
/// </summary>
public partial class SettingsForm : Form
{
    private readonly ConfigService _configService;
    private readonly Dictionary<string, string> _modifiedConfigs = new();
    private bool _permissionDenied = false;

    /// <summary>
    /// 检查是否因权限不足而被拒绝访问
    /// </summary>
    public bool IsPermissionDenied => _permissionDenied;

    public SettingsForm()
    {
        InitializeComponent();
        _configService = new ConfigService();

        // 检查权限 - 使用系统设置专用权限键
        if (!CurrentUser.HasPermission(PermissionKeys.SystemSettings))
        {
            MessageBox.Show("您没有权限访问系统设置功能！", "权限不足", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            // 不立即关闭窗体，设置标记让调用方处理
            _permissionDenied = true;
            return;
        }

        this.Text = "系统设置";
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(700, 550);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // 标题
        var lblTitle = new Label
        {
            Text = "系统设置",
            Font = new Font("微软雅黑", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        // 创建TabControl
        tabControl = new TabControl
        {
            Location = new Point(20, 55),
            Size = new Size(640, 400)
        };

        // 基本设置页
        var tabBasic = new TabPage("基本设置");
        InitializeBasicSettings(tabBasic);
        tabControl.TabPages.Add(tabBasic);

        // 安全设置页
        var tabSecurity = new TabPage("安全设置");
        InitializeSecuritySettings(tabSecurity);
        tabControl.TabPages.Add(tabSecurity);

        // 日志设置页
        var tabLog = new TabPage("日志设置");
        InitializeLogSettings(tabLog);
        tabControl.TabPages.Add(tabLog);

        // 按钮区域
        btnSave = new Button
        {
            Text = "保存",
            Location = new Point(480, 470),
            Size = new Size(80, 30)
        };
        btnSave.Click += BtnSave_Click;

        btnReset = new Button
        {
            Text = "重置",
            Location = new Point(380, 470),
            Size = new Size(80, 30)
        };
        btnReset.Click += BtnReset_Click;

        btnClose = new Button
        {
            Text = "关闭",
            Location = new Point(580, 470),
            Size = new Size(80, 30)
        };
        btnClose.Click += (s, e) => this.Close();

        this.Controls.Add(lblTitle);
        this.Controls.Add(tabControl);
        this.Controls.Add(btnSave);
        this.Controls.Add(btnReset);
        this.Controls.Add(btnClose);
    }

    #region 初始化各设置页

    private void InitializeBasicSettings(TabPage tab)
    {
        int labelWidth = 100;
        int inputWidth = 450;
        int startY = 20;
        int rowHeight = 45;

        // 系统名称
        var lblSystemName = new Label
        {
            Text = "系统名称：",
            Location = new Point(20, startY),
            Size = new Size(labelWidth, 25)
        };
        txtSystemName = new TextBox
        {
            Location = new Point(125, startY),
            Size = new Size(inputWidth, 25),
            Tag = ConfigKeys.SystemName
        };
        txtSystemName.TextChanged += ConfigValueChanged;

        // 公司名称
        var lblCompanyName = new Label
        {
            Text = "公司名称：",
            Location = new Point(20, startY + rowHeight),
            Size = new Size(labelWidth, 25)
        };
        txtCompanyName = new TextBox
        {
            Location = new Point(125, startY + rowHeight),
            Size = new Size(inputWidth, 25),
            Tag = ConfigKeys.CompanyName
        };
        txtCompanyName.TextChanged += ConfigValueChanged;

        // 系统版本
        var lblVersion = new Label
        {
            Text = "系统版本：",
            Location = new Point(20, startY + rowHeight * 2),
            Size = new Size(labelWidth, 25)
        };
        txtVersion = new TextBox
        {
            Location = new Point(125, startY + rowHeight * 2),
            Size = new Size(150, 25),
            Tag = ConfigKeys.SystemVersion,
            ReadOnly = true,
            BackColor = SystemColors.Control
        };

        // 默认分页大小
        var lblPageSize = new Label
        {
            Text = "分页大小：",
            Location = new Point(20, startY + rowHeight * 3),
            Size = new Size(labelWidth, 25)
        };
        numPageSize = new NumericUpDown
        {
            Location = new Point(125, startY + rowHeight * 3),
            Size = new Size(80, 25),
            Minimum = 5,
            Maximum = 100,
            Tag = ConfigKeys.DefaultPageSize
        };
        numPageSize.ValueChanged += ConfigValueChanged;

        var lblPageSizeHint = new Label
        {
            Text = "条/页（影响所有列表的默认显示条数）",
            Location = new Point(215, startY + rowHeight * 3 + 2),
            Size = new Size(250, 25),
            ForeColor = Color.Gray
        };

        // 日期格式
        var lblDateFormat = new Label
        {
            Text = "日期格式：",
            Location = new Point(20, startY + rowHeight * 4),
            Size = new Size(labelWidth, 25)
        };
        cmbDateFormat = new ComboBox
        {
            Location = new Point(125, startY + rowHeight * 4),
            Size = new Size(150, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Tag = ConfigKeys.DateFormat
        };
        cmbDateFormat.Items.AddRange(new object[] { "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "MM/dd/yyyy" });
        cmbDateFormat.SelectedIndexChanged += ConfigValueChanged;

        // 时间格式
        var lblTimeFormat = new Label
        {
            Text = "时间格式：",
            Location = new Point(20, startY + rowHeight * 5),
            Size = new Size(labelWidth, 25)
        };
        cmbTimeFormat = new ComboBox
        {
            Location = new Point(125, startY + rowHeight * 5),
            Size = new Size(150, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Tag = ConfigKeys.TimeFormat
        };
        cmbTimeFormat.Items.AddRange(new object[] { "HH:mm:ss", "HH:mm", "hh:mm:ss tt", "hh:mm tt" });
        cmbTimeFormat.SelectedIndexChanged += ConfigValueChanged;

        // 文件上传路径
        var lblUploadPath = new Label
        {
            Text = "上传路径：",
            Location = new Point(20, startY + rowHeight * 6),
            Size = new Size(labelWidth, 25)
        };
        txtUploadPath = new TextBox
        {
            Location = new Point(125, startY + rowHeight * 6),
            Size = new Size(350, 25),
            Tag = ConfigKeys.FileUploadPath
        };
        txtUploadPath.TextChanged += ConfigValueChanged;

        btnBrowseUpload = new Button
        {
            Text = "浏览...",
            Location = new Point(485, startY + rowHeight * 6),
            Size = new Size(70, 25)
        };
        btnBrowseUpload.Click += (s, e) =>
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtUploadPath.Text = dialog.SelectedPath;
            }
        };

        tab.Controls.Add(lblSystemName);
        tab.Controls.Add(txtSystemName);
        tab.Controls.Add(lblCompanyName);
        tab.Controls.Add(txtCompanyName);
        tab.Controls.Add(lblVersion);
        tab.Controls.Add(txtVersion);
        tab.Controls.Add(lblPageSize);
        tab.Controls.Add(numPageSize);
        tab.Controls.Add(lblPageSizeHint);
        tab.Controls.Add(lblDateFormat);
        tab.Controls.Add(cmbDateFormat);
        tab.Controls.Add(lblTimeFormat);
        tab.Controls.Add(cmbTimeFormat);
        tab.Controls.Add(lblUploadPath);
        tab.Controls.Add(txtUploadPath);
        tab.Controls.Add(btnBrowseUpload);
    }

    private void InitializeSecuritySettings(TabPage tab)
    {
        int labelWidth = 150;
        int inputWidth = 100;
        int startY = 20;
        int rowHeight = 40;

        // 密码策略组
        var grpPassword = new GroupBox
        {
            Text = "密码策略",
            Location = new Point(20, startY),
            Size = new Size(590, 180)
        };

        var lblMinLength = new Label
        {
            Text = "最小长度：",
            Location = new Point(20, 30),
            Size = new Size(labelWidth, 25)
        };
        numPasswordMinLength = new NumericUpDown
        {
            Location = new Point(170, 28),
            Size = new Size(80, 25),
            Minimum = 4,
            Maximum = 20,
            Tag = ConfigKeys.PasswordMinLength
        };
        numPasswordMinLength.ValueChanged += ConfigValueChanged;

        var lblMinLengthHint = new Label
        {
            Text = "位",
            Location = new Point(255, 30),
            Size = new Size(30, 25)
        };

        chkRequireUppercase = new CheckBox
        {
            Text = "要求包含大写字母",
            Location = new Point(20, 65),
            Size = new Size(200, 25),
            Tag = ConfigKeys.PasswordRequireUppercase
        };
        chkRequireUppercase.CheckedChanged += ConfigValueChanged;

        chkRequireLowercase = new CheckBox
        {
            Text = "要求包含小写字母",
            Location = new Point(20, 95),
            Size = new Size(200, 25),
            Tag = ConfigKeys.PasswordRequireLowercase
        };
        chkRequireLowercase.CheckedChanged += ConfigValueChanged;

        chkRequireDigit = new CheckBox
        {
            Text = "要求包含数字",
            Location = new Point(20, 125),
            Size = new Size(200, 25),
            Tag = ConfigKeys.PasswordRequireDigit
        };
        chkRequireDigit.CheckedChanged += ConfigValueChanged;

        chkRequireSpecialChar = new CheckBox
        {
            Text = "要求包含特殊字符",
            Location = new Point(20, 155),
            Size = new Size(200, 25),
            Tag = ConfigKeys.PasswordRequireSpecialChar
        };
        chkRequireSpecialChar.CheckedChanged += ConfigValueChanged;

        grpPassword.Controls.Add(lblMinLength);
        grpPassword.Controls.Add(numPasswordMinLength);
        grpPassword.Controls.Add(lblMinLengthHint);
        grpPassword.Controls.Add(chkRequireUppercase);
        grpPassword.Controls.Add(chkRequireLowercase);
        grpPassword.Controls.Add(chkRequireDigit);
        grpPassword.Controls.Add(chkRequireSpecialChar);

        // 登录策略组
        var grpLogin = new GroupBox
        {
            Text = "登录策略",
            Location = new Point(20, startY + 200),
            Size = new Size(590, 140)
        };

        var lblMaxFailures = new Label
        {
            Text = "最大登录失败次数：",
            Location = new Point(20, 30),
            Size = new Size(labelWidth, 25)
        };
        numMaxFailures = new NumericUpDown
        {
            Location = new Point(170, 28),
            Size = new Size(80, 25),
            Minimum = 3,
            Maximum = 10,
            Tag = ConfigKeys.MaxLoginFailures
        };
        numMaxFailures.ValueChanged += ConfigValueChanged;

        var lblMaxFailuresHint = new Label
        {
            Text = "次（超过后锁定账户）",
            Location = new Point(255, 30),
            Size = new Size(180, 25)
        };

        var lblLockoutDuration = new Label
        {
            Text = "账户锁定时间：",
            Location = new Point(20, 65),
            Size = new Size(labelWidth, 25)
        };
        numLockoutDuration = new NumericUpDown
        {
            Location = new Point(170, 63),
            Size = new Size(80, 25),
            Minimum = 5,
            Maximum = 120,
            Tag = ConfigKeys.LockoutDuration
        };
        numLockoutDuration.ValueChanged += ConfigValueChanged;

        var lblLockoutDurationHint = new Label
        {
            Text = "分钟",
            Location = new Point(255, 65),
            Size = new Size(50, 25)
        };

        var lblSessionTimeout = new Label
        {
            Text = "会话超时时间：",
            Location = new Point(20, 100),
            Size = new Size(labelWidth, 25)
        };
        numSessionTimeout = new NumericUpDown
        {
            Location = new Point(170, 98),
            Size = new Size(80, 25),
            Minimum = 10,
            Maximum = 240,
            Tag = ConfigKeys.SessionTimeout
        };
        numSessionTimeout.ValueChanged += ConfigValueChanged;

        var lblSessionTimeoutHint = new Label
        {
            Text = "分钟（无操作后自动退出）",
            Location = new Point(255, 100),
            Size = new Size(200, 25)
        };

        grpLogin.Controls.Add(lblMaxFailures);
        grpLogin.Controls.Add(numMaxFailures);
        grpLogin.Controls.Add(lblMaxFailuresHint);
        grpLogin.Controls.Add(lblLockoutDuration);
        grpLogin.Controls.Add(numLockoutDuration);
        grpLogin.Controls.Add(lblLockoutDurationHint);
        grpLogin.Controls.Add(lblSessionTimeout);
        grpLogin.Controls.Add(numSessionTimeout);
        grpLogin.Controls.Add(lblSessionTimeoutHint);

        tab.Controls.Add(grpPassword);
        tab.Controls.Add(grpLogin);
    }

    private void InitializeLogSettings(TabPage tab)
    {
        int labelWidth = 100;
        int startY = 20;
        int rowHeight = 50;

        // 日志级别
        var lblLogLevel = new Label
        {
            Text = "日志级别：",
            Location = new Point(20, startY),
            Size = new Size(labelWidth, 25)
        };
        cmbLogLevel = new ComboBox
        {
            Location = new Point(125, startY),
            Size = new Size(150, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Tag = ConfigKeys.LogLevel
        };
        cmbLogLevel.Items.AddRange(new object[] { "Debug", "Info", "Warning", "Error" });
        cmbLogLevel.SelectedIndexChanged += ConfigValueChanged;

        var lblLogLevelHint = new Label
        {
            Text = "（低于此级别的日志不会被记录）",
            Location = new Point(285, startY + 2),
            Size = new Size(250, 25),
            ForeColor = Color.Gray
        };

        // 日志保留天数
        var lblLogRetention = new Label
        {
            Text = "保留天数：",
            Location = new Point(20, startY + rowHeight),
            Size = new Size(labelWidth, 25)
        };
        numLogRetentionDays = new NumericUpDown
        {
            Location = new Point(125, startY + rowHeight),
            Size = new Size(80, 25),
            Minimum = 7,
            Maximum = 365,
            Tag = ConfigKeys.LogRetentionDays
        };
        numLogRetentionDays.ValueChanged += ConfigValueChanged;

        var lblLogRetentionHint = new Label
        {
            Text = "天（超过此天数的日志将被自动清理）",
            Location = new Point(215, startY + rowHeight + 2),
            Size = new Size(300, 25),
            ForeColor = Color.Gray
        };

        // 说明文本
        var lblNote = new Label
        {
            Text = "说明：\n\n" +
                   "• Debug：记录所有信息，包括调试信息\n" +
                   "• Info：记录一般信息、操作记录\n" +
                   "• Warning：记录警告信息\n" +
                   "• Error：仅记录错误信息\n\n" +
                   "建议生产环境使用 Info 或 Warning 级别",
            Location = new Point(20, startY + rowHeight * 3),
            Size = new Size(580, 150),
            ForeColor = Color.Gray
        };

        tab.Controls.Add(lblLogLevel);
        tab.Controls.Add(cmbLogLevel);
        tab.Controls.Add(lblLogLevelHint);
        tab.Controls.Add(lblLogRetention);
        tab.Controls.Add(numLogRetentionDays);
        tab.Controls.Add(lblLogRetentionHint);
        tab.Controls.Add(lblNote);
    }

    #endregion

    #region 数据加载与保存

    private void LoadSettings()
    {
        try
        {
            // 基本设置
            txtSystemName.Text = _configService.GetConfigValue(ConfigKeys.SystemName, "刀模管理系统");
            txtCompanyName.Text = _configService.GetConfigValue(ConfigKeys.CompanyName, "");
            txtVersion.Text = _configService.GetConfigValue(ConfigKeys.SystemVersion, "1.0.0");
            numPageSize.Value = _configService.GetConfigValueInt(ConfigKeys.DefaultPageSize, 20);

            var dateFormat = _configService.GetConfigValue(ConfigKeys.DateFormat, "yyyy-MM-dd");
            cmbDateFormat.SelectedItem = dateFormat ?? "yyyy-MM-dd";

            var timeFormat = _configService.GetConfigValue(ConfigKeys.TimeFormat, "HH:mm:ss");
            cmbTimeFormat.SelectedItem = timeFormat ?? "HH:mm:ss";

            txtUploadPath.Text = _configService.GetConfigValue(ConfigKeys.FileUploadPath, @"C:\DieMaking\Uploads");

            // 安全设置
            numPasswordMinLength.Value = _configService.GetConfigValueInt(ConfigKeys.PasswordMinLength, 6);
            chkRequireUppercase.Checked = _configService.GetConfigValueBool(ConfigKeys.PasswordRequireUppercase, false);
            chkRequireLowercase.Checked = _configService.GetConfigValueBool(ConfigKeys.PasswordRequireLowercase, false);
            chkRequireDigit.Checked = _configService.GetConfigValueBool(ConfigKeys.PasswordRequireDigit, false);
            chkRequireSpecialChar.Checked = _configService.GetConfigValueBool(ConfigKeys.PasswordRequireSpecialChar, false);

            numMaxFailures.Value = _configService.GetConfigValueInt(ConfigKeys.MaxLoginFailures, 5);
            numLockoutDuration.Value = _configService.GetConfigValueInt(ConfigKeys.LockoutDuration, 30);
            numSessionTimeout.Value = _configService.GetConfigValueInt(ConfigKeys.SessionTimeout, 30);

            // 日志设置
            var logLevel = _configService.GetConfigValue(ConfigKeys.LogLevel, "Info");
            cmbLogLevel.SelectedItem = logLevel ?? "Info";
            numLogRetentionDays.Value = _configService.GetConfigValueInt(ConfigKeys.LogRetentionDays, 30);

            _modifiedConfigs.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载设置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ConfigValueChanged(object? sender, EventArgs e)
    {
        if (sender is Control control && control.Tag is string configKey)
        {
            string value = control switch
            {
                TextBox textBox => textBox.Text,
                NumericUpDown numeric => numeric.Value.ToString(),
                ComboBox comboBox => comboBox.SelectedItem?.ToString() ?? "",
                CheckBox checkBox => checkBox.Checked.ToString(),
                _ => ""
            };

            // 空值检查：对于关键配置项，如果值为空则给出警告
            if (string.IsNullOrEmpty(value) && IsRequiredConfigKey(configKey))
            {
                // 不保存空值，使用默认值
                return;
            }

            _modifiedConfigs[configKey] = value;
        }
    }

    /// <summary>
    /// 检查配置键是否为必填项
    /// </summary>
    private bool IsRequiredConfigKey(string configKey)
    {
        var requiredKeys = new[]
        {
            ConfigKeys.DateFormat,
            ConfigKeys.TimeFormat,
            ConfigKeys.LogLevel
        };
        return requiredKeys.Contains(configKey);
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        // 空值检查：确保下拉框有选中项
        if (cmbDateFormat.SelectedItem == null)
        {
            MessageBox.Show("请选择日期格式", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tabControl.SelectedTab = tabControl.TabPages[0]; // 切换到基本设置页
            cmbDateFormat.Focus();
            return;
        }

        if (cmbTimeFormat.SelectedItem == null)
        {
            MessageBox.Show("请选择时间格式", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tabControl.SelectedTab = tabControl.TabPages[0]; // 切换到基本设置页
            cmbTimeFormat.Focus();
            return;
        }

        if (cmbLogLevel.SelectedItem == null)
        {
            MessageBox.Show("请选择日志级别", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tabControl.SelectedTab = tabControl.TabPages[2]; // 切换到日志设置页
            cmbLogLevel.Focus();
            return;
        }

        if (_modifiedConfigs.Count == 0)
        {
            MessageBox.Show("没有需要保存的更改", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            if (_configService.UpdateConfigs(_modifiedConfigs))
            {
                // 刷新配置缓存
                ConfigHelper.RefreshCache(_configService);

                // 记录操作日志
                LogOperation("修改系统设置", $"修改了 {_modifiedConfigs.Count} 项系统配置");

                MessageBox.Show("设置保存成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _modifiedConfigs.Clear();
            }
            else
            {
                MessageBox.Show("设置保存失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存设置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnReset_Click(object? sender, EventArgs e)
    {
        if (_modifiedConfigs.Count > 0)
        {
            if (MessageBox.Show("确定要重置所有更改吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                LoadSettings();
            }
        }
        else
        {
            LoadSettings();
        }
    }

    private void LogOperation(string operationType, string operationDesc)
    {
        try
        {
            var sql = @"INSERT INTO DM_OperationLog (UserID, Username, OperationType, OperationDesc, CreateTime) 
                        VALUES (@UserID, @Username, @OperationType, @OperationDesc, GETDATE())";
            DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@UserID", CurrentUser.User?.UserID),
                new SqlParameter("@Username", CurrentUser.User?.Username ?? ""),
                new SqlParameter("@OperationType", operationType),
                new SqlParameter("@OperationDesc", operationDesc));
        }
        catch
        {
            // 日志记录失败不影响主流程
        }
    }

    #endregion

    #region 控件声明

    private TabControl tabControl = null!;
    private Button btnSave = null!;
    private Button btnReset = null!;
    private Button btnClose = null!;

    // 基本设置
    private TextBox txtSystemName = null!;
    private TextBox txtCompanyName = null!;
    private TextBox txtVersion = null!;
    private NumericUpDown numPageSize = null!;
    private ComboBox cmbDateFormat = null!;
    private ComboBox cmbTimeFormat = null!;
    private TextBox txtUploadPath = null!;
    private Button btnBrowseUpload = null!;

    // 安全设置
    private NumericUpDown numPasswordMinLength = null!;
    private CheckBox chkRequireUppercase = null!;
    private CheckBox chkRequireLowercase = null!;
    private CheckBox chkRequireDigit = null!;
    private CheckBox chkRequireSpecialChar = null!;
    private NumericUpDown numMaxFailures = null!;
    private NumericUpDown numLockoutDuration = null!;
    private NumericUpDown numSessionTimeout = null!;

    // 日志设置
    private ComboBox cmbLogLevel = null!;
    private NumericUpDown numLogRetentionDays = null!;

    #endregion
}
