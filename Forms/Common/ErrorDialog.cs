using DieMaking.Helpers;
using System.Text;

namespace DieMaking.Forms;

/// <summary>
/// 全局错误提示对话框
/// </summary>
public partial class ErrorDialog : Form
{
    private ExceptionHandleResult _result = null!;
    private Panel _detailPanel = null!;
    private TextBox _detailTextBox = null!;
    private bool _isDetailExpanded = false;
    private const int DetailHeight = 200;

    private ErrorDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // 设置窗体基本属性
        this.Text = "系统提示";
        this.Size = new Size(500, 220);
        this.MinimumSize = new Size(400, 200);
        this.MaximumSize = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Icon = SystemIcons.Warning;
        this.Padding = new Padding(15);

        // 创建主布局面板
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            AutoSize = true
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 图标和消息行
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 10)); // 间距
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 详情面板
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 按钮行

        // 创建图标和消息区域
        var messagePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true
        };
        messagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        messagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        messagePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        messagePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // 错误图标
        var iconPictureBox = new PictureBox
        {
            Image = SystemIcons.Warning.ToBitmap(),
            Size = new Size(48, 48),
            SizeMode = PictureBoxSizeMode.StretchImage,
            Margin = new Padding(0, 0, 15, 0)
        };
        messagePanel.SetColumnSpan(iconPictureBox, 1);
        messagePanel.SetRowSpan(iconPictureBox, 2);
        messagePanel.Controls.Add(iconPictureBox, 0, 0);

        // 错误消息标题
        var titleLabel = new Label
        {
            Text = "操作失败",
            Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 5, 0, 5)
        };
        messagePanel.Controls.Add(titleLabel, 1, 0);

        // 错误消息内容
        var messageLabel = new Label
        {
            Name = "messageLabel",
            Text = "发生错误",
            Font = new Font(this.Font.FontFamily, 9),
            AutoSize = true,
            Dock = DockStyle.Fill,
            MaximumSize = new Size(400, 0),
            Margin = new Padding(0, 0, 0, 5)
        };
        messagePanel.Controls.Add(messageLabel, 1, 1);

        mainPanel.Controls.Add(messagePanel, 0, 0);

        // 详情面板（初始隐藏）
        _detailPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 0,
            Visible = false,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 5, 0, 5)
        };

        _detailTextBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9),
            BackColor = SystemColors.Window,
            WordWrap = false
        };
        _detailPanel.Controls.Add(_detailTextBox);

        mainPanel.Controls.Add(_detailPanel, 0, 2);

        // 按钮区域
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };

        // 取消/关闭按钮
        var closeButton = new Button
        {
            Text = "关闭",
            DialogResult = DialogResult.Cancel,
            Size = new Size(80, 30),
            Margin = new Padding(5, 0, 0, 0)
        };
        closeButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
        buttonPanel.Controls.Add(closeButton);

        // 重试按钮
        var retryButton = new Button
        {
            Name = "retryButton",
            Text = "重试",
            DialogResult = DialogResult.Retry,
            Size = new Size(80, 30),
            Margin = new Padding(5, 0, 0, 0),
            Visible = false
        };
        buttonPanel.Controls.Add(retryButton);

        // 复制按钮
        var copyButton = new Button
        {
            Text = "复制错误信息",
            Size = new Size(100, 30),
            Margin = new Padding(5, 0, 0, 0)
        };
        copyButton.Click += CopyButton_Click;
        buttonPanel.Controls.Add(copyButton);

        // 详情按钮
        var detailButton = new Button
        {
            Name = "detailButton",
            Text = "显示详情 ▼",
            Size = new Size(90, 30),
            Margin = new Padding(5, 0, 0, 0)
        };
        detailButton.Click += DetailButton_Click;
        buttonPanel.Controls.Add(detailButton);

        mainPanel.Controls.Add(buttonPanel, 0, 3);

        this.Controls.Add(mainPanel);

        // 设置默认按钮
        this.AcceptButton = closeButton;
        this.CancelButton = closeButton;
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    public static DialogResult ShowError(IWin32Window? owner, ExceptionHandleResult result)
    {
        using var dialog = new ErrorDialog();
        dialog._result = result;
        dialog.SetupDialog();
        return dialog.ShowDialog(owner);
    }

    /// <summary>
    /// 显示错误对话框（简化版）
    /// </summary>
    public static DialogResult ShowError(string message, string? details = null, bool canRetry = false)
    {
        var result = new ExceptionHandleResult
        {
            UserMessage = message,
            TechnicalDetails = details ?? string.Empty,
            CanRetry = canRetry,
            LogId = string.Empty,
            ExceptionType = ExceptionType.System
        };
        return ShowError(null, result);
    }

    /// <summary>
    /// 设置对话框内容
    /// </summary>
    private void SetupDialog()
    {
        // 设置消息
        var messageLabel = this.Controls.Find("messageLabel", true).FirstOrDefault() as Label;
        if (messageLabel != null)
        {
            messageLabel.Text = _result.UserMessage;
        }

        // 设置详情
        var detailBuilder = new StringBuilder();
        detailBuilder.AppendLine($"日志ID: {_result.LogId}");
        detailBuilder.AppendLine($"异常类型: {_result.ExceptionType}");
        detailBuilder.AppendLine($"发生时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        detailBuilder.AppendLine();
        detailBuilder.AppendLine("技术详情:");
        detailBuilder.AppendLine(_result.TechnicalDetails);
        _detailTextBox.Text = detailBuilder.ToString();

        // 设置重试按钮可见性
        var retryButton = this.Controls.Find("retryButton", true).FirstOrDefault() as Button;
        if (retryButton != null)
        {
            retryButton.Visible = _result.CanRetry;
        }

        // 根据消息长度调整窗体高度
        AdjustFormHeight();
    }

    /// <summary>
    /// 调整窗体高度
    /// </summary>
    private void AdjustFormHeight()
    {
        var messageLabel = this.Controls.Find("messageLabel", true).FirstOrDefault() as Label;
        if (messageLabel != null)
        {
            // 测量文本所需高度
            using var g = messageLabel.CreateGraphics();
            var size = g.MeasureString(messageLabel.Text, messageLabel.Font, messageLabel.MaximumSize.Width);
            var requiredHeight = (int)Math.Ceiling(size.Height) + 150; // 基础高度 + 消息高度 + 按钮区域
            
            if (_isDetailExpanded)
            {
                requiredHeight += DetailHeight + 20;
            }

            this.Height = Math.Min(Math.Max(requiredHeight, 200), 500);
        }
    }

    /// <summary>
    /// 详情按钮点击事件
    /// </summary>
    private void DetailButton_Click(object? sender, EventArgs e)
    {
        _isDetailExpanded = !_isDetailExpanded;
        
        var detailButton = sender as Button;
        if (detailButton != null)
        {
            detailButton.Text = _isDetailExpanded ? "隐藏详情 ▲" : "显示详情 ▼";
        }

        _detailPanel.Visible = _isDetailExpanded;
        _detailPanel.Height = _isDetailExpanded ? DetailHeight : 0;

        AdjustFormHeight();
    }

    /// <summary>
    /// 复制按钮点击事件
    /// </summary>
    private void CopyButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var clipboardText = new StringBuilder();
            clipboardText.AppendLine($"日志ID: {_result.LogId}");
            clipboardText.AppendLine($"错误信息: {_result.UserMessage}");
            clipboardText.AppendLine();
            clipboardText.AppendLine("技术详情:");
            clipboardText.AppendLine(_result.TechnicalDetails);

            Clipboard.SetText(clipboardText.ToString());
            
            // 显示复制成功提示
            var copyButton = sender as Button;
            if (copyButton != null)
            {
                var originalText = copyButton.Text;
                copyButton.Text = "已复制!";
                copyButton.Enabled = false;
                
                Task.Delay(1500).ContinueWith(_ =>
                {
                    this.Invoke(() =>
                    {
                        copyButton.Text = originalText;
                        copyButton.Enabled = true;
                    });
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"复制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
