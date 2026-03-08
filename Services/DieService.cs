using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

public class DieService
{
    #region 刀模基本信息操作

    /// <summary>
    /// 获取所有刀模列表
    /// </summary>
    public List<DieInfo> GetAllDies()
    {
        var sql = @"SELECT d.*, u.RealName as CreateUserName 
                     FROM DM_DieInfo d
                     LEFT JOIN DM_User u ON d.CreateUser = u.Username
                     ORDER BY d.CreateTime DESC";
        return DbHelper.ExecuteQuery(sql, MapToDieInfo);
    }

    /// <summary>
    /// 根据ID获取刀模信息
    /// </summary>
    public DieInfo? GetDieById(int dieId)
    {
        var sql = @"SELECT d.*, u.RealName as CreateUserName 
                     FROM DM_DieInfo d
                     LEFT JOIN DM_User u ON d.CreateUser = u.Username
                     WHERE d.DieID = @DieID";
        var dies = DbHelper.ExecuteQuery(sql, MapToDieInfo, new SqlParameter("@DieID", dieId));
        return dies.FirstOrDefault();
    }

    /// <summary>
    /// 搜索刀模
    /// </summary>
    public List<DieInfo> SearchDies(string? dieCode = null, string? customerName = null, 
                                     DieStatus? status = null, AuditStatus? auditStatus = null,
                                     DateTime? startDate = null, DateTime? endDate = null)
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

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        var sql = $@"SELECT d.*, u.RealName as CreateUserName 
                      FROM DM_DieInfo d
                      LEFT JOIN DM_User u ON d.CreateUser = u.Username
                      {whereClause}
                      ORDER BY d.CreateTime DESC";

        return DbHelper.ExecuteQuery(sql, MapToDieInfo, parameters.ToArray());
    }

    /// <summary>
    /// 创建刀模
    /// </summary>
    public int CreateDie(DieInfo die, List<DieProcess> processes)
    {
        using var connection = DbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 插入刀模基本信息
            var sql = @"INSERT INTO DM_DieInfo 
                         (DieCode, CustomerName, ProductName, Structure, ModelType, LayoutType, 
                          FluteType, Material, ManufactureLength, ManufactureWidth, ManufactureHeight,
                          BlankLength, BlankWidth, ProcessDesc, RequiredProcesses, Status, AuditStatus,
                          SourceFactory, ExternalOrderID, DeliveryDate, CreateTime, CreateUser, Remark)
                         VALUES 
                         (@DieCode, @CustomerName, @ProductName, @Structure, @ModelType, @LayoutType,
                          @FluteType, @Material, @ManufactureLength, @ManufactureWidth, @ManufactureHeight,
                          @BlankLength, @BlankWidth, @ProcessDesc, @RequiredProcesses, @Status, @AuditStatus,
                          @SourceFactory, @ExternalOrderID, @DeliveryDate, GETDATE(), @CreateUser, @Remark);
                         SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var command = new SqlCommand(sql, connection, transaction);
            AddDieParameters(command, die);
            var dieId = Convert.ToInt32(command.ExecuteScalar());

            // 插入工序信息
            if (processes != null && processes.Count > 0)
            {
                foreach (var process in processes)
                {
                    process.DieID = dieId;
                    InsertDieProcess(connection, transaction, process);
                }
            }

            transaction.Commit();
            return dieId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 更新刀模
    /// </summary>
    public bool UpdateDie(DieInfo die, List<DieProcess> processes)
    {
        using var connection = DbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 更新刀模基本信息
            var sql = @"UPDATE DM_DieInfo SET
                         DieCode = @DieCode,
                         CustomerName = @CustomerName,
                         ProductName = @ProductName,
                         Structure = @Structure,
                         ModelType = @ModelType,
                         LayoutType = @LayoutType,
                         FluteType = @FluteType,
                         Material = @Material,
                         ManufactureLength = @ManufactureLength,
                         ManufactureWidth = @ManufactureWidth,
                         ManufactureHeight = @ManufactureHeight,
                         BlankLength = @BlankLength,
                         BlankWidth = @BlankWidth,
                         ProcessDesc = @ProcessDesc,
                         RequiredProcesses = @RequiredProcesses,
                         Status = @Status,
                         AuditStatus = @AuditStatus,
                         SourceFactory = @SourceFactory,
                         ExternalOrderID = @ExternalOrderID,
                         DeliveryDate = @DeliveryDate,
                         UpdateTime = GETDATE(),
                         Remark = @Remark
                         WHERE DieID = @DieID";

            using var command = new SqlCommand(sql, connection, transaction);
            AddDieParameters(command, die);
            command.Parameters.AddWithValue("@DieID", die.DieID);
            command.ExecuteNonQuery();

            // 删除原有工序
            var deleteSql = "DELETE FROM DM_DieProcess WHERE DieID = @DieID";
            using var deleteCmd = new SqlCommand(deleteSql, connection, transaction);
            deleteCmd.Parameters.AddWithValue("@DieID", die.DieID);
            deleteCmd.ExecuteNonQuery();

            // 插入新工序
            if (processes != null && processes.Count > 0)
            {
                foreach (var process in processes)
                {
                    process.DieID = die.DieID;
                    InsertDieProcess(connection, transaction, process);
                }
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 删除刀模
    /// </summary>
    public bool DeleteDie(int dieId)
    {
        using var connection = DbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 删除工序
            var deleteProcessSql = "DELETE FROM DM_DieProcess WHERE DieID = @DieID";
            using var deleteProcessCmd = new SqlCommand(deleteProcessSql, connection, transaction);
            deleteProcessCmd.Parameters.AddWithValue("@DieID", dieId);
            deleteProcessCmd.ExecuteNonQuery();

            // 删除刀模
            var deleteDieSql = "DELETE FROM DM_DieInfo WHERE DieID = @DieID";
            using var deleteDieCmd = new SqlCommand(deleteDieSql, connection, transaction);
            deleteDieCmd.Parameters.AddWithValue("@DieID", dieId);
            var result = deleteDieCmd.ExecuteNonQuery() > 0;

            transaction.Commit();
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 审核刀模
    /// </summary>
    public bool AuditDie(int dieId, bool isApproved)
    {
        var sql = "UPDATE DM_DieInfo SET AuditStatus = @AuditStatus WHERE DieID = @DieID";
        return DbHelper.ExecuteNonQuery(sql,
            new SqlParameter("@AuditStatus", isApproved ? (int)AuditStatus.Audited : (int)AuditStatus.Unaudited),
            new SqlParameter("@DieID", dieId)) > 0;
    }

    /// <summary>
    /// 检查刀模编号是否已存在
    /// </summary>
    public bool IsDieCodeExists(string dieCode, int? excludeDieId = null)
    {
        var sql = excludeDieId.HasValue
            ? "SELECT COUNT(*) FROM DM_DieInfo WHERE DieCode = @DieCode AND DieID != @DieID"
            : "SELECT COUNT(*) FROM DM_DieInfo WHERE DieCode = @DieCode";

        var parameters = new List<SqlParameter> { new SqlParameter("@DieCode", dieCode) };
        if (excludeDieId.HasValue)
            parameters.Add(new SqlParameter("@DieID", excludeDieId.Value));

        var result = DbHelper.ExecuteScalar(sql, parameters.ToArray());
        return Convert.ToInt32(result) > 0;
    }

    #endregion

    #region 刀模工序操作

    /// <summary>
    /// 获取刀模的工序列表
    /// </summary>
    public List<DieProcess> GetDieProcesses(int dieId)
    {
        var sql = @"SELECT p.*, u.RealName as OperatorNameReal
                     FROM DM_DieProcess p
                     LEFT JOIN DM_User u ON p.OperatorNo = u.Username
                     WHERE p.DieID = @DieID
                     ORDER BY p.ProcessID";
        return DbHelper.ExecuteQuery(sql, MapToDieProcess, new SqlParameter("@DieID", dieId));
    }

    /// <summary>
    /// 更新工序状态
    /// </summary>
    public bool UpdateProcessStatus(int processId, ProcessStatus status, string? operatorNo = null, string? operatorName = null)
    {
        var sql = @"UPDATE DM_DieProcess SET
                     Status = @Status,
                     OperatorNo = @OperatorNo,
                     OperatorName = @OperatorName,
                     StartTime = CASE WHEN @Status = 1 THEN ISNULL(StartTime, GETDATE()) ELSE StartTime END,
                     CompleteTime = CASE WHEN @Status = 2 THEN GETDATE() ELSE NULL END
                     WHERE ProcessID = @ProcessID";

        return DbHelper.ExecuteNonQuery(sql,
            new SqlParameter("@Status", (int)status),
            new SqlParameter("@OperatorNo", operatorNo ?? (object)DBNull.Value),
            new SqlParameter("@OperatorName", operatorName ?? (object)DBNull.Value),
            new SqlParameter("@ProcessID", processId)) > 0;
    }

    #endregion

    #region 私有方法

    private void InsertDieProcess(SqlConnection connection, SqlTransaction transaction, DieProcess process)
    {
        var sql = @"INSERT INTO DM_DieProcess
                     (DieID, ProcessName, Status, OperatorNo, OperatorName, 
                      BoardLength, BoardWidth, KnifeLength, KnifeTraceLength,
                      Formula, Amount, PrevProcessID, IsPrevCompleted, CreateTime)
                     VALUES
                     (@DieID, @ProcessName, @Status, @OperatorNo, @OperatorName,
                      @BoardLength, @BoardWidth, @KnifeLength, @KnifeTraceLength,
                      @Formula, @Amount, @PrevProcessID, @IsPrevCompleted, GETDATE())";

        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@DieID", process.DieID);
        command.Parameters.AddWithValue("@ProcessName", process.ProcessName);
        command.Parameters.AddWithValue("@Status", (int)process.Status);
        command.Parameters.AddWithValue("@OperatorNo", string.IsNullOrEmpty(process.OperatorNo) ? (object)DBNull.Value : process.OperatorNo);
        command.Parameters.AddWithValue("@OperatorName", string.IsNullOrEmpty(process.OperatorName) ? (object)DBNull.Value : process.OperatorName);
        command.Parameters.AddWithValue("@BoardLength", process.BoardLength ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@BoardWidth", process.BoardWidth ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@KnifeLength", process.KnifeLength ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@KnifeTraceLength", process.KnifeTraceLength ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Formula", string.IsNullOrEmpty(process.Formula) ? (object)DBNull.Value : process.Formula);
        command.Parameters.AddWithValue("@Amount", process.Amount ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PrevProcessID", process.PrevProcessID ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@IsPrevCompleted", process.IsPrevCompleted);
        command.ExecuteNonQuery();
    }

    private void AddDieParameters(SqlCommand command, DieInfo die)
    {
        command.Parameters.AddWithValue("@DieCode", die.DieCode);
        command.Parameters.AddWithValue("@CustomerName", die.CustomerName);
        command.Parameters.AddWithValue("@ProductName", die.ProductName);
        command.Parameters.AddWithValue("@Structure", die.Structure);
        command.Parameters.AddWithValue("@ModelType", die.ModelType);
        command.Parameters.AddWithValue("@LayoutType", die.LayoutType);
        command.Parameters.AddWithValue("@FluteType", die.FluteType);
        command.Parameters.AddWithValue("@Material", die.Material);
        command.Parameters.AddWithValue("@ManufactureLength", die.ManufactureLength);
        command.Parameters.AddWithValue("@ManufactureWidth", die.ManufactureWidth);
        command.Parameters.AddWithValue("@ManufactureHeight", die.ManufactureHeight);
        command.Parameters.AddWithValue("@BlankLength", die.BlankLength);
        command.Parameters.AddWithValue("@BlankWidth", die.BlankWidth);
        command.Parameters.AddWithValue("@ProcessDesc", die.ProcessDesc);
        command.Parameters.AddWithValue("@RequiredProcesses", die.RequiredProcesses);
        command.Parameters.AddWithValue("@Status", (int)die.Status);
        command.Parameters.AddWithValue("@AuditStatus", (int)die.AuditStatus);
        command.Parameters.AddWithValue("@SourceFactory", die.SourceFactory);
        command.Parameters.AddWithValue("@ExternalOrderID", die.ExternalOrderID ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@DeliveryDate", die.DeliveryDate ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CreateUser", die.CreateUser);
        command.Parameters.AddWithValue("@Remark", die.Remark);
    }

    private DieInfo MapToDieInfo(SqlDataReader reader)
    {
        return new DieInfo
        {
            DieID = Convert.ToInt32(reader["DieID"]),
            DieCode = reader["DieCode"].ToString() ?? "",
            CustomerName = reader["CustomerName"].ToString() ?? "",
            ProductName = reader["ProductName"].ToString() ?? "",
            Structure = reader["Structure"].ToString() ?? "",
            ModelType = reader["ModelType"].ToString() ?? "",
            LayoutType = reader["LayoutType"].ToString() ?? "",
            FluteType = reader["FluteType"].ToString() ?? "",
            Material = reader["Material"].ToString() ?? "",
            ManufactureLength = reader["ManufactureLength"] != DBNull.Value ? Convert.ToDecimal(reader["ManufactureLength"]) : 0,
            ManufactureWidth = reader["ManufactureWidth"] != DBNull.Value ? Convert.ToDecimal(reader["ManufactureWidth"]) : 0,
            ManufactureHeight = reader["ManufactureHeight"] != DBNull.Value ? Convert.ToDecimal(reader["ManufactureHeight"]) : 0,
            BlankLength = reader["BlankLength"] != DBNull.Value ? Convert.ToDecimal(reader["BlankLength"]) : 0,
            BlankWidth = reader["BlankWidth"] != DBNull.Value ? Convert.ToDecimal(reader["BlankWidth"]) : 0,
            ProcessDesc = reader["ProcessDesc"].ToString() ?? "",
            RequiredProcesses = reader["RequiredProcesses"].ToString() ?? "",
            Status = reader["Status"] != DBNull.Value ? (DieStatus)Convert.ToInt32(reader["Status"]) : DieStatus.Pending,
            AuditStatus = reader["AuditStatus"] != DBNull.Value ? (AuditStatus)Convert.ToInt32(reader["AuditStatus"]) : AuditStatus.Unaudited,
            SourceFactory = reader["SourceFactory"].ToString() ?? "",
            ExternalOrderID = reader["ExternalOrderID"] != DBNull.Value ? Convert.ToInt32(reader["ExternalOrderID"]) : null,
            DeliveryDate = reader["DeliveryDate"] != DBNull.Value ? Convert.ToDateTime(reader["DeliveryDate"]) : null,
            CreateTime = reader["CreateTime"] != DBNull.Value ? Convert.ToDateTime(reader["CreateTime"]) : DateTime.Now,
            CreateUser = reader["CreateUser"].ToString() ?? "",
            UpdateTime = reader["UpdateTime"] != DBNull.Value ? Convert.ToDateTime(reader["UpdateTime"]) : null,
            Remark = reader["Remark"].ToString() ?? ""
        };
    }

    private DieProcess MapToDieProcess(SqlDataReader reader)
    {
        return new DieProcess
        {
            ProcessID = Convert.ToInt32(reader["ProcessID"]),
            DieID = Convert.ToInt32(reader["DieID"]),
            ProcessName = reader["ProcessName"].ToString() ?? "",
            Status = reader["Status"] != DBNull.Value ? (ProcessStatus)Convert.ToInt32(reader["Status"]) : ProcessStatus.Pending,
            StartTime = reader["StartTime"] != DBNull.Value ? Convert.ToDateTime(reader["StartTime"]) : null,
            CompleteTime = reader["CompleteTime"] != DBNull.Value ? Convert.ToDateTime(reader["CompleteTime"]) : null,
            OperatorNo = reader["OperatorNo"].ToString() ?? "",
            OperatorName = reader["OperatorName"].ToString() ?? "",
            BoardLength = reader["BoardLength"] != DBNull.Value ? Convert.ToInt32(reader["BoardLength"]) : null,
            BoardWidth = reader["BoardWidth"] != DBNull.Value ? Convert.ToInt32(reader["BoardWidth"]) : null,
            KnifeLength = reader["KnifeLength"] != DBNull.Value ? Convert.ToInt32(reader["KnifeLength"]) : null,
            KnifeTraceLength = reader["KnifeTraceLength"] != DBNull.Value ? Convert.ToInt32(reader["KnifeTraceLength"]) : null,
            Formula = reader["Formula"]?.ToString(),
            Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : null,
            PrevProcessID = reader["PrevProcessID"] != DBNull.Value ? Convert.ToInt32(reader["PrevProcessID"]) : null,
            IsPrevCompleted = reader["IsPrevCompleted"] != DBNull.Value && Convert.ToBoolean(reader["IsPrevCompleted"]),
            CreateTime = reader["CreateTime"] != DBNull.Value ? Convert.ToDateTime(reader["CreateTime"]) : DateTime.Now
        };
    }

    #endregion
}
