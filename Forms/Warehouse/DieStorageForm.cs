using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Warehouse;

public partial class DieStorageForm : BaseListForm
{
    private readonly WarehouseService _warehouseService;
    private readonly DieService _dieService;
    private List<DieInfo> _completedDies = new();
    private List<StorageLocation> _availableLocations = new();

    public DieStorageForm()
    {
        _warehouseService = new WarehouseService();
        _dieService = new DieService();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "刀模入库";
        this.Size = UIStyleHelper.SizeListForm;
        this.StartPosition = FormStartPosition.CenterParent;

        // 创建工具栏
        var toolPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(10, 5, 10, 5)
        };

        // 搜索区域
        var lblSearch = UIStyleHelper.CreateLabel("搜索：", new Point(10, 12), new Size(50, 25));
        txtSearch = UIStyleHelper.CreateTextBox(new Point(60, 9), new Size(200, 25), "输入刀模编号或客户名称");

        btnSearch = UIStyleHelper.CreateSearchButton();
        btnSearch.Location = new Point(270, 8);
        btnSearch.Click += (s, e) => SearchRecords();

        btnClear = UIStyleHelper.CreateCancelButton("清空");
        btnClear.Location = new Point(380, 8);
        btnClear.Click += (s, e) => ClearFilters();

        btnRefresh = new Button { Text = "刷新", Location = new Point(490, 8), Size = UIStyleHelper.SizeButton };
        ApplyButtonStyle(btnRefresh, ButtonStyle.Default);
        btnRefresh.Click += (s, e) => LoadData();

        btnInStock = UIStyleHelper.CreateAddButton("入库");
        btnInStock.Location = new Point(600, 8);
        btnInStock.Click += (s, e) => ShowInStockDialog();

        toolPanel.Controls.Add(lblSearch);
        toolPanel.Controls.Add(txtSearch);
        toolPanel.Controls.Add(btnSearch);
        toolPanel.Controls.Add(btnClear);
        toolPanel.Controls.Add(btnRefresh);
        toolPanel.Controls.Add(btnInStock);

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
            BorderStyle = BorderStyle.None
        };
        ApplyDataGridViewStyle(dgvRecords);

        // 添加列
        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "DieID",
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
            DataPropertyName = "DieType",
            HeaderText = "刀模类型",
            Width = 100
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Size",
            HeaderText = "规格尺寸",
            Width = 100
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "StatusText",
            HeaderText = "状态",
            Width = 80
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "CreateTime",
            HeaderText = "创建时间",
            Width = 130,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm" }
        });

        dgvRecords.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Remark",
            HeaderText = "备注",
            Width = 200
        });

        // 添加右键菜单
        var contextMenu = UIStyleHelper.CreateDataGridViewContextMenu(
            onView: null,
            onEdit: () => ShowInStockDialog(),
            onDelete: null
        );
        dgvRecords.ContextMenuStrip = contextMenu;

        // 状态栏
        var statusStrip = CreateStatusBar();

        // 布局
        var panelContent = new Panel { Dock = DockStyle.Fill };
        panelContent.Controls.Add(dgvRecords);

        this.Controls.Add(panelContent);
        this.Controls.Add(toolPanel);
        this.Controls.Add(statusStrip);
    }

    private DataGridView dgvRecords = null!;
    private TextBox txtSearch = null!;
    private Button btnSearch = null!;
    private Button btnClear = null!;
    private Button btnRefresh = null!;
    private Button btnInStock = null!;

    protected override void LoadData()
    {
        Form? loadingForm = null;
        try
        {
            loadingForm = UIStyleHelper.ShowLoading(this, "正在加载数据...");

            // 加载已完工但未入库的刀模
            _completedDies = _dieService.GetCompletedDiesNotInStock();
            dgvRecords.DataSource = _completedDies;

            // 更新状态栏
            if (StatusUserLabel != null)
            {
                StatusUserLabel.Text = $"共 {_completedDies.Count} 条待入库记录";
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载数据失败：{ex.Message}");
        }
        finally
        {
            loadingForm?.Close();
        }
    }

    private void SearchRecords()
    {
        try
        {
            var keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword) || keyword == (string?)txtSearch.Tag)
            {
                LoadData();
                return;
            }

            var filtered = _completedDies.Where(d =>
                d.DieCode.Contains(keyword) ||
                d.CustomerName.Contains(keyword) ||
                d.ProductName.Contains(keyword)).ToList();

            dgvRecords.DataSource = filtered;

            if (StatusUserLabel != null)
            {
                StatusUserLabel.Text = $"共 {filtered.Count} 条记录";
            }
        }
        catch (Exception ex)
        {
            ShowError($"搜索失败：{ex.Message}");
        }
    }

    private void ClearFilters()
    {
        txtSearch.Clear();
        LoadData();
    }

    private void ShowInStockDialog()
    {
        if (dgvRecords.SelectedRows.Count == 0)
        {
            MessageBox.Show("请选择要入库的刀模", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var dieId = (int)dgvRecords.SelectedRows[0].Cells["DieID"].Value;
        var die = _completedDies.FirstOrDefault(d => d.DieID == dieId);

        if (die == null) return;

        using var form = new DieInStockEditForm(die);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            ShowSuccess("入库成功");
            LoadData();
        }
    }
}

// 刀模入库编辑窗体
public class DieInStockEditForm : BaseDialogForm
{
    private readonly WarehouseService _warehouseService;
    private readonly DieInfo _die;
    private List<StorageLocation> _availableLocations = new();

    public DieInStockEditForm(DieInfo die)
    {
        _die = die;
        _warehouseService = new WarehouseService();
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "刀模入库";
        this.Size = UIStyleHelper.SizeDialog;
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
            Text = "刀模入库登记",
            Font = UIStyleHelper.GetLargeTitleFont(),
            AutoSize = true,
            Location = new Point(220, y)
        };
        y += 50;

        // 刀模编号
        var lblDieCode = UIStyleHelper.CreateLabel("刀模编号：", new Point(leftMargin, y), new Size(labelWidth, 25));
        lblDieCodeValue = new Label
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25),
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Bold, GraphicsUnit.Point, 134),
            ForeColor = UIStyleHelper.ColorInfo
        };
        y += 40;

        // 客户名称
        var lblCustomer = UIStyleHelper.CreateLabel("客户名称：", new Point(leftMargin, y), new Size(labelWidth, 25));
        lblCustomerValue = new Label
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25)
        };
        y += 40;

        // 产品名称
        var lblProduct = UIStyleHelper.CreateLabel("产品名称：", new Point(leftMargin, y), new Size(labelWidth, 25));
        lblProductValue = new Label
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25)
        };
        y += 40;

        // 规格尺寸
        var lblSize = UIStyleHelper.CreateLabel("规格尺寸：", new Point(leftMargin, y), new Size(labelWidth, 25));
        lblSizeValue = new Label
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25)
        };
        y += 40;

        // 入库库位选择
        var lblLocation = UIStyleHelper.CreateLabel("入库库位：", new Point(leftMargin, y), new Size(labelWidth, 25));
        cboLocation = new ComboBox
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(250, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        y += 40;

        // 入库时间
        var lblInStockTime = UIStyleHelper.CreateLabel("入库时间：", new Point(leftMargin, y), new Size(labelWidth, 25));
        dtpInStockTime = new DateTimePicker
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(200, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            Value = DateTime.Now,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        y += 40;

        // 入库操作人
        var lblOperator = UIStyleHelper.CreateLabel("入库操作人：", new Point(leftMargin, y), new Size(labelWidth, 25));
        txtOperator = UIStyleHelper.CreateTextBox(new Point(leftMargin + labelWidth, y), new Size(200, 25));
        // 默认填充当前用户
        if (CurrentUser.User != null)
        {
            txtOperator.Text = CurrentUser.User.RealName ?? CurrentUser.User.Username;
        }
        y += 40;

        // 入库备注
        var lblRemark = UIStyleHelper.CreateLabel("入库备注：", new Point(leftMargin, y), new Size(labelWidth, 25));
        txtRemark = new TextBox
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 60),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        y += 80;

        // 按钮
        var btnSave = UIStyleHelper.CreateSaveButton("确认入库");
        btnSave.Size = new Size(120, 35);
        btnSave.Location = new Point(180, y);
        btnSave.Click += BtnSave_Click;

        var btnCancel = UIStyleHelper.CreateCancelButton();
        btnCancel.Size = new Size(100, 35);
        btnCancel.Location = new Point(320, y);
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] {
            lblTitle,
            lblDieCode, lblDieCodeValue, lblCustomer, lblCustomerValue, lblProduct, lblProductValue,
            lblSize, lblSizeValue, lblLocation, cboLocation, lblInStockTime, dtpInStockTime,
            lblOperator, txtOperator, lblRemark, txtRemark,
            btnSave, btnCancel
        });

        // 注册回车跳转
        RegisterEnterToNext();
    }

    private void LoadData()
    {
        try
        {
            // 显示刀模信息
            lblDieCodeValue.Text = _die.DieCode;
            lblCustomerValue.Text = _die.CustomerName;
            lblProductValue.Text = _die.ProductName;
            lblSizeValue.Text = _die.ManufactureSize;

            // 加载空闲库位
            _availableLocations = _warehouseService.GetLocationsByStatus(LocationStatus.Free);

            if (_availableLocations.Count == 0)
            {
                MessageBox.Show("当前没有空闲库位，请先添加库位", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 创建显示列表
            var displayList = _availableLocations.Select(l => new
            {
                l.LocationID,
                Display = $"{l.LocationCode} ({l.Area}-{l.ShelfNo}-{l.LayerNo}-{l.PositionNo})"
            }).ToList();

            cboLocation.DataSource = displayList;
            cboLocation.DisplayMember = "Display";
            cboLocation.ValueMember = "LocationID";

            if (cboLocation.Items.Count > 0)
            {
                cboLocation.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载数据失败：{ex.Message}");
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (cboLocation.SelectedValue == null)
        {
            MessageBox.Show("请选择入库库位", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtOperator.Text))
        {
            UIStyleHelper.SetValidationError(txtOperator, true);
            MessageBox.Show("请输入入库操作人", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtOperator.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtOperator, false);

        try
        {
            var locationId = (int)cboLocation.SelectedValue;
            var operatorName = txtOperator.Text.Trim();

            var result = _warehouseService.InStockDie(_die.DieID, locationId, dtpInStockTime.Value, operatorName, txtRemark.Text.Trim());

            if (result)
            {
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                ShowError("入库失败");
            }
        }
        catch (Exception ex)
        {
            ShowError($"入库失败：{ex.Message}");
        }
    }

    private Label lblDieCodeValue = null!;
    private Label lblCustomerValue = null!;
    private Label lblProductValue = null!;
    private Label lblSizeValue = null!;
    private ComboBox cboLocation = null!;
    private DateTimePicker dtpInStockTime = null!;
    private TextBox txtOperator = null!;
    private TextBox txtRemark = null!;
}
