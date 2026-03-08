using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Warehouse;

public partial class DieStorageForm : Form
{
    private readonly WarehouseService _warehouseService;
    private readonly DieService _dieService;
    private List<DieInfo> _completedDies = new();
    private List<StorageLocation> _availableLocations = new();

    public DieStorageForm()
    {
        InitializeComponent();
        _warehouseService = new WarehouseService();
        _dieService = new DieService();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "刀模入库";
        this.Size = new Size(900, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        // 创建工具栏
        var toolStrip = new ToolStrip();
        
        var btnRefresh = new ToolStripButton("刷新") { Image = SystemIcons.Question.ToBitmap() };
        btnRefresh.Click += (s, e) => LoadData();
        
        var btnInStock = new ToolStripButton("入库") { Image = SystemIcons.Question.ToBitmap() };
        btnInStock.Click += (s, e) => ShowInStockDialog();

        toolStrip.Items.AddRange(new ToolStripItem[] { btnRefresh, new ToolStripSeparator(), btnInStock });

        // 搜索区域
        var panelSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(10, 5, 10, 5)
        };

        var lblSearch = new Label { Text = "搜索：", Location = new Point(10, 12), AutoSize = true };
        txtSearch = new TextBox { Location = new Point(50, 9), Size = new Size(200, 25) };
        
        var btnSearch = new Button { Text = "查询", Location = new Point(260, 8), Size = new Size(80, 28) };
        btnSearch.Click += (s, e) => SearchRecords();
        
        var btnClear = new Button { Text = "清空", Location = new Point(350, 8), Size = new Size(80, 28) };
        btnClear.Click += (s, e) => ClearFilters();

        panelSearch.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnSearch, btnClear });

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
            // 加载已完工但未入库的刀模
            _completedDies = _dieService.GetCompletedDiesNotInStock();
            dgvRecords.DataSource = _completedDies;
            lblStatus.Text = $"共 {_completedDies.Count} 条待入库记录";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SearchRecords()
    {
        try
        {
            var keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                LoadData();
                return;
            }

            var filtered = _completedDies.Where(d => 
                d.DieCode.Contains(keyword) || 
                d.CustomerName.Contains(keyword) || 
                d.ProductName.Contains(keyword)).ToList();
            
            dgvRecords.DataSource = filtered;
            lblStatus.Text = $"共 {filtered.Count} 条记录";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"搜索失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            LoadData();
        }
    }

    private DataGridView dgvRecords = null!;
    private TextBox txtSearch = null!;
    private ToolStripStatusLabel lblStatus = null!;
}

// 刀模入库编辑窗体
public class DieInStockEditForm : Form
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
        this.Size = new Size(600, 450);
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

        // 规格尺寸
        var lblSize = new Label { Text = "规格尺寸：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblSizeValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25)
        };
        y += 40;

        // 入库库位选择
        var lblLocation = new Label { Text = "入库库位：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        cboLocation = new ComboBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(250, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        y += 40;

        // 入库时间
        var lblInStockTime = new Label { Text = "入库时间：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        dtpInStockTime = new DateTimePicker 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(200, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            Value = DateTime.Now
        };
        y += 40;

        // 入库操作人
        var lblOperator = new Label { Text = "入库操作人：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        txtOperator = new TextBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(200, 25)
        };
        // 默认填充当前用户
        if (CurrentUser.User != null)
        {
            txtOperator.Text = CurrentUser.User.RealName ?? CurrentUser.User.Username;
        }
        y += 40;

        // 入库备注
        var lblRemark = new Label { Text = "入库备注：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        txtRemark = new TextBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 60),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        y += 80;

        // 按钮
        var btnSave = new Button { Text = "确认入库", Location = new Point(180, y), Size = new Size(120, 35) };
        btnSave.Click += BtnSave_Click;

        var btnCancel = new Button { Text = "取消", Location = new Point(320, y), Size = new Size(100, 35) };
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] {
            lblTitle,
            lblDieCode, lblDieCodeValue, lblCustomer, lblCustomerValue, lblProduct, lblProductValue,
            lblSize, lblSizeValue, lblLocation, cboLocation, lblInStockTime, dtpInStockTime,
            lblOperator, txtOperator, lblRemark, txtRemark,
            btnSave, btnCancel
        });
    }

    private void LoadData()
    {
        try
        {
            // 显示刀模信息
            lblDieCodeValue.Text = _die.DieCode;
            lblCustomerValue.Text = _die.CustomerName;
            lblProductValue.Text = _die.ProductName;
            lblSizeValue.Text = _die.Size;

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
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show("请输入入库操作人", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtOperator.Focus();
            return;
        }

        try
        {
            var locationId = (int)cboLocation.SelectedValue;
            var operatorName = txtOperator.Text.Trim();

            var result = _warehouseService.InStockDie(_die.DieID, locationId, dtpInStockTime.Value, operatorName, txtRemark.Text.Trim());
            
            if (result)
            {
                MessageBox.Show("入库成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("入库失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"入库失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
