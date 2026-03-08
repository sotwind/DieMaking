namespace DieMaking.Forms.Warehouse;

public partial class ScrapApplyForm : Form
{
    public ScrapApplyForm()
    {
        InitializeComponent();
        this.Text = "报废申请";
    }

    private void InitializeComponent()
    {
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        var lblInfo = new Label
        {
            Text = "报废申请功能开发中...",
            Font = new Font("微软雅黑", 14),
            AutoSize = true,
            Location = new Point(300, 250)
        };

        this.Controls.Add(lblInfo);
    }
}
