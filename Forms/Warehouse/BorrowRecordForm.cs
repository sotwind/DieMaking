namespace DieMaking.Forms.Warehouse;

public partial class BorrowRecordForm : Form
{
    public BorrowRecordForm()
    {
        InitializeComponent();
        this.Text = "借用记录";
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1000, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        var lblInfo = new Label
        {
            Text = "借用记录功能开发中...",
            Font = new Font("微软雅黑", 14),
            AutoSize = true,
            Location = new Point(350, 250)
        };

        this.Controls.Add(lblInfo);
    }
}
