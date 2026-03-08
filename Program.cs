using DieMaking.Forms;
using DieMaking.Helpers;

namespace DieMaking;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 初始化数据库
        try
        {
            var initResult = DatabaseInitializer.Initialize();
            if (!initResult.Success)
            {
                MessageBox.Show($"数据库初始化失败: {initResult.ErrorMessage}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 执行数据库迁移
            var migrationResult = DatabaseMigration.Upgrade();
            if (!migrationResult.Success)
            {
                MessageBox.Show($"数据库升级失败: {migrationResult.ErrorMessage}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"数据库初始化异常: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

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
