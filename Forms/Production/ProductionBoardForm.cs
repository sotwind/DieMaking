namespace DieMaking.Forms.Production;

public partial class ProductionBoardForm : Form
{
    public ProductionBoardForm()
    {
        InitializeComponent();
        this.Text = "生产看板";
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1000, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        var lblInfo = new Label
        {
            Text = "生产看板功能开发中...",
            Font = new Font("微软雅黑", 14),
            AutoSize = true,
            Location = new Point(350, 250)
        };

        this.Controls.Add(lblInfo);
    }
}
