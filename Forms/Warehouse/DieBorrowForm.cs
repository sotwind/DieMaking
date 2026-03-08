using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Warehouse;

public partial class DieBorrowForm : Form
{
    private readonly WarehouseService _warehouseService;
    private List<DieInventory> _inStockDies = new();
    private List<StorageLocation> _locations = new();

    public DieBorrowForm()
    {
        InitializeComponent();
        _warehouseService = new WarehouseService();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "刀模领用";
        this.Size = new Size(800, 550);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int y = 20;
        int labelWidth = 100;
        int controlWidth = 250;
        int leftMargin = 30;

        // 标题
        var lblTitle = new Label
        {
            Text = "刀模领用出库",
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(320, y)
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
            Size = new Size(controlWidth * 2, 25),
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

        // 领用类型
        var lblType = new Label { Text = "领用类型：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        cboType = new ComboBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboType.Items.Add(new { Text = "生产领用", Value = BorrowType.Production });
        cboType.Items.Add(new { Text = "外借", Value = BorrowType.External });
        cboType.Items.Add(new { Text = "调拨", Value = BorrowType.Transfer });
        cboType.DisplayMember = "Text";
        cboType.ValueMember = "Value";
        cboType.SelectedIndex = 0;
        y += 40;

        // 领用人
        var lblBorrower = new Label { Text = "领用人：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        txtBorrower = new TextBox { Location = new Point(leftMargin + labelWidth, y), Size = new Size(controlWidth, 25) };
        y += 40;

        // 领用部门
        var lblDept = new Label { Text = "领用部门：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        txtDept = new TextBox { Location = new Point(leftMargin + labelWidth, y), Size = new Size(controlWidth, 25) };
        y += 40;

        // 领用时间
        var lblTime = new Label { Text = "领用时间：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        dtpBorrowTime = new DateTimePicker 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss"
        };
        y += 40;

        // 预计归还时间
        var lblReturnTime = new Label { Text = "预计归还：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        dtpExpectedReturn = new DateTimePicker 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            ShowCheckBox = true,
            Checked = false
        };
        y += 40;

        // 用途
        var lblPurpose = new Label { Text = "用途说明：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        txtPurpose = new TextBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 60),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        y += 70;

        // 备注
        var lblRemark = new Label { Text = "备注：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        txtRemark = new TextBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 50),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        y += 70;

        // 按钮
        var btnSave = new Button { Text = "确认领用", Location = new Point(200, y), Size = new Size(120, 35) };
        btnSave.Click += BtnSave_Click;

        var btnCancel = new Button { Text = "取消", Location = new Point(350, y), Size = new Size(100, 35) };
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] {
            lblTitle,
            lblDie, cboDie, lblDieInfo, lblDieInfoValue, lblLocation, lblLocationValue,
            lblType, cboType, lblBorrower, txtBorrower, lblDept, txtDept,
            lblTime, dtpBorrowTime, lblReturnTime, dtpExpectedReturn,
            lblPurpose, txtPurpose, lblRemark, txtRemark,
            btnSave, btnCancel
        });
    }

    private void LoadData()
    {
        try
        {
            // 加载在库刀模
            _inStockDies = _warehouseService.GetInStockInventory();
            cboDie.DataSource = null;
            cboDie.DisplayMember = "DieCode";
            cboDie.ValueMember = "InventoryID";
            
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

            if (cboDie.Items.Count == 0)
            {
                MessageBox.Show("当前没有在库的刀模可供领用", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            MessageBox.Show("请选择要领用的刀模", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtBorrower.Text))
        {
            MessageBox.Show("请输入领用人", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtBorrower.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtDept.Text))
        {
            MessageBox.Show("请输入领用部门", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtDept.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(txtPurpose.Text))
        {
            MessageBox.Show("请输入用途说明", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPurpose.Focus();
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

            var record = new DieBorrowRecord
            {
                DieID = die.DieID,
                InventoryID = die.InventoryID,
                BorrowType = (BorrowType)((dynamic)cboType.SelectedItem).Value,
                BorrowerNo = txtBorrower.Text.Trim(),
                BorrowerName = txtBorrower.Text.Trim(),
                BorrowDept = txtDept.Text.Trim(),
                BorrowTime = dtpBorrowTime.Value,
                ExpectedReturnTime = dtpExpectedReturn.Checked ? dtpExpectedReturn.Value : null,
                Purpose = txtPurpose.Text.Trim(),
                Remark = txtRemark.Text.Trim()
            };

            var borrowId = _warehouseService.CreateBorrowRecord(record);
            
            if (borrowId > 0)
            {
                MessageBox.Show("领用成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("领用失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"领用失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private ComboBox cboDie = null!;
    private Label lblDieInfoValue = null!;
    private Label lblLocationValue = null!;
    private ComboBox cboType = null!;
    private TextBox txtBorrower = null!;
    private TextBox txtDept = null!;
    private DateTimePicker dtpBorrowTime = null!;
    private DateTimePicker dtpExpectedReturn = null!;
    private TextBox txtPurpose = null!;
    private TextBox txtRemark = null!;
}
