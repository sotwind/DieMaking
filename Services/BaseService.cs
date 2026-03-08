using DieMaking.Helpers;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

/// <summary>
/// 服务基类 - 提供通用的数据操作方法
/// </summary>
public abstract class BaseService
{
    #region 通用查询方法

    /// <summary>
    /// 获取所有记录
    /// </summary>
    protected List<T> GetAll<T>(string tableName, string orderByColumn, Func<SqlDataReader, T> mapper)
    {
        return ExecuteQuerySafe(
            $"SELECT * FROM {tableName} ORDER BY {orderByColumn}",
            mapper,
            $"获取所有{tableName}记录");
    }

    /// <summary>
    /// 根据ID获取单条记录
    /// </summary>
    protected T? GetById<T>(string tableName, string idColumn, int id, Func<SqlDataReader, T> mapper)
    {
        var sql = $"SELECT * FROM {tableName} WHERE {idColumn} = @ID";
        var results = ExecuteQuerySafe(sql, mapper, $"获取{tableName}记录(ID:{id})", new SqlParameter("@ID", id));
        return results.FirstOrDefault();
    }

    /// <summary>
    /// 根据条件获取记录列表
    /// </summary>
    protected List<T> GetByCondition<T>(string tableName, string condition, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        var sql = $"SELECT * FROM {tableName} WHERE {condition}";
        return ExecuteQuerySafe(sql, mapper, $"获取{tableName}记录", parameters);
    }

    /// <summary>
    /// 搜索记录（支持模糊查询）
    /// </summary>
    protected List<T> Search<T>(string baseSql, List<string> conditions, List<SqlParameter> parameters, 
        Func<SqlDataReader, T> mapper, string orderBy = "CreateTime DESC")
    {
        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        var sql = $"{baseSql} {whereClause} ORDER BY {orderBy}";
        return ExecuteQuerySafe(sql, mapper, "搜索记录", parameters.ToArray());
    }

    /// <summary>
    /// 检查记录是否存在
    /// </summary>
    protected bool Exists(string tableName, string columnName, object value, int? excludeId = null, string idColumn = "ID")
    {
        try
        {
            var sql = excludeId.HasValue
                ? $"SELECT COUNT(*) FROM {tableName} WHERE {columnName} = @Value AND {idColumn} != @ExcludeID"
                : $"SELECT COUNT(*) FROM {tableName} WHERE {columnName} = @Value";

            var parameters = new List<SqlParameter> { new SqlParameter("@Value", value) };
            if (excludeId.HasValue)
                parameters.Add(new SqlParameter("@ExcludeID", excludeId.Value));

            var result = DbHelper.ExecuteScalar(sql, parameters.ToArray());
            return Convert.ToInt32(result) > 0;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"检查{tableName}.{columnName}是否存在");
            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"检查{tableName}.{columnName}是否存在");
            return false;
        }
    }

    /// <summary>
    /// 删除记录
    /// </summary>
    protected bool Delete(string tableName, string idColumn, int id, string entityName)
    {
        try
        {
            var sql = $"DELETE FROM {tableName} WHERE {idColumn} = @ID";
            return DbHelper.ExecuteNonQuery(sql, new SqlParameter("@ID", id)) > 0;
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            ExceptionHelper.HandleException(new BusinessException($"该{entityName}有关联数据，无法删除。"), $"删除{entityName}");
            return false;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"删除{entityName}(ID:{id})");
            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"删除{entityName}(ID:{id})");
            return false;
        }
    }

    /// <summary>
    /// 更新记录状态
    /// </summary>
    protected bool UpdateStatus(string tableName, string idColumn, int id, string statusColumn, int status)
    {
        try
        {
            var sql = $"UPDATE {tableName} SET {statusColumn} = @Status WHERE {idColumn} = @ID";
            return DbHelper.ExecuteNonQuery(sql,
                new SqlParameter("@Status", status),
                new SqlParameter("@ID", id)) > 0;
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, $"更新状态(ID:{id})");
            return false;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, $"更新状态(ID:{id})");
            return false;
        }
    }

    #endregion

    #region 安全执行方法

    /// <summary>
    /// 安全执行查询（自动处理异常）
    /// </summary>
    protected List<T> ExecuteQuerySafe<T>(string sql, Func<SqlDataReader, T> mapper, string operationName, params SqlParameter[] parameters)
    {
        try
        {
            return DbHelper.ExecuteQuery(sql, mapper, parameters);
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, operationName);
            return new List<T>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, operationName);
            return new List<T>();
        }
    }

    /// <summary>
    /// 安全执行非查询（自动处理异常）
    /// </summary>
    protected int ExecuteNonQuerySafe(string sql, string operationName, params SqlParameter[] parameters)
    {
        try
        {
            return DbHelper.ExecuteNonQuery(sql, parameters);
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, operationName);
            return 0;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, operationName);
            return 0;
        }
    }

    /// <summary>
    /// 安全执行标量查询（自动处理异常）
    /// </summary>
    protected object? ExecuteScalarSafe(string sql, string operationName, params SqlParameter[] parameters)
    {
        try
        {
            return DbHelper.ExecuteScalar(sql, parameters);
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, operationName);
            return null;
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, operationName);
            return null;
        }
    }

    #endregion

    #region 事务处理

    /// <summary>
    /// 执行事务操作
    /// </summary>
    protected bool ExecuteInTransaction(Func<SqlConnection, SqlTransaction, bool> action, string operationName)
    {
        using var connection = DbHelper.CreateConnection();
        SqlTransaction? transaction = null;

        try
        {
            connection.Open();
            transaction = connection.BeginTransaction();
            var result = action(connection, transaction);
            
            if (result)
            {
                transaction.Commit();
            }
            else
            {
                transaction.Rollback();
            }
            return result;
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            transaction?.Rollback();
            ExceptionHelper.HandleException(new BusinessException("记录已存在，请勿重复添加。"), operationName);
            return false;
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            transaction?.Rollback();
            ExceptionHelper.HandleException(new BusinessException("该记录有关联数据，无法执行此操作。"), operationName);
            return false;
        }
        catch (SqlException ex)
        {
            transaction?.Rollback();
            ExceptionHelper.HandleException(ex, operationName);
            return false;
        }
        catch (Exception ex)
        {
            transaction?.Rollback();
            ExceptionHelper.HandleException(ex, operationName);
            return false;
        }
    }

    /// <summary>
    /// 执行事务操作（带自定义错误处理）
    /// </summary>
    protected bool ExecuteInTransaction(Func<SqlConnection, SqlTransaction, bool> action, 
        Dictionary<int, string> sqlErrorMessages, string defaultOperationName)
    {
        using var connection = DbHelper.CreateConnection();
        SqlTransaction? transaction = null;

        try
        {
            connection.Open();
            transaction = connection.BeginTransaction();
            var result = action(connection, transaction);
            
            if (result)
            {
                transaction.Commit();
            }
            else
            {
                transaction.Rollback();
            }
            return result;
        }
        catch (SqlException ex) when (sqlErrorMessages.TryGetValue(ex.Number, out var message))
        {
            transaction?.Rollback();
            ExceptionHelper.HandleException(new BusinessException(message), defaultOperationName);
            return false;
        }
        catch (SqlException ex)
        {
            transaction?.Rollback();
            ExceptionHelper.HandleException(ex, defaultOperationName);
            return false;
        }
        catch (Exception ex)
        {
            transaction?.Rollback();
            ExceptionHelper.HandleException(ex, defaultOperationName);
            return false;
        }
    }

    #endregion

    #region 分页查询

    /// <summary>
    /// 执行分页查询
    /// </summary>
    protected PagedResult<T> ExecutePagedQuery<T>(string baseSql, string orderBy, int pageIndex, int pageSize,
        Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
    {
        try
        {
            return DbHelper.ExecutePagedQuery(baseSql, orderBy, pageIndex, pageSize, mapper, parameters);
        }
        catch (SqlException ex)
        {
            ExceptionHelper.HandleException(ex, "分页查询");
            return new PagedResult<T>();
        }
        catch (Exception ex)
        {
            ExceptionHelper.HandleException(ex, "分页查询");
            return new PagedResult<T>();
        }
    }

    #endregion
}

/// <summary>
/// 分页查询结果
/// </summary>
public class PagedResult<T>
{
    /// <summary>数据列表</summary>
    public List<T> Items { get; set; } = new();

    /// <summary>总记录数</summary>
    public int TotalCount { get; set; }

    /// <summary>当前页码</summary>
    public int PageIndex { get; set; }

    /// <summary>每页大小</summary>
    public int PageSize { get; set; }

    /// <summary>总页数</summary>
    public int TotalPages { get; set; }

    /// <summary>是否有上一页</summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>是否有下一页</summary>
    public bool HasNextPage => PageIndex < TotalPages;
}
