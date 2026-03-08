using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms;

/// <summary>
/// 主窗体
/// </summary>
public partial class MainForm : Form
{
    private ToolStripStatusLabel _statusLabel = null!;
    private ToolStripStatusLabel _dbStatusLabel = null!;
    private string _systemName = "刀模管理系统";

    public MainForm()
    {
        // 初始化配置
        ConfigHelper.Initialize();
        _systemName = ConfigHelper.SystemName;

        InitializeComponent();
        SetupGlobalExceptionHandling();
        SetupKeyboardShortcuts();
        StartDbHealthCheck();
        ApplyUserPreference();

        // 订阅配置变更事件
        ConfigHelper.ConfigChanged += OnConfigChanged;
    }

    /// <summary>
    /// 配置变更事件处理
    /// </summary>
    private void OnConfigChanged(object? sender, ConfigChangedEventArgs e)
    {
        this.Invoke(() =>
        {
            if (e.ConfigKey == ConfigKeys.SystemName)
            {
                _systemName = e.NewValue;
                UpdateWindowTitle();
            }
        });
    }

    /// <summary>
    /// 更新窗口标题
    /// </summary>
    private void UpdateWindowTitle()
    {
        this.Text = $"{_systemName} - 当前用户：{CurrentUser.User?.RealName ?? CurrentUser.User?.Username}";
    }

    /// <summary>
    /// 应用用户个性化设置
    /// </summary>
    private void ApplyUserPreference()
    {
        try
        {
            // 加载用户偏好设置
            UserConfigContext.LoadUserPreference();

            // 应用主题（如果有深色主题支持）
            var theme = UserConfigContext.GetTheme();
            if (theme == "Dark")
            {
                // 可以在这里应用深色主题
                // 目前Windows Forms原生支持有限，可以后续扩展
            }
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleExceptionSilent(ex, "应用用户偏好设置");
        }
    }

    /// <summary>
    /// 设置全局异常处理
    /// </summary>
    private void SetupGlobalExceptionHandling()
    {
        // 捕获UI线程异常
        Application.ThreadException += (sender, e) =>
        {
            ExceptionHelper.HandleException(e.Exception, "应用程序UI线程异常");
        };

        // 捕获非UI线程异常
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                ExceptionHelper.HandleException(ex, "应用程序未处理异常");
            }
        };

        // 捕获Task异常
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            ExceptionHelper.HandleException(e.Exception, "Task未观察异常");
            e.SetObserved();
        };
    }

    /// <summary>
    /// 设置键盘快捷键
    /// </summary>
    private void SetupKeyboardShortcuts()
    {
        this.KeyPreview = true;
        this.KeyDown += MainForm_KeyDown;
    }

    /// <summary>
    /// 键盘按下事件处理
    /// </summary>
    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // F5 - 刷新当前窗体
        if (e.KeyCode == Keys.F5)
        {
            e.Handled = true;
            RefreshCurrentForm();
        }
        // Ctrl+S - 保存（如果当前窗体支持）
        else if (e.Control && e.KeyCode == Keys.S)
        {
            e.Handled = true;
            SaveCurrentForm();
        }
        // Ctrl+R - 刷新
        else if (e.Control && e.KeyCode == Keys.R)
        {
            e.Handled = true;
            RefreshCurrentForm();
        }
        // Ctrl+W 或 Ctrl+F4 - 关闭当前子窗体
        else if ((e.Control && e.KeyCode == Keys.W) || (e.Control && e.KeyCode == Keys.F4))
        {
            e.Handled = true;
            CloseCurrentForm();
        }
        // Ctrl+Q - 退出登录
        else if (e.Control && e.KeyCode == Keys.Q)
        {
            e.Handled = true;
            Logout();
        }
        // F1 - 帮助
        else if (e.KeyCode == Keys.F1)
        {
            e.Handled = true;
            ShowHelp();
        }
    }

    /// <summary>
    /// 刷新当前窗体
    /// </summary>
    private void RefreshCurrentForm()
    {
        try
        {
            var activeForm = this.ActiveMdiChild;
            if (activeForm != null)
            {
                // 尝试调用刷新方法（如果窗体实现了特定接口）
                var refreshMethod = activeForm.GetType().GetMethod("RefreshData");
                if (refreshMethod != null)
                {
                    refreshMethod.Invoke(activeForm, null);
                    UpdateStatus("数据已刷新");
                }
                else
                {
                    // 尝试刷新DataGridView
                    foreach (var control in activeForm.Controls.OfType<DataGridView>())
                    {
                        control.Refresh();
                    }
                    UpdateStatus("页面已刷新");
                }
            }
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "刷新窗体");
        }
    }

    /// <summary>
    /// 保存当前窗体
    /// </summary>
    private void SaveCurrentForm()
    {
        try
        {
            var activeForm = this.ActiveMdiChild;
            if (activeForm != null)
            {
                // 尝试调用保存方法（如果窗体实现了特定接口）
                var saveMethod = activeForm.GetType().GetMethod("Save");
                if (saveMethod != null)
                {
                    saveMethod.Invoke(activeForm, null);
                }
                else
                {
                    // 尝试触发保存按钮
                    var saveButton = activeForm.Controls.Find("btnSave", true).FirstOrDefault() as Button;
                    if (saveButton != null && saveButton.Enabled)
                    {
                        saveButton.PerformClick();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "保存数据");
        }
    }

    /// <summary>
    /// 关闭当前子窗体
    /// </summary>
    private void CloseCurrentForm()
    {
        try
        {
            var activeForm = this.ActiveMdiChild;
            if (activeForm != null)
            {
                activeForm.Close();
            }
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "关闭窗体");
        }
    }

    /// <summary>
    /// 显示帮助
    /// </summary>
    private void ShowHelp()
    {
        var helpText = @"快捷键说明：
F5 / Ctrl+R - 刷新当前页面
Ctrl+S - 保存当前数据
Ctrl+W / Ctrl+F4 - 关闭当前窗口
Ctrl+Q - 退出登录
F1 - 显示帮助";

        MessageBox.Show(this, helpText, "帮助", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// 启动数据库健康检查
    /// </summary>
    private void StartDbHealthCheck()
    {
        // 立即检查一次
        _ = CheckDatabaseHealthAsync();

        // 每30秒检查一次
        var timer = new System.Windows.Forms.Timer();
        timer.Interval = 30000;
        timer.Tick += async (s, e) => await CheckDatabaseHealthAsync();
        timer.Start();
    }

    /// <summary>
    /// 检查数据库连接健康状态
    /// </summary>
    private async Task CheckDatabaseHealthAsync()
    {
        try
        {
            var isHealthy = await Task.Run(() =>
            {
                try
                {
                    using var connection = DbHelper.CreateConnection();
                    connection.Open();
                    using var command = new Microsoft.Data.SqlClient.SqlCommand("SELECT 1", connection);
                    return command.ExecuteScalar() != null;
                }
                catch
                {
                    return false;
                }
            });

            this.Invoke(() =>
            {
                if (_dbStatusLabel != null)
                {
                    _dbStatusLabel.Text = isHealthy ? "数据库: 已连接" : "数据库: 断开";
                    _dbStatusLabel.ForeColor = isHealthy ? Color.Green : Color.Red;
                }
            });
        }
        catch
        {
            // 健康检查失败不显示错误
        }
    }

    /// <summary>
    /// 更新状态栏消息
    /// </summary>
    private void UpdateStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = $"{message} | {DateTime.Now:HH:mm:ss}";
        }
    }

    private void InitializeComponent()
    {
        UpdateWindowTitle();
        this.Size = new Size(1200, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.IsMdiContainer = true;
        this.Icon = SystemIcons.Application;

        // 创建菜单栏
        var menuStrip = new MenuStrip();

        // 刀模管理菜单
        var dieMenu = new ToolStripMenuItem("刀模管理(&D)");
        if (CurrentUser.HasPermission(PermissionKeys.DieManage))
        {
            dieMenu.DropDownItems.Add("刀模列表", null, (s, e) => ShowForm<Die.DieListForm>());
        }
        if (CurrentUser.HasPermission(PermissionKeys.DieAdd))
        {
            dieMenu.DropDownItems.Add("添加刀模", null, (s, e) => ShowForm<Die.DieAddForm>());
        }
        menuStrip.Items.Add(dieMenu);

        // 生产管理菜单
        var productionMenu = new ToolStripMenuItem("生产管理(&P)");
        if (CurrentUser.HasPermission(PermissionKeys.Production))
        {
            productionMenu.DropDownItems.Add("生产看板", null, (s, e) => ShowForm<Production.ProductionBoardForm>());
            productionMenu.DropDownItems.Add("完工查询", null, (s, e) => ShowForm<Production.CompletionQueryForm>());
            productionMenu.DropDownItems.Add("工序报产", null, (s, e) => ShowForm<Production.ProcessReportForm>());
        }
        menuStrip.Items.Add(productionMenu);

        // 仓库管理菜单
        var warehouseMenu = new ToolStripMenuItem("仓库管理(&W)");
        if (CurrentUser.HasPermission(PermissionKeys.WarehouseManage))
        {
            if (CurrentUser.HasPermission(PermissionKeys.LocationManage))
                warehouseMenu.DropDownItems.Add("库位管理", null, (s, e) => ShowForm<Warehouse.LocationManageForm>());
            if (CurrentUser.HasPermission(PermissionKeys.DieBorrow))
                warehouseMenu.DropDownItems.Add("刀模领用", null, (s, e) => ShowForm<Warehouse.DieBorrowForm>());
            if (CurrentUser.HasPermission(PermissionKeys.DieReturn))
                warehouseMenu.DropDownItems.Add("刀模归还", null, (s, e) => ShowForm<Warehouse.DieReturnForm>());
            if (CurrentUser.HasPermission(PermissionKeys.BorrowRecord))
                warehouseMenu.DropDownItems.Add("借用记录", null, (s, e) => ShowForm<Warehouse.BorrowRecordForm>());
            if (CurrentUser.HasPermission(PermissionKeys.ScrapApply))
                warehouseMenu.DropDownItems.Add("报废申请", null, (s, e) => ShowForm<Warehouse.ScrapApplyForm>());
        }
        menuStrip.Items.Add(warehouseMenu);

        // 报表统计菜单
        var reportMenu = new ToolStripMenuItem("报表统计(&R)");
        if (CurrentUser.HasPermission(PermissionKeys.Report))
        {
            reportMenu.DropDownItems.Add("完工统计", null, (s, e) => ShowForm<Report.CompletionStatsForm>());
            reportMenu.DropDownItems.Add("工序统计", null, (s, e) => ShowForm<Report.ProcessStatsForm>());
            reportMenu.DropDownItems.Add("库存统计", null, (s, e) => ShowForm<Report.InventoryStatsForm>());
        }
        menuStrip.Items.Add(reportMenu);

        // 系统管理菜单
        var systemMenu = new ToolStripMenuItem("系统管理(&S)");
        if (CurrentUser.HasPermission(PermissionKeys.UserManage))
        {
            systemMenu.DropDownItems.Add("用户管理", null, (s, e) => ShowForm<System.UserManageForm>());
        }
        systemMenu.DropDownItems.Add("系统设置", null, (s, e) => ShowForm<System.SettingsForm>());
        systemMenu.DropDownItems.Add("个人设置", null, (s, e) => ShowForm<System.UserSettingsForm>());
        systemMenu.DropDownItems.Add("操作日志", null, (s, e) => ShowForm<System.OperationLogForm>());
        systemMenu.DropDownItems.Add("-");
        systemMenu.DropDownItems.Add("帮助(F1)", null, (s, e) => ShowHelp());
        systemMenu.DropDownItems.Add("-");
        systemMenu.DropDownItems.Add("退出登录(Ctrl+Q)", null, (s, e) => Logout());
        menuStrip.Items.Add(systemMenu);

        this.MainMenuStrip = menuStrip;
        this.Controls.Add(menuStrip);

        // 状态栏
        var statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel($"当前用户：{CurrentUser.User?.RealName ?? CurrentUser.User?.Username} | 登录时间：{DateTime.Now:HH:mm:ss}");
        _dbStatusLabel = new ToolStripStatusLabel("数据库: 检查中...");
        _dbStatusLabel.ForeColor = Color.Orange;

        statusStrip.Items.Add(_statusLabel);
        statusStrip.Items.Add(new ToolStripStatusLabel("  |  "));
        statusStrip.Items.Add(_dbStatusLabel);

        this.Controls.Add(statusStrip);
    }

    private void ShowForm<T>() where T : Form, new()
    {
        try
        {
            // 检查是否已存在该类型的窗体
            foreach (Form form in this.MdiChildren)
            {
                if (form is T)
                {
                    form.Activate();
                    return;
                }
            }

            // 创建新窗体
            var newForm = new T
            {
                MdiParent = this,
                WindowState = FormWindowState.Maximized,
                Icon = this.Icon
            };
            newForm.Show();
            UpdateStatus($"打开 {newForm.Text}");
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"打开窗体({typeof(T).Name})");
        }
    }

    private void Logout()
    {
        try
        {
            if (MessageBox.Show(this, "确定要退出登录吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                CurrentUser.User = null;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "退出登录");
        }
    }
}
