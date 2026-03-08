using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Warehouse;

public partial class LocationManageForm : Form
{
    private readonly WarehouseService _warehouseService;
    private BindingSource _bindingSource = new();
    private List<StorageLocation> _locations = new();

    public LocationManageForm()
    {
        InitializeComponent();
        _warehouseService = new WarehouseService();
        LoadLocations();
    }

    private void InitializeComponent()
    {
        this.Text = "库位管理";
        this.Size = new Size(1000, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        // 创建工具栏
        var toolStrip = new ToolStrip();
        
        var btnAdd = new ToolStripButton("新增") { Image = SystemIcons.Question.ToBitmap() };
        btnAdd.Click += (s, e) => AddLocation();
        
        var btnEdit = new ToolStripButton("编辑") { Image = SystemIcons.Question.ToBitmap() };
        btnEdit.Click += (s, e) => EditLocation();
        
        var btnDelete = new ToolStripButton("删除") { Image = SystemIcons.Question.ToBitmap() };
        btnDelete.Click += (s, e) => DeleteLocation();
        
        var btnRefresh = new ToolStripButton("刷新") { Image = SystemIcons.Question.ToBitmap() };
        btnRefresh.Click += (s, e) => LoadLocations();

        toolStrip.Items.AddRange(new ToolStripItem[] { btnAdd, btnEdit, btnDelete, new ToolStripSeparator(), btnRefresh });

        // 搜索区域
        var panelSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 45,
            Padding = new Padding(10, 5, 10, 5)
        };

        var lblSearch = new Label
        {
            Text = "搜索：",
            Location = new Point(10, 12),
            AutoSize = true
        };

        txtSearch = new TextBox
        {
            Location = new Point(60, 9),
            Size = new Size(200, 25)
        };

        var btnSearch = new Button
        {
            Text = "查询",
            Location = new Point(270, 8),
            Size = new Size(80, 28)
        };
        btnSearch.Click += (s, e) => SearchLocations();

        var btnClear = new Button
        {
            Text = "清空",
            Location = new Point(360, 8),
            Size = new Size(80, 28)
        };
        btnClear.Click += (s, e) => { txtSearch.Clear(); LoadLocations(); };

        panelSearch.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnSearch, btnClear });

        // 数据表格
        dgvLocations = new DataGridView
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
        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "LocationID",
            HeaderText = "ID",
            Width = 60,
            Visible = false
        });

        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "LocationCode",
            HeaderText = "库位编号",
            Width = 120
        });

        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Area",
            HeaderText = "区域",
            Width = 100
        });

        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "ShelfNo",
            HeaderText = "货架号",
            Width = 80
        });

        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "LayerNo",
            HeaderText = "层号",
            Width = 80
        });

        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "PositionNo",
            HeaderText = "位置号",
            Width = 80
        });

        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Description",
            HeaderText = "描述",
            Width = 200
        });

        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "StatusText",
            HeaderText = "状态",
            Width = 80
        });

        dgvLocations.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "CreateTime",
            HeaderText = "创建时间",
            Width = 150,
            DefaultCellStyle = { Format = "yyyy-MM-dd HH:mm:ss" }
        });

        dgvLocations.DoubleClick += (s, e) => EditLocation();

        // 状态栏
        var statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel("就绪");
        statusStrip.Items.Add(lblStatus);

        // 布局
        var panelContent = new Panel { Dock = DockStyle.Fill };
        panelContent.Controls.Add(dgvLocations);

        this.Controls.Add(panelContent);
        this.Controls.Add(panelSearch);
        this.Controls.Add(toolStrip);
        this.Controls.Add(statusStrip);
    }

    private void LoadLocations()
    {
        try
        {
            _locations = _warehouseService.GetAllLocations();
            _bindingSource.DataSource = _locations;
            dgvLocations.DataSource = _bindingSource;
            lblStatus.Text = $"共 {_locations.Count} 条记录";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SearchLocations()
    {
        try
        {
            var keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                LoadLocations();
                return;
            }

            _locations = _warehouseService.SearchLocations(keyword);
            _bindingSource.DataSource = _locations;
            dgvLocations.DataSource = _bindingSource;
            lblStatus.Text = $"共 {_locations.Count} 条记录";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"搜索失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddLocation()
    {
        using var form = new LocationEditForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                var location = form.Location;
                if (_warehouseService.IsLocationCodeExists(location.LocationCode))
                {
                    MessageBox.Show("库位编号已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var id = _warehouseService.CreateLocation(location);
                if (id > 0)
                {
                    MessageBox.Show("新增成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadLocations();
                }
                else
                {
                    MessageBox.Show("新增失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"新增失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void EditLocation()
    {
        if (dgvLocations.CurrentRow == null) return;

        var location = (StorageLocation)dgvLocations.CurrentRow.DataBoundItem;
        using var form = new LocationEditForm(location);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                var updatedLocation = form.Location;
                if (_warehouseService.IsLocationCodeExists(updatedLocation.LocationCode, updatedLocation.LocationID))
                {
                    MessageBox.Show("库位编号已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_warehouseService.UpdateLocation(updatedLocation))
                {
                    MessageBox.Show("更新成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadLocations();
                }
                else
                {
                    MessageBox.Show("更新失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"更新失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void DeleteLocation()
    {
        if (dgvLocations.CurrentRow == null) return;

        var location = (StorageLocation)dgvLocations.CurrentRow.DataBoundItem;
        
        if (location.Status == LocationStatus.Occupied)
        {
            MessageBox.Show("该库位已被占用，不能删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show($"确定要删除库位 [{location.LocationCode}] 吗？", "确认", 
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            try
            {
                if (_warehouseService.DeleteLocation(location.LocationID))
                {
                    MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadLocations();
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

    private DataGridView dgvLocations = null!;
    private TextBox txtSearch = null!;
    private ToolStripStatusLabel lblStatus = null!;
}

// 库位编辑窗体
public class LocationEditForm : Form
{
    public StorageLocation Location { get; private set; }
    private bool _isEdit;

    public LocationEditForm(StorageLocation? location = null)
    {
        _isEdit = location != null;
        Location = location ?? new StorageLocation { Status = LocationStatus.Free };
        InitializeComponent();
        if (_isEdit) LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = _isEdit ? "编辑库位" : "新增库位";
        this.Size = new Size(450, 350);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int y = 20;
        int labelWidth = 80;
        int textBoxWidth = 280;

        // 库位编号
        var lblCode = new Label { Text = "库位编号：", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
        txtCode = new TextBox { Location = new Point(110, y), Size = new Size(textBoxWidth, 25) };
        y += 35;

        // 区域
        var lblArea = new Label { Text = "区域：", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
        txtArea = new TextBox { Location = new Point(110, y), Size = new Size(textBoxWidth, 25) };
        y += 35;

        // 货架号
        var lblShelf = new Label { Text = "货架号：", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
        txtShelf = new TextBox { Location = new Point(110, y), Size = new Size(textBoxWidth, 25) };
        y += 35;

        // 层号
        var lblLayer = new Label { Text = "层号：", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
        txtLayer = new TextBox { Location = new Point(110, y), Size = new Size(textBoxWidth, 25) };
        y += 35;

        // 位置号
        var lblPosition = new Label { Text = "位置号：", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
        txtPosition = new TextBox { Location = new Point(110, y), Size = new Size(textBoxWidth, 25) };
        y += 35;

        // 描述
        var lblDesc = new Label { Text = "描述：", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
        txtDesc = new TextBox { Location = new Point(110, y), Size = new Size(textBoxWidth, 25) };
        y += 35;

        // 状态
        var lblStatus = new Label { Text = "状态：", Location = new Point(20, y), Size = new Size(labelWidth, 25) };
        cboStatus = new ComboBox 
        { 
            Location = new Point(110, y), 
            Size = new Size(textBoxWidth, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboStatus.Items.Add(new { Text = "空闲", Value = LocationStatus.Free });
        cboStatus.Items.Add(new { Text = "占用", Value = LocationStatus.Occupied });
        cboStatus.Items.Add(new { Text = "禁用", Value = LocationStatus.Disabled });
        cboStatus.DisplayMember = "Text";
        cboStatus.ValueMember = "Value";
        cboStatus.SelectedIndex = 0;
        y += 50;

        // 按钮
        var btnSave = new Button { Text = "保存", Location = new Point(110, y), Size = new Size(100, 30) };
        btnSave.Click += Save_Click;

        var btnCancel = new Button { Text = "取消", Location = new Point(240, y), Size = new Size(100, 30) };
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] {
            lblCode, txtCode, lblArea, txtArea, lblShelf, txtShelf,
            lblLayer, txtLayer, lblPosition, txtPosition, lblDesc, txtDesc,
            lblStatus, cboStatus, btnSave, btnCancel
        });
    }

    private void LoadData()
    {
        txtCode.Text = Location.LocationCode;
        txtArea.Text = Location.Area;
        txtShelf.Text = Location.ShelfNo;
        txtLayer.Text = Location.LayerNo;
        txtPosition.Text = Location.PositionNo;
        txtDesc.Text = Location.Description;
        cboStatus.SelectedIndex = (int)Location.Status;
    }

    private void Save_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtCode.Text))
        {
            MessageBox.Show("请输入库位编号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtCode.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtArea.Text))
        {
            MessageBox.Show("请输入区域", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtArea.Focus();
            return;
        }

        Location.LocationCode = txtCode.Text.Trim();
        Location.Area = txtArea.Text.Trim();
        Location.ShelfNo = txtShelf.Text.Trim();
        Location.LayerNo = txtLayer.Text.Trim();
        Location.PositionNo = txtPosition.Text.Trim();
        Location.Description = txtDesc.Text.Trim();
        Location.Status = (LocationStatus)((dynamic)cboStatus.SelectedItem).Value;

        this.DialogResult = DialogResult.OK;
    }

    private TextBox txtCode = null!;
    private TextBox txtArea = null!;
    private TextBox txtShelf = null!;
    private TextBox txtLayer = null!;
    private TextBox txtPosition = null!;
    private TextBox txtDesc = null!;
    private ComboBox cboStatus = null!;
}
