using DieMaking.Models;

namespace DieMaking.Helpers;

/// <summary>
/// 主题管理器 - 统一管理应用程序主题颜色和样式
/// </summary>
public static class ThemeManager
{
    #region 主题颜色定义

    /// <summary>
    /// 浅色主题颜色
    /// </summary>
    public static class LightTheme
    {
        public static Color BackgroundColor { get; } = Color.White;
        public static Color ForegroundColor { get; } = Color.Black;
        public static Color ControlBackColor { get; } = SystemColors.Control;
        public static Color ControlForeColor { get; } = SystemColors.ControlText;
        public static Color MenuBackColor { get; } = SystemColors.Menu;
        public static Color MenuForeColor { get; } = SystemColors.MenuText;
        public static Color GridBackColor { get; } = Color.White;
        public static Color GridForeColor { get; } = Color.Black;
        public static Color GridHeaderBackColor { get; } = Color.FromArgb(240, 240, 240);
        public static Color GridHeaderForeColor { get; } = Color.Black;
        public static Color GridAlternatingBackColor { get; } = Color.FromArgb(250, 250, 250);
        public static Color BorderColor { get; } = Color.FromArgb(200, 200, 200);
        public static Color AccentColor { get; } = Color.FromArgb(0, 120, 215);
        public static Color StatusBarBackColor { get; } = SystemColors.Control;
        public static Color StatusBarForeColor { get; } = SystemColors.ControlText;
    }

    /// <summary>
    /// 深色主题颜色
    /// </summary>
    public static class DarkTheme
    {
        public static Color BackgroundColor { get; } = Color.FromArgb(45, 45, 48);
        public static Color ForegroundColor { get; } = Color.FromArgb(241, 241, 241);
        public static Color ControlBackColor { get; } = Color.FromArgb(51, 51, 55);
        public static Color ControlForeColor { get; } = Color.FromArgb(241, 241, 241);
        public static Color MenuBackColor { get; } = Color.FromArgb(51, 51, 55);
        public static Color MenuForeColor { get; } = Color.FromArgb(241, 241, 241);
        public static Color GridBackColor { get; } = Color.FromArgb(45, 45, 48);
        public static Color GridForeColor { get; } = Color.FromArgb(241, 241, 241);
        public static Color GridHeaderBackColor { get; } = Color.FromArgb(63, 63, 70);
        public static Color GridHeaderForeColor { get; } = Color.FromArgb(241, 241, 241);
        public static Color GridAlternatingBackColor { get; } = Color.FromArgb(51, 51, 55);
        public static Color BorderColor { get; } = Color.FromArgb(100, 100, 100);
        public static Color AccentColor { get; } = Color.FromArgb(0, 122, 204);
        public static Color StatusBarBackColor { get; } = Color.FromArgb(0, 122, 204);
        public static Color StatusBarForeColor { get; } = Color.White;
    }

    #endregion

    #region 当前主题

    private static string _currentTheme = "Light";

    /// <summary>
    /// 当前主题名称
    /// </summary>
    public static string CurrentTheme
    {
        get => _currentTheme;
        private set => _currentTheme = value;
    }

    /// <summary>
    /// 是否为深色主题
    /// </summary>
    public static bool IsDarkTheme => CurrentTheme == "Dark";

    /// <summary>
    /// 获取当前主题的背景色
    /// </summary>
    public static Color BackgroundColor => IsDarkTheme ? DarkTheme.BackgroundColor : LightTheme.BackgroundColor;

    /// <summary>
    /// 获取当前主题的前景色
    /// </summary>
    public static Color ForegroundColor => IsDarkTheme ? DarkTheme.ForegroundColor : LightTheme.ForegroundColor;

    /// <summary>
    /// 获取当前主题的控件背景色
    /// </summary>
    public static Color ControlBackColor => IsDarkTheme ? DarkTheme.ControlBackColor : LightTheme.ControlBackColor;

    /// <summary>
    /// 获取当前主题的控件前景色
    /// </summary>
    public static Color ControlForeColor => IsDarkTheme ? DarkTheme.ControlForeColor : LightTheme.ControlForeColor;

    /// <summary>
    /// 获取当前主题的菜单背景色
    /// </summary>
    public static Color MenuBackColor => IsDarkTheme ? DarkTheme.MenuBackColor : LightTheme.MenuBackColor;

    /// <summary>
    /// 获取当前主题的菜单前景色
    /// </summary>
    public static Color MenuForeColor => IsDarkTheme ? DarkTheme.MenuForeColor : LightTheme.MenuForeColor;

    /// <summary>
    /// 获取当前主题的表格背景色
    /// </summary>
    public static Color GridBackColor => IsDarkTheme ? DarkTheme.GridBackColor : LightTheme.GridBackColor;

    /// <summary>
    /// 获取当前主题的表格前景色
    /// </summary>
    public static Color GridForeColor => IsDarkTheme ? DarkTheme.GridForeColor : LightTheme.GridForeColor;

    /// <summary>
    /// 获取当前主题的表格表头背景色
    /// </summary>
    public static Color GridHeaderBackColor => IsDarkTheme ? DarkTheme.GridHeaderBackColor : LightTheme.GridHeaderBackColor;

    /// <summary>
    /// 获取当前主题的表格表头前景色
    /// </summary>
    public static Color GridHeaderForeColor => IsDarkTheme ? DarkTheme.GridHeaderForeColor : LightTheme.GridHeaderForeColor;

    /// <summary>
    /// 获取当前主题的表格交替行背景色
    /// </summary>
    public static Color GridAlternatingBackColor => IsDarkTheme ? DarkTheme.GridAlternatingBackColor : LightTheme.GridAlternatingBackColor;

    /// <summary>
    /// 获取当前主题的边框颜色
    /// </summary>
    public static Color BorderColor => IsDarkTheme ? DarkTheme.BorderColor : LightTheme.BorderColor;

    /// <summary>
    /// 获取当前主题的主题色
    /// </summary>
    public static Color AccentColor => IsDarkTheme ? DarkTheme.AccentColor : LightTheme.AccentColor;

    #endregion

    #region 事件

    /// <summary>
    /// 主题变更事件
    /// </summary>
    public static event EventHandler? ThemeChanged;

    #endregion

    #region 主题应用方法

    /// <summary>
    /// 设置主题
    /// </summary>
    public static void SetTheme(string theme)
    {
        if (theme != "Light" && theme != "Dark")
            theme = "Light";

        if (CurrentTheme != theme)
        {
            CurrentTheme = theme;
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 从用户配置加载主题
    /// </summary>
    public static void LoadThemeFromUserPreference()
    {
        var theme = UserConfigContext.GetTheme();
        SetTheme(theme);
    }

    /// <summary>
    /// 应用主题到窗体及其所有控件
    /// </summary>
    public static void ApplyTheme(Form form)
    {
        if (form == null) return;

        // 应用窗体背景色
        form.BackColor = BackgroundColor;
        form.ForeColor = ForegroundColor;

        // 递归应用主题到所有控件
        foreach (Control control in form.Controls)
        {
            ApplyThemeToControl(control);
        }
    }

    /// <summary>
    /// 应用主题到控件
    /// </summary>
    private static void ApplyThemeToControl(Control control)
    {
        if (control == null) return;

        switch (control)
        {
            case MenuStrip menuStrip:
                ApplyThemeToMenuStrip(menuStrip);
                break;

            case StatusStrip statusStrip:
                ApplyThemeToStatusStrip(statusStrip);
                break;

            case DataGridView dataGridView:
                ApplyThemeToDataGridView(dataGridView);
                break;

            case Button button:
                ApplyThemeToButton(button);
                break;

            case TextBox textBox:
                ApplyThemeToTextBox(textBox);
                break;

            case ComboBox comboBox:
                ApplyThemeToComboBox(comboBox);
                break;

            case Label label:
                ApplyThemeToLabel(label);
                break;

            case GroupBox groupBox:
                ApplyThemeToGroupBox(groupBox);
                break;

            case Panel panel:
                ApplyThemeToPanel(panel);
                break;

            case TabControl tabControl:
                ApplyThemeToTabControl(tabControl);
                break;

            case ListBox listBox:
                ApplyThemeToListBox(listBox);
                break;

            case CheckBox checkBox:
                ApplyThemeToCheckBox(checkBox);
                break;

            case RadioButton radioButton:
                ApplyThemeToRadioButton(radioButton);
                break;

            case NumericUpDown numericUpDown:
                ApplyThemeToNumericUpDown(numericUpDown);
                break;

            case DateTimePicker dateTimePicker:
                ApplyThemeToDateTimePicker(dateTimePicker);
                break;

            default:
                control.BackColor = ControlBackColor;
                control.ForeColor = ControlForeColor;
                break;
        }

        // 递归处理子控件
        foreach (Control child in control.Controls)
        {
            ApplyThemeToControl(child);
        }
    }

    private static void ApplyThemeToMenuStrip(MenuStrip menuStrip)
    {
        menuStrip.BackColor = MenuBackColor;
        menuStrip.ForeColor = MenuForeColor;
        menuStrip.Renderer = IsDarkTheme ? new DarkToolStripRenderer() : new ToolStripProfessionalRenderer();

        foreach (ToolStripMenuItem item in menuStrip.Items)
        {
            ApplyThemeToMenuItem(item);
        }
    }

    private static void ApplyThemeToMenuItem(ToolStripMenuItem item)
    {
        item.BackColor = MenuBackColor;
        item.ForeColor = MenuForeColor;

        foreach (ToolStripItem subItem in item.DropDownItems)
        {
            if (subItem is ToolStripMenuItem menuItem)
            {
                ApplyThemeToMenuItem(menuItem);
            }
            else
            {
                subItem.BackColor = MenuBackColor;
                subItem.ForeColor = MenuForeColor;
            }
        }
    }

    private static void ApplyThemeToStatusStrip(StatusStrip statusStrip)
    {
        statusStrip.BackColor = IsDarkTheme ? DarkTheme.StatusBarBackColor : LightTheme.StatusBarBackColor;
        statusStrip.ForeColor = IsDarkTheme ? DarkTheme.StatusBarForeColor : LightTheme.StatusBarForeColor;

        foreach (ToolStripItem item in statusStrip.Items)
        {
            item.BackColor = statusStrip.BackColor;
            item.ForeColor = statusStrip.ForeColor;
        }
    }

    private static void ApplyThemeToDataGridView(DataGridView dataGridView)
    {
        dataGridView.BackgroundColor = GridBackColor;
        dataGridView.ForeColor = GridForeColor;
        dataGridView.GridColor = BorderColor;

        // 默认单元格样式
        dataGridView.DefaultCellStyle.BackColor = GridBackColor;
        dataGridView.DefaultCellStyle.ForeColor = GridForeColor;
        dataGridView.DefaultCellStyle.SelectionBackColor = AccentColor;
        dataGridView.DefaultCellStyle.SelectionForeColor = Color.White;

        // 交替行样式
        dataGridView.AlternatingRowsDefaultCellStyle.BackColor = GridAlternatingBackColor;
        dataGridView.AlternatingRowsDefaultCellStyle.ForeColor = GridForeColor;

        // 列标题样式
        dataGridView.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBackColor;
        dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderForeColor;
        dataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderBackColor;
        dataGridView.EnableHeadersVisualStyles = false;

        // 行标题样式
        dataGridView.RowHeadersDefaultCellStyle.BackColor = GridHeaderBackColor;
        dataGridView.RowHeadersDefaultCellStyle.ForeColor = GridHeaderForeColor;
    }

    private static void ApplyThemeToButton(Button button)
    {
        button.BackColor = IsDarkTheme ? Color.FromArgb(63, 63, 70) : SystemColors.Control;
        button.ForeColor = ForegroundColor;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = BorderColor;
    }

    private static void ApplyThemeToTextBox(TextBox textBox)
    {
        textBox.BackColor = IsDarkTheme ? Color.FromArgb(63, 63, 70) : Color.White;
        textBox.ForeColor = ForegroundColor;
        textBox.BorderStyle = BorderStyle.FixedSingle;
    }

    private static void ApplyThemeToComboBox(ComboBox comboBox)
    {
        comboBox.BackColor = IsDarkTheme ? Color.FromArgb(63, 63, 70) : Color.White;
        comboBox.ForeColor = ForegroundColor;
        comboBox.FlatStyle = FlatStyle.Flat;
    }

    private static void ApplyThemeToLabel(Label label)
    {
        // 标签通常使用父控件背景，只改变前景色
        label.ForeColor = ForegroundColor;
    }

    private static void ApplyThemeToGroupBox(GroupBox groupBox)
    {
        groupBox.BackColor = BackgroundColor;
        groupBox.ForeColor = ForegroundColor;
    }

    private static void ApplyThemeToPanel(Panel panel)
    {
        panel.BackColor = ControlBackColor;
        panel.ForeColor = ControlForeColor;
    }

    private static void ApplyThemeToTabControl(TabControl tabControl)
    {
        tabControl.BackColor = BackgroundColor;
        tabControl.ForeColor = ForegroundColor;

        foreach (TabPage tabPage in tabControl.TabPages)
        {
            tabPage.BackColor = BackgroundColor;
            tabPage.ForeColor = ForegroundColor;
        }
    }

    private static void ApplyThemeToListBox(ListBox listBox)
    {
        listBox.BackColor = IsDarkTheme ? Color.FromArgb(63, 63, 70) : Color.White;
        listBox.ForeColor = ForegroundColor;
        listBox.BorderStyle = BorderStyle.FixedSingle;
    }

    private static void ApplyThemeToCheckBox(CheckBox checkBox)
    {
        checkBox.BackColor = BackgroundColor;
        checkBox.ForeColor = ForegroundColor;
    }

    private static void ApplyThemeToRadioButton(RadioButton radioButton)
    {
        radioButton.BackColor = BackgroundColor;
        radioButton.ForeColor = ForegroundColor;
    }

    private static void ApplyThemeToNumericUpDown(NumericUpDown numericUpDown)
    {
        numericUpDown.BackColor = IsDarkTheme ? Color.FromArgb(63, 63, 70) : Color.White;
        numericUpDown.ForeColor = ForegroundColor;
        numericUpDown.BorderStyle = BorderStyle.FixedSingle;
    }

    private static void ApplyThemeToDateTimePicker(DateTimePicker dateTimePicker)
    {
        dateTimePicker.BackColor = IsDarkTheme ? Color.FromArgb(63, 63, 70) : Color.White;
        dateTimePicker.ForeColor = ForegroundColor;
    }

    #endregion
}

/// <summary>
/// 深色主题 ToolStrip 渲染器
/// </summary>
public class DarkToolStripRenderer : ToolStripProfessionalRenderer
{
    public DarkToolStripRenderer() : base(new DarkColorTable()) { }
}

/// <summary>
/// 深色主题颜色表
/// </summary>
public class DarkColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => Color.FromArgb(100, 100, 100);
    public override Color MenuItemBorder => Color.FromArgb(0, 122, 204);
    public override Color MenuItemSelected => Color.FromArgb(63, 63, 70);
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(63, 63, 70);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(63, 63, 70);
    public override Color MenuItemPressedGradientBegin => Color.FromArgb(51, 51, 55);
    public override Color MenuItemPressedGradientEnd => Color.FromArgb(51, 51, 55);
    public override Color MenuStripGradientBegin => Color.FromArgb(51, 51, 55);
    public override Color MenuStripGradientEnd => Color.FromArgb(51, 51, 55);
    public override Color ToolStripDropDownBackground => Color.FromArgb(51, 51, 55);
    public override Color ImageMarginGradientBegin => Color.FromArgb(51, 51, 55);
    public override Color ImageMarginGradientMiddle => Color.FromArgb(51, 51, 55);
    public override Color ImageMarginGradientEnd => Color.FromArgb(51, 51, 55);
}
