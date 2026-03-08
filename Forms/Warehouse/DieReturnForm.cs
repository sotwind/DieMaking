using DieMaking.Models;
using DieMaking.Services;
using DieMaking.Helpers;

namespace DieMaking.Forms.Warehouse;

public partial class DieReturnForm : Form
{
    private readonly WarehouseService _warehouseService;
    private List<DieBorrowRecord> _borrowingRecords = new();

    public DieReturnForm()
    {
        InitializeComponent();
        _warehouseService = new WarehouseService();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "刀模归还";
        this.Size = new Size(750, 500);
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
            Font = new Font("微软雅黑", 14, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(300, y)
        };
        y += 50;

        // 借用记录选择
        var lblRecord = new Label { Text = "选择借用记录：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        cboRecord = new ComboBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboRecord.SelectedIndexChanged += CboRecord_SelectedIndexChanged;
        y += 40;

        // 刀模信息
        var lblDieInfo = new Label { Text = "刀模信息：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblDieInfoValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25),
            ForeColor = Color.Blue
        };
        y += 40;

        // 借用信息
        var lblBorrowInfo = new Label { Text = "借用信息：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblBorrowInfoValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25),
            ForeColor = Color.Green
        };
        y += 40;

        // 领用人
        var lblBorrower = new Label { Text = "领用人：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblBorrowerValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 25)
        };
        y += 40;

        // 用途
        var lblPurpose = new Label { Text = "用途：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        lblPurposeValue = new Label 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 50),
            AutoSize = false
        };
        y += 60;

        // 归还时间
        var lblReturnTime = new Label { Text = "归还时间：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        dtpReturnTime = new DateTimePicker 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(200, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss",
            Value = DateTime.Now
        };
        y += 40;

        // 归还操作人
        var lblOperator = new Label { Text = "归还操作人：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
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

        // 备注
        var lblRemark = new Label { Text = "归还备注：", Location = new Point(leftMargin, y), Size = new Size(labelWidth, 25) };
        txtRemark = new TextBox 
        { 
            Location = new Point(leftMargin + labelWidth, y), 
            Size = new Size(controlWidth, 60),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        y += 80;

        // 按钮
        var btnSave = new Button { Text = "确认归还", Location = new Point(140, y), Size = new Size(120, 35) };
        btnSave.Click += BtnSave_Click;

        var btnPrint = new Button { Text = "打印", Location = new Point(280, y), Size = new Size(100, 35) };
        btnPrint.Click += BtnPrint_Click;

        var btnCancel = new Button { Text = "取消", Location = new Point(400, y), Size = new Size(100, 35) };
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        this.Controls.AddRange(new Control[] {
            lblTitle,
            lblRecord, cboRecord, lblDieInfo, lblDieInfoValue, lblBorrowInfo, lblBorrowInfoValue,
            lblBorrower, lblBorrowerValue, lblPurpose, lblPurposeValue,
            lblReturnTime, dtpReturnTime, lblOperator, txtOperator, lblRemark, txtRemark,
            btnSave, btnPrint, btnCancel
        });
    }

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
            MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        if (string.IsNullOrWhiteSpace(txtOperator.Text))
        {
            MessageBox.Show("请输入归还操作人", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtOperator.Focus();
            return;
        }

        try
        {
            var borrowId = (int)cboRecord.SelectedValue;
            var operatorName = txtOperator.Text.Trim();
            var operatorNo = operatorName; // 简化处理，实际应该使用工号

            var result = _warehouseService.ReturnDie(borrowId, operatorNo, operatorName, txtRemark.Text.Trim());
            
            if (result)
            {
                MessageBox.Show("归还成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show("归还失败，请检查记录状态", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"归还失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    private ComboBox cboRecord = null!;
    private Label lblDieInfoValue = null!;
    private Label lblBorrowInfoValue = null!;
    private Label lblBorrowerValue = null!;
    private Label lblPurposeValue = null!;
    private DateTimePicker dtpReturnTime = null!;
    private TextBox txtOperator = null!;
    private TextBox txtRemark = null!;
}
