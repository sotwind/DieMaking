using DieMaking.Helpers;

namespace DieMaking.Forms;

/// <summary>
/// 窗体基类 - 提供统一的窗体样式和行为
/// </summary>
public class BaseForm : Form
{
    #region 静态属性

    /// <summary>
    /// 应用程序图标
    /// </summary>
    protected static Icon? AppIcon { get; set; }

    #endregion

    #region 实例属性

    /// <summary>
    /// 状态栏用户标签
    /// </summary>
    protected ToolStripStatusLabel? StatusUserLabel { get; set; }

    /// <summary>
    /// 状态栏时间标签
    /// </summary>
    protected ToolStripStatusLabel? StatusTimeLabel { get; set; }

    /// <summary>
    /// 是否有未保存的更改
    /// </summary>
    protected virtual bool HasUnsavedChanges => false;

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    public BaseForm()
    {
        InitializeBaseForm();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化基础窗体设置
    /// </summary>
    private void InitializeBaseForm()
    {
        // 设置默认图标
        this.Icon = AppIcon ?? SystemIcons.Application;

        // 设置默认窗体样式
        this.StartPosition = FormStartPosition.CenterParent;
        
        // 应用统一字体
        UIStyleHelper.ApplyFont(this);
        
        // 设置快捷键
        this.KeyPreview = true;
        this.KeyDown += BaseForm_KeyDown;
    }

    #endregion

    #region 静态方法

    /// <summary>
    /// 设置应用程序图标（静态方法，应在程序启动时调用一次）
    /// </summary>
    public static void SetApplicationIcon(Icon icon)
    {
        AppIcon = icon;
    }

    #endregion

    #region 标题设置

    /// <summary>
    /// 设置窗体标题（统一格式）
    /// </summary>
    protected void SetFormTitle(string title)
    {
        this.Text = $"{title} - 刀模管理系统";
    }

    #endregion

    #region 键盘快捷键

    /// <summary>
    /// 键盘快捷键处理
    /// </summary>
    private void BaseForm_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F5:
                e.Handled = true;
                OnRefresh();
                break;
            case Keys.S when e.Control:
                e.Handled = true;
                OnSave();
                break;
            case Keys.N when e.Control:
                e.Handled = true;
                OnAdd();
                break;
            case Keys.F when e.Control:
                e.Handled = true;
                OnSearch();
                break;
            case Keys.P when e.Control:
                e.Handled = true;
                OnPrint();
                break;
            case Keys.E when e.Control:
                e.Handled = true;
                OnExport();
                break;
            case Keys.Escape:
                e.Handled = true;
                OnCancel();
                break;
            case Keys.F1:
                e.Handled = true;
                OnHelp();
                break;
        }
    }

    #endregion

    #region 虚方法（可重写）

    /// <summary>
    /// 刷新操作
    /// </summary>
    protected virtual void OnRefresh() { }

    /// <summary>
    /// 保存操作
    /// </summary>
    protected virtual void OnSave() { }

    /// <summary>
    /// 新增操作
    /// </summary>
    protected virtual void OnAdd() { }

    /// <summary>
    /// 搜索操作
    /// </summary>
    protected virtual void OnSearch() { }

    /// <summary>
    /// 打印操作
    /// </summary>
    protected virtual void OnPrint() { }

    /// <summary>
    /// 导出操作
    /// </summary>
    protected virtual void OnExport() { }

    /// <summary>
    /// 取消/关闭操作
    /// </summary>
    protected virtual void OnCancel()
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    /// <summary>
    /// 帮助操作
    /// </summary>
    protected virtual void OnHelp()
    {
        ShortcutHelper.ShowShortcutHelp(this);
    }

    /// <summary>
    /// 保存操作并返回结果
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

    #endregion

    #region 消息显示

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

    #endregion

    #region 异常处理

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
    /// 执行带异常处理的操作（带返回值）
    /// </summary>
    protected T? ExecuteWithExceptionHandling<T>(Func<T> func, string operationName) where T : class
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, operationName);
            return null;
        }
    }

    #endregion

    #region UI 辅助方法

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

    #endregion

    #region 事件处理

    /// <summary>
    /// 窗体加载时的事件处理
    /// </summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // 子类可在此添加加载逻辑
    }

    /// <summary>
    /// 窗体关闭时的事件处理
    /// </summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        
        // 如果有未保存的更改，提示保存
        if (HasUnsavedChanges)
        {
            var result = ShowWarning("有未保存的更改，是否保存？", MessageBoxButtons.YesNoCancel);
            
            switch (result)
            {
                case DialogResult.Yes:
                    if (!OnSaveWithResult())
                    {
                        e.Cancel = true; // 保存失败，取消关闭
                    }
                    break;
                case DialogResult.Cancel:
                    e.Cancel = true; // 取消关闭
                    break;
            }
        }
    }

    #endregion
}

/// <summary>
/// 列表窗体基类
/// </summary>
public abstract class BaseListForm : BaseForm
{
    #region 属性

    /// <summary>
    /// 数据表格控件
    /// </summary>
    protected DataGridView? DataGridView { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    protected int CurrentPage { get; set; } = 1;

    /// <summary>
    /// 每页大小
    /// </summary>
    protected int PageSize { get; set; } = 20;

    /// <summary>
    /// 总记录数
    /// </summary>
    protected int TotalCount { get; set; }

    #endregion

    #region 抽象方法

    /// <summary>
    /// 刷新数据（子类必须实现）
    /// </summary>
    protected abstract void LoadData();

    /// <summary>
    /// 获取选中的记录ID
    /// </summary>
    protected virtual int? GetSelectedId()
    {
        if (DataGridView?.SelectedRows.Count > 0)
        {
            var row = DataGridView.SelectedRows[0];
            if (row.Cells["ID"].Value != null)
            {
                return Convert.ToInt32(row.Cells["ID"].Value);
            }
        }
        return null;
    }

    /// <summary>
    /// 检查是否有选中记录
    /// </summary>
    protected bool HasSelectedRecord()
    {
        return GetSelectedId().HasValue;
    }

    #endregion

    #region 分页方法

    /// <summary>
    /// 跳转到指定页
    /// </summary>
    protected void GoToPage(int page)
    {
        var totalPages = (TotalCount + PageSize - 1) / PageSize;
        if (totalPages == 0) totalPages = 1;

        CurrentPage = Math.Max(1, Math.Min(page, totalPages));
        LoadData();
    }

    /// <summary>
    /// 更新分页控件状态
    /// </summary>
    protected void UpdatePaginationControls(Button btnFirst, Button btnPrev, Button btnNext, Button btnLast, Label lblPageInfo)
    {
        var totalPages = (TotalCount + PageSize - 1) / PageSize;
        if (totalPages == 0) totalPages = 1;

        lblPageInfo.Text = $"第 {CurrentPage} 页 / 共 {totalPages} 页 (共 {TotalCount} 条)";

        btnFirst.Enabled = CurrentPage > 1;
        btnPrev.Enabled = CurrentPage > 1;
        btnNext.Enabled = CurrentPage < totalPages;
        btnLast.Enabled = CurrentPage < totalPages;
    }

    #endregion

    #region 重写方法

    protected override void OnRefresh()
    {
        base.OnRefresh();
        LoadData();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // 设置列表窗体默认大小
        if (this.Size.Width < 800)
        {
            this.Size = UIStyleHelper.SizeListForm;
        }
        
        // 加载数据
        LoadData();
    }

    #endregion
}

/// <summary>
/// 编辑窗体基类
/// </summary>
public abstract class BaseEditForm : BaseForm
{
    #region 属性

    /// <summary>
    /// 是否处于编辑模式
    /// </summary>
    protected bool IsEditMode { get; set; }

    /// <summary>
    /// 编辑的记录ID
    /// </summary>
    protected int? EditId { get; set; }

    /// <summary>
    /// 是否只读模式
    /// </summary>
    protected bool IsReadOnly { get; set; }

    #endregion

    #region 抽象方法

    /// <summary>
    /// 验证输入（子类可重写）
    /// </summary>
    protected virtual bool ValidateInput()
    {
        return true;
    }

    /// <summary>
    /// 保存数据（子类必须实现）
    /// </summary>
    protected abstract bool SaveData();

    /// <summary>
    /// 加载数据（子类可重写）
    /// </summary>
    protected virtual void LoadEditData() { }

    #endregion

    #region 重写方法

    protected override void OnSave()
    {
        if (!ValidateInput()) return;

        if (SaveData())
        {
            ShowSuccess("保存成功");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    protected override bool OnSaveWithResult()
    {
        if (!ValidateInput()) return false;
        return SaveData();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // 设置编辑窗体默认大小
        if (this.Size.Width < 600)
        {
            this.Size = UIStyleHelper.SizeEditForm;
        }
        
        // 注册回车跳转
        RegisterEnterToNext();

        // 加载编辑数据
        if (IsEditMode && EditId.HasValue)
        {
            LoadEditData();
        }

        // 如果只读模式，禁用编辑
        if (IsReadOnly)
        {
            SetReadOnlyMode();
        }
    }

    #endregion

    #region 保护方法

    /// <summary>
    /// 设置只读模式
    /// </summary>
    protected virtual void SetReadOnlyMode()
    {
        // 禁用所有输入控件
        foreach (Control control in this.Controls)
        {
            SetControlReadOnly(control);
        }
    }

    /// <summary>
    /// 递归设置控件只读
    /// </summary>
    private void SetControlReadOnly(Control control)
    {
        switch (control)
        {
            case TextBox textBox:
                textBox.ReadOnly = true;
                break;
            case ComboBox comboBox:
                comboBox.Enabled = false;
                break;
            case DateTimePicker picker:
                picker.Enabled = false;
                break;
            case CheckBox checkBox:
                checkBox.Enabled = false;
                break;
            case Button button when !button.Name.Contains("Close") && !button.Name.Contains("Cancel"):
                button.Enabled = false;
                break;
        }

        // 递归处理子控件
        foreach (Control child in control.Controls)
        {
            SetControlReadOnly(child);
        }
    }

    #endregion
}

/// <summary>
/// 对话框基类
/// </summary>
public abstract class BaseDialogForm : BaseForm
{
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        
        // 设置对话框默认大小
        if (this.Size.Width < 500)
        {
            this.Size = UIStyleHelper.SizeDialog;
        }
        
        // 设置对话框样式
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
    }
}
