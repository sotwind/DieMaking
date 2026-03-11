using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Die;

public partial class DieAddForm : BaseEditForm
{
    private readonly DieService _dieService;
    private readonly int? _dieId;
    private readonly bool _isViewMode;
    private List<DieProcess> _processes = new();

    // 新增模式
    public DieAddForm()
    {
        _dieService = new DieService();
        _dieId = null;
        _isViewMode = false;
        InitializeComponent();
    }

    // 编辑模式
    public DieAddForm(int dieId)
    {
        _dieService = new DieService();
        _dieId = dieId;
        _isViewMode = false;
        IsEditMode = true;
        InitializeComponent();
        LoadDieData();
    }

    // 查看模式
    public DieAddForm(int dieId, bool isViewMode)
    {
        _dieService = new DieService();
        _dieId = dieId;
        _isViewMode = isViewMode;
        InitializeComponent();
        LoadDieData();
    }

    private void InitializeComponent()
    {
        this.Text = _isViewMode ? "查看刀模" : (_dieId.HasValue ? "编辑刀模" : "新增刀模");
        this.Size = new Size(800, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int y = 15;
        int labelWidth = 80;
        int inputWidth = 180;
        int rowHeight = 30;

        // ===== 基本信息区域 =====
        var grpBasic = UIStyleHelper.CreateGroupBox("基本信息", new Point(10, 10), new Size(760, 150));

        // 刀模编号
        var lblDieCode = UIStyleHelper.CreateLabel("刀模编号：", new Point(15, y), new Size(labelWidth, 23));
        txtDieCode = UIStyleHelper.CreateTextBox(new Point(100, y), new Size(inputWidth, 23), "请输入刀模编号");
        grpBasic.Controls.Add(lblDieCode);
        grpBasic.Controls.Add(txtDieCode);

        // 客户名称
        var lblCustomer = UIStyleHelper.CreateLabel("客户名称：", new Point(300, y), new Size(labelWidth, 23));
        txtCustomer = UIStyleHelper.CreateTextBox(new Point(385, y), new Size(inputWidth, 23), "请输入客户名称");
        grpBasic.Controls.Add(lblCustomer);
        grpBasic.Controls.Add(txtCustomer);

        // 产品名称
        var lblProduct = UIStyleHelper.CreateLabel("产品名称：", new Point(585, y), new Size(labelWidth, 23));
        txtProduct = UIStyleHelper.CreateTextBox(new Point(670, y), new Size(inputWidth, 23), "请输入产品名称");
        grpBasic.Controls.Add(lblProduct);
        grpBasic.Controls.Add(txtProduct);

        y += rowHeight;

        // 结构类型
        var lblStructure = UIStyleHelper.CreateLabel("结构类型：", new Point(15, y), new Size(labelWidth, 23));
        txtStructure = UIStyleHelper.CreateTextBox(new Point(100, y), new Size(inputWidth, 23));
        grpBasic.Controls.Add(lblStructure);
        grpBasic.Controls.Add(txtStructure);

        // 模型类型
        var lblModelType = UIStyleHelper.CreateLabel("模型类型：", new Point(300, y), new Size(labelWidth, 23));
        txtModelType = UIStyleHelper.CreateTextBox(new Point(385, y), new Size(inputWidth, 23));
        grpBasic.Controls.Add(lblModelType);
        grpBasic.Controls.Add(txtModelType);

        // 排版方式
        var lblLayout = UIStyleHelper.CreateLabel("排版方式：", new Point(585, y), new Size(labelWidth, 23));
        txtLayoutType = UIStyleHelper.CreateTextBox(new Point(670, y), new Size(inputWidth, 23));
        grpBasic.Controls.Add(lblLayout);
        grpBasic.Controls.Add(txtLayoutType);

        y += rowHeight;

        // 瓦楞类型
        var lblFlute = UIStyleHelper.CreateLabel("瓦楞类型：", new Point(15, y), new Size(labelWidth, 23));
        txtFluteType = UIStyleHelper.CreateTextBox(new Point(100, y), new Size(inputWidth, 23));
        grpBasic.Controls.Add(lblFlute);
        grpBasic.Controls.Add(txtFluteType);

        // 材质
        var lblMaterial = UIStyleHelper.CreateLabel("材质：", new Point(300, y), new Size(labelWidth, 23));
        txtMaterial = UIStyleHelper.CreateTextBox(new Point(385, y), new Size(inputWidth, 23));
        grpBasic.Controls.Add(lblMaterial);
        grpBasic.Controls.Add(txtMaterial);

        // 来源工厂
        var lblSource = UIStyleHelper.CreateLabel("来源工厂：", new Point(585, y), new Size(labelWidth, 23));
        txtSourceFactory = UIStyleHelper.CreateTextBox(new Point(670, y), new Size(inputWidth, 23));
        grpBasic.Controls.Add(lblSource);
        grpBasic.Controls.Add(txtSourceFactory);

        y += rowHeight;

        // 交货日期
        var lblDelivery = UIStyleHelper.CreateLabel("交货日期：", new Point(15, y), new Size(labelWidth, 23));
        dtpDelivery = new DateTimePicker
        {
            Location = new Point(100, y),
            Size = new Size(inputWidth, 23),
            Format = DateTimePickerFormat.Short,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        grpBasic.Controls.Add(lblDelivery);
        grpBasic.Controls.Add(dtpDelivery);

        // 状态
        var lblStatus = UIStyleHelper.CreateLabel("状态：", new Point(300, y), new Size(labelWidth, 23));
        cmbStatus = new ComboBox
        {
            Location = new Point(385, y),
            Size = new Size(inputWidth, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        cmbStatus.Items.AddRange(Enum.GetNames(typeof(DieStatus)).Select(s => ((DieStatus)Enum.Parse(typeof(DieStatus), s)).GetDisplayName()).ToArray());
        cmbStatus.SelectedIndex = 0;
        grpBasic.Controls.Add(lblStatus);
        grpBasic.Controls.Add(cmbStatus);

        // ===== 尺寸信息区域 =====
        y = 15;
        var grpSize = UIStyleHelper.CreateGroupBox("尺寸信息", new Point(10, 170), new Size(760, 100));

        // 制造尺寸
        var lblManuSize = UIStyleHelper.CreateLabel("制造尺寸：", new Point(15, y), new Size(labelWidth, 23));
        txtManuLength = UIStyleHelper.CreateTextBox(new Point(100, y), new Size(60, 23));
        var lblX1 = UIStyleHelper.CreateLabel("×", new Point(165, y), new Size(15, 23));
        lblX1.TextAlign = ContentAlignment.MiddleCenter;
        txtManuWidth = UIStyleHelper.CreateTextBox(new Point(185, y), new Size(60, 23));
        var lblX2 = UIStyleHelper.CreateLabel("×", new Point(250, y), new Size(15, 23));
        lblX2.TextAlign = ContentAlignment.MiddleCenter;
        txtManuHeight = UIStyleHelper.CreateTextBox(new Point(270, y), new Size(60, 23));
        grpSize.Controls.Add(lblManuSize);
        grpSize.Controls.Add(txtManuLength);
        grpSize.Controls.Add(lblX1);
        grpSize.Controls.Add(txtManuWidth);
        grpSize.Controls.Add(lblX2);
        grpSize.Controls.Add(txtManuHeight);

        // 毛坯尺寸
        var lblBlankSize = UIStyleHelper.CreateLabel("毛坯尺寸：", new Point(350, y), new Size(labelWidth, 23));
        txtBlankLength = UIStyleHelper.CreateTextBox(new Point(435, y), new Size(60, 23));
        var lblX3 = UIStyleHelper.CreateLabel("×", new Point(500, y), new Size(15, 23));
        lblX3.TextAlign = ContentAlignment.MiddleCenter;
        txtBlankWidth = UIStyleHelper.CreateTextBox(new Point(520, y), new Size(60, 23));
        grpSize.Controls.Add(lblBlankSize);
        grpSize.Controls.Add(txtBlankLength);
        grpSize.Controls.Add(lblX3);
        grpSize.Controls.Add(txtBlankWidth);

        y += rowHeight + 5;

        // 工艺说明
        var lblProcessDesc = UIStyleHelper.CreateLabel("工艺说明：", new Point(15, y), new Size(labelWidth, 23));
        txtProcessDesc = new TextBox
        {
            Location = new Point(100, y),
            Size = new Size(480, 50),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        grpSize.Controls.Add(lblProcessDesc);
        grpSize.Controls.Add(txtProcessDesc);

        // ===== 工序设置区域 =====
        var grpProcess = UIStyleHelper.CreateGroupBox("工序设置", new Point(10, 280), new Size(760, 200));

        // 工序列表
        dgvProcesses = new DataGridView
        {
            Location = new Point(15, 25),
            Size = new Size(730, 130),
            AutoGenerateColumns = false
        };
        ApplyDataGridViewStyle(dgvProcesses);

        dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProcessID", HeaderText = "ID", Visible = false, DataPropertyName = "ProcessID" });
        dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProcessName", HeaderText = "工序名称", Width = 120, DataPropertyName = "ProcessName" });
        dgvProcesses.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "Status",
            HeaderText = "状态",
            Width = 80,
            DataSource = Enum.GetValues(typeof(ProcessStatus)).Cast<ProcessStatus>().Select(s => new { Value = s, Text = s.GetDisplayName() }).ToList(),
            DisplayMember = "Text",
            ValueMember = "Value",
            DataPropertyName = "Status"
        });
        dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "OperatorName", HeaderText = "操作员", Width = 80, DataPropertyName = "OperatorName" });
        dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "Formula", HeaderText = "计算公式", Width = 150, DataPropertyName = "Formula" });
        dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "金额", Width = 80, DataPropertyName = "Amount" });

        // 工序按钮
        btnAddProcess = UIStyleHelper.CreateAddButton("添加工序");
        btnAddProcess.Size = new Size(90, 28);
        btnAddProcess.Location = new Point(15, 165);
        btnAddProcess.Click += BtnAddProcess_Click;

        btnEditProcess = UIStyleHelper.CreateEditButton("编辑工序");
        btnEditProcess.Size = new Size(90, 28);
        btnEditProcess.Location = new Point(115, 165);
        btnEditProcess.Click += BtnEditProcess_Click;

        btnDeleteProcess = UIStyleHelper.CreateDeleteButton("删除工序");
        btnDeleteProcess.Size = new Size(90, 28);
        btnDeleteProcess.Location = new Point(215, 165);
        btnDeleteProcess.Click += BtnDeleteProcess_Click;

        btnMoveUp = new Button { Text = "上移", Location = new Point(315, 165), Size = new Size(60, 28) };
        ApplyButtonStyle(btnMoveUp, ButtonStyle.Default);
        btnMoveUp.Click += BtnMoveUp_Click;

        btnMoveDown = new Button { Text = "下移", Location = new Point(385, 165), Size = new Size(60, 28) };
        ApplyButtonStyle(btnMoveDown, ButtonStyle.Default);
        btnMoveDown.Click += BtnMoveDown_Click;

        grpProcess.Controls.Add(dgvProcesses);
        grpProcess.Controls.Add(btnAddProcess);
        grpProcess.Controls.Add(btnEditProcess);
        grpProcess.Controls.Add(btnDeleteProcess);
        grpProcess.Controls.Add(btnMoveUp);
        grpProcess.Controls.Add(btnMoveDown);

        // ===== 备注区域 =====
        var grpRemark = UIStyleHelper.CreateGroupBox("备注", new Point(10, 550), new Size(760, 60));

        txtRemark = new TextBox
        {
            Location = new Point(15, 20),
            Size = new Size(730, 35),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        grpRemark.Controls.Add(txtRemark);

        // ===== 按钮区域 =====
        int btnY = 620;
        if (!_isViewMode)
        {
            btnSave = UIStyleHelper.CreateSaveButton();
            btnSave.Location = new Point(250, btnY);
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

        }

        btnCancel = UIStyleHelper.CreateCancelButton(_isViewMode ? "关闭" : "取消");
        btnCancel.Location = new Point(360, btnY);
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
        this.Controls.Add(btnCancel);

        // 添加所有分组到窗体
        this.Controls.Add(grpBasic);
        this.Controls.Add(grpSize);
        this.Controls.Add(grpProcess);
        this.Controls.Add(grpRemark);

        // 如果是查看模式，禁用所有编辑控件
        if (_isViewMode)
        {
            SetControlsReadOnly(this, true);
        }

        // 注册回车跳转
        RegisterEnterToNext();
    }

    #region 控件声明
    // 基本信息
    private TextBox txtDieCode = null!;
    private TextBox txtCustomer = null!;
    private TextBox txtProduct = null!;
    private TextBox txtStructure = null!;
    private TextBox txtModelType = null!;
    private TextBox txtLayoutType = null!;
    private TextBox txtFluteType = null!;
    private TextBox txtMaterial = null!;
    private TextBox txtSourceFactory = null!;
    private DateTimePicker dtpDelivery = null!;
    private ComboBox cmbStatus = null!;

    // 尺寸信息
    private TextBox txtManuLength = null!;
    private TextBox txtManuWidth = null!;
    private TextBox txtManuHeight = null!;
    private TextBox txtBlankLength = null!;
    private TextBox txtBlankWidth = null!;
    private TextBox txtProcessDesc = null!;

    // 工序
    private DataGridView dgvProcesses = null!;
    private Button btnAddProcess = null!;
    private Button btnEditProcess = null!;
    private Button btnDeleteProcess = null!;
    private Button btnMoveUp = null!;
    private Button btnMoveDown = null!;

    // 备注和按钮
    private TextBox txtRemark = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;
    #endregion

    #region 事件处理

    private void BtnAddProcess_Click(object? sender, EventArgs e)
    {
        using var form = new DieProcessEditForm();
        if (form.ShowDialog(this) == DialogResult.OK && form.Process != null)
        {
            _processes.Add(form.Process);
            RefreshProcessGrid();
        }
    }

    private void BtnEditProcess_Click(object? sender, EventArgs e)
    {
        if (dgvProcesses.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先选择要编辑的工序", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int index = dgvProcesses.SelectedRows[0].Index;
        if (index < 0 || index >= _processes.Count) return;

        using var form = new DieProcessEditForm(_processes[index]);
        if (form.ShowDialog(this) == DialogResult.OK && form.Process != null)
        {
            _processes[index] = form.Process;
            RefreshProcessGrid();
        }
    }

    private void BtnDeleteProcess_Click(object? sender, EventArgs e)
    {
        if (dgvProcesses.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先选择要删除的工序", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int index = dgvProcesses.SelectedRows[0].Index;
        if (index < 0 || index >= _processes.Count) return;

        if (MessageBox.Show("确定要删除该工序吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _processes.RemoveAt(index);
            RefreshProcessGrid();
        }
    }

    private void BtnMoveUp_Click(object? sender, EventArgs e)
    {
        if (dgvProcesses.SelectedRows.Count == 0) return;
        int index = dgvProcesses.SelectedRows[0].Index;
        if (index <= 0) return;

        var temp = _processes[index];
        _processes[index] = _processes[index - 1];
        _processes[index - 1] = temp;
        RefreshProcessGrid();
        dgvProcesses.Rows[index - 1].Selected = true;
    }

    private void BtnMoveDown_Click(object? sender, EventArgs e)
    {
        if (dgvProcesses.SelectedRows.Count == 0) return;
        int index = dgvProcesses.SelectedRows[0].Index;
        if (index >= _processes.Count - 1) return;

        var temp = _processes[index];
        _processes[index] = _processes[index + 1];
        _processes[index + 1] = temp;
        RefreshProcessGrid();
        dgvProcesses.Rows[index + 1].Selected = true;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        SaveDie();
    }

    #endregion

    #region 私有方法

    private void LoadDieData()
    {
        if (!_dieId.HasValue) return;

        try
        {
            var die = _dieService.GetDieById(_dieId.Value);
            if (die == null)
            {
                MessageBox.Show("刀模信息不存在", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.Cancel;
                return;
            }

            // 填充基本信息
            txtDieCode.Text = die.DieCode;
            txtCustomer.Text = die.CustomerName;
            txtProduct.Text = die.ProductName;
            txtStructure.Text = die.Structure;
            txtModelType.Text = die.ModelType;
            txtLayoutType.Text = die.LayoutType;
            txtFluteType.Text = die.FluteType;
            txtMaterial.Text = die.Material;
            txtSourceFactory.Text = die.SourceFactory;
            dtpDelivery.Value = die.DeliveryDate ?? DateTime.Now.AddDays(7);
            cmbStatus.SelectedIndex = (int)die.Status;

            // 填充尺寸信息
            txtManuLength.Text = die.ManufactureLength.ToString();
            txtManuWidth.Text = die.ManufactureWidth.ToString();
            txtManuHeight.Text = die.ManufactureHeight.ToString();
            txtBlankLength.Text = die.BlankLength.ToString();
            txtBlankWidth.Text = die.BlankWidth.ToString();
            txtProcessDesc.Text = die.ProcessDesc;

            // 填充备注
            txtRemark.Text = die.Remark;

            // 加载工序
            _processes = _dieService.GetDieProcesses(die.DieID);
            RefreshProcessGrid();
        }
        catch (Exception ex)
        {
            ShowError($"加载刀模数据失败：{ex.Message}");
        }
    }

    private void RefreshProcessGrid()
    {
        dgvProcesses.DataSource = null;
        dgvProcesses.DataSource = _processes;
    }

    private void SaveDie()
    {
        // 验证必填字段
        if (string.IsNullOrWhiteSpace(txtDieCode.Text) || txtDieCode.Text == (string?)txtDieCode.Tag)
        {
            UIStyleHelper.SetValidationError(txtDieCode, true);
            MessageBox.Show("请输入刀模编号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtDieCode.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtDieCode, false);

        if (string.IsNullOrWhiteSpace(txtCustomer.Text) || txtCustomer.Text == (string?)txtCustomer.Tag)
        {
            UIStyleHelper.SetValidationError(txtCustomer, true);
            MessageBox.Show("请输入客户名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtCustomer.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtCustomer, false);

        // 检查刀模编号是否重复
        if (!_dieId.HasValue || (_dieId.HasValue && _dieService.GetDieById(_dieId.Value)?.DieCode != txtDieCode.Text.Trim()))
        {
            if (_dieService.IsDieCodeExists(txtDieCode.Text.Trim(), _dieId))
            {
                UIStyleHelper.SetValidationError(txtDieCode, true);
                MessageBox.Show("刀模编号已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDieCode.Focus();
                return;
            }
        }
        UIStyleHelper.SetValidationError(txtDieCode, false);

        // 解析尺寸
        if (!decimal.TryParse(txtManuLength.Text, out decimal manuLength))
            manuLength = 0;
        if (!decimal.TryParse(txtManuWidth.Text, out decimal manuWidth))
            manuWidth = 0;
        if (!decimal.TryParse(txtManuHeight.Text, out decimal manuHeight))
            manuHeight = 0;
        if (!decimal.TryParse(txtBlankLength.Text, out decimal blankLength))
            blankLength = 0;
        if (!decimal.TryParse(txtBlankWidth.Text, out decimal blankWidth))
            blankWidth = 0;

        // 构建刀模对象
        var die = new DieInfo
        {
            DieID = _dieId ?? 0,
            DieCode = txtDieCode.Text.Trim(),
            CustomerName = txtCustomer.Text.Trim(),
            ProductName = txtProduct.Text.Trim(),
            Structure = txtStructure.Text.Trim(),
            ModelType = txtModelType.Text.Trim(),
            LayoutType = txtLayoutType.Text.Trim(),
            FluteType = txtFluteType.Text.Trim(),
            Material = txtMaterial.Text.Trim(),
            SourceFactory = txtSourceFactory.Text.Trim(),
            DeliveryDate = dtpDelivery.Value,
            Status = (DieStatus)cmbStatus.SelectedIndex,
            ManufactureLength = manuLength,
            ManufactureWidth = manuWidth,
            ManufactureHeight = manuHeight,
            BlankLength = blankLength,
            BlankWidth = blankWidth,
            ProcessDesc = txtProcessDesc.Text.Trim(),
            RequiredProcesses = string.Join(",", _processes.Select(p => p.ProcessName)),
            Remark = txtRemark.Text.Trim(),
            CreateUser = CurrentUser.User?.Username ?? ""
        };

        try
        {
            if (_dieId.HasValue)
            {
                // 更新
                if (_dieService.UpdateDie(die, _processes))
                {
                    ShowSuccess("保存成功");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    ShowError("保存失败");
                }
            }
            else
            {
                // 新增
                int newId = _dieService.CreateDie(die, _processes);
                if (newId > 0)
                {
                    ShowSuccess("保存成功");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    ShowError("保存失败");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"保存失败：{ex.Message}");
        }
    }

    private void SetControlsReadOnly(Control parent, bool readOnly)
    {
        foreach (Control ctrl in parent.Controls)
        {
            if (ctrl is TextBox txt)
                txt.ReadOnly = readOnly;
            else if (ctrl is ComboBox cmb)
                cmb.Enabled = !readOnly;
            else if (ctrl is DateTimePicker dtp)
                dtp.Enabled = !readOnly;
            else if (ctrl is DataGridView dgv)
                dgv.ReadOnly = readOnly;
            else if (ctrl is Button btn && btn != btnCancel)
                btn.Enabled = !readOnly;

            if (ctrl.HasChildren)
                SetControlsReadOnly(ctrl, readOnly);
        }
    }

    protected override bool SaveData()
    {
        // 此方法在基类中被调用，实际逻辑在SaveDie中实现
        return true;
    }

    #endregion
}

/// <summary>
/// 工序编辑对话框
/// </summary>
public class DieProcessEditForm : BaseDialogForm
{
    private DieProcess? _process;
    public DieProcess? Process => _process;

    public DieProcessEditForm(DieProcess? process = null)
    {
        _process = process;
        InitializeComponent();
        if (process != null)
        {
            LoadProcessData();
        }
    }

    private TextBox txtProcessName = null!;
    private ComboBox cmbStatus = null!;
    private TextBox txtOperatorNo = null!;
    private TextBox txtOperatorName = null!;
    private TextBox txtFormula = null!;
    private TextBox txtAmount = null!;

    private void InitializeComponent()
    {
        this.Text = _process == null ? "添加工序" : "编辑工序";
        this.Size = UIStyleHelper.SizeDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int y = 20;
        int labelWidth = 80;
        int rowHeight = 35;

        // 工序名称
        var lblName = UIStyleHelper.CreateLabel("工序名称：", new Point(20, y), new Size(labelWidth, 23));
        txtProcessName = UIStyleHelper.CreateTextBox(new Point(110, y), new Size(250, 23), "请输入工序名称");
        this.Controls.Add(lblName);
        this.Controls.Add(txtProcessName);
        y += rowHeight;

        // 状态
        var lblStatus = UIStyleHelper.CreateLabel("状态：", new Point(20, y), new Size(labelWidth, 23));
        cmbStatus = new ComboBox
        {
            Location = new Point(110, y),
            Size = new Size(150, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        cmbStatus.Items.AddRange(Enum.GetNames(typeof(ProcessStatus)).Select(s => ((ProcessStatus)Enum.Parse(typeof(ProcessStatus), s)).GetDisplayName()).ToArray());
        cmbStatus.SelectedIndex = 0;
        this.Controls.Add(lblStatus);
        this.Controls.Add(cmbStatus);
        y += rowHeight;

        // 操作员工号
        var lblOpNo = UIStyleHelper.CreateLabel("员工号：", new Point(20, y), new Size(labelWidth, 23));
        txtOperatorNo = UIStyleHelper.CreateTextBox(new Point(110, y), new Size(150, 23));
        this.Controls.Add(lblOpNo);
        this.Controls.Add(txtOperatorNo);
        y += rowHeight;

        // 操作员姓名
        var lblOpName = UIStyleHelper.CreateLabel("员工姓名：", new Point(20, y), new Size(labelWidth, 23));
        txtOperatorName = UIStyleHelper.CreateTextBox(new Point(110, y), new Size(150, 23));
        this.Controls.Add(lblOpName);
        this.Controls.Add(txtOperatorName);
        y += rowHeight;

        // 计算公式
        var lblFormula = UIStyleHelper.CreateLabel("计算公式：", new Point(20, y), new Size(labelWidth, 23));
        txtFormula = UIStyleHelper.CreateTextBox(new Point(110, y), new Size(250, 23));
        this.Controls.Add(lblFormula);
        this.Controls.Add(txtFormula);
        y += rowHeight;

        // 金额
        var lblAmount = UIStyleHelper.CreateLabel("金额：", new Point(20, y), new Size(labelWidth, 23));
        txtAmount = UIStyleHelper.CreateTextBox(new Point(110, y), new Size(150, 23), "0.00");
        this.Controls.Add(lblAmount);
        this.Controls.Add(txtAmount);
        y += rowHeight + 20;

        // 按钮
        var btnOk = UIStyleHelper.CreateSaveButton("确定");
        btnOk.Location = new Point(110, y);
        btnOk.Click += BtnOk_Click;
        this.Controls.Add(btnOk);

        var btnCancel = UIStyleHelper.CreateCancelButton();
        btnCancel.Location = new Point(220, y);
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;
        this.Controls.Add(btnCancel);

        // 注册回车跳转
        RegisterEnterToNext();
    }

    private void LoadProcessData()
    {
        if (_process == null) return;
        txtProcessName.Text = _process.ProcessName;
        cmbStatus.SelectedIndex = (int)_process.Status;
        txtOperatorNo.Text = _process.OperatorNo;
        txtOperatorName.Text = _process.OperatorName;
        txtFormula.Text = _process.Formula ?? "";
        txtAmount.Text = _process.Amount?.ToString() ?? "";
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtProcessName.Text) || txtProcessName.Text == (string?)txtProcessName.Tag)
        {
            UIStyleHelper.SetValidationError(txtProcessName, true);
            MessageBox.Show("请输入工序名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        UIStyleHelper.SetValidationError(txtProcessName, false);

        decimal? amount = null;
        if (!string.IsNullOrWhiteSpace(txtAmount.Text) && txtAmount.Text != (string?)txtAmount.Tag && decimal.TryParse(txtAmount.Text, out decimal amt))
        {
            amount = amt;
        }

        _process = new DieProcess
        {
            ProcessID = _process?.ProcessID ?? 0,
            ProcessName = txtProcessName.Text.Trim(),
            Status = (ProcessStatus)cmbStatus.SelectedIndex,
            OperatorNo = txtOperatorNo.Text.Trim(),
            OperatorName = txtOperatorName.Text.Trim(),
            Formula = string.IsNullOrWhiteSpace(txtFormula.Text) ? null : txtFormula.Text.Trim(),
            Amount = amount
        };

        this.DialogResult = DialogResult.OK;
    }
}
