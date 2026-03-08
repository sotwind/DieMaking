using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Warehouse;

public partial class ScrapApplyForm : Form
{
    private readonly WarehouseService _warehouseService;
    private List<DieInventory> _inStockDies = new();
    private List<DieScrapRecord> _scrapRecords = new();

    public ScrapApplyForm()
    {
        InitializeComponent();
        _warehouseService = new WarehouseService();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "报废申请";
        this.Size = new Size(1100, 700);
        this.StartPosition = FormStartPosition.CenterParent;

        // 创建工具栏
        var toolStrip = new ToolStrip();
        
        var btnApply = new ToolStripButton("新增申请") { Image = SystemIcons.Question.ToBitmap() };
        btnApply.Click += (s, e) => ShowApplyDialog();
        
        var btnRefresh = new ToolStripButton("刷新") { Image = SystemIcons.Question.ToBitmap() };
        btnRefresh.Click += (s, e) => LoadData();
        
        var btnAudit = new ToolStripButton("审核") { Image = SystemIcons.Question.ToBitmap() };
        btnAudit.Click += (s, e) => ShowAuditDialog();
        
        var btnDelete = new ToolStripButton("删除") { Image = SystemIcons.Question.ToBitmap() };
        btnDelete.Click += (s, e) => DeleteRecord();

        toolStrip.Items.AddRange(new ToolStripItem[] { btnApply, new ToolStripSeparator(), btnAudit, btnDelete, new ToolStripSeparator(), btnRefresh });

        // 搜索区域
        var panelSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45,
            Padding = new Padding(10, 5, 10, 5)
        };

        var lblStatus = new Label { Text = "审核状态：", Location = new Point(10, 12), AutoSize = true };
        cboFilterStatus = new ComboBox 
        { 
            Location = new Point(80, 9), 
            Size = new Size(120, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboFilterStatus.Items.Add("全部");
        cboFilterStatus.Items.Add("待审核");
        cboFilterStatus.Items.Add("已通过");
        cboFilterStatus.Items.Add("已驳回");
        cboFilterStatus.SelectedIndex = 0;
        cboFilterStatus.SelectedIndexChanged += (s, e) => ApplyFilters();

        var btnFilter = new Button { Text = "筛选", Location = new Point(210, 8), Size = new Size(80, 28) };
        btnFilter.Click += (s, e) => ApplyFilters();

        panelSearch.Controls.AddRange(new Control[] { lblStatus, cboFilterStatus, btnFilter });

        // 数据表格
        dgvRecords = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            RowHeadersVisible = false,
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.AliceBlue }
        };

        // 添加列
        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ScrapID",
            HeaderText = "ID",
            Width = 60,
            Visible = false
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "DieCode",
            HeaderText = "刀模编号",
            Width = 120
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "CustomerName",
            HeaderText = "客户名称",
            Width = 150
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ProductName",
            HeaderText = "产品名称",
            Width = 150
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ScrapType",
            HeaderText = "报废类型",
            Width = 100
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ScrapReason",
            HeaderText = "报废原因",
            Width = 200
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ApplicantName",
            HeaderText = "申请人",
            Width = 80
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ApplyTime",
            HeaderText = "申请时间",
            Width = 130,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" }
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "AuditStatusText",
            HeaderText = "审核状态",
            Width = 80
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "AuditorName",
            HeaderText = "审核人",
            Width = 80
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "AuditTime",
            HeaderText = "审核时间",
            Width = 130,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" }
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "AuditRemark",
            HeaderText = "审核备注",
            Width = 150
        });

        // 状态栏
        var statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel("就绪");
        statusStrip.Items.Add(lblStatus);

        // 布局
        var panelContent = new Panel { Dock = DockStyle.Fill };
        panelContent.Controls.Add(dgvRecords);

        this.Controls.Add(panelContent);
        this.Controls.Add(panelSearch);
        this.Controls.Add(toolStrip);
        this.Controls.Add(statusStrip);
    }

    private void LoadData()
    {
        try
        {
            _scrapRecords = _warehouseService.GetAllScrapRecords();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyFilters()
    {
        var filteredRecords = _scrapRecords.AsEnumerable();

        // 状态筛选
        if (cboFilterStatus.SelectedIndex > 0)
        {
            var status = cboFilterStatus.SelectedIndex switch
            {
                1 => ScrapAuditStatus.Pending,
                2 => ScrapAuditStatus.Approved,
                3 => ScrapAuditStatus.Rejected,
                _ => (ScrapAuditStatus?)null
            };

            if (status.HasValue)
            {
                filteredRecords = filteredRecords.Where(r => r.AuditStatus == status.Value);
            }
        }

        var result = filteredRecords.ToList();
        dgvRecords.DataSource = result;
        lblStatus.Text = $"共 {result.Count} 条记录";
    }

    private void ShowApplyDialog()
    {
        using var form = new ScrapApplyEditForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            LoadData();
        }
    }

    private void ShowAuditDialog()
    {
        if (dgvRecords.SelectedRows.Count == 0)
        {
            MessageBox.Show("请选择要审核的报废申请", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var scrapId = (int)dgvRecords.SelectedRows[0].Cells["ScrapID"].Value;
        var record = _scrapRecords.FirstOrDefault(r => r.ScrapID == scrapId);
        
        if (record == null) return;

        if (record.AuditStatus != ScrapAuditStatus.Pending)
        {
            MessageBox.Show("该申请已审核，不能重复审核", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new ScrapAuditEditForm(record);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            LoadData();
        }
    }

    private void DeleteRecord()
    {
        if (dgvRecords.SelectedRows.Count == 0)
        {
            MessageBox.Show("请选择要删除的记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var scrapId = (int)dgvRecords.SelectedRows[0].Cells["ScrapID"].Value;
        var record = _scrapRecords.FirstOrDefault(r => r.ScrapID == scrapId);
        
        if (record == null) return;

        if (record.AuditStatus != ScrapAuditStatus.Pending)
        {
            MessageBox.Show("已审核的记录不能删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show("确定要删除该报废申请吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            try
            {
                var result = _warehouseService.DeleteScrapRecord(scrapId);
                if (result)
                {
                    MessageBox.Show("删除成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                }
                else
                {
                    MessageBox.Show("删除失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private DataGridView dgvRecords = null!;
    private ComboBox cboFilterStatus = null!;
    private ToolStripStatusLabel lblStatus = null!;
}

// 报废申请编辑窗体
public class ScrapApplyEditForm : Form
{
    private readonly WarehouseService _warehouseService;
    private List<DieInventory> _inStockDies = new();

    public ScrapApplyEditForm()
    {
        _warehouseService = new WarehouseService();
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "新增报废申请";
        this.Size = new Size(650, 450);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int y = 20;
        int labelWidth = 100;
        int controlWidth = 450;
        int leftMargin = 30;

        // 标题
        var lblTitle = new Label
        {
            Text = "刀模报废申请",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(250, y)
        };
        y += 50;

        // 刀模选择
        var lblDie = new Label { Text = "选择刀模：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        cboDie = new ComboBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboDie.SelectedIndexChanged += CboDie_SelectedIndexChanged;
        y += 40;

        // 刀模信息
        var lblDieInfo = new Label { Text = "刀模信息：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblDieInfoValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25),
            ForeColor = Color.Blue
        };
        y += 40;

        // 当前库位
        var lblLocation = new Label { Text = "当前库位：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblLocationValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25),
            ForeColor = Color.Green
        };
        y += 40;

        // 报废类型
        var lblType = new Label { Text = "报废类型：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        txtScrapType = new TextBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(200, 25)
        };
        y += 40;

        // 报废原因
        var lblReason = new Label { Text = "报废原因：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        txtReason = new TextBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 80),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        y += 100;

        // 申请人
        var lblApplicant = new Label { Text = "申请人：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        txtApplicant = new TextBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(200, 25)
        };
        // 默认填充当前用户
        if (CurrentUser.User != null)
        {
            txtApplicant.Text = CurrentUser.User.RealName ?? CurrentUser.User.Username;
        }
        y += 40;

        // 申请时间
        var lblApplyTime = new Label { Text = "申请时间：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        dtpApplyTime = new DateTimePicker 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(200, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            Value = DateTime.Now
        };
        y += 50;

        // 按钮
        var btnSave = new Button { Text = "提交申请", Location = new Point(200, y), Size = new Size(120, 35) };
        btnSave.Click += BtnSave_Click;

        var btnCancel = new Button { Text = "取消", Location = new Point(350, y), Size = new Size(100, 35) };
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] {
            lblTitle,
            lblDie, cboDie, lblDieInfo, lblDieInfoValue, lblLocation, lblLocationValue,
            lblType, txtScrapType, lblReason, txtReason, lblApplicant, txtApplicant,
            lblApplyTime, dtpApplyTime, btnSave, btnCancel
        });
    }

    private void LoadData()
    {
        try
        {
            // 加载在库刀模（已借出和报废的不能申请报废）
            _inStockDies = _warehouseService.GetInStockInventory();
            
            cboDie.DataSource = null;
            
            if (_inStockDies.Count == 0)
            {
                MessageBox.Show("当前没有在库的刀模可供申请报废", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 创建显示列表
            var displayList = _inStockDies.Select(d => new 
            { 
                d.InventoryID, 
                d.DieID,
                d.DieCode, 
                Display = $"{d.DieCode} - {d.CustomerName} - {d.ProductName}"
            }).ToList();
            
            cboDie.DataSource = displayList;
            cboDie.DisplayMember = "Display";
            cboDie.ValueMember = "InventoryID";

            if (cboDie.Items.Count > 0)
            {
                cboDie.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CboDie_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cboDie.SelectedValue == null) return;

        var inventoryId = (int)cboDie.SelectedValue;
        var die = _inStockDies.FirstOrDefault(d => d.InventoryID == inventoryId);
        
        if (die != null)
        {
            lblDieInfoValue.Text = $"客户：{die.CustomerName}  产品：{die.ProductName}";
            lblLocationValue.Text = die.LocationCode ?? "未分配库位";
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (cboDie.SelectedValue == null)
        {
            MessageBox.Show("请选择要申请报废的刀模", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtScrapType.Text))
        {
            MessageBox.Show("请输入报废类型", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtScrapType.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtReason.Text))
        {
            MessageBox.Show("请输入报废原因", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtReason.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtApplicant.Text))
        {
            MessageBox.Show("请输入申请人", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtApplicant.Focus();
            return;
        }

        try
        {
            var inventoryId = (int)cboDie.SelectedValue;
            var die = _inStockDies.FirstOrDefault(d => d.InventoryID == inventoryId);
            
            if (die == null)
            {
                MessageBox.Show("选择的刀模信息无效", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var record = new DieScrapRecord
            {
                DieID = die.DieID,
                InventoryID = die.InventoryID,
                ScrapType = txtScrapType.Text.Trim(),
                ScrapReason = txtReason.Text.Trim(),
                ApplicantNo = txtApplicant.Text.Trim(),
                ApplicantName = txtApplicant.Text.Trim(),
                ApplyTime = dtpApplyTime.Value
            };

            var scrapId = _warehouseService.CreateScrapRecord(record);
            
            if (scrapId > 0)
            {
                MessageBox.Show("报废申请提交成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("申请提交失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"申请提交失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private ComboBox cboDie = null!;
    private Label lblDieInfoValue = null!;
    private Label lblLocationValue = null!;
    private TextBox txtScrapType = null!;
    private TextBox txtReason = null!;
    private TextBox txtApplicant = null!;
    private DateTimePicker dtpApplyTime = null!;
}

// 报废审核编辑窗体
public class ScrapAuditEditForm : Form
{
    private readonly WarehouseService _warehouseService;
    private readonly DieScrapRecord _record;

    public ScrapAuditEditForm(DieScrapRecord record)
    {
        _record = record;
        _warehouseService = new WarehouseService();
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "报废审核";
        this.Size = new Size(600, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int y = 20;
        int labelWidth = 100;
        int controlWidth = 400;
        int leftMargin = 30;

        // 标题
        var lblTitle = new Label
        {
            Text = "刀模报废审核",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(220, y)
        };
        y += 50;

        // 刀模编号
        var lblDieCode = new Label { Text = "刀模编号：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblDieCodeValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25),
            Font = new Font("微软雅黑", 9, FontStyle.Bold),
            ForeColor = Color.Blue
        };
        y += 40;

        // 客户名称
        var lblCustomer = new Label { Text = "客户名称：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblCustomerValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25)
        };
        y += 40;

        // 产品名称
        var lblProduct = new Label { Text = "产品名称：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblProductValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25)
        };
        y += 40;

        // 报废类型
        var lblType = new Label { Text = "报废类型：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblTypeValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25)
        };
        y += 40;

        // 报废原因
        var lblReason = new Label { Text = "报废原因：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblReasonValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 60),
            AutoSize = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.WhiteSmoke
        };
        y += 70;

        // 申请人
        var lblApplicant = new Label { Text = "申请人：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblApplicantValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(150, 25)
        };
        y += 40;

        // 申请时间
        var lblApplyTime = new Label { Text = "申请时间：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblApplyTimeValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(200, 25)
        };
        y += 50;

        // 审核结果
        var lblAuditResult = new Label { Text = "审核结果：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        rbApproved = new RadioButton 
        { 
            Text = "通过", 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(80, 25),
            Checked = true
        };
        rbRejected = new RadioButton 
        { 
            Text = "驳回", 
            Location = new Point(leftMargin + labelWidth + 90, y), 
            Size = new Size(80, 25)
        };
        y += 40;

        // 审核备注
        var lblAuditRemark = new Label { Text = "审核备注：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        txtAuditRemark = new TextBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 60),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        y += 80;

        // 按钮
        var btnSave = new Button { Text = "确认审核", Location = new Point(180, y), Size = new Size(120, 35) };
        btnSave.Click += BtnSave_Click;

        var btnCancel = new Button { Text = "取消", Location = new Point(320, y), Size = new Size(100, 35) };
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] {
            lblTitle,
            lblDieCode, lblDieCodeValue, lblCustomer, lblCustomerValue, lblProduct, lblProductValue,
            lblType, lblTypeValue, lblReason, lblReasonValue, lblApplicant, lblApplicantValue,
            lblApplyTime, lblApplyTimeValue, lblAuditResult, rbApproved, rbRejected,
            lblAuditRemark, txtAuditRemark, btnSave, btnCancel
        });
    }

    private void LoadData()
    {
        lblDieCodeValue.Text = _record.DieCode ?? "";
        lblCustomerValue.Text = _record.CustomerName ?? "";
        lblProductValue.Text = _record.ProductName ?? "";
        lblTypeValue.Text = _record.ScrapType;
        lblReasonValue.Text = _record.ScrapReason;
        lblApplicantValue.Text = _record.ApplicantName;
        lblApplyTimeValue.Text = _record.ApplyTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            var auditorName = CurrentUser.User?.RealName ?? CurrentUser.User?.Username ?? "未知";
            var auditorNo = CurrentUser.User?.Username ?? "";
            var approved = rbApproved.Checked;

            var result = _warehouseService.AuditScrapRecord(
                _record.ScrapID, 
                approved, 
                auditorNo, 
                auditorName, 
                txtAuditRemark.Text.Trim()
            );
            
            if (result)
            {
                var msg = approved ? "审核通过" : "审核驳回";
                MessageBox.Show($"报废申请{msg}成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("审核失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"审核失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Label lblDieCodeValue = null!;
    private Label lblCustomerValue = null!;
    private Label lblProductValue = null!;
    private Label lblTypeValue = null!;
    private Label lblReasonValue = null!;
    private Label lblApplicantValue = null!;
    private Label lblApplyTimeValue = null!;
    private RadioButton rbApproved = null!;
    private RadioButton rbRejected = null!;
    private TextBox txtAuditRemark = null!;
}
