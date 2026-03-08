using DieMaking.Forms;

namespace DieMaking;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 显示登录窗体
        using (var loginForm = new LoginForm())
        {
            if (loginForm.ShowDialog() != DialogResult.OK)
            {
                return; // 登录失败或取消，退出程序
            }
        }

        // 登录成功，显示主窗体
        Application.Run(new MainForm());
    }
}
