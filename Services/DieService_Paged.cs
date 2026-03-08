using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

/// <summary>
/// 刀模服务 - 分页查询扩展
/// </summary>
public partial class DieService
{
    /// <summary>
    /// 分页搜索刀模
    /// </summary>
    public PagedResult<DieInfo> SearchDiesPaged(
        string? dieCode = null,
        string? customerName = null,
        DieStatus? status = null,
        AuditStatus? auditStatus = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int pageIndex = 1,
        int pageSize = 20)
    {
        var conditions = new List<string>();
        var parameters = new List<SqlParameter>();

        if (!string.IsNullOrWhiteSpace(dieCode))
        {
            conditions.Add("d.DieCode LIKE @DieCode");
            parameters.Add(new SqlParameter("@DieCode", $"%{dieCode}%"));
        }

        if (!string.IsNullOrWhiteSpace(customerName))
        {
            conditions.Add("d.CustomerName LIKE @CustomerName");
            parameters.Add(new SqlParameter("@CustomerName", $"%{customerName}%"));
        }

        if (status.HasValue)
        {
            conditions.Add("d.Status = @Status");
            parameters.Add(new SqlParameter("@Status", (int)status.Value));
        }

        if (auditStatus.HasValue)
        {
            conditions.Add("d.AuditStatus = @AuditStatus");
            parameters.Add(new SqlParameter("@AuditStatus", (int)auditStatus.Value));
        }

        if (startDate.HasValue)
        {
            conditions.Add("d.CreateTime >= @StartDate");
            parameters.Add(new SqlParameter("@StartDate", startDate.Value));
        }

        if (endDate.HasValue)
        {
            conditions.Add("d.CreateTime <= @EndDate");
            parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1)));
        }

        var baseSql = @"SELECT d.*, u.RealName as CreateUserName 
                          FROM DM_DieInfo d
                          LEFT JOIN DM_User u ON d.CreateUser = u.Username";

        if (conditions.Count > 0)
        {
            baseSql += " WHERE " + string.Join(" AND ", conditions);
        }

        return ExecutePagedQuery(baseSql, "d.CreateTime DESC", pageIndex, pageSize, 
            MapToDieInfo, parameters.ToArray());
    }
}
