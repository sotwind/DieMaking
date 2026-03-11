using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Die;

/// <summary>
/// 刀模列表窗体 - 优化版本（使用虚拟模式和分页优化）
/// </summary>
public partial class DieListFormOptimized : BaseListForm
{
    private readonly DieServiceOptimized _dieService;
    private List<DieInfo> _dieList = new();
    private int _currentPage = 1;
    private int _pageSize = 20;
    private int _totalCount = 0;
    private bool _useVirtualMode = false;
    private const int VirtualModeThreshold = 1000;

    public DieListFormOptimized()
    {
        _dieService = new DieServiceOptimized();
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "刀模列表（优化版）";
        this.Size = UIStyleHelper.SizeListForm;
        this.StartPosition = FormStartPosition.CenterParent;

        // 顶部搜索区域
        var grpSearch = UIStyleHelper.CreateGroupBox("搜索条件", new Point(10, 10), new Size(1160, 80));

        // 刀模编号
        var lblDieCode = UIStyleHelper.CreateLabel("刀模编号：", new Point(15, 25), new Size(70, 23));
        txtDieCode = UIStyleHelper.CreateTextBox(new Point(85, 22), new Size(120, 23), "请输入刀模编号");

        // 客户名称
        var lblCustomer = UIStyleHelper.CreateLabel("客户名称：", new Point(220, 25), new Size(70, 23));
        txtCustomer = UIStyleHelper.CreateTextBox(new Point(290, 22), new Size(120, 23), "请输入客户名称");

        // 状态
        var lblStatus = UIStyleHelper.CreateLabel("状态：", new Point(425, 25), new Size(50, 23));
        cmbStatus = new ComboBox
        {
            Location = new Point(475, 22),
            Size = new Size(100, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        cmbStatus.Items.Add("全部");
        cmbStatus.Items.AddRange(Enum.GetNames(typeof(DieStatus)).Select(s => ((DieStatus)Enum.Parse(typeof(DieStatus), s)).GetDisplayName()).ToArray());
        cmbStatus.SelectedIndex = 0;

        // 创建日期范围
        var lblDateFrom = UIStyleHelper.CreateLabel("创建日期：", new Point(775, 25), new Size(70, 23));
        dtpDateFrom = new DateTimePicker
        {
            Location = new Point(845, 22),
            Size = new Size(120, 23),
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Checked = false,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        var lblDateTo = UIStyleHelper.CreateLabel("至", new Point(970, 25), new Size(20, 23));
        lblDateTo.TextAlign = ContentAlignment.MiddleCenter;
        dtpDateTo = new DateTimePicker
        {
            Location = new Point(995, 22),
            Size = new Size(120, 23),
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Checked = false,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        // 搜索按钮
        btnSearch = UIStyleHelper.CreateSearchButton();
        btnSearch.Location = new Point(15, 50);
        btnSearch.Click += BtnSearch_Click;

        // 重置按钮
        btnReset = UIStyleHelper.CreateCancelButton();
        btnReset.Text = "重置";
        btnReset.Location = new Point(125, 50);
        btnReset.Click += BtnReset_Click;

        // 虚拟模式切换
        chkVirtualMode = new CheckBox
        {
            Text = "大数据模式(>1000条)",
            Location = new Point(250, 52),
            Size = new Size(150, 23),
            Checked = false
        };
        chkVirtualMode.CheckedChanged += (s, e) => _useVirtualMode = chkVirtualMode.Checked;

        grpSearch.Controls.Add(lblDieCode);
        grpSearch.Controls.Add(txtDieCode);
        grpSearch.Controls.Add(lblCustomer);
        grpSearch.Controls.Add(txtCustomer);
        grpSearch.Controls.Add(lblStatus);
        grpSearch.Controls.Add(cmbStatus);
        grpSearch.Controls.Add(lblDateFrom);
        grpSearch.Controls.Add(dtpDateFrom);
        grpSearch.Controls.Add(lblDateTo);
        grpSearch.Controls.Add(dtpDateTo);
        grpSearch.Controls.Add(btnSearch);
        grpSearch.Controls.Add(btnReset);
        grpSearch.Controls.Add(chkVirtualMode);

        // 使用虚拟模式DataGridView
        dgvDieList = new VirtualDataGridViewWithProgress
        {
            Location = new Point(10, 100),
            Size = new Size(1160, 480)
        };
        
        // 配置虚拟模式事件
        ((VirtualDataGridViewWithProgress)dgvDieList).GetCellValueOverride += DieListFormOptimized_GetCellValueOverride;
        
        ApplyDataGridViewStyle(dgvDieList);
        dgvDieList.CellDoubleClick += DgvDieList_CellDoubleClick;

        // 设置列
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "DieID", HeaderText = "ID", DataPropertyName = "DieID", Visible = false });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "DieCode", HeaderText = "刀模编号", DataPropertyName = "DieCode", Width = 120 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "客户名称", DataPropertyName = "CustomerName", Width = 150 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "产品名称", DataPropertyName = "ProductName", Width = 150 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ManufactureSize", HeaderText = "制造尺寸", DataPropertyName = "ManufactureSize", Width = 100 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "Material", HeaderText = "材质", DataPropertyName = "Material", Width = 80 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "StatusText", HeaderText = "状态", DataPropertyName = "StatusText", Width = 80 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreateTime", HeaderText = "创建时间", DataPropertyName = "CreateTime", Width = 120, DefaultCellStyle = { Format = "yyyy-MM-dd" } });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreateUser", HeaderText = "创建人", DataPropertyName = "CreateUser", Width = 80 });

        // 添加右键菜单
        var contextMenu = UIStyleHelper.CreateDataGridViewContextMenu(
            onEdit: () => BtnEdit_Click(null, EventArgs.Empty),
            onDelete: () => BtnDelete_Click(null, EventArgs.Empty)
        );
        dgvDieList.ContextMenuStrip = contextMenu;

        // 按钮区域
        var grpButtons = new GroupBox
        {
            Location = new Point(10, 590),
            Size = new Size(1160, 50),
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Bold, GraphicsUnit.Point, 134)
        };

        btnAdd = UIStyleHelper.CreateAddButton();
        btnAdd.Location = new Point(15, 15);
        btnAdd.Click += BtnAdd_Click;

        btnEdit = UIStyleHelper.CreateEditButton();
        btnEdit.Location = new Point(125, 15);
        btnEdit.Click += BtnEdit_Click;

        btnDelete = UIStyleHelper.CreateDeleteButton();
        btnDelete.Location = new Point(235, 15);
        btnDelete.Click += BtnDelete_Click;

        btnImport = new Button { Text = "批量导入", Location = new Point(345, 15), Size = UIStyleHelper.SizeButton };
        ApplyButtonStyle(btnImport, ButtonStyle.Default);
        btnImport.Click += BtnImport_Click;

        // 分页控件
        btnFirst = new Button { Text = "首页", Location = new Point(515, 15), Size = new Size(60, 28) };
        ApplyButtonStyle(btnFirst, ButtonStyle.Default);
        btnFirst.Click += (s, e) => GoToPage(1);

        btnPrev = new Button { Text = "上一页", Location = new Point(580, 15), Size = new Size(60, 28) };
        ApplyButtonStyle(btnPrev, ButtonStyle.Default);
        btnPrev.Click += (s, e) => GoToPage(_currentPage - 1);

        lblPageInfo = new Label
        {
            Text = "第 1 页 / 共 1 页 (共 0 条)",
            Location = new Point(645, 20),
            Size = new Size(180, 23),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        btnNext = new Button { Text = "下一页", Location = new Point(830, 15), Size = new Size(60, 28) };
        ApplyButtonStyle(btnNext, ButtonStyle.Default);
        btnNext.Click += (s, e) => GoToPage(_currentPage + 1);

        btnLast = new Button { Text = "末页", Location = new Point(895, 15), Size = new Size(60, 28) };
        ApplyButtonStyle(btnLast, ButtonStyle.Default);
        btnLast.Click += (s, e) => GoToPage((_totalCount + _pageSize - 1) / _pageSize);

        grpButtons.Controls.Add(btnAdd);
        grpButtons.Controls.Add(btnEdit);
        grpButtons.Controls.Add(btnDelete);
        grpButtons.Controls.Add(btnImport);
        grpButtons.Controls.Add(btnFirst);
        grpButtons.Controls.Add(btnPrev);
        grpButtons.Controls.Add(lblPageInfo);
        grpButtons.Controls.Add(btnNext);
        grpButtons.Controls.Add(btnLast);

        // 状态栏
        var statusStrip = CreateStatusBar();

        this.Controls.Add(grpSearch);
        this.Controls.Add(dgvDieList);
        this.Controls.Add(grpButtons);
        this.Controls.Add(statusStrip);
    }

    private void DieListFormOptimized_GetCellValueOverride(object? sender, GetCellValueEventArgs e)
    {
        if (e.RowData is DieInfo die)
        {
            switch (e.ColumnName)
            {
                case "ManufactureSize":
                    e.Value = die.ManufactureSize;
                    e.Handled = true;
                    break;
                case "StatusText":
                    e.Value = die.StatusText;
                    e.Handled = true;
                    break;
                case "CreateUser":
                    e.Value = die.CreateUser;
                    e.Handled = true;
                    break;
            }
        }
    }

    #region 控件声明
    private TextBox txtDieCode = null!;
    private TextBox txtCustomer = null!;
    private ComboBox cmbStatus = null!;
    private DateTimePicker dtpDateFrom = null!;
    private DateTimePicker dtpDateTo = null!;
    private Button btnSearch = null!;
    private Button btnReset = null!;
    private CheckBox chkVirtualMode = null!;
    private DataGridView dgvDieList = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private Button btnImport = null!;
    private Button btnFirst = null!;
    private Button btnPrev = null!;
    private Button btnNext = null!;
    private Button btnLast = null!;
    private Label lblPageInfo = null!;
    #endregion

    #region 事件处理

    private void BtnSearch_Click(object? sender, EventArgs e)
    {
        _currentPage = 1;
        LoadData();
    }

    private void BtnReset_Click(object? sender, EventArgs e)
    {
        txtDieCode.Clear();
        txtCustomer.Clear();
        cmbStatus.SelectedIndex = 0;
        dtpDateFrom.Checked = false;
        dtpDateTo.Checked = false;
        _currentPage = 1;
        LoadData();
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        var form = new DieAddForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            LoadData();
        }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        var die = GetSelectedDie();
        if (die == null) return;

        var form = new DieAddForm(die.DieID);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            LoadData();
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        var die = GetSelectedDie();
        if (die == null) return;

        if (MessageBox.Show($"确定要删除刀模 [{die.DieCode}] 吗？", "确认删除",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            if (_dieService.DeleteDie(die.DieID))
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

    private void BtnView_Click(object? sender, EventArgs e)
    {
        var die = GetSelectedDie();
        if (die == null) return;

        var form = new DieAddForm(die.DieID, true);
        form.ShowDialog(this);
    }

    private void BtnImport_Click(object? sender, EventArgs e)
    {
        // 显示批量导入对话框
        using var openFileDialog = new OpenFileDialog
        {
            Filter = "Excel文件|*.xlsx;*.xls|CSV文件|*.csv|所有文件|*.*",
            Title = "选择导入文件"
        };

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            // 显示进度对话框并执行导入
            var progressDialog = new BulkProgressDialog();
            var progress = new Progress<BulkProgress>(p =>
            {
                progressDialog.UpdateProgress(p);
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    // 这里应该解析文件并导入
                    // 简化示例：
                    var importData = new List<DieImportModel>();
                    // ... 解析文件 ...

                    var result = await _dieService.BulkImportDiesAsync(importData, progress);

                    this.Invoke(new Action(() =>
                    {
                        progressDialog.Close();
                        if (result.Success)
                        {
                            ShowSuccess(result.Message);
                            LoadData();
                        }
                        else
                        {
                            ShowError(result.Message);
                        }
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        progressDialog.Close();
                        ShowError($"导入失败：{ex.Message}");
                    }));
                }
            });

            progressDialog.ShowDialog(this);
        }
    }

    private void DgvDieList_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        BtnEdit_Click(sender, e);
    }

    #endregion

    #region 私有方法

    protected override void LoadData()
    {
        try
        {
            // 获取搜索条件
            string? dieCode = string.IsNullOrWhiteSpace(txtDieCode.Text) || txtDieCode.Text == (string?)txtDieCode.Tag
                ? null : txtDieCode.Text.Trim();
            string? customerName = string.IsNullOrWhiteSpace(txtCustomer.Text) || txtCustomer.Text == (string?)txtCustomer.Tag
                ? null : txtCustomer.Text.Trim();
            DieStatus? status = cmbStatus.SelectedIndex > 0 ? (DieStatus?)(cmbStatus.SelectedIndex - 1) : null;
            DateTime? startDate = dtpDateFrom.Checked ? dtpDateFrom.Value : null;
            DateTime? endDate = dtpDateTo.Checked ? dtpDateTo.Value : null;

            if (_useVirtualMode)
            {
                // 使用虚拟模式加载大数据
                LoadDataVirtualAsync(dieCode, customerName, status);
            }
            else
            {
                // 使用分页查询
                LoadDataPaged(dieCode, customerName, status, startDate, endDate);
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载数据失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 使用分页查询加载数据
    /// </summary>
    private void LoadDataPaged(string? dieCode, string? customerName, DieStatus? status, DateTime? startDate, DateTime? endDate)
    {
        var result = _dieService.SearchDiesPaged(dieCode, customerName, status, startDate, endDate, _currentPage, _pageSize);

        _dieList = result.Items;
        _totalCount = result.TotalCount;

        // 绑定到DataGridView
        if (dgvDieList is VirtualDataGridView virtualDgv)
        {
            virtualDgv.VirtualDataSource = _dieList;
        }
        else
        {
            dgvDieList.DataSource = _dieList;
        }

        // 更新分页信息
        UpdatePaginationInfo();

        // 更新状态栏
        if (StatusUserLabel != null)
        {
            StatusUserLabel.Text = $"共 {_totalCount} 条记录" + (result.CountFromCache ? " (计数缓存)" : "");
        }
    }

    /// <summary>
    /// 使用虚拟模式异步加载数据
    /// </summary>
    private async void LoadDataVirtualAsync(string? dieCode, string? customerName, DieStatus? status)
    {
        if (dgvDieList is VirtualDataGridViewWithProgress virtualDgv)
        {
            var progress = new Progress<LoadingProgress>(p =>
            {
                virtualDgv.UpdateLoadingProgress(p);
            });

            _dieList = await _dieService.LoadDiesAsync(dieCode, customerName, status, progress);
            _totalCount = _dieList.Count;

            virtualDgv.LoadData(_dieList);

            // 隐藏分页控件（虚拟模式不需要）
            UpdatePaginationInfo();

            if (StatusUserLabel != null)
            {
                StatusUserLabel.Text = $"共 {_totalCount} 条记录 (虚拟模式)";
            }
        }
    }

    /// <summary>
    /// 更新分页信息
    /// </summary>
    private void UpdatePaginationInfo()
    {
        int totalPages = (_totalCount + _pageSize - 1) / _pageSize;
        if (totalPages == 0) totalPages = 1;

        lblPageInfo.Text = $"第 {_currentPage} 页 / 共 {totalPages} 页 (共 {_totalCount} 条)";

        // 更新按钮状态
        btnFirst.Enabled = _currentPage > 1;
        btnPrev.Enabled = _currentPage > 1;
        btnNext.Enabled = _currentPage < totalPages;
        btnLast.Enabled = _currentPage < totalPages;
    }

    private void GoToPage(int page)
    {
        int totalPages = (_totalCount + _pageSize - 1) / _pageSize;
        if (totalPages == 0) totalPages = 1;

        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        _currentPage = page;
        LoadData();
    }

    private DieInfo? GetSelectedDie()
    {
        if (dgvDieList.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先选择一条记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var rowIndex = dgvDieList.SelectedRows[0].Index;
        
        // 如果是虚拟模式，从虚拟数据源获取
        if (dgvDieList is VirtualDataGridView virtualDgv)
        {
            return virtualDgv.GetRowData<DieInfo>(rowIndex);
        }
        
        // 否则从列表获取
        var dieId = Convert.ToInt32(dgvDieList.SelectedRows[0].Cells["DieID"].Value);
        return _dieList.FirstOrDefault(d => d.DieID == dieId);
    }

    #endregion
}
