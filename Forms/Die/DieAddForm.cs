namespace DieMaking.Forms.Die;

public partial class DieAddForm : Form
{
    public DieAddForm()
    {
        InitializeComponent();
        this.Text = "添加刀模";
    }

    private void InitializeComponent()
    {
        this.Size = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        var lblInfo = new Label
        {
            Text = "添加刀模功能开发中...",
            Font = new Font("微软雅黑", 14),
            AutoSize = true,
            Location = new Point(300, 250)
        };

        this.Controls.Add(lblInfo);
    }
}
