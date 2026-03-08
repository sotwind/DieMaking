namespace DieMaking.Helpers;

/// <summary>
/// 快捷键处理帮助类
/// </summary>
public static class ShortcutHelper
{
    /// <summary>
    /// 注册窗体快捷键
    /// </summary>
    public static void RegisterFormShortcuts(Form form, ShortcutActions actions)
    {
        form.KeyPreview = true;
        form.KeyDown += (sender, e) =>
        {
            switch (e.KeyCode)
            {
                case Keys.F5:
                    if (actions.OnRefresh != null)
                    {
                        e.Handled = true;
                        actions.OnRefresh();
                    }
                    break;

                case Keys.S when e.Control:
                    if (actions.OnSave != null)
                    {
                        e.Handled = true;
                        actions.OnSave();
                    }
                    break;

                case Keys.N when e.Control:
                    if (actions.OnAdd != null)
                    {
                        e.Handled = true;
                        actions.OnAdd();
                    }
                    break;

                case Keys.F when e.Control:
                    if (actions.OnSearch != null)
                    {
                        e.Handled = true;
                        actions.OnSearch();
                    }
                    break;

                case Keys.P when e.Control:
                    if (actions.OnPrint != null)
                    {
                        e.Handled = true;
                        actions.OnPrint();
                    }
                    break;

                case Keys.E when e.Control:
                    if (actions.OnExport != null)
                    {
                        e.Handled = true;
                        actions.OnExport();
                    }
                    break;

                case Keys.Escape:
                    if (actions.OnCancel != null)
                    {
                        e.Handled = true;
                        actions.OnCancel();
                    }
                    break;

                case Keys.Enter when actions.OnConfirm != null && form.ActiveControl is Button:
                    e.Handled = true;
                    actions.OnConfirm();
                    break;

                case Keys.F1:
                    if (actions.OnHelp != null)
                    {
                        e.Handled = true;
                        actions.OnHelp();
                    }
                    break;
            }
        };
    }

    /// <summary>
    /// 注册输入框回车跳转
    /// </summary>
    public static void RegisterEnterToNext(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            if (control is TextBox textBox && !textBox.Multiline)
            {
                textBox.KeyDown += (sender, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        parent.SelectNextControl(textBox, true, true, true, true);
                    }
                };
            }

            if (control.HasChildren)
            {
                RegisterEnterToNext(control);
            }
        }
    }

    /// <summary>
    /// 显示快捷键帮助
    /// </summary>
    public static void ShowShortcutHelp(Form parent)
    {
        var message = @"快捷键说明：

F5              - 刷新数据
Ctrl + S        - 保存
Ctrl + N        - 新增
Ctrl + F        - 搜索/筛选
Ctrl + P        - 打印
Ctrl + E        - 导出Excel/CSV
Esc             - 关闭窗体/取消操作
Enter           - 确认/保存（在输入框中）
F1              - 显示帮助

表格操作：
双击行          - 查看详情
右键            - 快捷菜单
点击表头        - 排序";

        MessageBox.Show(parent, message, "快捷键帮助", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

/// <summary>
/// 快捷键动作集合
/// </summary>
public class ShortcutActions
{
    public Action? OnRefresh { get; set; }
    public Action? OnSave { get; set; }
    public Action? OnAdd { get; set; }
    public Action? OnSearch { get; set; }
    public Action? OnPrint { get; set; }
    public Action? OnExport { get; set; }
    public Action? OnCancel { get; set; }
    public Action? OnConfirm { get; set; }
    public Action? OnHelp { get; set; }
}
