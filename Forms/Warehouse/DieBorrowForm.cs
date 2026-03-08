using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Warehouse;

public partial class DieBorrowForm : BaseDialogForm
{
    private readonly WarehouseService _warehouseService;
    private List<DieInventory> _inStockDies = new();
    private List<StorageLocation> _locations = new();

    public DieBorrowForm()
    {
        _warehouseService = new WarehouseService();
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "刀模领用";
        this.Size = UIStyleHelper.SizeEditForm;
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
            Font = UIStyleHelper.GetLargeTitleFont(),
            AutoSize = true,
            Location = new Point(320, y)
        };
        y += 50;

        // 刀模选择
        var lblDie = UIStyleHelper.CreateLabel("选择刀模：", new Point(leftMargin, y), new Size(labelWidth, 25));
        cboDie = new ComboBox
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        cboDie.SelectedIndexChanged += CboDie_SelectedIndexChanged;
        y += 40;

        // 刀模信息
        var lblDieInfo = UIStyleHelper.CreateLabel("刀模信息：", new Point(leftMargin, y), new Size(labelWidth, 25));
        lblDieInfoValue = new Label
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth * 2, 25),
            ForeColor = UIStyleHelper.ColorInfo
        };
        y += 40;

        // 当前库位
        var lblLocation = UIStyleHelper.CreateLabel("当前库位：", new Point(leftMargin, y), new Size(labelWidth, 25));
        lblLocationValue = new Label
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25),
            ForeColor = UIStyleHelper.ColorSuccess
        };
        y += 40;

        // 领用类型
        var lblType = UIStyleHelper.CreateLabel("领用类型：", new Point(leftMargin, y), new Size(labelWidth, 25));
        cboType = new ComboBox
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        cboType.Items.Add(new { Text = "生产领用", Value = BorrowType.Production });
        cboType.Items.Add(new { Text = "外借", Value = BorrowType.External });
        cboType.Items.Add(new { Text = "调拨", Value = BorrowType.Transfer });
        cboType.DisplayMember = "Text";
        cboType.ValueMember = "Value";
        cboType.SelectedIndex = 0;
        y += 40;

        // 领用人
        var lblBorrower = UIStyleHelper.CreateLabel("领用人：", new Point(leftMargin, y), new Size(labelWidth, 25));
        txtBorrower = UIStyleHelper.CreateTextBox(new Point(leftMargin + labelWidth, y), new Size(controlWidth, 25), "请输入领用人姓名");
        y += 40;

        // 领用部门
        var lblDept = UIStyleHelper.CreateLabel("领用部门：", new Point(leftMargin, y), new Size(labelWidth, 25));
        txtDept = UIStyleHelper.CreateTextBox(new Point(leftMargin + labelWidth, y), new Size(controlWidth, 25), "请输入领用部门");
        y += 40;

        // 领用时间
        var lblTime = UIStyleHelper.CreateLabel("领用时间：", new Point(leftMargin, y), new Size(labelWidth, 25));
        dtpBorrowTime = new DateTimePicker
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        y += 40;

        // 预计归还时间
        var lblReturnTime = UIStyleHelper.CreateLabel("预计归还：", new Point(leftMargin, y), new Size(labelWidth, 25));
        dtpExpectedReturn = new DateTimePicker
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            ShowCheckBox = true,
            Checked = false,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        y += 40;

        // 用途
        var lblPurpose = UIStyleHelper.CreateLabel("用途说明：", new Point(leftMargin, y), new Size(labelWidth, 25));
        txtPurpose = new TextBox
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 60),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        y += 70;

        // 备注
        var lblRemark = UIStyleHelper.CreateLabel("备注：", new Point(leftMargin, y), new Size(labelWidth, 25));
        txtRemark = new TextBox
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 50),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        y += 70;

        // 按钮
        var btnSave = UIStyleHelper.CreateSaveButton("确认领用");
        btnSave.Size = new Size(120, 35);
        btnSave.Location = new Point(200, y);
        btnSave.Click += BtnSave_Click;

        var btnCancel = UIStyleHelper.CreateCancelButton();
        btnCancel.Size = new Size(100, 35);
        btnCancel.Location = new Point(350, y);
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] {
            lblTitle,
            lblDie, cboDie, lblDieInfo, lblDieInfoValue, lblLocation, lblLocationValue,
            lblType, cboType, lblBorrower, txtBorrower, lblDept, txtDept,
            lblTime, dtpBorrowTime, lblReturnTime, dtpExpectedReturn,
            lblPurpose, txtPurpose, lblRemark, txtRemark,
            btnSave, btnCancel
        });

        // 注册回车跳转
        RegisterEnterToNext();
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
            ShowError($"加载数据失败：{ex.Message}");
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

        if (string.IsNullOrWhiteSpace(txtBorrower.Text) || txtBorrower.Text == (string?)txtBorrower.Tag)
        {
            UIStyleHelper.SetValidationError(txtBorrower, true);
            MessageBox.Show("请输入领用人", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtBorrower.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtBorrower, false);

        if (string.IsNullOrWhiteSpace(txtDept.Text) || txtDept.Text == (string?)txtDept.Tag)
        {
            UIStyleHelper.SetValidationError(txtDept, true);
            MessageBox.Show("请输入领用部门", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtDept.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtDept, false);

        if (string.IsNullOrWhiteSpace(txtPurpose.Text))
        {
            UIStyleHelper.SetValidationError(txtPurpose, true);
            MessageBox.Show("请输入用途说明", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPurpose.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtPurpose, false);

        try
        {
            var inventoryId = (int)cboDie.SelectedValue;
            var die = _inStockDies.FirstOrDefault(d => d.InventoryID == inventoryId);

            if (die == null)
            {
                ShowError("选择的刀模信息无效");
                return;
            }

            var record = new DieBorrowRecord
            {
                DieID = die.DieID,
                InventoryID = die.InventoryID,
                BorrowType = (BorrowType)((dynamic)cboType.SelectedItem!).Value,
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
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                ShowError("领用失败");
            }
        }
        catch (Exception ex)
        {
            ShowError($"领用失败：{ex.Message}");
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
