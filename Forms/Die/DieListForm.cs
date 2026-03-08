namespace DieMaking.Forms.Die;

public partial class DieListForm : Form
{
    public DieListForm()
    {
        InitializeComponent();
        this.Text = "刀模列表";
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1000, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        var lblInfo = new Label
        {
            Text = "刀模列表功能开发中...",
            Font = new Font("微软雅黑", 14),
            AutoSize = true,
            Location = new Point(350, 250)
        };

        this.Controls.Add(lblInfo);
    }
}
