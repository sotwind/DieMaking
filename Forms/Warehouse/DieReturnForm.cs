using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.Warehouse;

public partial class DieReturnForm : BaseDialogForm
{
    private readonly WarehouseService _warehouseService;
    private List<DieBorrowRecord> _borrowingRecords = new();

    public DieReturnForm()
    {
        _warehouseService = new WarehouseService();
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "刀模归还";
        this.Size = UIStyleHelper.SizeEditForm;
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int y = 20;
        int labelWidth = 100;
        int controlWidth = 450;
        int leftMargin = 30;

        // 标题
        var lblTitle = new Label
        {
            Text = "刀模归还入库",
            Font = UIStyleHelper.GetLargeTitleFont(),
            AutoSize = true,
            Location = new Point(300, y)
        };
        y += 50;

        // 借用记录选择
        var lblRecord = UIStyleHelper.CreateLabel("选择借用记录：", new Point(leftMargin, y), new Size(labelWidth, 25));
        cboRecord = new ComboBox
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        cboRecord.SelectedIndexChanged += CboRecord_SelectedIndexChanged;
        y += 40;

        // 刀模信息
        var lblDieInfo = UIStyleHelper.CreateLabel("刀模信息：", new Point(leftMargin, y), new Size(labelWidth, 25));
        lblDieInfoValue = new Label
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25),
            ForeColor = UIStyleHelper.ColorInfo
        };
        y += 40;

        // 借用信息
        var lblBorrowInfo = UIStyleHelper.CreateLabel("借用信息：", new Point(leftMargin, y), new Size(labelWidth, 25));
        lblBorrowInfoValue = new Label
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25),
            ForeColor = UIStyleHelper.ColorSuccess
        };
        y += 40;

        // 领用人
        var lblBorrower = UIStyleHelper.CreateLabel("领用人：", new Point(leftMargin, y), new Size(labelWidth, 25));
        lblBorrowerValue = new Label
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 25)
        };
        y += 40;

        // 用途
        var lblPurpose = UIStyleHelper.CreateLabel("用途：", new Point(leftMargin, y), new Size(labelWidth, 25));
        lblPurposeValue = new Label
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(controlWidth, 50),
            AutoSize = false
        };
        y += 60;

        // 归还时间
        var lblReturnTime = UIStyleHelper.CreateLabel("归还时间：", new Point(leftMargin, y), new Size(labelWidth, 25));
        dtpReturnTime = new DateTimePicker
        {
            Location = new Point(leftMargin + labelWidth, y),
            Size = new Size(200, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            Value = DateTime.Now,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        y += 40;

        // 归还操作人
        var lblOperator = UIStyleHelper.CreateLabel("归还操作人：", new Point(leftMargin, y), new Size(labelWidth, 25));
        txtOperator = UIStyleHelper.CreateTextBox(new Point(leftMargin + labelWidth, y), new Size(200, 25), "请输入操作人");
        // 默认填充当前用户
        if (CurrentUser.User != null)
        {
            txtOperator.Text = CurrentUser.User.RealName ?? CurrentUser.User.Username;
        }
        y += 40;

        // 备注
        var lblRemark = UIStyleHelper.CreateLabel("归还备注：", new Point(leftMargin, y), new Size(labelWidth, 25));
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
        var btnSave = UIStyleHelper.CreateSaveButton("确认归还");
        btnSave.Size = new Size(120, 35);
        btnSave.Location = new Point(140, y);
        btnSave.Click += BtnSave_Click;

        var btnPrint = UIStyleHelper.CreatePrintButton();
        btnPrint.Location = new Point(280, y);
        btnPrint.Click += BtnPrint_Click;

        var btnCancel = UIStyleHelper.CreateCancelButton();
        btnCancel.Location = new Point(400, y);
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] {
            lblTitle,
            lblRecord, cboRecord, lblDieInfo, lblDieInfoValue, lblBorrowInfo, lblBorrowInfoValue,
            lblBorrower, lblBorrowerValue, lblPurpose, lblPurposeValue,
            lblReturnTime, dtpReturnTime, lblOperator, txtOperator, lblRemark, txtRemark,
            btnSave, btnPrint, btnCancel
        });

        // 注册回车跳转
        RegisterEnterToNext();
    }

    private ComboBox cboRecord = null!;
    private Label lblDieInfoValue = null!;
    private Label lblBorrowInfoValue = null!;
    private Label lblBorrowerValue = null!;
    private Label lblPurposeValue = null!;
    private DateTimePicker dtpReturnTime = null!;
    private TextBox txtOperator = null!;
    private TextBox txtRemark = null!;

    private void LoadData()
    {
        try
        {
            // 加载借用中的记录
            _borrowingRecords = _warehouseService.GetBorrowRecordsByStatus(BorrowStatus.Borrowing);

            cboRecord.DataSource = null;

            if (_borrowingRecords.Count == 0)
            {
                MessageBox.Show("当前没有借用中的刀模需要归还", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 创建显示列表
            var displayList = _borrowingRecords.Select(r => new
            {
                r.BorrowID,
                Display = $"{r.DieCode} - {r.CustomerName} - 领用人：{r.BorrowerName} - 借用时间：{r.BorrowTime:yyyy-MM-dd}"
            }).ToList();

            cboRecord.DataSource = displayList;
            cboRecord.DisplayMember = "Display";
            cboRecord.ValueMember = "BorrowID";

            if (cboRecord.Items.Count > 0)
            {
                cboRecord.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载数据失败：{ex.Message}");
        }
    }

    private void CboRecord_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cboRecord.SelectedValue == null) return;

        var borrowId = (int)cboRecord.SelectedValue;
        var record = _borrowingRecords.FirstOrDefault(r => r.BorrowID == borrowId);

        if (record != null)
        {
            lblDieInfoValue.Text = $"刀模编号：{record.DieCode}  客户：{record.CustomerName}  产品：{record.ProductName}";
            lblBorrowInfoValue.Text = $"借用类型：{record.BorrowTypeText}  借用时间：{record.BorrowTime:yyyy-MM-dd HH:mm}";
            lblBorrowerValue.Text = $"{record.BorrowerName} ({record.BorrowDept})";
            lblPurposeValue.Text = record.Purpose;
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (cboRecord.SelectedValue == null)
        {
            MessageBox.Show("请选择要归还的借用记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtOperator.Text) || txtOperator.Text == (string?)txtOperator.Tag)
        {
            UIStyleHelper.SetValidationError(txtOperator, true);
            MessageBox.Show("请输入归还操作人", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtOperator.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtOperator, false);

        try
        {
            var borrowId = (int)cboRecord.SelectedValue;
            var operatorName = txtOperator.Text.Trim();
            var operatorNo = operatorName; // 简化处理，实际应该使用工号

            var result = _warehouseService.ReturnDie(borrowId, operatorNo, operatorName, txtRemark.Text.Trim());

            if (result)
            {
                ShowSuccess("归还成功！");
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                ShowError("归还失败，请检查记录状态");
            }
        }
        catch (Exception ex)
        {
            ShowError($"归还失败：{ex.Message}");
        }
    }

    private void BtnPrint_Click(object? sender, EventArgs e)
    {
        if (cboRecord.SelectedValue == null)
        {
            MessageBox.Show("请先选择借用记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 创建打印用的DataGridView
        var dgvPrint = new DataGridView();
        dgvPrint.Columns.Add("Item", "项目");
        dgvPrint.Columns.Add("Value", "内容");

        var borrowId = (int)cboRecord.SelectedValue;
        var record = _borrowingRecords.FirstOrDefault(r => r.BorrowID == borrowId);

        if (record != null)
        {
            dgvPrint.Rows.Add("刀模编号", record.DieCode);
            dgvPrint.Rows.Add("客户名称", record.CustomerName);
            dgvPrint.Rows.Add("产品名称", record.ProductName);
            dgvPrint.Rows.Add("借用类型", record.BorrowTypeText);
            dgvPrint.Rows.Add("借用时间", record.BorrowTime.ToString("yyyy-MM-dd HH:mm"));
            dgvPrint.Rows.Add("领用人", record.BorrowerName);
            dgvPrint.Rows.Add("领用部门", record.BorrowDept);
            dgvPrint.Rows.Add("用途", record.Purpose);
        }

        dgvPrint.Rows.Add("归还时间", dtpReturnTime.Value.ToString("yyyy-MM-dd HH:mm:ss"));
        dgvPrint.Rows.Add("归还操作人", txtOperator.Text.Trim());
        dgvPrint.Rows.Add("归还备注", txtRemark.Text.Trim());

        var printService = new PrintService();
        var subtitle = $"打印时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}  操作员：{CurrentUser.User?.RealName ?? CurrentUser.User?.Username ?? "未知"}";
        printService.PrintPreview(dgvPrint, "刀模管理系统 - 归还记录", subtitle);
    }
}
