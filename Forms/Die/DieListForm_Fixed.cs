// DieListForm.cs 修改后的 LoadData 方法
// 将内存分页改为数据库分页

/*
原代码（内存分页 - 问题）：
----------------------------------------
protected override void LoadData()
{
    ...
    _dieList = _dieService.SearchDies(dieCode, customerName, status, auditStatus, startDate, endDate);
    _totalCount = _dieList.Count;
    var pageData = _dieList.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
    ...
}

修改为（数据库分页）：
----------------------------------------
*/

using DieMaking.Services;
using DieMaking.Models;

namespace DieMaking.Forms.Die;

public partial class DieListForm
{
    // 移除：private List<DieInfo> _dieList = new();  // 不再需要全量存储
    
    /// <summary>
    /// 修改后的 LoadData 方法 - 使用数据库分页
    /// </summary>
    protected override void LoadData()
    {
        LoadingForm? loadingForm = null;
        try
        {
            loadingForm = UIStyleHelper.ShowLoading(this, "正在加载数据...");

            // 获取搜索条件
            string? dieCode = string.IsNullOrWhiteSpace(txtDieCode.Text) || txtDieCode.Text == (string?)txtDieCode.Tag
                ? null : txtDieCode.Text.Trim();
            string? customerName = string.IsNullOrWhiteSpace(txtCustomer.Text) || txtCustomer.Text == (string?)txtCustomer.Tag
                ? null : txtCustomer.Text.Trim();
            DieStatus? status = cmbStatus.SelectedIndex > 0 ? (DieStatus?)(cmbStatus.SelectedIndex - 1) : null;
            AuditStatus? auditStatus = cmbAuditStatus.SelectedIndex > 0 ? (AuditStatus?)(cmbAuditStatus.SelectedIndex - 1) : null;
            DateTime? startDate = dtpDateFrom.Checked ? dtpDateFrom.Value : null;
            DateTime? endDate = dtpDateTo.Checked ? dtpDateTo.Value : null;

            // 使用数据库分页查询（关键修改）
            var pagedResult = _dieService.SearchDiesPaged(
                dieCode, customerName, status, auditStatus, startDate, endDate,
                _currentPage, _pageSize);

            // 绑定当前页数据
            dgvDieList.DataSource = null;
            dgvDieList.DataSource = pagedResult.Items;
            
            // 更新分页信息
            _totalCount = pagedResult.TotalCount;
            int totalPages = pagedResult.TotalPages;
            if (totalPages == 0) totalPages = 1;
            
            lblPageInfo.Text = $"第 {_currentPage} 页 / 共 {totalPages} 页 (共 {_totalCount} 条)";

            // 更新按钮状态
            btnFirst.Enabled = _currentPage > 1;
            btnPrev.Enabled = _currentPage > 1;
            btnNext.Enabled = _currentPage < totalPages;
            btnLast.Enabled = _currentPage < totalPages;

            // 更新状态栏
            if (StatusUserLabel != null)
            {
                StatusUserLabel.Text = $"共 {_totalCount} 条记录";
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载数据失败：{ex.Message}");
        }
        finally
        {
            loadingForm?.Close();
        }
    }

    /// <summary>
    /// 修改后的 GetSelectedDie 方法
    /// </summary>
    private DieInfo? GetSelectedDie()
    {
        if (dgvDieList.SelectedRows.Count == 0)
        {
            MessageBox.Show("请先选择一条记录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var dieId = Convert.ToInt32(dgvDieList.SelectedRows[0].Cells["DieID"].Value);
        // 改为实时查询，而不是从内存列表中查找
        return _dieService.GetDieById(dieId);
    }
}
