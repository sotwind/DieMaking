using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Warehouse;

public partial class LocationManageForm : BaseListForm
{
    private readonly WarehouseService _warehouseService;
    private BindingSource _bindingSource = new();
    private List<StorageLocation> _locations = new();

    public LocationManageForm()
    {
        _warehouseService = new WarehouseService();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "库位管理";
        this.Size = UIStyleHelper.SizeListForm;
        this.StartPosition = FormStartPosition.CenterParent;

        // 搜索区域
        var panelSearch = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            Padding = new Padding(10, 5, 10, 5)
        };

        var lblSearch = UIStyleHelper.CreateLabel("搜索：", new Point(10, 12), new Size(50, 25));

        txtSearch = UIStyleHelper.CreateTextBox(new Point(60, 9), new Size(200, 25), "输入库位编号或区域");

        btnSearch = UIStyleHelper.CreateSearchButton();
        btnSearch.Location = new Point(270, 8);
        btnSearch.Click += (s, e) => SearchLocations();

        btnClear = UIStyleHelper.CreateCancelButton("清空");
        btnClear.Location = new Point(380, 8);
        btnClear.Click += (s, e) => { txtSearch.Clear(); LoadData(); };

        btnRefresh = UIStyleHelper.CreateSearchButton("刷新");
        btnRefresh.Location = new Point(490, 8);
        btnRefresh.Click += (s, e) => LoadData();

        btnAdd = UIStyleHelper.CreateAddButton("新增");
        btnAdd.Location = new Point(600, 8);
        btnAdd.Click += (s, e) => AddLocation();

        btnEdit = UIStyleHelper.CreateEditButton("编辑");
        btnEdit.Location = new Point(710, 8);
        btnEdit.Click += (s, e) => EditLocation();

        btnDelete = UIStyleHelper.CreateDeleteButton("删除");
        btnDelete.Location = new Point(820, 8);
        btnDelete.Click += (s, e) => DeleteLocation();

        btnExport = UIStyleHelper.CreateExportButton("导出");
        btnExport.Location = new Point(930, 8);
        btnExport.Click += (s, e) => ExportLocations();

        panelSearch.Controls.Add(lblSearch);
        panelSearch.Controls.Add(txtSearch);
        panelSearch.Controls.Add(btnSearch);
        panelSearch.Controls.Add(btnClear);
        panelSearch.Controls.Add(btnRefresh);
        panelSearch.Controls.Add(btnAdd);
        panelSearch.Controls.Add(btnEdit);
        panelSearch.Controls.Add(btnDelete);
        panelSearch.Controls.Add(btnExport);

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
            BorderStyle = BorderStyle.None
        };
        ApplyDataGridViewStyle(dgvLocations);

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

        // 添加右键菜单
        var contextMenu = UIStyleHelper.CreateDataGridViewContextMenu(
            onView: null,
            onEdit: () => EditLocation(),
            onDelete: () => DeleteLocation()
        );
        dgvLocations.ContextMenuStrip = contextMenu;

        // 状态栏
        var statusStrip = CreateStatusBar();

        // 布局
        var panelContent = new Panel { Dock = DockStyle.Fill };
        panelContent.Controls.Add(dgvLocations);

        this.Controls.Add(panelContent);
        this.Controls.Add(panelSearch);
        this.Controls.Add(statusStrip);
    }

    private DataGridView dgvLocations = null!;
    private TextBox txtSearch = null!;
    private Button btnSearch = null!;
    private Button btnClear = null!;
    private Button btnRefresh = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private Button btnExport = null!;

    protected override void LoadData()
    {
        try
        {
            _locations = _warehouseService.GetAllLocations();
            _bindingSource.DataSource = _locations;
            dgvLocations.DataSource = _bindingSource;

            if (StatusUserLabel != null)
            {
                StatusUserLabel.Text = $"共 {_locations.Count} 条记录";
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载数据失败：{ex.Message}");
        }
    }

    private void SearchLocations()
    {
        try
        {
            var keyword = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(keyword) || keyword == (string?)txtSearch.Tag)
            {
                LoadData();
                return;
            }

            _locations = _warehouseService.SearchLocations(keyword);
            _bindingSource.DataSource = _locations;
            dgvLocations.DataSource = _bindingSource;

            if (StatusUserLabel != null)
            {
                StatusUserLabel.Text = $"共 {_locations.Count} 条记录";
            }
        }
        catch (Exception ex)
        {
            ShowError($"搜索失败：{ex.Message}");
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
                    ShowSuccess("新增成功");
                    LoadData();
                }
                else
                {
                    ShowError("新增失败");
                }
            }
            catch (Exception ex)
            {
                ShowError($"新增失败：{ex.Message}");
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
                    ShowSuccess("更新成功");
                    LoadData();
                }
                else
                {
                    ShowError("更新失败");
                }
            }
            catch (Exception ex)
            {
                ShowError($"更新失败：{ex.Message}");
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
                    ShowSuccess("删除成功");
                    LoadData();
                }
                else
                {
                    ShowError("删除失败");
                }
            }
            catch (Exception ex)
            {
                ShowError($"删除失败：{ex.Message}");
            }
        }
    }

    private void ExportLocations()
    {
        if (_locations.Count == 0)
        {
            MessageBox.Show("没有数据可导出", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            using var saveDialog = new SaveFileDialog
            {
                Filter = "Excel文件|*.xlsx|CSV文件|*.csv",
                FileName = $"库位列表_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                var importExportService = new ImportExportService();

                var dataTable = new global::System.Data.DataTable();
                dataTable.Columns.Add("库位编号", typeof(string));
                dataTable.Columns.Add("区域", typeof(string));
                dataTable.Columns.Add("货架号", typeof(string));
                dataTable.Columns.Add("层号", typeof(string));
                dataTable.Columns.Add("位置号", typeof(string));
                dataTable.Columns.Add("描述", typeof(string));
                dataTable.Columns.Add("状态", typeof(string));
                dataTable.Columns.Add("创建时间", typeof(string));

                foreach (var loc in _locations)
                {
                    dataTable.Rows.Add(
                        loc.LocationCode,
                        loc.Area,
                        loc.ShelfNo,
                        loc.LayerNo,
                        loc.PositionNo,
                        loc.Description,
                        loc.StatusText,
                        loc.CreateTime.ToString("yyyy-MM-dd HH:mm")
                    );
                }

                if (saveDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    importExportService.ExportToCsv(dataTable, saveDialog.FileName);
                }
                else
                {
                    importExportService.ExportToExcel(dataTable, "库位列表", saveDialog.FileName);
                }

                ShowSuccess("导出成功！");
            }
        }
        catch (Exception ex)
        {
            ShowError($"导出失败：{ex.Message}");
        }
    }
}

// 库位编辑窗体
public class LocationEditForm : BaseDialogForm
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

    private TextBox txtCode = null!;
    private TextBox txtArea = null!;
    private TextBox txtShelf = null!;
    private TextBox txtLayer = null!;
    private TextBox txtPosition = null!;
    private TextBox txtDesc = null!;
    private ComboBox cboStatus = null!;

    private void InitializeComponent()
    {
        this.Text = _isEdit ? "编辑库位" : "新增库位";
        this.Size = UIStyleHelper.SizeDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int y = 20;
        int labelWidth = 80;
        int textBoxWidth = 280;

        // 库位编号
        var lblCode = UIStyleHelper.CreateLabel("库位编号：", new Point(20, y), new Size(labelWidth, 25));
        txtCode = UIStyleHelper.CreateTextBox(new Point(110, y), new Size(textBoxWidth, 25), "请输入库位编号");
        y += 35;

        // 区域
        var lblArea = UIStyleHelper.CreateLabel("区域：", new Point(20, y), new Size(labelWidth, 25));
        txtArea = UIStyleHelper.CreateTextBox(new Point(110, y), new Size(textBoxWidth, 25), "请输入区域");
        y += 35;

        // 货架号
        var lblShelf = UIStyleHelper.CreateLabel("货架号：", new Point(20, y), new Size(labelWidth, 25));
        txtShelf = UIStyleHelper.CreateTextBox(new Point(110, y), new Size(textBoxWidth, 25), "请输入货架号");
        y += 35;

        // 层号
        var lblLayer = UIStyleHelper.CreateLabel("层号：", new Point(20, y), new Size(labelWidth, 25));
        txtLayer = UIStyleHelper.CreateTextBox(new Point(110, y), new Size(textBoxWidth, 25), "请输入层号");
        y += 35;

        // 位置号
        var lblPosition = UIStyleHelper.CreateLabel("位置号：", new Point(20, y), new Size(labelWidth, 25));
        txtPosition = UIStyleHelper.CreateTextBox(new Point(110, y), new Size(textBoxWidth, 25), "请输入位置号");
        y += 35;

        // 描述
        var lblDesc = UIStyleHelper.CreateLabel("描述：", new Point(20, y), new Size(labelWidth, 25));
        txtDesc = UIStyleHelper.CreateTextBox(new Point(110, y), new Size(textBoxWidth, 25), "请输入描述");
        y += 35;

        // 状态
        var lblStatus = UIStyleHelper.CreateLabel("状态：", new Point(20, y), new Size(labelWidth, 25));
        cboStatus = new ComboBox
        {
            Location = new Point(110, y),
            Size = new Size(textBoxWidth, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        cboStatus.Items.Add(new { Text = "空闲", Value = LocationStatus.Free });
        cboStatus.Items.Add(new { Text = "占用", Value = LocationStatus.Occupied });
        cboStatus.Items.Add(new { Text = "禁用", Value = LocationStatus.Disabled });
        cboStatus.DisplayMember = "Text";
        cboStatus.ValueMember = "Value";
        cboStatus.SelectedIndex = 0;
        y += 50;

        // 按钮
        var btnSave = UIStyleHelper.CreateSaveButton();
        btnSave.Location = new Point(110, y);
        btnSave.Click += Save_Click;

        var btnCancel = UIStyleHelper.CreateCancelButton();
        btnCancel.Location = new Point(240, y);
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] {
            lblCode, txtCode, lblArea, txtArea, lblShelf, txtShelf,
            lblLayer, txtLayer, lblPosition, txtPosition, lblDesc, txtDesc,
            lblStatus, cboStatus, btnSave, btnCancel
        });

        // 注册回车跳转
        RegisterEnterToNext();
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
        if (string.IsNullOrWhiteSpace(txtCode.Text) || txtCode.Text == (string?)txtCode.Tag)
        {
            UIStyleHelper.SetValidationError(txtCode, true);
            MessageBox.Show("请输入库位编号", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtCode.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtCode, false);

        if (string.IsNullOrWhiteSpace(txtArea.Text) || txtArea.Text == (string?)txtArea.Tag)
        {
            UIStyleHelper.SetValidationError(txtArea, true);
            MessageBox.Show("请输入区域", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtArea.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtArea, false);

        Location.LocationCode = txtCode.Text.Trim();
        Location.Area = txtArea.Text.Trim();
        Location.ShelfNo = txtShelf.Text.Trim();
        Location.LayerNo = txtLayer.Text.Trim();
        Location.PositionNo = txtPosition.Text.Trim();
        Location.Description = txtDesc.Text.Trim();
        Location.Status = (LocationStatus)((dynamic)cboStatus.SelectedItem!).Value;

        this.DialogResult = DialogResult.OK;
    }
}
