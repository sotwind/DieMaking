using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

public class DieService : BaseService
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
        return ExecuteQuerySafe(sql, MapToDieInfo, "获取所有刀模列表");
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
        var dies = ExecuteQuerySafe(sql, MapToDieInfo, $"获取刀模信息(ID:{dieId})", new SqlParameter("@DieID", dieId));
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

        var baseSql = @"SELECT d.*, u.RealName as CreateUserName 
                          FROM DM_DieInfo d
                          LEFT JOIN DM_User u ON d.CreateUser = u.Username";
        
        return Search(baseSql, conditions, parameters, MapToDieInfo);
    }

    /// <summary>
    /// 创建刀模
    /// </summary>
    public int CreateDie(DieInfo die, List<DieProcess> processes)
    {
        var errorMessages = new Dictionary<int, string>
        {
            { 2627, "刀模编号已存在，请使用其他编号。" },
            { 2601, "刀模编号已存在，请使用其他编号。" }
        };

        int resultId = 0;
        bool success = ExecuteInTransaction((connection, transaction) =>
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
            resultId = dieId;

            // 插入工序信息
            if (processes?.Count > 0)
            {
                foreach (var process in processes)
                {
                    process.DieID = dieId;
                    InsertDieProcess(connection, transaction, process);
                }
            }

            return true;
        }, errorMessages, "创建刀模");

        return success ? resultId : 0;
    }

    /// <summary>
    /// 更新刀模
    /// </summary>
    public bool UpdateDie(DieInfo die, List<DieProcess> processes)
    {
        var errorMessages = new Dictionary<int, string>
        {
            { 2627, "刀模编号已存在，请使用其他编号。" },
            { 2601, "刀模编号已存在，请使用其他编号。" }
        };

        return ExecuteInTransaction((connection, transaction) =>
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
            if (processes?.Count > 0)
            {
                foreach (var process in processes)
                {
                    process.DieID = die.DieID;
                    InsertDieProcess(connection, transaction, process);
                }
            }

            return true;
        }, errorMessages, $"更新刀模(ID:{die.DieID})");
    }

    /// <summary>
    /// 删除刀模
    /// </summary>
    public bool DeleteDie(int dieId)
    {
        var errorMessages = new Dictionary<int, string>
        {
            { 547, "该刀模有关联数据（如库存、借用记录等），无法删除。" }
        };

        return ExecuteInTransaction((connection, transaction) =>
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
            return deleteDieCmd.ExecuteNonQuery() > 0;
        }, errorMessages, $"删除刀模(ID:{dieId})");
    }

    /// <summary>
    /// 审核刀模
    /// </summary>
    public bool AuditDie(int dieId, bool isApproved)
    {
        var sql = "UPDATE DM_DieInfo SET AuditStatus = @AuditStatus WHERE DieID = @DieID";
        return ExecuteNonQuerySafe(sql, $"审核刀模(ID:{dieId})",
            new SqlParameter("@AuditStatus", isApproved ? (int)AuditStatus.Audited : (int)AuditStatus.Unaudited),
            new SqlParameter("@DieID", dieId)) > 0;
    }

    /// <summary>
    /// 检查刀模编号是否已存在
    /// </summary>
    public bool IsDieCodeExists(string dieCode, int? excludeDieId = null)
    {
        return Exists("DM_DieInfo", "DieCode", dieCode, excludeDieId, "DieID");
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
        return ExecuteQuerySafe(sql, MapToDieProcess, $"获取刀模工序列表(DieID:{dieId})", new SqlParameter("@DieID", dieId));
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

        return ExecuteNonQuerySafe(sql, $"更新工序状态(ProcessID:{processId})",
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
            DieID = ConvertHelper.ToInt(reader["DieID"]),
            DieCode = ConvertHelper.ToString(reader["DieCode"]),
            CustomerName = ConvertHelper.ToString(reader["CustomerName"]),
            ProductName = ConvertHelper.ToString(reader["ProductName"]),
            Structure = ConvertHelper.ToString(reader["Structure"]),
            ModelType = ConvertHelper.ToString(reader["ModelType"]),
            LayoutType = ConvertHelper.ToString(reader["LayoutType"]),
            FluteType = ConvertHelper.ToString(reader["FluteType"]),
            Material = ConvertHelper.ToString(reader["Material"]),
            ManufactureLength = ConvertHelper.ToDecimal(reader["ManufactureLength"]),
            ManufactureWidth = ConvertHelper.ToDecimal(reader["ManufactureWidth"]),
            ManufactureHeight = ConvertHelper.ToDecimal(reader["ManufactureHeight"]),
            BlankLength = ConvertHelper.ToDecimal(reader["BlankLength"]),
            BlankWidth = ConvertHelper.ToDecimal(reader["BlankWidth"]),
            ProcessDesc = ConvertHelper.ToString(reader["ProcessDesc"]),
            RequiredProcesses = ConvertHelper.ToString(reader["RequiredProcesses"]),
            Status = ConvertHelper.ToEnum(reader["Status"], DieStatus.Pending),
            AuditStatus = ConvertHelper.ToEnum(reader["AuditStatus"], AuditStatus.Unaudited),
            SourceFactory = ConvertHelper.ToString(reader["SourceFactory"]),
            ExternalOrderID = ConvertHelper.ToNullableInt(reader["ExternalOrderID"]),
            DeliveryDate = ConvertHelper.ToNullableDateTime(reader["DeliveryDate"]),
            CreateTime = ConvertHelper.ToDateTime(reader["CreateTime"], DateTime.Now),
            CreateUser = ConvertHelper.ToString(reader["CreateUser"]),
            UpdateTime = ConvertHelper.ToNullableDateTime(reader["UpdateTime"]),
            Remark = ConvertHelper.ToString(reader["Remark"])
        };
    }

    private DieProcess MapToDieProcess(SqlDataReader reader)
    {
        return new DieProcess
        {
            ProcessID = ConvertHelper.ToInt(reader["ProcessID"]),
            DieID = ConvertHelper.ToInt(reader["DieID"]),
            ProcessName = ConvertHelper.ToString(reader["ProcessName"]),
            Status = ConvertHelper.ToEnum(reader["Status"], ProcessStatus.Pending),
            StartTime = ConvertHelper.ToNullableDateTime(reader["StartTime"]),
            CompleteTime = ConvertHelper.ToNullableDateTime(reader["CompleteTime"]),
            OperatorNo = ConvertHelper.ToString(reader["OperatorNo"]),
            OperatorName = ConvertHelper.ToString(reader["OperatorName"]),
            BoardLength = ConvertHelper.ToNullableInt(reader["BoardLength"]),
            BoardWidth = ConvertHelper.ToNullableInt(reader["BoardWidth"]),
            KnifeLength = ConvertHelper.ToNullableInt(reader["KnifeLength"]),
            KnifeTraceLength = ConvertHelper.ToNullableInt(reader["KnifeTraceLength"]),
            Formula = ConvertHelper.ToString(reader["Formula"]),
            Amount = ConvertHelper.ToNullableDecimal(reader["Amount"]),
            PrevProcessID = ConvertHelper.ToNullableInt(reader["PrevProcessID"]),
            IsPrevCompleted = ConvertHelper.ToBool(reader["IsPrevCompleted"]),
            CreateTime = ConvertHelper.ToDateTime(reader["CreateTime"], DateTime.Now)
        };
    }

    #endregion

    #region 入库相关

    /// <summary>
    /// 获取已完工但未入库的刀模列表
    /// </summary>
    public List<DieInfo> GetCompletedDiesNotInStock()
    {
        var sql = @"SELECT d.*, u.RealName as CreateUserName 
                     FROM DM_DieInfo d
                     LEFT JOIN DM_User u ON d.CreateUser = u.Username
                     WHERE d.Status = @Status
                     AND NOT EXISTS (
                         SELECT 1 FROM DM_DieInventory i 
                         WHERE i.DieID = d.DieID AND i.Status != @DeletedStatus
                     )
                     ORDER BY d.CompleteTime DESC";
        
        var parameters = new[]
        {
            new SqlParameter("@Status", (int)DieStatus.Completed),
            new SqlParameter("@DeletedStatus", (int)InventoryStatus.Scrap)
        };
        
        return ExecuteQuerySafe(sql, MapToDieInfo, "获取已完工未入库刀模", parameters);
    }

    #endregion
}
