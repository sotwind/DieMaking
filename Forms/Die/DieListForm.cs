using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Die;

public partial class DieListForm : Form
{
    private readonly DieService _dieService;
    private List<DieInfo> _dieList = new();
    private int _currentPage = 1;
    private int _pageSize = 20;
    private int _totalCount = 0;

    public DieListForm()
    {
        _dieService = new DieService();
        InitializeComponent();
        LoadDieList();
    }

    private void InitializeComponent()
    {
        this.Text = "刀模列表";
        this.Size = new Size(1200, 700);
        this.StartPosition = FormStartPosition.CenterParent;
        this.WindowState = FormWindowState.Maximized;

        // 顶部搜索区域
        var grpSearch = new GroupBox
        {
            Text = "搜索条件",
            Location = new Point(10, 10),
            Size = new Size(1160, 80)
        };

        // 刀模编号
        var lblDieCode = new Label
        {
            Text = "刀模编号：",
            Location = new Point(15, 25),
            Size = new Size(70, 23)
        };
        txtDieCode = new TextBox
        {
            Location = new Point(85, 22),
            Size = new Size(120, 23)
        };

        // 客户名称
        var lblCustomer = new Label
        {
            Text = "客户名称：",
            Location = new Point(220, 25),
            Size = new Size(70, 23)
        };
        txtCustomer = new TextBox
        {
            Location = new Point(290, 22),
            Size = new Size(120, 23)
        };

        // 状态
        var lblStatus = new Label
        {
            Text = "状态：",
            Location = new Point(425, 25),
            Size = new Size(50, 23)
        };
        cmbStatus = new ComboBox
        {
            Location = new Point(475, 22),
            Size = new Size(100, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbStatus.Items.Add("全部");
        cmbStatus.Items.AddRange(Enum.GetNames(typeof(DieStatus)).Select(s => ((DieStatus)Enum.Parse(typeof(DieStatus), s)).GetDisplayName()).ToArray());
        cmbStatus.SelectedIndex = 0;

        // 审核状态
        var lblAuditStatus = new Label
        {
            Text = "审核状态：",
            Location = new Point(590, 25),
            Size = new Size(70, 23)
        };
        cmbAuditStatus = new ComboBox
        {
            Location = new Point(660, 22),
            Size = new Size(100, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cmbAuditStatus.Items.Add("全部");
        cmbAuditStatus.Items.AddRange(Enum.GetNames(typeof(AuditStatus)).Select(s => ((AuditStatus)Enum.Parse(typeof(AuditStatus), s)).GetDisplayName()).ToArray());
        cmbAuditStatus.SelectedIndex = 0;

        // 创建日期范围
        var lblDateFrom = new Label
        {
            Text = "创建日期：",
            Location = new Point(775, 25),
            Size = new Size(70, 23)
        };
        dtpDateFrom = new DateTimePicker
        {
            Location = new Point(845, 22),
            Size = new Size(120, 23),
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Checked = false
        };
        var lblDateTo = new Label
        {
            Text = "至",
            Location = new Point(970, 25),
            Size = new Size(20, 23)
        };
        dtpDateTo = new DateTimePicker
        {
            Location = new Point(995, 22),
            Size = new Size(120, 23),
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Checked = false
        };

        // 搜索按钮
        btnSearch = new Button
        {
            Text = "搜索",
            Location = new Point(15, 50),
            Size = new Size(80, 25)
        };
        btnSearch.Click += BtnSearch_Click;

        // 重置按钮
        btnReset = new Button
        {
            Text = "重置",
            Location = new Point(105, 50),
            Size = new Size(80, 25)
        };
        btnReset.Click += BtnReset_Click;

        grpSearch.Controls.Add(lblDieCode);
        grpSearch.Controls.Add(txtDieCode);
        grpSearch.Controls.Add(lblCustomer);
        grpSearch.Controls.Add(txtCustomer);
        grpSearch.Controls.Add(lblStatus);
        grpSearch.Controls.Add(cmbStatus);
        grpSearch.Controls.Add(lblAuditStatus);
        grpSearch.Controls.Add(cmbAuditStatus);
        grpSearch.Controls.Add(lblDateFrom);
        grpSearch.Controls.Add(dtpDateFrom);
        grpSearch.Controls.Add(lblDateTo);
        grpSearch.Controls.Add(dtpDateTo);
        grpSearch.Controls.Add(btnSearch);
        grpSearch.Controls.Add(btnReset);

        // 数据表格
        dgvDieList = new DataGridView
        {
            Location = new Point(10, 100),
            Size = new Size(1160, 480),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.LightGray }
        };
        dgvDieList.CellDoubleClick += DgvDieList_CellDoubleClick;

        // 设置列
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "DieID", HeaderText = "ID", DataPropertyName = "DieID", Visible = false });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "DieCode", HeaderText = "刀模编号", DataPropertyName = "DieCode", Width = 120 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "客户名称", DataPropertyName = "CustomerName", Width = 150 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "产品名称", DataPropertyName = "ProductName", Width = 150 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "ManufactureSize", HeaderText = "制造尺寸", DataPropertyName = "ManufactureSize", Width = 100 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "Material", HeaderText = "材质", DataPropertyName = "Material", Width = 80 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "StatusText", HeaderText = "状态", DataPropertyName = "StatusText", Width = 80 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "AuditStatusText", HeaderText = "审核状态", DataPropertyName = "AuditStatusText", Width = 80 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreateTime", HeaderText = "创建时间", DataPropertyName = "CreateTime", Width = 120 });
        dgvDieList.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreateUser", HeaderText = "创建人", DataPropertyName = "CreateUser", Width = 80 });

        // 按钮区域
        var grpButtons = new GroupBox
        {
            Location = new Point(10, 590),
            Size = new Size(1160, 50)
        };

        btnAdd = new Button
        {
            Text = "新增",
            Location = new Point(15, 15),
            Size = new Size(80, 28)
        };
        btnAdd.Click += BtnAdd_Click;

        btnEdit = new Button
        {
            Text = "编辑",
            Location = new Point(105, 15),
            Size = new Size(80, 28)
        };
        btnEdit.Click += BtnEdit_Click;

        btnDelete = new Button
        {
            Text = "删除",
            Location = new Point(195, 15),
            Size = new Size(80, 28)
        };
        btnDelete.Click += BtnDelete_Click;

        btnView = new Button
        {
            Text = "查看详情",
            Location = new Point(285, 15),
            Size = new Size(80, 28)
        };
        btnView.Click += BtnView_Click;

        btnAudit = new Button
        {
            Text = "审核",
            Location = new Point(375, 15),
            Size = new Size(80, 28)
        };
        btnAudit.Click += BtnAudit_Click;

        // 分页控件
        btnFirst = new Button
        {
            Text = "首页",
            Location = new Point(850, 15),
            Size = new Size(60, 28)
        };
        btnFirst.Click += (s, e) => GoToPage(1);

        btnPrev = new Button
        {
            Text = "上一页",
            Location = new Point(915, 15),
            Size = new Size(60, 28)
        };
        btnPrev.Click += (s, e) => GoToPage(_currentPage - 1);

        lblPageInfo = new Label
        {
            Text = "第 1 页 / 共 1 页 (共 0 条)",
            Location = new Point(980, 20),
            Size = new Size(150, 23),
            TextAlign = ContentAlignment.MiddleCenter
        };

        btnNext = new Button
        {
            Text = "下一页",
            Location = new Point(1135, 15),
            Size = new Size(60, 28)
        };
        btnNext.Click += (s, e) => GoToPage(_currentPage + 1);

        btnLast = new Button
        {
            Text = "末页",
            Location = new Point(1200, 15),
            Size = new Size(60, 28)
        };
        btnLast.Click += (s, e) => GoToPage((_totalCount + _pageSize - 1) / _pageSize);

        grpButtons.Controls.Add(btnAdd);
        grpButtons.Controls.Add(btnEdit);
        grpButtons.Controls.Add(btnDelete);
        grpButtons.Controls.Add(btnView);
        grpButtons.Controls.Add(btnAudit);
        grpButtons.Controls.Add(btnFirst);
        grpButtons.Controls.Add(btnPrev);
        grpButtons.Controls.Add(lblPageInfo);
        grpButtons.Controls.Add(btnNext);
        grpButtons.Controls.Add(btnLast);

        this.Controls.Add(grpSearch);
        this.Controls.Add(dgvDieList);
        this.Controls.Add(grpButtons);
    }

    #region 控件声明
    private TextBox txtDieCode = null!;
    private TextBox txtCustomer = null!;
    private ComboBox cmbStatus = null!;
    private ComboBox cmbAuditStatus = null!;
    private DateTimePicker dtpDateFrom = null!;
    private DateTimePicker dtpDateTo = null!;
    private Button btnSearch = null!;
    private Button btnReset = null!;
    private DataGridView dgvDieList = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private Button btnView = null!;
    private Button btnAudit = null!;
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
        LoadDieList();
    }

    private void BtnReset_Click(object? sender, EventArgs e)
    {
        txtDieCode.Clear();
        txtCustomer.Clear();
        cmbStatus.SelectedIndex = 0;
        cmbAuditStatus.SelectedIndex = 0;
        dtpDateFrom.Checked = false;
        dtpDateTo.Checked = false;
        _currentPage = 1;
        LoadDieList();
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        var form = new DieAddForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            LoadDieList();
        }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        var die = GetSelectedDie();
        if (die == null) return;

        if (die.AuditStatus == AuditStatus.Audited)
        {
            MessageBox.Show("已审核的刀模不能编辑", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var form = new DieAddForm(die.DieID);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            LoadDieList();
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        var die = GetSelectedDie();
        if (die == null) return;

        if (die.AuditStatus == AuditStatus.Audited)
        {
            MessageBox.Show("已审核的刀模不能删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show($"确定要删除刀模 [{die.DieCode}] 吗？", "确认删除", 
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            if (_dieService.DeleteDie(die.DieID))
            {
                MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDieList();
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

    private void BtnView_Click(object? sender, EventArgs e)
    {
        var die = GetSelectedDie();
        if (die == null) return;

        var form = new DieAddForm(die.DieID, true);
        form.ShowDialog(this);
    }

    private void BtnAudit_Click(object? sender, EventArgs e)
    {
        var die = GetSelectedDie();
        if (die == null) return;

        string action = die.AuditStatus == AuditStatus.Audited ? "取消审核" : "审核";
        if (MessageBox.Show($"确定要{action}刀模 [{die.DieCode}] 吗？", "确认", 
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            bool isApproved = die.AuditStatus != AuditStatus.Audited;
            if (_dieService.AuditDie(die.DieID, isApproved))
            {
                MessageBox.Show($"{action}成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDieList();
            }
            else
            {
                MessageBox.Show($"{action}失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"{action}失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DgvDieList_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        BtnView_Click(sender, e);
    }

    #endregion

    #region 私有方法

    private void LoadDieList()
    {
        try
        {
            // 获取搜索条件
            string? dieCode = string.IsNullOrWhiteSpace(txtDieCode.Text) ? null : txtDieCode.Text.Trim();
            string? customerName = string.IsNullOrWhiteSpace(txtCustomer.Text) ? null : txtCustomer.Text.Trim();
            DieStatus? status = cmbStatus.SelectedIndex > 0 ? (DieStatus?)(cmbStatus.SelectedIndex - 1) : null;
            AuditStatus? auditStatus = cmbAuditStatus.SelectedIndex > 0 ? (AuditStatus?)(cmbAuditStatus.SelectedIndex - 1) : null;
            DateTime? startDate = dtpDateFrom.Checked ? dtpDateFrom.Value : null;
            DateTime? endDate = dtpDateTo.Checked ? dtpDateTo.Value : null;

            // 搜索数据
            _dieList = _dieService.SearchDies(dieCode, customerName, status, auditStatus, startDate, endDate);
            _totalCount = _dieList.Count;

            // 分页显示
            var pageData = _dieList
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            dgvDieList.DataSource = null;
            dgvDieList.DataSource = pageData;

            // 更新分页信息
            int totalPages = (_totalCount + _pageSize - 1) / _pageSize;
            if (totalPages == 0) totalPages = 1;
            lblPageInfo.Text = $"第 {_currentPage} 页 / 共 {totalPages} 页 (共 {_totalCount} 条)";

            // 更新按钮状态
            btnFirst.Enabled = _currentPage > 1;
            btnPrev.Enabled = _currentPage > 1;
            btnNext.Enabled = _currentPage < totalPages;
            btnLast.Enabled = _currentPage < totalPages;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void GoToPage(int page)
    {
        int totalPages = (_totalCount + _pageSize - 1) / _pageSize;
        if (totalPages == 0) totalPages = 1;

        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        _currentPage = page;
        LoadDieList();
    }

    private DieInfo? GetSelectedDie()
    {
        if (dgvDieList.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先选择一条记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var dieId = Convert.ToInt32(dgvDieList.SelectedRows[0].Cells["DieID"].Value);
        return _dieList.FirstOrDefault(d => d.DieID == dieId);
    }

    #endregion
}
