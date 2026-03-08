using DieMaking.Services;
using System.ComponentModel;

namespace DieMaking.Forms.System;

/// <summary>
/// 数据备份管理窗体
/// </summary>
public partial class BackupManageForm : Form
{
    private readonly BackupService _backupService;
    private BindingList<BackupRecordViewModel> _backupList;
    private CancellationTokenSource? _currentOperationCts;

    public BackupManageForm()
    {
        InitializeComponent();
        _backupService = new BackupService();
        _backupList = new BindingList<BackupRecordViewModel>();

        InitializeEvents();
        LoadBackupList();
    }

    /// <summary>
    /// 初始化事件
    /// </summary>
    private void InitializeEvents()
    {
        btnBackup.Click += BtnBackup_Click;
        btnRestore.Click += BtnRestore_Click;
        btnDelete.Click += BtnDelete_Click;
        btnRefresh.Click += BtnRefresh_Click;
        btnClose.Click += BtnClose_Click;
        dgvBackups.SelectionChanged += DgvBackups_SelectionChanged;
        _backupService.BackupProgress += BackupService_BackupProgress;
    }

    /// <summary>
    /// 加载备份列表
    /// </summary>
    private void LoadBackupList()
    {
        try
        {
            var records = _backupService.GetBackupList();
            _backupList.Clear();

            foreach (var record in records)
            {
                _backupList.Add(new BackupRecordViewModel
                {
                    BackupId = record.BackupId,
                    FileName = record.BackupFileName,
                    Size = record.BackupSizeText,
                    Type = GetBackupTypeText(record.BackupType),
                    StartTime = record.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = record.StatusText,
                    CreatedBy = record.CreatedBy ?? "系统"
                });
            }

            dgvBackups.DataSource = _backupList;
            UpdateStatistics();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载备份列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        try
        {
            var stats = _backupService.GetBackupStatistics();
            lblTotalCount.Text = $"总备份数: {stats.TotalCount}";
            lblSuccessCount.Text = $"成功: {stats.SuccessCount}";
            lblFailedCount.Text = $"失败: {stats.FailedCount}";
            lblTotalSize.Text = $"总大小: {FormatFileSize(stats.TotalSize)}";

            if (stats.LastBackupTime.HasValue)
            {
                lblLastBackup.Text = $"最后备份: {stats.LastBackupTime.Value:yyyy-MM-dd HH:mm:ss}";
            }
            else
            {
                lblLastBackup.Text = "最后备份: 无";
            }
        }
        catch
        {
            // 忽略错误
        }
    }

    /// <summary>
    /// 执行备份
    /// </summary>
    private async void BtnBackup_Click(object? sender, EventArgs e)
    {
        if (_currentOperationCts != null)
        {
            MessageBox.Show("已有操作正在进行中", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var dialog = new BackupDialog();
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _currentOperationCts = new CancellationTokenSource();
        var progressForm = new BackupProgressForm(_currentOperationCts);

        try
        {
            progressForm.Show(this);
            EnableControls(false);

            var result = await _backupService.BackupAsync(
                dialog.BackupName,
                dialog.BackupType,
                dialog.Compress,
                _currentOperationCts.Token);

            progressForm.Close();

            if (result.Success)
            {
                MessageBox.Show(result.Message, "备份成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBackupList();
            }
            else
            {
                MessageBox.Show(result.Message, "备份失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (OperationCanceledException)
        {
            progressForm.Close();
            MessageBox.Show("备份已取消", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            progressForm.Close();
            MessageBox.Show($"备份失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _currentOperationCts?.Dispose();
            _currentOperationCts = null;
            EnableControls(true);
        }
    }

    /// <summary>
    /// 恢复备份
    /// </summary>
    private async void BtnRestore_Click(object? sender, EventArgs e)
    {
        if (dgvBackups.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先选择一个备份文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedRow = dgvBackups.SelectedRows[0];
        var fileName = selectedRow.Cells["colFileName"].Value?.ToString();

        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        // 确认恢复
        var result = MessageBox.Show(
            $"确定要从备份 \"{fileName}\" 恢复数据库吗？\n\n警告：这将覆盖当前数据库中的所有数据！",
            "确认恢复",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        // 再次确认
        result = MessageBox.Show(
            "再次确认：此操作不可撤销，确定要继续吗？",
            "最终确认",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _currentOperationCts = new CancellationTokenSource();
        var progressForm = new BackupProgressForm(_currentOperationCts);

        try
        {
            progressForm.Show(this);
            EnableControls(false);

            var restoreResult = await _backupService.RestoreAsync(fileName, _currentOperationCts.Token);

            progressForm.Close();

            if (restoreResult.Success)
            {
                MessageBox.Show("数据库恢复成功！请重新启动应用程序。", "恢复成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
            }
            else
            {
                MessageBox.Show(restoreResult.Message, "恢复失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (OperationCanceledException)
        {
            progressForm.Close();
            MessageBox.Show("恢复已取消", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            progressForm.Close();
            MessageBox.Show($"恢复失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _currentOperationCts?.Dispose();
            _currentOperationCts = null;
            EnableControls(true);
        }
    }

    /// <summary>
    /// 删除备份
    /// </summary>
    private async void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvBackups.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先选择一个备份文件", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selectedRow = dgvBackups.SelectedRows[0];
        var backupId = Convert.ToInt32(selectedRow.Cells["colBackupId"].Value);
        var fileName = selectedRow.Cells["colFileName"].Value?.ToString();

        if (MessageBox.Show($"确定要删除备份 \"{fileName}\" 吗？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            var success = await _backupService.DeleteBackupAsync(backupId);
            if (success)
            {
                MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBackupList();
            }
            else
            {
                MessageBox.Show("删除失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"删除失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// 刷新列表
    /// </summary>
    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        LoadBackupList();
    }

    /// <summary>
    /// 关闭窗体
    /// </summary>
    private void BtnClose_Click(object? sender, EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// 选择行变更
    /// </summary>
    private void DgvBackups_SelectionChanged(object? sender, EventArgs e)
    {
        var hasSelection = dgvBackups.SelectedRows.Count > 0;
        btnRestore.Enabled = hasSelection;
        btnDelete.Enabled = hasSelection;
    }

    /// <summary>
    /// 备份进度事件
    /// </summary>
    private void BackupService_BackupProgress(object? sender, BackupProgressEventArgs e)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => BackupService_BackupProgress(sender, e)));
            return;
        }

        // 更新进度条和状态
        progressBar.Value = Math.Min(e.PercentComplete, 100);
        lblStatus.Text = e.Message;
    }

    /// <summary>
    /// 启用/禁用控件
    /// </summary>
    private void EnableControls(bool enabled)
    {
        btnBackup.Enabled = enabled;
        btnRestore.Enabled = enabled && dgvBackups.SelectedRows.Count > 0;
        btnDelete.Enabled = enabled && dgvBackups.SelectedRows.Count > 0;
        btnRefresh.Enabled = enabled;
        dgvBackups.Enabled = enabled;
    }

    /// <summary>
    /// 获取备份类型文本
    /// </summary>
    private static string GetBackupTypeText(BackupType type)
    {
        return type switch
        {
            BackupType.Full => "完整备份",
            BackupType.Differential => "差异备份",
            BackupType.Log => "日志备份",
            _ => "未知"
        };
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F2} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F2} KB";
        return $"{bytes} B";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_currentOperationCts != null)
        {
            var result = MessageBox.Show("有操作正在进行中，确定要取消并关闭吗？", "确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _currentOperationCts.Cancel();
            }
            else
            {
                e.Cancel = true;
            }
        }

        base.OnFormClosing(e);
    }
}

/// <summary>
/// 备份记录视图模型
/// </summary>
public class BackupRecordViewModel
{
    public int BackupId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// 备份对话框
/// </summary>
public class BackupDialog : Form
{
    private TextBox txtBackupName;
    private ComboBox cmbBackupType;
    private CheckBox chkCompress;
    private Button btnOK;
    private Button btnCancel;

    public string BackupName => txtBackupName.Text.Trim();
    public BackupType BackupType => (BackupType)cmbBackupType.SelectedIndex;
    public bool Compress => chkCompress.Checked;

    public BackupDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "新建备份";
        Size = new Size(400, 250);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblName = new Label { Text = "备份名称:", Location = new Point(20, 20), AutoSize = true };
        txtBackupName = new TextBox { Location = new Point(100, 17), Size = new Size(250, 25) };
        txtBackupName.Text = $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}";

        var lblType = new Label { Text = "备份类型:", Location = new Point(20, 60), AutoSize = true };
        cmbBackupType = new ComboBox { Location = new Point(100, 57), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        cmbBackupType.Items.AddRange(new object[] { "完整备份", "差异备份", "日志备份" });
        cmbBackupType.SelectedIndex = 0;

        chkCompress = new CheckBox { Text = "压缩备份文件", Location = new Point(100, 100), AutoSize = true, Checked = true };

        btnOK = new Button { Text = "确定", DialogResult = DialogResult.OK, Location = new Point(180, 150) };
        btnCancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Location = new Point(275, 150) };

        Controls.AddRange(new Control[] { lblName, txtBackupName, lblType, cmbBackupType, chkCompress, btnOK, btnCancel });

        AcceptButton = btnOK;
        CancelButton = btnCancel;
    }
}

/// <summary>
/// 备份进度窗体
/// </summary>
public class BackupProgressForm : Form
{
    private ProgressBar progressBar;
    private Label lblMessage;
    private Button btnCancel;
    private CancellationTokenSource _cts;

    public BackupProgressForm(CancellationTokenSource cts)
    {
        _cts = cts;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "正在处理...";
        Size = new Size(400, 150);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;

        progressBar = new ProgressBar { Location = new Point(20, 20), Size = new Size(350, 25), Style = ProgressBarStyle.Marquee };
        lblMessage = new Label { Text = "正在准备...", Location = new Point(20, 55), AutoSize = true };
        btnCancel = new Button { Text = "取消", Location = new Point(160, 85) };
        btnCancel.Click += (s, e) =>
        {
            lblMessage.Text = "正在取消...";
            _cts.Cancel();
            btnCancel.Enabled = false;
        };

        Controls.AddRange(new Control[] { progressBar, lblMessage, btnCancel });
    }

    public void UpdateProgress(int percent, string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<int, string>(UpdateProgress), percent, message);
            return;
        }

        progressBar.Style = ProgressBarStyle.Continuous;
        progressBar.Value = Math.Min(percent, 100);
        lblMessage.Text = message;
    }
}
