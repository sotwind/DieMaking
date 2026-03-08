using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Production;

public partial class ProcessReportForm : BaseForm
{
    private readonly ProductionService _productionService;
    private ComboBox _cmbDie = null!;
    private DataGridView _dgvProcesses = null!;
    private TextBox _txtOperatorNo = null!;
    private TextBox _txtOperatorName = null!;
    private TextBox _txtAmount = null!;
    private TextBox _txtRemark = null!;
    private Button _btnStart = null!;
    private Button _btnComplete = null!;
    private Label _lblDieInfo = null!;
    private Label _lblSelectedProcess = null!;
    private int? _selectedProcessId;

    public ProcessReportForm()
    {
        _productionService = new ProductionService();
        InitializeComponent();
        this.Text = "工序报产";
    }

    private void InitializeComponent()
    {
        this.Size = UIStyleHelper.SizeListForm;
        this.StartPosition = FormStartPosition.CenterParent;

        // 左侧选择区域
        var panelLeft = new Panel
        {
            Dock = DockStyle.Left,
            Width = 350,
            Padding = new Padding(10),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblTitle = new Label
        {
            Text = "工序报产",
            Font = UIStyleHelper.GetTitleFont(),
            Location = new Point(10, 10),
            AutoSize = true
        };

        var lblDie = UIStyleHelper.CreateLabel("选择刀模：", new Point(10, 50), new Size(70, 23));

        _cmbDie = new ComboBox
        {
            Location = new Point(10, 75),
            Width = 320,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        _cmbDie.SelectedIndexChanged += CmbDie_SelectedIndexChanged;

        _lblDieInfo = new Label
        {
            Location = new Point(10, 105),
            Size = new Size(320, 40),
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134),
            ForeColor = UIStyleHelper.ColorInfo
        };

        var lblProcessList = UIStyleHelper.CreateLabel("工序列表：", new Point(10, 155), new Size(70, 23));

        // 工序列表
        _dgvProcesses = new DataGridView
        {
            Location = new Point(10, 180),
            Size = new Size(320, 280),
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D
        };
        ApplyDataGridViewStyle(_dgvProcesses);

        _dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ProcessID",
            HeaderText = "ID",
            Visible = false
        });

        _dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ProcessName",
            HeaderText = "工序名称",
            Width = 100
        });

        _dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "StatusText",
            HeaderText = "状态",
            Width = 80
        });

        _dgvProcesses.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "OperatorName",
            HeaderText = "操作员",
            Width = 80
        });

        _dgvProcesses.SelectionChanged += DgvProcesses_SelectionChanged;

        panelLeft.Controls.AddRange(new Control[]
        {
            lblTitle, lblDie, _cmbDie, _lblDieInfo, lblProcessList, _dgvProcesses
        });

        // 右侧操作区域
        var panelRight = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var lblOperationTitle = new Label
        {
            Text = "报产操作",
            Font = UIStyleHelper.GetTitleFont(),
            Location = new Point(20, 20),
            AutoSize = true
        };

        _lblSelectedProcess = new Label
        {
            Text = "请选择工序",
            Location = new Point(20, 60),
            Size = new Size(400, 25),
            Font = new Font(UIStyleHelper.FontName, 10f, FontStyle.Bold, GraphicsUnit.Point, 134),
            ForeColor = UIStyleHelper.ColorSuccess
        };

        var lblOperatorNo = UIStyleHelper.CreateLabel("工号：", new Point(20, 100), new Size(50, 23));

        _txtOperatorNo = UIStyleHelper.CreateTextBox(new Point(80, 96), new Size(150, 23), "请输入工号");

        var lblOperatorName = UIStyleHelper.CreateLabel("姓名：", new Point(250, 100), new Size(50, 23));

        _txtOperatorName = UIStyleHelper.CreateTextBox(new Point(310, 96), new Size(150, 23), "请输入姓名");

        // 自动填充当前用户信息
        if (CurrentUser.User != null)
        {
            _txtOperatorNo.Text = CurrentUser.User.UserID.ToString();
            _txtOperatorName.Text = CurrentUser.User.RealName;
        }

        var lblAmount = UIStyleHelper.CreateLabel("金额：", new Point(20, 140), new Size(50, 23));

        _txtAmount = UIStyleHelper.CreateTextBox(new Point(80, 136), new Size(150, 23), "0.00");

        var lblRemark = UIStyleHelper.CreateLabel("备注：", new Point(20, 180), new Size(50, 23));

        _txtRemark = new TextBox
        {
            Location = new Point(80, 176),
            Width = 380,
            Height = 60,
            Multiline = true,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        _btnStart = UIStyleHelper.CreateAddButton("开始生产");
        _btnStart.Size = new Size(120, 40);
        _btnStart.Location = new Point(80, 260);
        _btnStart.Enabled = false;
        _btnStart.Click += BtnStart_Click;

        _btnComplete = UIStyleHelper.CreateSaveButton("完成生产");
        _btnComplete.Size = new Size(120, 40);
        _btnComplete.Location = new Point(220, 260);
        _btnComplete.Enabled = false;
        _btnComplete.Click += BtnComplete_Click;

        // 状态说明
        var lblStatusTip = new Label
        {
            Text = "状态说明：\n• 待生产（橙色）- 可以开始生产\n• 生产中（蓝色）- 可以完成生产\n• 已完成（绿色）- 已完工",
            Location = new Point(20, 320),
            Size = new Size(400, 80),
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134),
            ForeColor = Color.Gray
        };

        panelRight.Controls.AddRange(new Control[]
        {
            lblOperationTitle, _lblSelectedProcess,
            lblOperatorNo, _txtOperatorNo, lblOperatorName, _txtOperatorName,
            lblAmount, _txtAmount, lblRemark, _txtRemark,
            _btnStart, _btnComplete, lblStatusTip
        });

        // 状态栏
        var statusStrip = CreateStatusBar();

        this.Controls.Add(panelRight);
        this.Controls.Add(panelLeft);
        this.Controls.Add(statusStrip);

        // 注册回车跳转
        RegisterEnterToNext();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LoadDieList();
    }

    private void LoadDieList()
    {
        try
        {
            var dies = _productionService.GetAvailableDiesForReport();

            _cmbDie.DisplayMember = "DisplayText";
            _cmbDie.ValueMember = "DieID";
            _cmbDie.DataSource = dies;

            if (_cmbDie.Items.Count > 0)
            {
                _cmbDie.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载刀模列表失败：{ex.Message}");
        }
    }

    private void CmbDie_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_cmbDie.SelectedItem is DieInfoForReport die)
        {
            _lblDieInfo.Text = $"客户：{die.CustomerName}\n产品：{die.ProductName}";
            LoadProcessList(die.DieID);
        }
    }

    private void LoadProcessList(int dieId)
    {
        try
        {
            var processes = _productionService.GetDieProcessesForReport(dieId);

            _dgvProcesses.Rows.Clear();

            foreach (var process in processes)
            {
                var rowIndex = _dgvProcesses.Rows.Add();
                var row = _dgvProcesses.Rows[rowIndex];

                row.Cells["ProcessID"].Value = process.ProcessID;
                row.Cells["ProcessName"].Value = process.ProcessName;
                row.Cells["StatusText"].Value = process.StatusText;
                row.Cells["OperatorName"].Value = process.OperatorName;

                // 根据状态设置行颜色
                switch (process.Status)
                {
                    case ProcessStatus.Pending:
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(133, 100, 4);
                        break;
                    case ProcessStatus.InProgress:
                        row.DefaultCellStyle.BackColor = Color.FromArgb(204, 229, 255);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 51, 102);
                        break;
                    case ProcessStatus.Completed:
                        row.DefaultCellStyle.BackColor = Color.FromArgb(212, 237, 218);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(21, 87, 36);
                        break;
                }

                row.Tag = process;
            }

            // 清除选择
            _selectedProcessId = null;
            _lblSelectedProcess.Text = "请选择工序";
            _btnStart.Enabled = false;
            _btnComplete.Enabled = false;
        }
        catch (Exception ex)
        {
            ShowError($"加载工序列表失败：{ex.Message}");
        }
    }

    private void DgvProcesses_SelectionChanged(object? sender, EventArgs e)
    {
        if (_dgvProcesses.SelectedRows.Count > 0)
        {
            var row = _dgvProcesses.SelectedRows[0];
            if (row.Tag is DieProcessForReport process)
            {
                _selectedProcessId = process.ProcessID;
                _lblSelectedProcess.Text = $"已选择：{process.ProcessName} ({process.StatusText})";

                // 根据状态启用按钮
                _btnStart.Enabled = process.CanStart;
                _btnComplete.Enabled = process.CanComplete;

                // 如果已有操作员信息，显示出来
                if (!string.IsNullOrEmpty(process.OperatorNo))
                {
                    _txtOperatorNo.Text = process.OperatorNo;
                    _txtOperatorName.Text = process.OperatorName;
                }

                // 如果已有金额，显示出来
                if (process.Amount.HasValue)
                {
                    _txtAmount.Text = process.Amount.Value.ToString("N2");
                }
                else
                {
                    _txtAmount.Clear();
                }
            }
        }
        else
        {
            _selectedProcessId = null;
            _lblSelectedProcess.Text = "请选择工序";
            _btnStart.Enabled = false;
            _btnComplete.Enabled = false;
        }
    }

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        if (!_selectedProcessId.HasValue)
            return;

        // 验证输入
        if (string.IsNullOrEmpty(_txtOperatorNo.Text.Trim()) || _txtOperatorNo.Text == (string?)_txtOperatorNo.Tag)
        {
            UIStyleHelper.SetValidationError(_txtOperatorNo, true);
            MessageBox.Show("请输入工号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtOperatorNo.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(_txtOperatorNo, false);

        if (string.IsNullOrEmpty(_txtOperatorName.Text.Trim()) || _txtOperatorName.Text == (string?)_txtOperatorName.Tag)
        {
            UIStyleHelper.SetValidationError(_txtOperatorName, true);
            MessageBox.Show("请输入姓名", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtOperatorName.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(_txtOperatorName, false);

        // 检查前道工序是否已完成
        if (!_productionService.IsPrevProcessCompleted(_selectedProcessId.Value))
        {
            MessageBox.Show("前道工序尚未完成，无法开始本工序", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var success = _productionService.StartProcess(
                _selectedProcessId.Value,
                _txtOperatorNo.Text.Trim(),
                _txtOperatorName.Text.Trim()
            );

            if (success)
            {
                ShowSuccess("工序开始成功！");

                // 刷新列表
                if (_cmbDie.SelectedItem is DieInfoForReport die)
                {
                    LoadProcessList(die.DieID);
                }
            }
            else
            {
                ShowError("工序开始失败，请重试");
            }
        }
        catch (Exception ex)
        {
            ShowError($"操作失败：{ex.Message}");
        }
    }

    private void BtnComplete_Click(object? sender, EventArgs e)
    {
        if (!_selectedProcessId.HasValue)
            return;

        // 验证输入
        if (string.IsNullOrEmpty(_txtOperatorNo.Text.Trim()) || _txtOperatorNo.Text == (string?)_txtOperatorNo.Tag)
        {
            UIStyleHelper.SetValidationError(_txtOperatorNo, true);
            MessageBox.Show("请输入工号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtOperatorNo.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(_txtOperatorNo, false);

        if (string.IsNullOrEmpty(_txtOperatorName.Text.Trim()) || _txtOperatorName.Text == (string?)_txtOperatorName.Tag)
        {
            UIStyleHelper.SetValidationError(_txtOperatorName, true);
            MessageBox.Show("请输入姓名", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtOperatorName.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(_txtOperatorName, false);

        // 解析金额
        decimal? amount = null;
        if (!string.IsNullOrEmpty(_txtAmount.Text.Trim()) && _txtAmount.Text != (string?)_txtAmount.Tag)
        {
            if (!decimal.TryParse(_txtAmount.Text.Trim(), out var parsedAmount))
            {
                UIStyleHelper.SetValidationError(_txtAmount, true);
                MessageBox.Show("金额格式不正确", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtAmount.Focus();
                return;
            }
            amount = parsedAmount;
        }
        UIStyleHelper.SetValidationError(_txtAmount, false);

        try
        {
            var success = _productionService.CompleteProcess(
                _selectedProcessId.Value,
                amount,
                _txtOperatorNo.Text.Trim(),
                _txtOperatorName.Text.Trim(),
                _txtRemark.Text.Trim()
            );

            if (success)
            {
                ShowSuccess("工序完成成功！");

                // 清空输入
                _txtAmount.Clear();
                _txtRemark.Clear();

                // 刷新列表
                if (_cmbDie.SelectedItem is DieInfoForReport die)
                {
                    LoadProcessList(die.DieID);
                }
            }
            else
            {
                ShowError("工序完成失败，请重试");
            }
        }
        catch (Exception ex)
        {
            ShowError($"操作失败：{ex.Message}");
        }
    }
}
