# SettingsForm.cs 拆分方案（860行 → 多个partial类）

## 拆分策略

使用 C# partial 类将 860 行的 SettingsForm 拆分为多个文件：

### 1. SettingsForm.cs（主文件，约150行）

```csharp
using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;

namespace DieMaking.Forms.System;

public partial class SettingsForm : Form
{
    private readonly ConfigService _configService;
    private readonly Dictionary<string, string> _modifiedConfigs = new();

    public SettingsForm()
    {
        InitializeComponent();
        _configService = new ConfigService();

        // 检查权限
        if (!CurrentUser.HasPermission(PermissionKeys.UserManage))
        {
            MessageBox.Show("您没有权限访问系统设置功能！", "权限不足", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.Close();
            return;
        }

        this.Text = "系统设置";
        LoadSettings();
    }

    // TabControl 和按钮定义
    private TabControl tabControl = null!;
    private Button btnSave = null!;
    private Button btnReset = null!;
    private Button btnClose = null!;

    /// <summary>
    /// 加载所有设置
    /// </summary>
    private void LoadSettings()
    {
        LoadBasicSettings();
        LoadSecuritySettings();
        LoadLogSettings();
        LoadBackupSettings();
    }

    /// <summary>
    /// 保存所有设置
    /// </summary>
    private void BtnSave_Click(object? sender, EventArgs e)
    {
        SaveBasicSettings();
        SaveSecuritySettings();
        SaveLogSettings();
        SaveBackupSettings();

        if (_modifiedConfigs.Count > 0)
        {
            foreach (var config in _modifiedConfigs)
            {
                _configService.SetConfig(config.Key, config.Value);
            }
            MessageBox.Show("设置保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _modifiedConfigs.Clear();
        }
    }

    private void BtnReset_Click(object? sender, EventArgs e)
    {
        LoadSettings();
        _modifiedConfigs.Clear();
    }
}
```

### 2. SettingsForm.Designer.cs（UI初始化，约200行）

```csharp
namespace DieMaking.Forms.System;

public partial class SettingsForm
{
    private void InitializeComponent()
    {
        this.Size = new Size(700, 550);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // 标题
        var lblTitle = new Label
        {
            Text = "系统设置",
            Font = new Font("微软雅黑", 16, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        // 创建TabControl
        tabControl = new TabControl
        {
            Location = new Point(20, 55),
            Size = new Size(640, 400)
        };

        // 添加各Tab页
        tabControl.TabPages.Add(CreateBasicSettingsTab());
        tabControl.TabPages.Add(CreateSecuritySettingsTab());
        tabControl.TabPages.Add(CreateLogSettingsTab());
        tabControl.TabPages.Add(CreateBackupSettingsTab());

        // 按钮区域
        btnSave = new Button
        {
            Text = "保存",
            Location = new Point(480, 470),
            Size = new Size(80, 30)
        };
        btnSave.Click += BtnSave_Click;

        btnReset = new Button
        {
            Text = "重置",
            Location = new Point(380, 470),
            Size = new Size(80, 30)
        };
        btnReset.Click += BtnReset_Click;

        btnClose = new Button
        {
            Text = "关闭",
            Location = new Point(580, 470),
            Size = new Size(80, 30)
        };
        btnClose.Click += (s, e) => this.Close();

        // 添加到窗体
        this.Controls.Add(lblTitle);
        this.Controls.Add(tabControl);
        this.Controls.Add(btnSave);
        this.Controls.Add(btnReset);
        this.Controls.Add(btnClose);
    }
}
```

### 3. SettingsForm.Basic.cs（基本设置，约150行）

```csharp
namespace DieMaking.Forms.System;

public partial class SettingsForm
{
    // 基本设置控件
    private TextBox txtCompanyName = null!;
    private TextBox txtContactPhone = null!;
    private TextBox txtAddress = null!;
    private NumericUpDown numPageSize = null!;

    private TabPage CreateBasicSettingsTab()
    {
        var tab = new TabPage("基本设置");
        // 初始化控件...
        return tab;
    }

    private void LoadBasicSettings()
    {
        // 加载基本设置...
    }

    private void SaveBasicSettings()
    {
        // 保存基本设置...
    }
}
```

### 4. SettingsForm.Security.cs（安全设置，约150行）

```csharp
namespace DieMaking.Forms.System;

public partial class SettingsForm
{
    // 安全设置控件
    private NumericUpDown numPasswordMinLength = null!;
    private CheckBox chkRequireUppercase = null!;
    private CheckBox chkRequireNumber = null!;
    private NumericUpDown numSessionTimeout = null!;

    private TabPage CreateSecuritySettingsTab()
    {
        var tab = new TabPage("安全设置");
        // 初始化控件...
        return tab;
    }

    private void LoadSecuritySettings()
    {
        // 加载安全设置...
    }

    private void SaveSecuritySettings()
    {
        // 保存安全设置...
    }
}
```

### 5. SettingsForm.Log.cs（日志设置，约100行）

```csharp
namespace DieMaking.Forms.System;

public partial class SettingsForm
{
    private TabPage CreateLogSettingsTab()
    {
        var tab = new TabPage("日志设置");
        // ...
        return tab;
    }

    private void LoadLogSettings() { }
    private void SaveLogSettings() { }
}
```

### 6. SettingsForm.Backup.cs（备份设置，约100行）

```csharp
namespace DieMaking.Forms.System;

public partial class SettingsForm
{
    private TabPage CreateBackupSettingsTab()
    {
        var tab = new TabPage("备份设置");
        // ...
        return tab;
    }

    private void LoadBackupSettings() { }
    private void SaveBackupSettings() { }
}
```

## 文件结构

```
Forms/System/
├── SettingsForm.cs              # 主类（150行）
├── SettingsForm.Designer.cs     # UI初始化（200行）
├── SettingsForm.Basic.cs        # 基本设置（150行）
├── SettingsForm.Security.cs     # 安全设置（150行）
├── SettingsForm.Log.cs          # 日志设置（100行）
└── SettingsForm.Backup.cs       # 备份设置（100行）
```

## 优势

1. 每个文件职责单一，易于维护
2. 使用 partial 类，编译后仍是一个类
3. 方便多人协作开发不同设置模块
4. 代码行数控制在合理范围
