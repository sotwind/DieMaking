using DieMaking.Helpers;
using DieMaking.Models;
using DieMaking.Services;
using Microsoft.Data.SqlClient;

namespace DieMaking.Forms.System;

public partial class UserManageForm : BaseListForm
{
    private readonly UserService _userService;
    private BindingSource _bindingSource = new();
    private List<User> _users = new();

    public UserManageForm()
    {
        InitializeComponent();
        _userService = new UserService();

        // 检查权限 - 由调用方处理权限不足的情况
        if (!CurrentUser.HasPermission(PermissionKeys.UserManage))
        {
            MessageBox.Show("您没有权限访问用户管理功能！", "权限不足", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            // 不立即关闭窗体，设置标记让调用方处理
            _permissionDenied = true;
            return;
        }

        this.Text = "用户管理";
    }

    private bool _permissionDenied = false;

    /// <summary>
    /// 检查是否因权限不足而被拒绝访问
    /// </summary>
    public bool IsPermissionDenied => _permissionDenied;

    private void InitializeComponent()
    {
        this.Size = UIStyleHelper.SizeListForm;
        this.StartPosition = FormStartPosition.CenterParent;

        // 标题标签
        var lblTitle = new Label
        {
            Text = "用户管理",
            Font = UIStyleHelper.GetTitleFont(),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        // 搜索区域
        var lblSearch = UIStyleHelper.CreateLabel("搜索：", new Point(20, 55), new Size(50, 25));

        txtSearch = UIStyleHelper.CreateTextBox(new Point(75, 52), new Size(200, 25), "输入用户名或姓名");
        txtSearch.TextChanged += TxtSearch_TextChanged;

        // 状态筛选
        var lblStatus = UIStyleHelper.CreateLabel("状态：", new Point(290, 55), new Size(50, 25));

        cmbStatus = new ComboBox
        {
            Location = new Point(345, 52),
            Size = new Size(100, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };
        cmbStatus.Items.AddRange(new object[] { "全部", "启用", "禁用" });
        cmbStatus.SelectedIndex = 0;
        cmbStatus.SelectedIndexChanged += CmbStatus_SelectedIndexChanged;

        // 按钮区域
        btnAdd = UIStyleHelper.CreateAddButton("新增用户");
        btnAdd.Location = new Point(470, 50);
        btnAdd.Click += BtnAdd_Click;

        btnEdit = UIStyleHelper.CreateEditButton("编辑用户");
        btnEdit.Location = new Point(580, 50);
        btnEdit.Click += BtnEdit_Click;

        btnResetPassword = new Button { Text = "重置密码", Location = new Point(690, 50), Size = UIStyleHelper.SizeButton };
        ApplyButtonStyle(btnResetPassword, ButtonStyle.Default);
        btnResetPassword.Click += BtnResetPassword_Click;

        btnToggleStatus = new Button { Text = "启用/禁用", Location = new Point(800, 50), Size = UIStyleHelper.SizeButton };
        ApplyButtonStyle(btnToggleStatus, ButtonStyle.Default);
        btnToggleStatus.Click += BtnToggleStatus_Click;

        btnDelete = UIStyleHelper.CreateDeleteButton("删除用户");
        btnDelete.Location = new Point(910, 50);
        btnDelete.Click += BtnDelete_Click;

        btnRefresh = UIStyleHelper.CreateSearchButton("刷新");
        btnRefresh.Location = new Point(1020, 50);
        btnRefresh.Click += BtnRefresh_Click;

        // 数据表格
        dgvUsers = new DataGridView
        {
            Location = new Point(20, 95),
            Size = new Size(1140, 500)
        };
        ApplyDataGridViewStyle(dgvUsers);

        // 添加列
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "UserID",
            HeaderText = "用户ID",
            DataPropertyName = "UserID",
            Width = 80
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Username",
            HeaderText = "用户名",
            DataPropertyName = "Username",
            Width = 120
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RealName",
            HeaderText = "姓名",
            DataPropertyName = "RealName",
            Width = 120
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Permissions",
            HeaderText = "角色/权限",
            DataPropertyName = "Permissions",
            Width = 200
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Workstation",
            HeaderText = "工位",
            DataPropertyName = "Workstation",
            Width = 100
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "状态",
            Width = 80
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CreateTime",
            HeaderText = "创建时间",
            DataPropertyName = "CreateTime",
            Width = 150
        });
        dgvUsers.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "LastLoginTime",
            HeaderText = "最后登录",
            DataPropertyName = "LastLoginTime",
            Width = 150
        });

        dgvUsers.CellFormatting += DgvUsers_CellFormatting;
        dgvUsers.CellDoubleClick += DgvUsers_CellDoubleClick;

        // 添加右键菜单
        var contextMenu = UIStyleHelper.CreateDataGridViewContextMenu(
            onView: null,
            onEdit: () => BtnEdit_Click(null, EventArgs.Empty),
            onDelete: null
        );
        dgvUsers.ContextMenuStrip = contextMenu;

        // 状态栏
        var statusStrip = CreateStatusBar();

        // 添加控件
        this.Controls.Add(lblTitle);
        this.Controls.Add(lblSearch);
        this.Controls.Add(txtSearch);
        this.Controls.Add(lblStatus);
        this.Controls.Add(cmbStatus);
        this.Controls.Add(btnAdd);
        this.Controls.Add(btnEdit);
        this.Controls.Add(btnResetPassword);
        this.Controls.Add(btnToggleStatus);
        this.Controls.Add(btnDelete);
        this.Controls.Add(btnRefresh);
        this.Controls.Add(dgvUsers);
        this.Controls.Add(statusStrip);
    }

    private TextBox txtSearch = null!;
    private ComboBox cmbStatus = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnResetPassword = null!;
    private Button btnToggleStatus = null!;
    private Button btnDelete = null!;
    private Button btnRefresh = null!;
    private DataGridView dgvUsers = null!;

    protected override void LoadData()
    {
        try
        {
            _users = _userService.GetAllUsers();
            FilterUsers();
        }
        catch (Exception ex)
        {
            ShowError($"加载用户数据失败：{ex.Message}");
        }
    }

    private void FilterUsers()
    {
        var filteredUsers = _users.AsEnumerable();

        // 搜索过滤
        var searchText = txtSearch.Text.Trim().ToLower();
        if (!string.IsNullOrEmpty(searchText) && searchText != ((string?)txtSearch.Tag)?.ToLower())
        {
            filteredUsers = filteredUsers.Where(u =>
                u.Username.ToLower().Contains(searchText) ||
                u.RealName.ToLower().Contains(searchText));
        }

        // 状态过滤
        if (cmbStatus.SelectedIndex == 1) // 启用
        {
            filteredUsers = filteredUsers.Where(u => u.IsActive);
        }
        else if (cmbStatus.SelectedIndex == 2) // 禁用
        {
            filteredUsers = filteredUsers.Where(u => !u.IsActive);
        }

        _bindingSource.DataSource = filteredUsers.ToList();
        dgvUsers.DataSource = _bindingSource;

        // 更新状态栏
        if (StatusUserLabel != null)
        {
            StatusUserLabel.Text = $"共 {filteredUsers.Count()} 位用户";
        }
    }

    private void TxtSearch_TextChanged(object? sender, EventArgs e)
    {
        FilterUsers();
    }

    private void CmbStatus_SelectedIndexChanged(object? sender, EventArgs e)
    {
        FilterUsers();
    }

    private void DgvUsers_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex == dgvUsers.Columns["Status"].Index && e.RowIndex >= 0)
        {
            var user = (User)dgvUsers.Rows[e.RowIndex].DataBoundItem;
            e.Value = user.IsActive ? "启用" : "禁用";
            e.CellStyle!.ForeColor = user.IsActive ? UIStyleHelper.ColorSuccess : UIStyleHelper.ColorDanger;
        }

        if (e.ColumnIndex == dgvUsers.Columns["LastLoginTime"].Index && e.RowIndex >= 0)
        {
            if (e.Value == null)
            {
                e.Value = "从未登录";
            }
        }
    }

    private void DgvUsers_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            BtnEdit_Click(sender, e);
        }
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        var form = new UserEditForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            LoadData();
        }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (dgvUsers.CurrentRow == null)
        {
            MessageBox.Show("请先选择一个用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var user = (User)dgvUsers.CurrentRow.DataBoundItem;
        var form = new UserEditForm(user);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            LoadData();
        }
    }

    private void BtnResetPassword_Click(object? sender, EventArgs e)
    {
        if (dgvUsers.CurrentRow == null)
        {
            MessageBox.Show("请先选择一个用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var user = (User)dgvUsers.CurrentRow.DataBoundItem;

        if (MessageBox.Show($"确定要重置用户 [{user.Username}] 的密码吗？\n密码将重置为：123456",
            "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            try
            {
                if (_userService.UpdatePassword(user.UserID, "123456"))
                {
                    ShowSuccess("密码重置成功！");
                    LogOperation("重置密码", $"重置用户 {user.Username} 的密码");
                }
                else
                {
                    ShowError("密码重置失败！");
                }
            }
            catch (Exception ex)
            {
                ShowError($"密码重置失败：{ex.Message}");
            }
        }
    }

    private void BtnToggleStatus_Click(object? sender, EventArgs e)
    {
        if (dgvUsers.CurrentRow == null)
        {
            MessageBox.Show("请先选择一个用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var user = (User)dgvUsers.CurrentRow.DataBoundItem;

        // 不能禁用自己
        if (user.UserID == CurrentUser.User?.UserID)
        {
            MessageBox.Show("不能禁用当前登录的用户！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var newStatus = !user.IsActive;
        var actionText = newStatus ? "启用" : "禁用";

        if (MessageBox.Show($"确定要{actionText}用户 [{user.Username}] 吗？",
            "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            try
            {
                user.IsActive = newStatus;
                if (_userService.UpdateUser(user))
                {
                    ShowSuccess($"用户{actionText}成功！");
                    LogOperation($"{actionText}用户", $"{actionText}用户 {user.Username}");
                    LoadData();
                }
                else
                {
                    ShowError($"用户{actionText}失败！");
                }
            }
            catch (Exception ex)
            {
                ShowError($"操作失败：{ex.Message}");
            }
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvUsers.CurrentRow == null)
        {
            MessageBox.Show("请先选择一个用户", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var user = (User)dgvUsers.CurrentRow.DataBoundItem;

        // 不能删除自己
        if (user.UserID == CurrentUser.User?.UserID)
        {
            MessageBox.Show("不能删除当前登录的用户！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 不能删除admin用户
        if (user.Username.ToLower() == "admin")
        {
            MessageBox.Show("不能删除系统管理员账号！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show($"确定要删除用户 [{user.Username}] 吗？\n此操作不可恢复！",
            "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            try
            {
                if (_userService.DeleteUser(user.UserID))
                {
                    ShowSuccess("用户删除成功！");
                    LogOperation("删除用户", $"删除用户 {user.Username}");
                    LoadData();
                }
                else
                {
                    ShowError("用户删除失败！");
                }
            }
            catch (Exception ex)
            {
                ShowError($"删除失败：{ex.Message}");
            }
        }
    }

    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        LoadData();
    }

    private void LogOperation(string operationType, string operationDesc)
    {
        try
        {
            var sql = @"INSERT INTO DM_OperationLog (UserID, Username, OperationType, OperationDesc, CreateTime) 
                        VALUES (@UserID, @Username, @OperationType, @OperationDesc, GETDATE())";
            DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@UserID", CurrentUser.User?.UserID),
                new SqlParameter("@Username", CurrentUser.User?.Username ?? ""),
                new SqlParameter("@OperationType", operationType),
                new SqlParameter("@OperationDesc", operationDesc));
        }
        catch
        {
            // 日志记录失败不影响主流程
        }
    }
}

/// <summary>
/// 用户编辑窗体（新增/编辑）
/// </summary>
public partial class UserEditForm : BaseDialogForm
{
    private readonly UserService _userService;
    private readonly User? _user;
    private readonly bool _isEdit;

    public UserEditForm(User? user = null)
    {
        InitializeComponent();

        _userService = new UserService();
        _user = user;
        _isEdit = user != null;

        this.Text = _isEdit ? "编辑用户" : "新增用户";

        if (_isEdit && _user != null)
        {
            LoadUserData();
        }
    }

    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private TextBox txtRealName = null!;
    private TextBox txtWorkstation = null!;
    private CheckedListBox clbPermissions = null!;
    private CheckBox chkIsActive = null!;
    private Button btnSave = null!;
    private Button btnCancel = null!;

    private void InitializeComponent()
    {
        this.Size = new Size(450, 520);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int labelWidth = 80;
        int inputWidth = 300;
        int startY = 30;
        int rowHeight = 45;

        // 用户名
        var lblUsername = UIStyleHelper.CreateLabel("用户名：", new Point(30, startY), new Size(labelWidth, 25));
        txtUsername = UIStyleHelper.CreateTextBox(new Point(115, startY), new Size(inputWidth, 25), "请输入用户名");

        // 密码
        var lblPassword = UIStyleHelper.CreateLabel("密码：", new Point(30, startY + rowHeight), new Size(labelWidth, 25));
        txtPassword = new TextBox
        {
            Location = new Point(115, startY + rowHeight),
            Size = new Size(inputWidth, 25),
            PasswordChar = '*',
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        var lblPasswordHint = new Label
        {
            Text = "（编辑时留空表示不修改密码）",
            Location = new Point(115, startY + rowHeight + 25),
            Size = new Size(250, 20),
            ForeColor = Color.Gray,
            Font = new Font(UIStyleHelper.FontName, 9f, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        // 姓名
        var lblRealName = UIStyleHelper.CreateLabel("姓名：", new Point(30, startY + rowHeight * 2 + 10), new Size(labelWidth, 25));
        txtRealName = UIStyleHelper.CreateTextBox(new Point(115, startY + rowHeight * 2 + 10), new Size(inputWidth, 25), "请输入姓名");

        // 工位
        var lblWorkstation = UIStyleHelper.CreateLabel("工位：", new Point(30, startY + rowHeight * 3 + 10), new Size(labelWidth, 25));
        txtWorkstation = UIStyleHelper.CreateTextBox(new Point(115, startY + rowHeight * 3 + 10), new Size(inputWidth, 25), "请输入工位");

        // 权限
        var lblPermissions = UIStyleHelper.CreateLabel("权限：", new Point(30, startY + rowHeight * 4 + 10), new Size(labelWidth, 25));
        clbPermissions = new CheckedListBox
        {
            Location = new Point(115, startY + rowHeight * 4 + 10),
            Size = new Size(inputWidth, 120),
            CheckOnClick = true,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        // 添加权限选项
        clbPermissions.Items.Add(PermissionKeys.DieManage);
        clbPermissions.Items.Add(PermissionKeys.DieAdd);
        clbPermissions.Items.Add(PermissionKeys.DieEdit);
        clbPermissions.Items.Add(PermissionKeys.DieAudit);
        clbPermissions.Items.Add(PermissionKeys.Production);
        clbPermissions.Items.Add(PermissionKeys.WarehouseManage);
        clbPermissions.Items.Add(PermissionKeys.LocationManage);
        clbPermissions.Items.Add(PermissionKeys.DieBorrow);
        clbPermissions.Items.Add(PermissionKeys.DieReturn);
        clbPermissions.Items.Add(PermissionKeys.BorrowRecord);
        clbPermissions.Items.Add(PermissionKeys.ScrapApply);
        clbPermissions.Items.Add(PermissionKeys.ScrapAudit);
        clbPermissions.Items.Add(PermissionKeys.Report);
        clbPermissions.Items.Add(PermissionKeys.UserManage);

        // 状态
        chkIsActive = new CheckBox
        {
            Text = "启用",
            Location = new Point(115, startY + rowHeight * 4 + 140),
            Size = new Size(100, 25),
            Checked = true,
            Font = new Font(UIStyleHelper.FontName, UIStyleHelper.FontSizeNormal, FontStyle.Regular, GraphicsUnit.Point, 134)
        };

        // 按钮
        btnSave = UIStyleHelper.CreateSaveButton();
        btnSave.Location = new Point(115, 365);
        btnSave.Click += BtnSave_Click;

        btnCancel = UIStyleHelper.CreateCancelButton();
        btnCancel.Location = new Point(225, 365);
        btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

        // 添加控件
        this.Controls.Add(lblUsername);
        this.Controls.Add(txtUsername);
        this.Controls.Add(lblPassword);
        this.Controls.Add(txtPassword);
        this.Controls.Add(lblPasswordHint);
        this.Controls.Add(lblRealName);
        this.Controls.Add(txtRealName);
        this.Controls.Add(lblWorkstation);
        this.Controls.Add(txtWorkstation);
        this.Controls.Add(lblPermissions);
        this.Controls.Add(clbPermissions);
        this.Controls.Add(chkIsActive);
        this.Controls.Add(btnSave);
        this.Controls.Add(btnCancel);

        // 注册回车跳转
        RegisterEnterToNext();
    }

    private void LoadUserData()
    {
        if (_user == null) return;

        txtUsername.Text = _user.Username;
        txtPassword.Text = ""; // 编辑时密码为空表示不修改
        txtRealName.Text = _user.RealName;
        txtWorkstation.Text = _user.Workstation;
        chkIsActive.Checked = _user.IsActive;

        // 设置权限选中状态
        var permissions = _user.GetPermissionList();
        for (int i = 0; i < clbPermissions.Items.Count; i++)
        {
            if (permissions.Contains(clbPermissions.Items[i].ToString()))
            {
                clbPermissions.SetItemChecked(i, true);
            }
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        // 验证输入
        var username = txtUsername.Text.Trim();
        var password = txtPassword.Text;
        var realName = txtRealName.Text.Trim();

        if (string.IsNullOrEmpty(username) || username == (string?)txtUsername.Tag)
        {
            UIStyleHelper.SetValidationError(txtUsername, true);
            MessageBox.Show("请输入用户名", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtUsername.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtUsername, false);

        if (!_isEdit && string.IsNullOrEmpty(password))
        {
            UIStyleHelper.SetValidationError(txtPassword, true);
            MessageBox.Show("请输入密码", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPassword.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtPassword, false);

        // 验证密码策略（新增用户或修改密码时）
        if (!string.IsNullOrEmpty(password))
        {
            var configService = new ConfigService();
            var (isValid, message) = configService.ValidatePassword(password);
            if (!isValid)
            {
                UIStyleHelper.SetValidationError(txtPassword, true);
                MessageBox.Show($"密码不符合策略要求：{message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }
            UIStyleHelper.SetValidationError(txtPassword, false);
        }

        if (string.IsNullOrEmpty(realName) || realName == (string?)txtRealName.Tag)
        {
            UIStyleHelper.SetValidationError(txtRealName, true);
            MessageBox.Show("请输入姓名", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtRealName.Focus();
            return;
        }
        UIStyleHelper.SetValidationError(txtRealName, false);

        // 获取选中的权限
        var selectedPermissions = new List<string>();
        foreach (var item in clbPermissions.CheckedItems)
        {
            selectedPermissions.Add(item?.ToString() ?? "");
        }

        try
        {
            if (_isEdit && _user != null)
            {
                // 检查用户名是否已存在（排除当前用户）
                if (_userService.IsUsernameExists(username, _user.UserID))
                {
                    UIStyleHelper.SetValidationError(txtUsername, true);
                    MessageBox.Show("该用户名已存在！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUsername.Focus();
                    return;
                }
                UIStyleHelper.SetValidationError(txtUsername, false);

                // 编辑用户
                _user.Username = username;
                _user.RealName = realName;
                _user.Workstation = txtWorkstation.Text.Trim();
                _user.Permissions = string.Join(",", selectedPermissions);
                _user.IsActive = chkIsActive.Checked;

                if (_userService.UpdateUser(_user))
                {
                    // 如果需要修改密码
                    if (!string.IsNullOrEmpty(password))
                    {
                        _userService.UpdatePassword(_user.UserID, password);
                    }

                    ShowSuccess("用户更新成功！");
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    ShowError("用户更新失败！");
                }
            }
            else
            {
                // 检查用户名是否已存在
                if (_userService.IsUsernameExists(username))
                {
                    UIStyleHelper.SetValidationError(txtUsername, true);
                    MessageBox.Show("该用户名已存在！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtUsername.Focus();
                    return;
                }
                UIStyleHelper.SetValidationError(txtUsername, false);

                // 新增用户
                var newUser = new User
                {
                    Username = username,
                    Password = password,
                    RealName = realName,
                    Workstation = txtWorkstation.Text.Trim(),
                    Permissions = string.Join(",", selectedPermissions),
                    IsActive = chkIsActive.Checked
                };

                var userId = _userService.CreateUser(newUser);
                if (userId > 0)
                {
                    ShowSuccess("用户创建成功！");
                    this.DialogResult = DialogResult.OK;
                }
                else
                {
                    ShowError("用户创建失败！");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"保存失败：{ex.Message}");
        }
    }
}
