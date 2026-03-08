namespace DieMaking.Forms.System;

partial class BackupManageForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.panelTop = new Panel();
        this.groupBoxStats = new GroupBox();
        this.lblLastBackup = new Label();
        this.lblTotalSize = new Label();
        this.lblFailedCount = new Label();
        this.lblSuccessCount = new Label();
        this.lblTotalCount = new Label();
        this.panelButtons = new Panel();
        this.btnClose = new Button();
        this.btnRefresh = new Button();
        this.btnDelete = new Button();
        this.btnRestore = new Button();
        this.btnBackup = new Button();
        this.dgvBackups = new DataGridView();
        this.colBackupId = new DataGridViewTextBoxColumn();
        this.colFileName = new DataGridViewTextBoxColumn();
        this.colSize = new DataGridViewTextBoxColumn();
        this.colType = new DataGridViewTextBoxColumn();
        this.colStartTime = new DataGridViewTextBoxColumn();
        this.colStatus = new DataGridViewTextBoxColumn();
        this.colCreatedBy = new DataGridViewTextBoxColumn();
        this.progressBar = new ProgressBar();
        this.lblStatus = new Label();
        this.panelTop.SuspendLayout();
        this.groupBoxStats.SuspendLayout();
        this.panelButtons.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)this.dgvBackups).BeginInit();
        this.SuspendLayout();
        //
        // panelTop
        //
        this.panelTop.Controls.Add(this.groupBoxStats);
        this.panelTop.Dock = DockStyle.Top;
        this.panelTop.Location = new Point(0, 0);
        this.panelTop.Name = "panelTop";
        this.panelTop.Size = new Size(900, 100);
        this.panelTop.TabIndex = 0;
        //
        // groupBoxStats
        //
        this.groupBoxStats.Controls.Add(this.lblLastBackup);
        this.groupBoxStats.Controls.Add(this.lblTotalSize);
        this.groupBoxStats.Controls.Add(this.lblFailedCount);
        this.groupBoxStats.Controls.Add(this.lblSuccessCount);
        this.groupBoxStats.Controls.Add(this.lblTotalCount);
        this.groupBoxStats.Location = new Point(12, 12);
        this.groupBoxStats.Name = "groupBoxStats";
        this.groupBoxStats.Size = new Size(876, 75);
        this.groupBoxStats.TabIndex = 0;
        this.groupBoxStats.TabStop = false;
        this.groupBoxStats.Text = "统计信息";
        //
        // lblLastBackup
        //
        this.lblLastBackup.AutoSize = true;
        this.lblLastBackup.Location = new Point(650, 35);
        this.lblLastBackup.Name = "lblLastBackup";
        this.lblLastBackup.Size = new Size(80, 17);
        this.lblLastBackup.TabIndex = 4;
        this.lblLastBackup.Text = "最后备份: 无";
        //
        // lblTotalSize
        //
        this.lblTotalSize.AutoSize = true;
        this.lblTotalSize.Location = new Point(500, 35);
        this.lblTotalSize.Name = "lblTotalSize";
        this.lblTotalSize.Size = new Size(80, 17);
        this.lblTotalSize.TabIndex = 3;
        this.lblTotalSize.Text = "总大小: 0 MB";
        //
        // lblFailedCount
        //
        this.lblFailedCount.AutoSize = true;
        this.lblFailedCount.ForeColor = Color.Red;
        this.lblFailedCount.Location = new Point(350, 35);
        this.lblFailedCount.Name = "lblFailedCount";
        this.lblFailedCount.Size = new Size(50, 17);
        this.lblFailedCount.TabIndex = 2;
        this.lblFailedCount.Text = "失败: 0";
        //
        // lblSuccessCount
        //
        this.lblSuccessCount.AutoSize = true;
        this.lblSuccessCount.ForeColor = Color.Green;
        this.lblSuccessCount.Location = new Point(200, 35);
        this.lblSuccessCount.Name = "lblSuccessCount";
        this.lblSuccessCount.Size = new Size(50, 17);
        this.lblSuccessCount.TabIndex = 1;
        this.lblSuccessCount.Text = "成功: 0";
        //
        // lblTotalCount
        //
        this.lblTotalCount.AutoSize = true;
        this.lblTotalCount.Location = new Point(20, 35);
        this.lblTotalCount.Name = "lblTotalCount";
        this.lblTotalCount.Size = new Size(80, 17);
        this.lblTotalCount.TabIndex = 0;
        this.lblTotalCount.Text = "总备份数: 0";
        //
        // panelButtons
        //
        this.panelButtons.Controls.Add(this.lblStatus);
        this.panelButtons.Controls.Add(this.progressBar);
        this.panelButtons.Controls.Add(this.btnClose);
        this.panelButtons.Controls.Add(this.btnRefresh);
        this.panelButtons.Controls.Add(this.btnDelete);
        this.panelButtons.Controls.Add(this.btnRestore);
        this.panelButtons.Controls.Add(this.btnBackup);
        this.panelButtons.Dock = DockStyle.Bottom;
        this.panelButtons.Location = new Point(0, 500);
        this.panelButtons.Name = "panelButtons";
        this.panelButtons.Size = new Size(900, 100);
        this.panelButtons.TabIndex = 1;
        //
        // btnClose
        //
        this.btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnClose.Location = new Point(800, 55);
        this.btnClose.Name = "btnClose";
        this.btnClose.Size = new Size(88, 30);
        this.btnClose.TabIndex = 4;
        this.btnClose.Text = "关闭";
        this.btnClose.UseVisualStyleBackColor = true;
        //
        // btnRefresh
        //
        this.btnRefresh.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnRefresh.Location = new Point(700, 55);
        this.btnRefresh.Name = "btnRefresh";
        this.btnRefresh.Size = new Size(88, 30);
        this.btnRefresh.TabIndex = 3;
        this.btnRefresh.Text = "刷新";
        this.btnRefresh.UseVisualStyleBackColor = true;
        //
        // btnDelete
        //
        this.btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnDelete.Enabled = false;
        this.btnDelete.Location = new Point(600, 55);
        this.btnDelete.Name = "btnDelete";
        this.btnDelete.Size = new Size(88, 30);
        this.btnDelete.TabIndex = 2;
        this.btnDelete.Text = "删除";
        this.btnDelete.UseVisualStyleBackColor = true;
        //
        // btnRestore
        //
        this.btnRestore.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnRestore.Enabled = false;
        this.btnRestore.Location = new Point(500, 55);
        this.btnRestore.Name = "btnRestore";
        this.btnRestore.Size = new Size(88, 30);
        this.btnRestore.TabIndex = 1;
        this.btnRestore.Text = "恢复";
        this.btnRestore.UseVisualStyleBackColor = true;
        //
        // btnBackup
        //
        this.btnBackup.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnBackup.Location = new Point(400, 55);
        this.btnBackup.Name = "btnBackup";
        this.btnBackup.Size = new Size(88, 30);
        this.btnBackup.TabIndex = 0;
        this.btnBackup.Text = "新建备份";
        this.btnBackup.UseVisualStyleBackColor = true;
        //
        // dgvBackups
        //
        this.dgvBackups.AllowUserToAddRows = false;
        this.dgvBackups.AllowUserToDeleteRows = false;
        this.dgvBackups.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        this.dgvBackups.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvBackups.Columns.AddRange(new DataGridViewColumn[] {
            this.colBackupId,
            this.colFileName,
            this.colSize,
            this.colType,
            this.colStartTime,
            this.colStatus,
            this.colCreatedBy});
        this.dgvBackups.Dock = DockStyle.Fill;
        this.dgvBackups.Location = new Point(0, 100);
        this.dgvBackups.MultiSelect = false;
        this.dgvBackups.Name = "dgvBackups";
        this.dgvBackups.ReadOnly = true;
        this.dgvBackups.RowHeadersVisible = false;
        this.dgvBackups.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.dgvBackups.Size = new Size(900, 400);
        this.dgvBackups.TabIndex = 2;
        //
        // colBackupId
        //
        this.colBackupId.DataPropertyName = "BackupId";
        this.colBackupId.HeaderText = "ID";
        this.colBackupId.Name = "colBackupId";
        this.colBackupId.ReadOnly = true;
        this.colBackupId.Visible = false;
        //
        // colFileName
        //
        this.colFileName.DataPropertyName = "FileName";
        this.colFileName.HeaderText = "文件名";
        this.colFileName.Name = "colFileName";
        this.colFileName.ReadOnly = true;
        //
        // colSize
        //
        this.colSize.DataPropertyName = "Size";
        this.colSize.HeaderText = "大小";
        this.colSize.Name = "colSize";
        this.colSize.ReadOnly = true;
        this.colSize.Width = 80;
        //
        // colType
        //
        this.colType.DataPropertyName = "Type";
        this.colType.HeaderText = "类型";
        this.colType.Name = "colType";
        this.colType.ReadOnly = true;
        this.colType.Width = 80;
        //
        // colStartTime
        //
        this.colStartTime.DataPropertyName = "StartTime";
        this.colStartTime.HeaderText = "备份时间";
        this.colStartTime.Name = "colStartTime";
        this.colStartTime.ReadOnly = true;
        this.colStartTime.Width = 150;
        //
        // colStatus
        //
        this.colStatus.DataPropertyName = "Status";
        this.colStatus.HeaderText = "状态";
        this.colStatus.Name = "colStatus";
        this.colStatus.ReadOnly = true;
        this.colStatus.Width = 80;
        //
        // colCreatedBy
        //
        this.colCreatedBy.DataPropertyName = "CreatedBy";
        this.colCreatedBy.HeaderText = "创建人";
        this.colCreatedBy.Name = "colCreatedBy";
        this.colCreatedBy.ReadOnly = true;
        this.colCreatedBy.Width = 100;
        //
        // progressBar
        //
        this.progressBar.Location = new Point(12, 15);
        this.progressBar.Name = "progressBar";
        this.progressBar.Size = new Size(300, 20);
        this.progressBar.TabIndex = 5;
        this.progressBar.Visible = false;
        //
        // lblStatus
        //
        this.lblStatus.AutoSize = true;
        this.lblStatus.Location = new Point(12, 45);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new Size(0, 17);
        this.lblStatus.TabIndex = 6;
        //
        // BackupManageForm
        //
        this.AutoScaleDimensions = new SizeF(7F, 17F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(900, 600);
        this.Controls.Add(this.dgvBackups);
        this.Controls.Add(this.panelButtons);
        this.Controls.Add(this.panelTop);
        this.Name = "BackupManageForm";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "数据备份管理";
        this.panelTop.ResumeLayout(false);
        this.groupBoxStats.ResumeLayout(false);
        this.groupBoxStats.PerformLayout();
        this.panelButtons.ResumeLayout(false);
        this.panelButtons.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)this.dgvBackups).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private Panel panelTop;
    private GroupBox groupBoxStats;
    private Label lblTotalCount;
    private Label lblSuccessCount;
    private Label lblFailedCount;
    private Label lblTotalSize;
    private Label lblLastBackup;
    private Panel panelButtons;
    private Button btnBackup;
    private Button btnRestore;
    private Button btnDelete;
    private Button btnRefresh;
    private Button btnClose;
    private DataGridView dgvBackups;
    private DataGridViewTextBoxColumn colBackupId;
    private DataGridViewTextBoxColumn colFileName;
    private DataGridViewTextBoxColumn colSize;
    private DataGridViewTextBoxColumn colType;
    private DataGridViewTextBoxColumn colStartTime;
    private DataGridViewTextBoxColumn colStatus;
    private DataGridViewTextBoxColumn colCreatedBy;
    private ProgressBar progressBar;
    private Label lblStatus;
}
