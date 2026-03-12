using DieMaking.Forms;
using DieMaking.Helpers;
using DieMaking.Services;

namespace DieMaking;

static class Program
{
    private static System.Threading.Timer? _logCleanupTimer;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 初始化系统配置
        ConfigHelper.Initialize();

        // 启动日志自动清理定时器（每天执行一次）
        StartLogCleanupTimer();

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

        // 程序退出时清理定时器
        _logCleanupTimer?.Dispose();
    }

    /// <summary>
    /// 启动日志自动清理定时器
    /// </summary>
    private static void StartLogCleanupTimer()
    {
        try
        {
            // 计算到明天凌晨2点的时间差
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1).AddHours(2); // 明天凌晨2点
            var dueTime = nextRun - now;

            // 创建定时器，首次在明天凌晨2点执行，之后每24小时执行一次
            _logCleanupTimer = new System.Threading.Timer(
                callback: _ =>
                {
                    try
                    {
                        LogService.CleanupExpiredLogsAsync();
                    }
                    catch
                    {
                        // 定时任务异常不影响主程序
                    }
                },
                state: null,
                dueTime: dueTime,
                period: TimeSpan.FromHours(24));
        }
        catch
        {
            // 定时器启动失败不影响主程序运行
        }
    }
}
