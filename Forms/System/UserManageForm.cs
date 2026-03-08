namespace DieMaking.Forms.System;

public partial class UserManageForm : Form
{
    public UserManageForm()
    {
        InitializeComponent();
        this.Text = "用户管理";
    }

    private void InitializeComponent()
    {
        this.Size = new Size(1000, 600);
        this.StartPosition = FormStartPosition.CenterParent;

        var lblInfo = new Label
        {
            Text = "用户管理功能开发中...",
            Font = new Font("微软雅黑", 14),
            AutoSize = true,
            Location = new Point(350, 250)
        };

        this.Controls.Add(lblInfo);
    }
}
