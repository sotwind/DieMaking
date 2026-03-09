using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms;

public partial class LoginForm : Form
{
    private readonly UserService _userService;
    private readonly string _configFilePath;

    public bool IsLoggedIn { get; private set; }

    public LoginForm()
    {
        InitializeComponent();
        _userService = new UserService();

        // 设置配置文件路径
        _configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DieMaking",
            "login.config"
        );

        // 加载保存的登录信息
        LoadSavedLoginInfo();

        // 应用统一字体
        UIStyleHelper.ApplyFont(this);
    }

    private void LoadSavedLoginInfo()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var lines = File.ReadAllLines(_configFilePath);
                if (lines.Length >= 3)
                {
                    string savedUsername = lines[0];
                    string savedPassword = lines[1];
                    bool rememberPassword = bool.Parse(lines[2]);

                    txtUsername.Text = savedUsername;

                    if (rememberPassword)
                    {
                        txtPassword.Text = savedPassword;
                        chkRememberPassword.Checked = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 如果读取失败，记录日志但忽略错误
            ExceptionHelper.HandleExceptionSilent(ex, "加载保存的登录信息");
        }
    }

    private void SaveLoginInfo()
    {
        try
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 保存登录信息
            var lines = new List<string>
            {
                txtUsername.Text.Trim(),
                chkRememberPassword.Checked ? txtPassword.Text.Trim() : "",
                chkRememberPassword.Checked.ToString()
            };

            File.WriteAllLines(_configFilePath, lines);
        }
        catch (Exception ex)
        {
            // 如果保存失败，记录日志但忽略错误
            ExceptionHelper.HandleExceptionSilent(ex, "保存登录信息");
        }
    }

    private void btnLogin_Click(object sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text.Trim();

        // 检查placeholder
        if (string.IsNullOrEmpty(username) || username == (string?)txtUsername.Tag)
        {
            UIStyleHelper.SetValidationError(txtUsername, true);
            MessageBox.Show("请输入用户名", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtUsername.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtUsername, false);

        if (string.IsNullOrEmpty(password))
        {
            UIStyleHelper.SetValidationError(txtPassword, true);
            MessageBox.Show("请输入密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPassword.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtPassword, false);

        Form? loadingForm = null;
        try
        {
            loadingForm = UIStyleHelper.ShowLoading(this, "正在登录...");

            var user = _userService.Login(username, password);

            if (user != null)
            {
                CurrentUser.User = user;

                // 保存登录信息
                SaveLoginInfo();

                IsLoggedIn = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                UIStyleHelper.SetValidationError(txtPassword, true);
                MessageBox.Show("用户名或密码错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("登录失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            loadingForm?.Close();
        }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    private void txtPassword_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == (char)Keys.Enter)
        {
            btnLogin_Click(sender, e);
        }
    }

    private void InitializeComponent()
    {
        this.Text = "刀模管理系统 - 登录";
        this.Size = new Size(400, 300);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // 标题标签
        var lblTitle = new Label
        {
            Text = "刀模管理系统",
            Font = UIStyleHelper.GetLargeTitleFont(),
            AutoSize = true,
            Location = new Point(120, 20)
        };

        // 用户名标签
        var lblUsername = UIStyleHelper.CreateLabel("用户名：", new Point(50, 70), new Size(70, 25));

        // 用户名输入框
        txtUsername = UIStyleHelper.CreateTextBox(new Point(130, 70), new Size(200, 25), "请输入用户名");

        // 密码标签
        var lblPassword = UIStyleHelper.CreateLabel("密码：", new Point(50, 110), new Size(70, 25));

        // 密码输入框
        txtPassword = new TextBox
        {
            Location = new Point(130, 110),
            Size = new Size(200, 25),
            PasswordChar = '*',
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        txtPassword.KeyPress += txtPassword_KeyPress;

        // 记住密码复选框
        chkRememberPassword = new CheckBox
        {
            Text = "记住密码",
            Location = new Point(130, 145),
            AutoSize = true,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        // 登录按钮
        btnLogin = UIStyleHelper.CreateSaveButton("登录");
        btnLogin.Location = new Point(130, 180);
        btnLogin.Click += btnLogin_Click;

        // 取消按钮
        btnCancel = UIStyleHelper.CreateCancelButton();
        btnCancel.Location = new Point(240, 180);
        btnCancel.Click += btnCancel_Click;

        // 添加控件
        this.Controls.Add(lblTitle);
        this.Controls.Add(lblUsername);
        this.Controls.Add(txtUsername);
        this.Controls.Add(lblPassword);
        this.Controls.Add(txtPassword);
        this.Controls.Add(chkRememberPassword);
        this.Controls.Add(btnLogin);
        this.Controls.Add(btnCancel);
    }

    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private CheckBox chkRememberPassword = null!;
    private Button btnLogin = null!;
    private Button btnCancel = null!;
}
