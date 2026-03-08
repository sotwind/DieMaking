using DieMaking.Models;

namespace DieMaking.Forms;

public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        this.Text = $"刀模管理系统 - 当前用户：{CurrentUser.User?.RealName ?? CurrentUser.User?.Username}";
    }

    private void InitializeComponent()
    {
        this.Text = "刀模管理系统";
        this.Size = new Size(1200, 800);
        this.StartPosition = FormStartPosition.CenterScreen;

        // 创建菜单栏
        var menuStrip = new MenuStrip();

        // 刀模管理菜单
        var dieMenu = new ToolStripMenuItem("刀模管理");
        if (CurrentUser.HasPermission(PermissionKeys.DieManage))
        {
            dieMenu.DropDownItems.Add("刀模列表", null, (s, e) => ShowForm<Die.DieListForm>());
        }
        if (CurrentUser.HasPermission(PermissionKeys.DieAdd))
        {
            dieMenu.DropDownItems.Add("添加刀模", null, (s, e) => ShowForm<Die.DieAddForm>());
        }
        menuStrip.Items.Add(dieMenu);

        // 生产管理菜单
        var productionMenu = new ToolStripMenuItem("生产管理");
        if (CurrentUser.HasPermission(PermissionKeys.Production))
        {
            productionMenu.DropDownItems.Add("生产看板", null, (s, e) => ShowForm<Production.ProductionBoardForm>());
            productionMenu.DropDownItems.Add("完工查询", null, (s, e) => ShowForm<Production.CompletionQueryForm>());
        }
        menuStrip.Items.Add(productionMenu);

        // 仓库管理菜单
        var warehouseMenu = new ToolStripMenuItem("仓库管理");
        if (CurrentUser.HasPermission(PermissionKeys.WarehouseManage))
        {
            if (CurrentUser.HasPermission(PermissionKeys.LocationManage))
                warehouseMenu.DropDownItems.Add("库位管理", null, (s, e) => ShowForm<Warehouse.LocationManageForm>());
            if (CurrentUser.HasPermission(PermissionKeys.DieBorrow))
                warehouseMenu.DropDownItems.Add("刀模领用", null, (s, e) => ShowForm<Warehouse.DieBorrowForm>());
            if (CurrentUser.HasPermission(PermissionKeys.DieReturn))
                warehouseMenu.DropDownItems.Add("刀模归还", null, (s, e) => ShowForm<Warehouse.DieReturnForm>());
            if (CurrentUser.HasPermission(PermissionKeys.BorrowRecord))
                warehouseMenu.DropDownItems.Add("借用记录", null, (s, e) => ShowForm<Warehouse.BorrowRecordForm>());
            if (CurrentUser.HasPermission(PermissionKeys.ScrapApply))
                warehouseMenu.DropDownItems.Add("报废申请", null, (s, e) => ShowForm<Warehouse.ScrapApplyForm>());
        }
        menuStrip.Items.Add(warehouseMenu);

        // 报表统计菜单
        var reportMenu = new ToolStripMenuItem("报表统计");
        if (CurrentUser.HasPermission(PermissionKeys.Report))
        {
            reportMenu.DropDownItems.Add("完工统计", null, (s, e) => ShowForm<Report.CompletionStatsForm>());
            reportMenu.DropDownItems.Add("工序统计", null, (s, e) => ShowForm<Report.ProcessStatsForm>());
            reportMenu.DropDownItems.Add("库存统计", null, (s, e) => ShowForm<Report.InventoryStatsForm>());
        }
        menuStrip.Items.Add(reportMenu);

        // 系统管理菜单
        var systemMenu = new ToolStripMenuItem("系统管理");
        if (CurrentUser.HasPermission(PermissionKeys.UserManage))
        {
            systemMenu.DropDownItems.Add("用户管理", null, (s, e) => ShowForm<System.UserManageForm>());
        }
        systemMenu.DropDownItems.Add("-");
        systemMenu.DropDownItems.Add("退出登录", null, (s, e) => Logout());
        menuStrip.Items.Add(systemMenu);

        this.MainMenuStrip = menuStrip;
        this.Controls.Add(menuStrip);

        // 状态栏
        var statusStrip = new StatusStrip();
        var statusLabel = new ToolStripStatusLabel($"当前用户：{CurrentUser.User?.RealName ?? CurrentUser.User?.Username} | 登录时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        statusStrip.Items.Add(statusLabel);
        this.Controls.Add(statusStrip);
    }

    private void ShowForm<T>() where T : Form, new()
    {
        // 检查是否已存在该类型的窗体
        foreach (Form form in this.MdiChildren)
        {
            if (form is T)
            {
                form.Activate();
                return;
            }
        }

        // 创建新窗体
        var newForm = new T
        {
            MdiParent = this,
            WindowState = FormWindowState.Maximized
        };
        newForm.Show();
    }

    private void Logout()
    {
        if (MessageBox.Show("确定要退出登录吗？", "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            CurrentUser.User = null;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
