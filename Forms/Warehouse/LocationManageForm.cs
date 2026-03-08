using DieMaking.Models;
using DieMaking.Services;
using DieMaking.Helpers;

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

        var btnExport = new ToolStripButton("导出Excel") { Image = SystemIcons.Question.ToBitmap() };
        btnExport.Click += (s, e) => ExportLocations();

        var btnImport = new ToolStripButton("批量导入") { Image = SystemIcons.Question.ToBitmap() };
        btnImport.Click += (s, e) => ImportLocations();

        var btnTemplate = new ToolStripButton("下载模板") { Image = SystemIcons.Question.ToBitmap() };
        btnTemplate.Click += (s, e) => DownloadTemplate();
        
        var btnPrint = new ToolStripButton("打印") { Image = SystemIcons.Question.ToBitmap() };
        btnPrint.Click += (s, e) => PrintLocations();

        toolStrip.Items.AddRange(new ToolStripItem[] { btnAdd, btnEdit, btnDelete, new ToolStripSeparator(), btnRefresh, new ToolStripSeparator(), btnExport, btnImport, btnTemplate, new ToolStripSeparator(), btnPrint });

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

                var dataTable = new System.Data.DataTable();
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

                MessageBox.Show("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportLocations()
    {
        using var openDialog = new OpenFileDialog
        {
            Filter = "Excel文件|*.xlsx;*.xls|CSV文件|*.csv",
            Title = "选择要导入的文件"
        };

        if (openDialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        try
        {
            var importExportService = new ImportExportService();
            System.Data.DataTable importData;

            if (openDialog.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                importData = importExportService.ImportFromCsv(openDialog.FileName);
            }
            else
            {
                importData = importExportService.ImportFromExcel(openDialog.FileName);
            }

            if (importData.Rows.Count == 0)
            {
                MessageBox.Show("导入的文件没有数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 显示预览窗体
            var previewForm = new Forms.Common.ImportPreviewForm(importData, "库位导入预览");
            if (previewForm.ShowDialog(this) == DialogResult.OK)
            {
                var result = ImportLocationsData(importData);

                var message = $"导入完成！\n\n总计：{result.TotalCount} 条\n成功：{result.SuccessCount} 条\n失败：{result.FailCount} 条";

                if (result.FailCount > 0)
                {
                    message += "\n\n失败详情：\n" + string.Join("\n", result.Errors.Take(10).Select(e => $"第{e.RowIndex}行 - {e.ErrorMessage}"));
                    if (result.Errors.Count > 10)
                    {
                        message += $"\n... 还有 {result.Errors.Count - 10} 条错误";
                    }
                }

                MessageBox.Show(message, "导入结果", MessageBoxButtons.OK, result.FailCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                if (result.SuccessCount > 0)
                {
                    LoadLocations();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private ImportExportService.ImportResult ImportLocationsData(System.Data.DataTable data)
    {
        var result = new ImportExportService.ImportResult { TotalCount = data.Rows.Count };

        foreach (System.Data.DataRow row in data.Rows)
        {
            try
            {
                // 验证必填字段
                var locationCode = row["库位编号"].ToString();
                if (string.IsNullOrWhiteSpace(locationCode))
                {
                    result.Errors.Add(new ImportExportService.ImportError
                    {
                        RowIndex = data.Rows.IndexOf(row) + 1,
                        ColumnName = "库位编号",
                        ErrorMessage = "库位编号不能为空"
                    });
                    result.FailCount++;
                    continue;
                }

                // 检查库位编号是否已存在
                if (_warehouseService.IsLocationCodeExists(locationCode))
                {
                    result.Errors.Add(new ImportExportService.ImportError
                    {
                        RowIndex = data.Rows.IndexOf(row) + 1,
                        ColumnName = "库位编号",
                        ErrorMessage = $"库位编号 '{locationCode}' 已存在"
                    });
                    result.FailCount++;
                    continue;
                }

                // 验证区域
                var area = row["区域"].ToString();
                if (string.IsNullOrWhiteSpace(area))
                {
                    result.Errors.Add(new ImportExportService.ImportError
                    {
                        RowIndex = data.Rows.IndexOf(row) + 1,
                        ColumnName = "区域",
                        ErrorMessage = "区域不能为空"
                    });
                    result.FailCount++;
                    continue;
                }

                // 创建库位对象
                var location = new StorageLocation
                {
                    LocationCode = locationCode,
                    Area = area,
                    ShelfNo = row["货架号"].ToString() ?? "",
                    LayerNo = row["层号"].ToString() ?? "",
                    PositionNo = row["位置号"].ToString() ?? "",
                    Description = row["描述"].ToString() ?? "",
                    Status = LocationStatus.Free,
                    CreateTime = DateTime.Now
                };

                // 保存库位
                var id = _warehouseService.CreateLocation(location);
                if (id > 0)
                {
                    result.SuccessCount++;
                }
                else
                {
                    result.Errors.Add(new ImportExportService.ImportError
                    {
                        RowIndex = data.Rows.IndexOf(row) + 1,
                        ColumnName = "",
                        ErrorMessage = "保存库位失败"
                    });
                    result.FailCount++;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportExportService.ImportError
                {
                    RowIndex = data.Rows.IndexOf(row) + 1,
                    ColumnName = "",
                    ErrorMessage = $"导入异常：{ex.Message}"
                });
                result.FailCount++;
            }
        }

        return result;
    }

    private void DownloadTemplate()
    {
        using var saveDialog = new SaveFileDialog
        {
            Filter = "CSV文件|*.csv",
            FileName = "库位导入模板.csv"
        };

        if (saveDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                var importExportService = new ImportExportService();
                var columns = new List<ImportExportService.TemplateColumn>
                {
                    new() { HeaderText = "库位编号", ExampleValue = "A-01-01-01", Description = "必填，唯一标识", IsRequired = true },
                    new() { HeaderText = "区域", ExampleValue = "A区", Description = "必填，库位所在区域", IsRequired = true },
                    new() { HeaderText = "货架号", ExampleValue = "01", Description = "货架编号" },
                    new() { HeaderText = "层号", ExampleValue = "01", Description = "货架层号" },
                    new() { HeaderText = "位置号", ExampleValue = "01", Description = "具体位置号" },
                    new() { HeaderText = "描述", ExampleValue = "主仓库A区1号货架", Description = "可选，库位描述" }
                };

                importExportService.GenerateImportTemplate(saveDialog.FileName, columns);
                MessageBox.Show("模板下载成功！\n\n请按照模板格式填写数据后导入。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载模板失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void PrintLocations()
    {
        if (dgvLocations.Rows.Count == 0)
        {
            MessageBox.Show("没有数据可打印", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var printService = new PrintService();
        var subtitle = $"打印时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}  操作员：{CurrentUser.User?.RealName ?? CurrentUser.User?.Username ?? "未知"}  共 {dgvLocations.Rows.Count} 条记录";
        printService.PrintPreview(dgvLocations, "刀模管理系统 - 库位列表", subtitle);
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
