using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;
using Microsoft.Data.SqlClient;

namespace DieMaking.Forms.System;

/// <summary>
/// 个人设置窗体
/// </summary>
public partial class UserSettingsForm : Form
{
    private readonly ConfigService _configService;
    private UserPreference _preference = null!;

    public UserSettingsForm()
    {
        InitializeComponent();
        _configService = new ConfigService();

        if (CurrentUser.User == null)
        {
            MessageBox.Show("请先登录！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.Close();
            return;
        }

        this.Text = "个人设置";
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(550, 450);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // 标题
        var lblTitle = new Label
        {
            Text = "个人设置",
            Font = new Font("微软雅黑", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        // 创建TabControl
        tabControl = new TabControl
        {
            Location = new Point(20, 55),
            Size = new Size(490, 300)
        };

        // 界面设置页
        var tabUI = new TabPage("界面设置");
        InitializeUISettings(tabUI);
        tabControl.TabPages.Add(tabUI);

        // 格式设置页
        var tabFormat = new TabPage("格式设置");
        InitializeFormatSettings(tabFormat);
        tabControl.TabPages.Add(tabFormat);

        // 默认页面设置页
        var tabDefaultPage = new TabPage("默认页面");
        InitializeDefaultPageSettings(tabDefaultPage);
        tabControl.TabPages.Add(tabDefaultPage);

        // 修改密码页
        var tabPassword = new TabPage("修改密码");
        InitializePasswordSettings(tabPassword);
        tabControl.TabPages.Add(tabPassword);

        // 按钮区域
        btnSave = new Button
        {
            Text = "保存",
            Location = new Point(330, 370),
            Size = new Size(80, 30)
        };
        btnSave.Click += BtnSave_Click;

        btnReset = new Button
        {
            Text = "重置",
            Location = new Point(230, 370),
            Size = new Size(80, 30)
        };
        btnReset.Click += BtnReset_Click;

        btnClose = new Button
        {
            Text = "关闭",
            Location = new Point(430, 370),
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

    private void InitializeUISettings(TabPage tab)
    {
        int labelWidth = 100;
        int startY = 30;
        int rowHeight = 60;

        // 主题颜色
        var lblTheme = new Label
        {
            Text = "主题颜色：",
            Location = new Point(20, startY),
            Size = new Size(labelWidth, 25)
        };

        // 浅色主题选项
        pnlLightTheme = new Panel
        {
            Location = new Point(125, startY - 5),
            Size = new Size(150, 80),
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand
        };
        pnlLightTheme.Click += (s, e) => SelectTheme("Light");

        var pnlLightPreview = new Panel
        {
            Location = new Point(10, 10),
            Size = new Size(130, 35),
            BackColor = Color.White
        };

        var lblLightText = new Label
        {
            Text = "浅色主题",
            Location = new Point(10, 50),
            Size = new Size(100, 25),
            ForeColor = Color.Black
        };

        chkLightTheme = new CheckBox
        {
            Location = new Point(115, 52),
            Size = new Size(20, 20),
            Enabled = false
        };

        pnlLightTheme.Controls.Add(pnlLightPreview);
        pnlLightTheme.Controls.Add(lblLightText);
        pnlLightTheme.Controls.Add(chkLightTheme);

        // 深色主题选项
        pnlDarkTheme = new Panel
        {
            Location = new Point(295, startY - 5),
            Size = new Size(150, 80),
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand
        };
        pnlDarkTheme.Click += (s, e) => SelectTheme("Dark");

        var pnlDarkPreview = new Panel
        {
            Location = new Point(10, 10),
            Size = new Size(130, 35),
            BackColor = Color.FromArgb(45, 45, 48)
        };

        var lblDarkText = new Label
        {
            Text = "深色主题",
            Location = new Point(10, 50),
            Size = new Size(100, 25),
            ForeColor = Color.Black
        };

        chkDarkTheme = new CheckBox
        {
            Location = new Point(115, 52),
            Size = new Size(20, 20),
            Enabled = false
        };

        pnlDarkTheme.Controls.Add(pnlDarkPreview);
        pnlDarkTheme.Controls.Add(lblDarkText);
        pnlDarkTheme.Controls.Add(chkDarkTheme);

        // 默认分页大小
        var lblPageSize = new Label
        {
            Text = "分页大小：",
            Location = new Point(20, startY + rowHeight * 2),
            Size = new Size(labelWidth, 25)
        };

        numPageSize = new NumericUpDown
        {
            Location = new Point(125, startY + rowHeight * 2),
            Size = new Size(80, 25),
            Minimum = 5,
            Maximum = 100,
            Value = 20
        };

        var lblPageSizeHint = new Label
        {
            Text = "条/页（影响所有列表的默认显示条数）",
            Location = new Point(215, startY + rowHeight * 2 + 2),
            Size = new Size(250, 25),
            ForeColor = Color.Gray
        };

        tab.Controls.Add(lblTheme);
        tab.Controls.Add(pnlLightTheme);
        tab.Controls.Add(pnlDarkTheme);
        tab.Controls.Add(lblPageSize);
        tab.Controls.Add(numPageSize);
        tab.Controls.Add(lblPageSizeHint);
    }

    private void InitializeFormatSettings(TabPage tab)
    {
        int labelWidth = 100;
        int startY = 30;
        int rowHeight = 50;

        // 日期格式
        var lblDateFormat = new Label
        {
            Text = "日期格式：",
            Location = new Point(20, startY),
            Size = new Size(labelWidth, 25)
        };

        cmbDateFormat = new ComboBox
        {
            Location = new Point(125, startY),
            Size = new Size(150, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbDateFormat.Items.AddRange(new object[] { "yyyy-MM-dd", "yyyy/MM/dd", "dd/MM/yyyy", "MM/dd/yyyy" });

        lblDatePreview = new Label
        {
            Text = "",
            Location = new Point(285, startY + 2),
            Size = new Size(150, 25),
            ForeColor = Color.Gray
        };
        cmbDateFormat.SelectedIndexChanged += (s, e) =>
        {
            if (cmbDateFormat.SelectedItem != null)
            {
                lblDatePreview.Text = $"示例：{DateTime.Now.ToString(cmbDateFormat.SelectedItem.ToString())}";
            }
        };

        // 时间格式
        var lblTimeFormat = new Label
        {
            Text = "时间格式：",
            Location = new Point(20, startY + rowHeight),
            Size = new Size(labelWidth, 25)
        };

        cmbTimeFormat = new ComboBox
        {
            Location = new Point(125, startY + rowHeight),
            Size = new Size(150, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbTimeFormat.Items.AddRange(new object[] { "HH:mm:ss", "HH:mm", "hh:mm:ss tt", "hh:mm tt" });

        lblTimePreview = new Label
        {
            Text = "",
            Location = new Point(285, startY + rowHeight + 2),
            Size = new Size(150, 25),
            ForeColor = Color.Gray
        };
        cmbTimeFormat.SelectedIndexChanged += (s, e) =>
        {
            if (cmbTimeFormat.SelectedItem != null)
            {
                lblTimePreview.Text = $"示例：{DateTime.Now.ToString(cmbTimeFormat.SelectedItem.ToString())}";
            }
        };

        // 说明
        var lblNote = new Label
        {
            Text = "说明：日期时间格式将影响系统中所有日期时间的显示方式。",
            Location = new Point(20, startY + rowHeight * 3),
            Size = new Size(450, 50),
            ForeColor = Color.Gray
        };

        tab.Controls.Add(lblDateFormat);
        tab.Controls.Add(cmbDateFormat);
        tab.Controls.Add(lblDatePreview);
        tab.Controls.Add(lblTimeFormat);
        tab.Controls.Add(cmbTimeFormat);
        tab.Controls.Add(lblTimePreview);
        tab.Controls.Add(lblNote);
    }

    private void InitializeDefaultPageSettings(TabPage tab)
    {
        int startY = 30;
        int rowHeight = 40;

        var lblDesc = new Label
        {
            Text = "选择登录后默认显示的页面：",
            Location = new Point(20, startY),
            Size = new Size(300, 25)
        };

        // 创建单选按钮组
        rbDieList = new RadioButton
        {
            Text = $"刀模列表 - {DefaultPageOptions.DisplayNames[DefaultPageOptions.DieList]}",
            Location = new Point(40, startY + rowHeight),
            Size = new Size(400, 25)
        };

        rbProductionBoard = new RadioButton
        {
            Text = $"生产看板 - {DefaultPageOptions.DisplayNames[DefaultPageOptions.ProductionBoard]}",
            Location = new Point(40, startY + rowHeight * 2),
            Size = new Size(400, 25)
        };

        rbWarehouse = new RadioButton
        {
            Text = $"仓库管理 - {DefaultPageOptions.DisplayNames[DefaultPageOptions.Warehouse]}",
            Location = new Point(40, startY + rowHeight * 3),
            Size = new Size(400, 25)
        };

        rbReport = new RadioButton
        {
            Text = $"报表统计 - {DefaultPageOptions.DisplayNames[DefaultPageOptions.Report]}",
            Location = new Point(40, startY + rowHeight * 4),
            Size = new Size(400, 25)
        };

        // 说明
        var lblNote = new Label
        {
            Text = "说明：设置后，下次登录时将自动打开所选页面。\n" +
                   "（仅在有权限的情况下生效）",
            Location = new Point(20, startY + rowHeight * 6),
            Size = new Size(450, 50),
            ForeColor = Color.Gray
        };

        tab.Controls.Add(lblDesc);
        tab.Controls.Add(rbDieList);
        tab.Controls.Add(rbProductionBoard);
        tab.Controls.Add(rbWarehouse);
        tab.Controls.Add(rbReport);
        tab.Controls.Add(lblNote);
    }

    private void InitializePasswordSettings(TabPage tab)
    {
        int labelWidth = 100;
        int inputWidth = 250;
        int startY = 20;
        int rowHeight = 40;

        // 当前密码
        var lblOldPassword = new Label
        {
            Text = "当前密码：",
            Location = new Point(20, startY),
            Size = new Size(labelWidth, 25)
        };

        txtOldPassword = new TextBox
        {
            Location = new Point(125, startY),
            Size = new Size(inputWidth, 25),
            PasswordChar = '*'
        };

        // 新密码
        var lblNewPassword = new Label
        {
            Text = "新密码：",
            Location = new Point(20, startY + rowHeight),
            Size = new Size(labelWidth, 25)
        };

        txtNewPassword = new TextBox
        {
            Location = new Point(125, startY + rowHeight),
            Size = new Size(inputWidth, 25),
            PasswordChar = '*'
        };
        txtNewPassword.TextChanged += TxtNewPassword_TextChanged;

        // 确认密码
        var lblConfirmPassword = new Label
        {
            Text = "确认密码：",
            Location = new Point(20, startY + rowHeight * 2),
            Size = new Size(labelWidth, 25)
        };

        txtConfirmPassword = new TextBox
        {
            Location = new Point(125, startY + rowHeight * 2),
            Size = new Size(inputWidth, 25),
            PasswordChar = '*'
        };

        // 密码策略提示
        lblPasswordPolicy = new Label
        {
            Text = "",
            Location = new Point(20, startY + rowHeight * 3 + 5),
            Size = new Size(450, 25),
            ForeColor = Color.Gray
        };

        // 密码强度提示
        lblPasswordStrength = new Label
        {
            Text = "",
            Location = new Point(125, startY + rowHeight * 4),
            Size = new Size(250, 25)
        };

        btnChangePassword = new Button
        {
            Text = "修改密码",
            Location = new Point(125, startY + rowHeight * 5),
            Size = new Size(100, 30)
        };
        btnChangePassword.Click += BtnChangePassword_Click;

        tab.Controls.Add(lblOldPassword);
        tab.Controls.Add(txtOldPassword);
        tab.Controls.Add(lblNewPassword);
        tab.Controls.Add(txtNewPassword);
        tab.Controls.Add(lblConfirmPassword);
        tab.Controls.Add(txtConfirmPassword);
        tab.Controls.Add(lblPasswordPolicy);
        tab.Controls.Add(lblPasswordStrength);
        tab.Controls.Add(btnChangePassword);
    }

    #endregion

    #region 数据加载与保存

    private void LoadSettings()
    {
        try
        {
            // 加载用户偏好设置
            _preference = _configService.GetUserPreference(CurrentUser.User!.UserID);

            // 界面设置
            SelectTheme(_preference.Theme);
            numPageSize.Value = _preference.DefaultPageSize;

            // 格式设置
            cmbDateFormat.SelectedItem = _preference.DateFormat;
            if (cmbDateFormat.SelectedItem == null)
            {
                cmbDateFormat.SelectedIndex = 0;
            }

            cmbTimeFormat.SelectedItem = _preference.TimeFormat;
            if (cmbTimeFormat.SelectedItem == null)
            {
                cmbTimeFormat.SelectedIndex = 0;
            }

            // 默认页面
            switch (_preference.DefaultPage)
            {
                case DefaultPageOptions.DieList:
                    rbDieList.Checked = true;
                    break;
                case DefaultPageOptions.ProductionBoard:
                    rbProductionBoard.Checked = true;
                    break;
                case DefaultPageOptions.Warehouse:
                    rbWarehouse.Checked = true;
                    break;
                case DefaultPageOptions.Report:
                    rbReport.Checked = true;
                    break;
                default:
                    rbDieList.Checked = true;
                    break;
            }

            // 密码策略提示
            var policy = _configService.GetPasswordPolicy();
            lblPasswordPolicy.Text = $"密码策略：{policy}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载设置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SelectTheme(string theme)
    {
        _preference.Theme = theme;
        chkLightTheme.Checked = theme == "Light";
        chkDarkTheme.Checked = theme == "Dark";

        pnlLightTheme.BorderStyle = theme == "Light" ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;
        pnlDarkTheme.BorderStyle = theme == "Dark" ? BorderStyle.Fixed3D : BorderStyle.FixedSingle;
    }

    private void TxtNewPassword_TextChanged(object? sender, EventArgs e)
    {
        var password = txtNewPassword.Text;
        if (string.IsNullOrEmpty(password))
        {
            lblPasswordStrength.Text = "";
            return;
        }

        var strength = CalculatePasswordStrength(password);
        lblPasswordStrength.Text = $"密码强度：{strength.Text}";
        lblPasswordStrength.ForeColor = strength.Color;
    }

    private (string Text, Color Color) CalculatePasswordStrength(string password)
    {
        int score = 0;

        if (password.Length >= 6) score++;
        if (password.Length >= 10) score++;
        if (password.Any(char.IsUpper)) score++;
        if (password.Any(char.IsLower)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) score++;

        return score switch
        {
            <= 2 => ("弱", Color.Red),
            <= 4 => ("中", Color.Orange),
            _ => ("强", Color.Green)
        };
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            // 更新偏好设置
            _preference.Theme = chkLightTheme.Checked ? "Light" : "Dark";
            _preference.DefaultPageSize = (int)numPageSize.Value;
            _preference.DateFormat = cmbDateFormat.SelectedItem?.ToString() ?? "yyyy-MM-dd";
            _preference.TimeFormat = cmbTimeFormat.SelectedItem?.ToString() ?? "HH:mm:ss";

            // 获取默认页面
            _preference.DefaultPage = rbDieList.Checked ? DefaultPageOptions.DieList :
                                     rbProductionBoard.Checked ? DefaultPageOptions.ProductionBoard :
                                     rbWarehouse.Checked ? DefaultPageOptions.Warehouse :
                                     DefaultPageOptions.Report;

            if (_configService.SaveUserPreference(_preference))
            {
                // 更新当前用户上下文
                UserConfigContext.CurrentPreference = _preference;

                // 应用主题变更
                var newTheme = _preference.Theme;
                ThemeManager.SetTheme(newTheme);

                // 记录操作日志
                LogOperation("修改个人设置", $"修改了个人偏好设置，主题切换为：{(newTheme == "Dark" ? "深色" : "浅色")}");

                MessageBox.Show("设置保存成功！主题将在下次打开窗体时生效。", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        if (MessageBox.Show("确定要重置所有更改吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            LoadSettings();
        }
    }

    private void BtnChangePassword_Click(object? sender, EventArgs e)
    {
        var oldPassword = txtOldPassword.Text;
        var newPassword = txtNewPassword.Text;
        var confirmPassword = txtConfirmPassword.Text;

        // 验证输入
        if (string.IsNullOrEmpty(oldPassword))
        {
            MessageBox.Show("请输入当前密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtOldPassword.Focus();
            return;
        }

        if (string.IsNullOrEmpty(newPassword))
        {
            MessageBox.Show("请输入新密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtNewPassword.Focus();
            return;
        }

        if (newPassword != confirmPassword)
        {
            MessageBox.Show("两次输入的新密码不一致", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtConfirmPassword.Focus();
            return;
        }

        // 验证当前密码
        if (oldPassword != CurrentUser.User!.Password)
        {
            MessageBox.Show("当前密码不正确", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            txtOldPassword.Focus();
            return;
        }

        // 验证新密码是否符合策略
        var (isValid, message) = _configService.ValidatePassword(newPassword);
        if (!isValid)
        {
            MessageBox.Show($"新密码不符合策略要求：{message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtNewPassword.Focus();
            return;
        }

        try
        {
            var userService = new UserService();
            if (userService.UpdatePassword(CurrentUser.User.UserID, newPassword))
            {
                // 更新当前用户密码
                CurrentUser.User.Password = newPassword;

                MessageBox.Show("密码修改成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LogOperation("修改密码", "用户修改了自己的密码");

                // 清空密码输入框
                txtOldPassword.Clear();
                txtNewPassword.Clear();
                txtConfirmPassword.Clear();
                lblPasswordStrength.Text = "";
            }
            else
            {
                MessageBox.Show("密码修改失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"密码修改失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    // 界面设置
    private Panel pnlLightTheme = null!;
    private Panel pnlDarkTheme = null!;
    private CheckBox chkLightTheme = null!;
    private CheckBox chkDarkTheme = null!;
    private NumericUpDown numPageSize = null!;

    // 格式设置
    private ComboBox cmbDateFormat = null!;
    private ComboBox cmbTimeFormat = null!;
    private Label lblDatePreview = null!;
    private Label lblTimePreview = null!;

    // 默认页面
    private RadioButton rbDieList = null!;
    private RadioButton rbProductionBoard = null!;
    private RadioButton rbWarehouse = null!;
    private RadioButton rbReport = null!;

    // 修改密码
    private TextBox txtOldPassword = null!;
    private TextBox txtNewPassword = null!;
    private TextBox txtConfirmPassword = null!;
    private Label lblPasswordPolicy = null!;
    private Label lblPasswordStrength = null!;
    private Button btnChangePassword = null!;

    #endregion
}
