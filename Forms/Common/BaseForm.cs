using DieMaking.Helpers;

namespace DieMaking.Forms;

/// <summary>
/// 窗体基类 - 提供统一的窗体样式和行为
/// </summary>
public class BaseForm : Form
{
    /// <summary>
    /// 应用程序图标
    /// </summary>
    protected static Icon? AppIcon { get; set; }

    /// <summary>
    /// 状态栏用户标签
    /// </summary>
    protected ToolStripStatusLabel? StatusUserLabel { get; set; }

    /// <summary>
    /// 状态栏时间标签
    /// </summary>
    protected ToolStripStatusLabel? StatusTimeLabel { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public BaseForm()
    {
        InitializeBaseForm();
    }

    /// <summary>
    /// 初始化基础窗体设置
    /// </summary>
    private void InitializeBaseForm()
    {
        // 设置默认图标
        if (AppIcon != null)
        {
            this.Icon = AppIcon;
        }
        else
        {
            this.Icon = SystemIcons.Application;
        }

        // 设置默认窗体样式
        this.StartPosition = FormStartPosition.CenterParent;
        
        // 应用统一字体
        UIStyleHelper.ApplyFont(this);
        
        // 设置快捷键
        this.KeyPreview = true;
        this.KeyDown += BaseForm_KeyDown;
    }

    /// <summary>
    /// 设置窗体标题（统一格式）
    /// </summary>
    protected void SetFormTitle(string title)
    {
        this.Text = $"{title} - 刀模管理系统";
    }

    /// <summary>
    /// 设置应用程序图标（静态方法，应在程序启动时调用一次）
    /// </summary>
    public static void SetApplicationIcon(Icon icon)
    {
        AppIcon = icon;
    }

    /// <summary>
    /// 键盘快捷键处理
    /// </summary>
    private void BaseForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // F5 - 刷新
        if (e.KeyCode == Keys.F5)
        {
            e.Handled = true;
            OnRefresh();
        }
        // Ctrl+S - 保存
        else if (e.Control && e.KeyCode == Keys.S)
        {
            e.Handled = true;
            OnSave();
        }
        // Ctrl+N - 新增
        else if (e.Control && e.KeyCode == Keys.N)
        {
            e.Handled = true;
            OnAdd();
        }
        // Ctrl+F - 搜索
        else if (e.Control && e.KeyCode == Keys.F)
        {
            e.Handled = true;
            OnSearch();
        }
        // Ctrl+P - 打印
        else if (e.Control && e.KeyCode == Keys.P)
        {
            e.Handled = true;
            OnPrint();
        }
        // Ctrl+E - 导出
        else if (e.Control && e.KeyCode == Keys.E)
        {
            e.Handled = true;
            OnExport();
        }
        // Esc - 关闭窗体
        else if (e.KeyCode == Keys.Escape)
        {
            e.Handled = true;
            OnCancel();
        }
        // F1 - 帮助
        else if (e.KeyCode == Keys.F1)
        {
            e.Handled = true;
            OnHelp();
        }
    }

    /// <summary>
    /// 刷新操作（子类可重写）
    /// </summary>
    protected virtual void OnRefresh()
    {
        // 默认实现为空，子类可重写
    }

    /// <summary>
    /// 保存操作（子类可重写）
    /// </summary>
    protected virtual void OnSave()
    {
        // 默认实现为空，子类可重写
    }

    /// <summary>
    /// 新增操作（子类可重写）
    /// </summary>
    protected virtual void OnAdd()
    {
        // 默认实现为空，子类可重写
    }

    /// <summary>
    /// 搜索操作（子类可重写）
    /// </summary>
    protected virtual void OnSearch()
    {
        // 默认实现为空，子类可重写
    }

    /// <summary>
    /// 打印操作（子类可重写）
    /// </summary>
    protected virtual void OnPrint()
    {
        // 默认实现为空，子类可重写
    }

    /// <summary>
    /// 导出操作（子类可重写）
    /// </summary>
    protected virtual void OnExport()
    {
        // 默认实现为空，子类可重写
    }

    /// <summary>
    /// 取消/关闭操作（子类可重写）
    /// </summary>
    protected virtual void OnCancel()
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    /// <summary>
    /// 帮助操作（子类可重写）
    /// </summary>
    protected virtual void OnHelp()
    {
        ShortcutHelper.ShowShortcutHelp(this);
    }

    /// <summary>
    /// 显示错误消息
    /// </summary>
    protected void ShowError(string message)
    {
        ErrorDialog.ShowError(message);
    }

    /// <summary>
    /// 显示错误消息（带详情）
    /// </summary>
    protected void ShowError(string message, string details)
    {
        ErrorDialog.ShowError(message, details);
    }

    /// <summary>
    /// 显示警告消息
    /// </summary>
    protected DialogResult ShowWarning(string message, MessageBoxButtons buttons = MessageBoxButtons.OK)
    {
        return MessageBox.Show(this, message, "警告", buttons, MessageBoxIcon.Warning);
    }

    /// <summary>
    /// 显示信息消息
    /// </summary>
    protected void ShowInfo(string message)
    {
        MessageBox.Show(this, message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// 显示成功提示（右下角弹出）
    /// </summary>
    protected void ShowSuccess(string message)
    {
        UIStyleHelper.ShowSuccessToast(this, message);
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    protected DialogResult ShowConfirm(string message)
    {
        return MessageBox.Show(this, message, "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    }

    /// <summary>
    /// 执行带异常处理的操作
    /// </summary>
    protected bool ExecuteWithExceptionHandling(Action action, string operationName)
    {
        try
        {
            action();
            return true;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, operationName);
            return false;
        }
    }

    /// <summary>
    /// 配置 DataGridView 性能优化
    /// </summary>
    protected void ConfigureDataGridView(DataGridView dataGridView, int expectedRowCount = 0)
    {
        dataGridView.ConfigureForPerformance(expectedRowCount > 1000, expectedRowCount);
        dataGridView.SetDefaultColumnStyles();
    }

    /// <summary>
    /// 应用统一DataGridView样式
    /// </summary>
    protected void ApplyDataGridViewStyle(DataGridView dataGridView)
    {
        UIStyleHelper.ConfigureDataGridView(dataGridView);
    }

    /// <summary>
    /// 创建状态栏
    /// </summary>
    protected StatusStrip CreateStatusBar()
    {
        var statusStrip = UIStyleHelper.CreateStatusStrip(out var userLabel, out var timeLabel);
        StatusUserLabel = userLabel;
        StatusTimeLabel = timeLabel;
        return statusStrip;
    }

    /// <summary>
    /// 应用按钮样式
    /// </summary>
    protected void ApplyButtonStyle(Button button, ButtonStyle style)
    {
        UIStyleHelper.ApplyButtonStyle(button, style);
    }

    /// <summary>
    /// 注册输入框回车跳转
    /// </summary>
    protected void RegisterEnterToNext()
    {
        ShortcutHelper.RegisterEnterToNext(this);
    }

    /// <summary>
    /// 窗体加载时的事件处理
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // 记录窗体打开日志
        try
        {
            // 可以在这里添加操作日志记录
        }
        catch
        {
            // 日志记录失败不影响窗体显示
        }
    }

    /// <summary>
    /// 窗体关闭时的事件处理
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        
        // 如果有未保存的更改，提示保存
        if (HasUnsavedChanges())
        {
            var result = ShowWarning("有未保存的更改，是否保存？", MessageBoxButtons.YesNoCancel);
            
            if (result == DialogResult.Yes)
            {
                if (!OnSaveWithResult())
                {
                    e.Cancel = true; // 保存失败，取消关闭
                }
            }
            else if (result == DialogResult.Cancel)
            {
                e.Cancel = true; // 取消关闭
            }
        }
    }

    /// <summary>
    /// 检查是否有未保存的更改（子类可重写）
    /// </summary>
    protected virtual bool HasUnsavedChanges()
    {
        return false;
    }

    /// <summary>
    /// 保存操作并返回结果（子类可重写）
    /// </summary>
    protected virtual bool OnSaveWithResult()
    {
        try
        {
            OnSave();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// 列表窗体基类
/// </summary>
public class BaseListForm : BaseForm
{
    protected DataGridView? DataGridView { get; set; }

    /// <summary>
    /// 刷新数据（子类必须实现）
    /// </summary>
    protected virtual void LoadData()
    {
        throw new NotImplementedException("子类必须实现 LoadData 方法");
    }

    protected override void OnRefresh()
    {
        base.OnRefresh();
        LoadData();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // 设置列表窗体默认大小
        if (this.Size == new Size(0, 0) || this.Size.Width < 800)
        {
            this.Size = UIStyleHelper.SizeListForm;
        }
        
        // 加载数据
        LoadData();
    }
}

/// <summary>
/// 编辑窗体基类
/// </summary>
public class BaseEditForm : BaseForm
{
    /// <summary>
    /// 是否处于编辑模式
    /// </summary>
    protected bool IsEditMode { get; set; }

    /// <summary>
    /// 验证输入（子类可重写）
    /// </summary>
    protected virtual bool ValidateInput()
    {
        return true;
    }

    protected override void OnSave()
    {
        if (ValidateInput())
        {
            if (SaveData())
            {
                ShowSuccess("保存成功");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }

    /// <summary>
    /// 保存数据（子类必须实现）
    /// </summary>
    protected virtual bool SaveData()
    {
        throw new NotImplementedException("子类必须实现 SaveData 方法");
    }

    protected override bool OnSaveWithResult()
    {
        if (ValidateInput())
        {
            return SaveData();
        }
        return false;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // 设置编辑窗体默认大小
        if (this.Size == new Size(0, 0) || this.Size.Width < 600)
        {
            this.Size = UIStyleHelper.SizeEditForm;
        }
        
        // 注册回车跳转
        RegisterEnterToNext();
    }
}

/// <summary>
/// 对话框基类
/// </summary>
public class BaseDialogForm : BaseForm
{
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // 设置对话框默认大小
        if (this.Size == new Size(0, 0) || this.Size.Width < 500)
        {
            this.Size = UIStyleHelper.SizeDialog;
        }
        
        // 设置对话框样式
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
    }
}
