namespace DieMaking.Forms.Report;

public partial class ProcessStatsForm : Form
{
    public ProcessStatsForm()
    {
        InitializeComponent();
        this.Text = "工序统计";
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1000, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        var lblInfo = new Label
        {
            Text = "工序统计功能开发中...",
            Font = new Font("微软雅黑", 14),
            AutoSize = true,
            Location = new Point(350, 250)
        };

        this.Controls.Add(lblInfo);
    }
}
