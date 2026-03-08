namespace DieMaking.Forms.Production;

public partial class CompletionQueryForm : Form
{
    public CompletionQueryForm()
    {
        InitializeComponent();
        this.Text = "完工查询";
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1000, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        var lblInfo = new Label
        {
            Text = "完工查询功能开发中...",
            Font = new Font("微软雅黑", 14),
            AutoSize = true,
            Location = new Point(350, 250)
        };

        this.Controls.Add(lblInfo);
    }
}
