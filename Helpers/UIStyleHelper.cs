using System.Drawing.Drawing2D;

namespace DieMaking.Helpers;

/// <summary>
/// UI样式统一帮助类
/// </summary>
public static class UIStyleHelper
{
    // 颜色定义
    public static readonly Color ColorPrimary = Color.FromArgb(0, 122, 204);      // 主色调 - 蓝色
    public static readonly Color ColorSuccess = Color.FromArgb(76, 175, 80);      // 成功 - 绿色
    public static readonly Color ColorWarning = Color.FromArgb(255, 152, 0);      // 警告 - 橙色
    public static readonly Color ColorDanger = Color.FromArgb(244, 67, 54);       // 危险 - 红色
    public static readonly Color ColorInfo = Color.FromArgb(33, 150, 243);        // 信息 - 蓝色
    public static readonly Color ColorPurple = Color.FromArgb(156, 39, 176);      // 紫色
    public static readonly Color ColorGray = Color.FromArgb(158, 158, 158);       // 灰色
    public static readonly Color ColorLightGray = Color.FromArgb(240, 240, 240);  // 浅灰色
    public static readonly Color ColorAlternateRow = Color.FromArgb(245, 245, 245); // 交替行颜色

    // 字体定义
    public static readonly string FontName = "微软雅黑";
    public static readonly float FontSizeNormal = 9f;
    public static readonly float FontSizeTitle = 12f;
    public static readonly float FontSizeLarge = 14f;

    // 窗体尺寸定义
    public static readonly Size SizeListForm = new Size(1200, 700);
    public static readonly Size SizeEditForm = new Size(800, 600);
    public static readonly Size SizeDialog = new Size(600, 400);

    // 按钮尺寸
    public static readonly Size SizeButton = new Size(100, 30);

    // 表格行高
    public const int DataGridRowHeight = 25;

    /// <summary>
    /// 应用统一字体到窗体
    /// </summary>
    public static void ApplyFont(Form form)
    {
        form.Font = new Font(FontName, FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134);
    }

    /// <summary>
    /// 获取标题字体
    /// </summary>
    public static Font GetTitleFont()
    {
        return new Font(FontName, FontSizeTitle, FontStyle.Bold, GraphicsUnit.Point, 134);
    }

    /// <summary>
    /// 获取大标题字体
    /// </summary>
    public static Font GetLargeTitleFont()
    {
        return new Font(FontName, FontSizeLarge, FontStyle.Bold, GraphicsUnit.Point, 134);
    }

    /// <summary>
    /// 创建标准按钮（新增）
    /// </summary>
    public static Button CreateAddButton(string text = "新增")
    {
        return CreateStyledButton(text, ColorSuccess, "+");
    }

    /// <summary>
    /// 创建标准按钮（编辑）
    /// </summary>
    public static Button CreateEditButton(string text = "编辑")
    {
        return CreateStyledButton(text, ColorInfo, "✎");
    }

    /// <summary>
    /// 创建标准按钮（删除）
    /// </summary>
    public static Button CreateDeleteButton(string text = "删除")
    {
        return CreateStyledButton(text, ColorDanger, "✕");
    }

    /// <summary>
    /// 创建标准按钮（保存）
    /// </summary>
    public static Button CreateSaveButton(string text = "保存")
    {
        return CreateStyledButton(text, ColorSuccess, "✓");
    }

    /// <summary>
    /// 创建标准按钮（取消）
    /// </summary>
    public static Button CreateCancelButton(string text = "取消")
    {
        return CreateStyledButton(text, ColorGray, null);
    }

    /// <summary>
    /// 创建标准按钮（查询）
    /// </summary>
    public static Button CreateSearchButton(string text = "查询")
    {
        return CreateStyledButton(text, ColorInfo, "🔍");
    }

    /// <summary>
    /// 创建标准按钮（导出）
    /// </summary>
    public static Button CreateExportButton(string text = "导出")
    {
        return CreateStyledButton(text, ColorWarning, null);
    }

    /// <summary>
    /// 创建标准按钮（打印）
    /// </summary>
    public static Button CreatePrintButton(string text = "打印")
    {
        return CreateStyledButton(text, ColorPurple, null);
    }

    /// <summary>
    /// 创建样式化按钮
    /// </summary>
    private static Button CreateStyledButton(string text, Color color, string? icon)
    {
        var button = new Button
        {
            Text = icon != null ? $"{icon} {text}" : text,
            Size = SizeButton,
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font(FontName, FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.BorderColor = color;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.1f);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(color, 0.1f);
        return button;
    }

    /// <summary>
    /// 应用按钮样式
    /// </summary>
    public static void ApplyButtonStyle(Button button, ButtonStyle style)
    {
        Color color = style switch
        {
            ButtonStyle.Add => ColorSuccess,
            ButtonStyle.Edit => ColorInfo,
            ButtonStyle.Delete => ColorDanger,
            ButtonStyle.Save => ColorSuccess,
            ButtonStyle.Cancel => ColorGray,
            ButtonStyle.Search => ColorInfo,
            ButtonStyle.Export => ColorWarning,
            ButtonStyle.Print => ColorPurple,
            _ => ColorPrimary
        };

        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = color;
        button.ForeColor = Color.White;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.BorderColor = color;
        button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.1f);
        button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(color, 0.1f);
        button.Cursor = Cursors.Hand;

        // 添加图标前缀
        string? icon = style switch
        {
            ButtonStyle.Add => "+",
            ButtonStyle.Edit => "✎",
            ButtonStyle.Delete => "✕",
            ButtonStyle.Save => "✓",
            _ => null
        };

        if (icon != null && !button.Text.StartsWith(icon))
        {
            button.Text = $"{icon} {button.Text}";
        }
    }

    /// <summary>
    /// 配置DataGridView样式
    /// </summary>
    public static void ConfigureDataGridView(DataGridView dgv)
    {
        // 基础设置
        dgv.AutoGenerateColumns = false;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.ReadOnly = true;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.MultiSelect = false;
        dgv.BackgroundColor = Color.White;
        dgv.BorderStyle = BorderStyle.None;
        dgv.GridColor = Color.LightGray;
        dgv.RowHeadersVisible = true;
        dgv.RowHeadersWidth = 50;

        // 行高设置
        dgv.RowTemplate.Height = DataGridRowHeight;

        // 交替行颜色
        dgv.AlternatingRowsDefaultCellStyle.BackColor = ColorAlternateRow;

        // 列标题样式
        dgv.ColumnHeadersDefaultCellStyle.BackColor = ColorPrimary;
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font(FontName, FontSizeNormal, FontStyle.Bold, GraphicsUnit.Point, 134);
        dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dgv.ColumnHeadersHeight = 30;
        dgv.EnableHeadersVisualStyles = false;

        // 选中行样式
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(51, 153, 255);
        dgv.DefaultCellStyle.SelectionForeColor = Color.White;

        // 字体
        dgv.DefaultCellStyle.Font = new Font(FontName, FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134);

        // 添加行号
        dgv.RowPostPaint += (sender, e) =>
        {
            var grid = sender as DataGridView;
            if (grid == null) return;

            var rowIdx = (e.RowIndex + 1).ToString();
            var centerFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(rowIdx, grid.Font, SystemBrushes.ControlText, headerBounds, centerFormat);
        };

        // 启用双缓冲减少闪烁
        EnableDoubleBuffering(dgv);
    }

    /// <summary>
    /// 为DataGridView添加右键菜单
    /// </summary>
    public static ContextMenuStrip CreateDataGridViewContextMenu(
        Action? onView = null,
        Action? onEdit = null,
        Action? onDelete = null)
    {
        var contextMenu = new ContextMenuStrip();

        if (onView != null)
        {
            var viewItem = new ToolStripMenuItem("查看详情", null, (s, e) => onView());
            viewItem.ShortcutKeyDisplayString = "Enter";
            contextMenu.Items.Add(viewItem);
        }

        if (onEdit != null)
        {
            var editItem = new ToolStripMenuItem("编辑", null, (s, e) => onEdit());
            editItem.ShortcutKeyDisplayString = "Ctrl+E";
            contextMenu.Items.Add(editItem);
        }

        if (onDelete != null)
        {
            var deleteItem = new ToolStripMenuItem("删除", null, (s, e) => onDelete());
            deleteItem.ShortcutKeyDisplayString = "Del";
            contextMenu.Items.Add(deleteItem);
        }

        return contextMenu;
    }

    /// <summary>
    /// 启用双缓冲
    /// </summary>
    private static void EnableDoubleBuffering(Control control)
    {
        var property = typeof(Control).GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        property?.SetValue(control, true, null);
    }

    /// <summary>
    /// 创建状态栏
    /// </summary>
    public static StatusStrip CreateStatusStrip(out ToolStripStatusLabel userLabel, out ToolStripStatusLabel timeLabel)
    {
        var statusStrip = new StatusStrip
        {
            BackColor = ColorLightGray,
            Font = new Font(FontName, 8f, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        userLabel = new ToolStripStatusLabel
        {
            Text = $"当前用户：{CurrentUser.User?.RealName ?? CurrentUser.User?.Username ?? "未登录"}",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        timeLabel = new ToolStripStatusLabel
        {
            Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            TextAlign = ContentAlignment.MiddleRight
        };

        statusStrip.Items.Add(userLabel);
        statusStrip.Items.Add(new ToolStripSeparator());
        statusStrip.Items.Add(timeLabel);

        // 启动定时器更新时间
        var timer = new Timer { Interval = 1000 };
        timer.Tick += (s, e) => timeLabel.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        timer.Start();

        return statusStrip;
    }

    /// <summary>
    /// 显示操作成功提示（右下角弹出）
    /// </summary>
    public static void ShowSuccessToast(Form parent, string message)
    {
        var toast = new Form
        {
            Size = new Size(250, 60),
            StartPosition = FormStartPosition.Manual,
            Location = new Point(
                parent.Location.X + parent.Width - 270,
                parent.Location.Y + parent.Height - 100
            ),
            FormBorderStyle = FormBorderStyle.None,
            BackColor = ColorSuccess,
            ShowInTaskbar = false,
            Opacity = 0
        };

        var label = new Label
        {
            Text = message,
            ForeColor = Color.White,
            Font = new Font(FontName, 10f, FontStyle.Bold, GraphicsUnit.Point, 134),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        toast.Controls.Add(label);

        toast.Show(parent);

        // 淡入淡出动画
        var fadeIn = new Timer { Interval = 20 };
        double opacity = 0;
        fadeIn.Tick += (s, e) =>
        {
            opacity += 0.1;
            toast.Opacity = opacity;
            if (opacity >= 1)
            {
                fadeIn.Stop();
                // 2秒后淡出
                var closeTimer = new Timer { Interval = 2000 };
                closeTimer.Tick += (s2, e2) =>
                {
                    closeTimer.Stop();
                    var fadeOut = new Timer { Interval = 20 };
                    fadeOut.Tick += (s3, e3) =>
                    {
                        opacity -= 0.1;
                        toast.Opacity = opacity;
                        if (opacity <= 0)
                        {
                            fadeOut.Stop();
                            toast.Close();
                        }
                    };
                    fadeOut.Start();
                };
                closeTimer.Start();
            }
        };
        fadeIn.Start();
    }

    /// <summary>
    /// 显示加载动画
    /// </summary>
    public static Form ShowLoading(Form parent, string message = "正在加载...")
    {
        var loadingForm = new Form
        {
            Size = new Size(300, 100),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.None,
            BackColor = Color.White,
            ShowInTaskbar = false,
            Owner = parent
        };

        // 添加阴影效果
        loadingForm.Paint += (s, e) =>
        {
            var rect = new Rectangle(0, 0, loadingForm.Width - 1, loadingForm.Height - 1);
            using var pen = new Pen(Color.LightGray, 2);
            e.Graphics.DrawRectangle(pen, rect);
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var progressBar = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30,
            Dock = DockStyle.Top,
            Height = 10
        };

        var label = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(FontName, 10f, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        panel.Controls.Add(label);
        panel.Controls.Add(progressBar);
        loadingForm.Controls.Add(panel);

        loadingForm.Show();
        loadingForm.Refresh();

        return loadingForm;
    }

    /// <summary>
    /// 设置输入框提示文字
    /// </summary>
    public static void SetPlaceholder(TextBox textBox, string placeholder)
    {
        textBox.Tag = placeholder;
        textBox.Text = placeholder;
        textBox.ForeColor = Color.Gray;

        textBox.Enter += (s, e) =>
        {
            if (textBox.Text == placeholder)
            {
                textBox.Text = "";
                textBox.ForeColor = Color.Black;
            }
        };

        textBox.Leave += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = placeholder;
                textBox.ForeColor = Color.Gray;
            }
        };
    }

    /// <summary>
    /// 设置数据验证错误样式
    /// </summary>
    public static void SetValidationError(Control control, bool hasError)
    {
        if (hasError)
        {
            control.BackColor = Color.FromArgb(255, 235, 238);
            control.ForeColor = ColorDanger;
        }
        else
        {
            control.BackColor = Color.White;
            control.ForeColor = Color.Black;
        }
    }

    /// <summary>
    /// 创建分组框
    /// </summary>
    public static GroupBox CreateGroupBox(string title, Point location, Size size)
    {
        return new GroupBox
        {
            Text = title,
            Location = location,
            Size = size,
            Font = new Font(FontName, FontSizeNormal, FontStyle.Bold, GraphicsUnit.Point, 134)
        };
    }

    /// <summary>
    /// 创建标签
    /// </summary>
    public static Label CreateLabel(string text, Point location, Size size)
    {
        return new Label
        {
            Text = text,
            Location = location,
            Size = size,
            Font = new Font(FontName, FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    /// <summary>
    /// 创建文本框
    /// </summary>
    public static TextBox CreateTextBox(Point location, Size size, string? placeholder = null)
    {
        var textBox = new TextBox
        {
            Location = location,
            Size = size,
            Font = new Font(FontName, FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        if (!string.IsNullOrEmpty(placeholder))
        {
            SetPlaceholder(textBox, placeholder);
        }

        return textBox;
    }
}

/// <summary>
/// 按钮样式枚举
/// </summary>
public enum ButtonStyle
{
    Default,
    Add,
    Edit,
    Delete,
    Save,
    Cancel,
    Search,
    Export,
    Print
}
