using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

public class WarehouseService : BaseService
{
    #region 库位管理

    /// <summary>
    /// 获取所有库位
    /// </summary>
    public List<StorageLocation> GetAllLocations()
    {
        var sql = @"SELECT LocationID, LocationCode, Area, ShelfNo, LayerNo, PositionNo, 
                     Description, Status, CreateTime 
                     FROM DM_StorageLocation 
                     ORDER BY Area, ShelfNo, LayerNo, PositionNo";
        return ExecuteQuerySafe(sql, MapToStorageLocation, "获取所有库位");
    }

    /// <summary>
    /// 根据状态获取库位
    /// </summary>
    public List<StorageLocation> GetLocationsByStatus(LocationStatus status)
    {
        var sql = @"SELECT LocationID, LocationCode, Area, ShelfNo, LayerNo, PositionNo, 
                     Description, Status, CreateTime 
                     FROM DM_StorageLocation 
                     WHERE Status = @Status
                     ORDER BY Area, ShelfNo, LayerNo, PositionNo";
        return ExecuteQuerySafe(sql, MapToStorageLocation, $"获取库位(Status:{status})", 
            new SqlParameter("@Status", (int)status));
    }

    /// <summary>
    /// 搜索库位
    /// </summary>
    public List<StorageLocation> SearchLocations(string keyword)
    {
        var sql = @"SELECT LocationID, LocationCode, Area, ShelfNo, LayerNo, PositionNo, 
                     Description, Status, CreateTime 
                     FROM DM_StorageLocation 
                     WHERE LocationCode LIKE @Keyword OR Area LIKE @Keyword OR Description LIKE @Keyword
                     ORDER BY Area, ShelfNo, LayerNo, PositionNo";
        return ExecuteQuerySafe(sql, MapToStorageLocation, "搜索库位", 
            new SqlParameter("@Keyword", $"%{keyword}%"));
    }

    /// <summary>
    /// 根据ID获取库位
    /// </summary>
    public StorageLocation? GetLocationById(int locationId)
    {
        return GetById("DM_StorageLocation", "LocationID", locationId, MapToStorageLocation);
    }

    /// <summary>
    /// 创建库位
    /// </summary>
    public int CreateLocation(StorageLocation location)
    {
        try
        {
            var sql = @"INSERT INTO DM_StorageLocation (LocationCode, Area, ShelfNo, LayerNo, PositionNo, Description, Status, CreateTime) 
                         VALUES (@LocationCode, @Area, @ShelfNo, @LayerNo, @PositionNo, @Description, @Status, GETDATE());
                         SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var result = ExecuteScalarSafe(sql, "创建库位",
                new SqlParameter("@LocationCode", location.LocationCode),
                new SqlParameter("@Area", location.Area),
                new SqlParameter("@ShelfNo", location.ShelfNo),
                new SqlParameter("@LayerNo", location.LayerNo),
                new SqlParameter("@PositionNo", location.PositionNo),
                new SqlParameter("@Description", location.Description),
                new SqlParameter("@Status", (int)location.Status));

            return result == DBNull.Value ? 0 : Convert.ToInt32(result);
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            ExceptionHelper.HandleException(new BusinessException("库位编号已存在，请使用其他编号。"), "创建库位");
            return 0;
        }
    }

    /// <summary>
    /// 更新库位
    /// </summary>
    public bool UpdateLocation(StorageLocation location)
    {
        try
        {
            var sql = @"UPDATE DM_StorageLocation SET 
                         LocationCode = @LocationCode,
                         Area = @Area,
                         ShelfNo = @ShelfNo,
                         LayerNo = @LayerNo,
                         PositionNo = @PositionNo,
                         Description = @Description,
                         Status = @Status
                         WHERE LocationID = @LocationID";

            return ExecuteNonQuerySafe(sql, $"更新库位(ID:{location.LocationID})",
                new SqlParameter("@LocationID", location.LocationID),
                new SqlParameter("@LocationCode", location.LocationCode),
                new SqlParameter("@Area", location.Area),
                new SqlParameter("@ShelfNo", location.ShelfNo),
                new SqlParameter("@LayerNo", location.LayerNo),
                new SqlParameter("@PositionNo", location.PositionNo),
                new SqlParameter("@Description", location.Description),
                new SqlParameter("@Status", (int)location.Status)) > 0;
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            ExceptionHelper.HandleException(new BusinessException("库位编号已存在，请使用其他编号。"), "更新库位");
            return false;
        }
    }

    /// <summary>
    /// 删除库位
    /// </summary>
    public bool DeleteLocation(int locationId)
    {
        var errorMessages = new Dictionary<int, string>
        {
            { 547, "该库位有关联的刀模库存，无法删除。" }
        };

        return ExecuteInTransaction((connection, transaction) =>
        {
            var sql = "DELETE FROM DM_StorageLocation WHERE LocationID = @LocationID";
            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@LocationID", locationId);
            return command.ExecuteNonQuery() > 0;
        }, errorMessages, $"删除库位(ID:{locationId})");
    }

    /// <summary>
    /// 检查库位编号是否已存在
    /// </summary>
    public bool IsLocationCodeExists(string locationCode, int? excludeLocationId = null)
    {
        return Exists("DM_StorageLocation", "LocationCode", locationCode, excludeLocationId, "LocationID");
    }

    /// <summary>
    /// 将数据读取器映射为库位对象
    /// </summary>
    private StorageLocation MapToStorageLocation(SqlDataReader reader)
    {
        return new StorageLocation
        {
            LocationID = ConvertHelper.ToInt(reader["LocationID"]),
            LocationCode = ConvertHelper.ToString(reader["LocationCode"]),
            Area = ConvertHelper.ToString(reader["Area"]),
            ShelfNo = ConvertHelper.ToString(reader["ShelfNo"]),
            LayerNo = ConvertHelper.ToString(reader["LayerNo"]),
            PositionNo = ConvertHelper.ToString(reader["PositionNo"]),
            Description = ConvertHelper.ToString(reader["Description"]),
            Status = ConvertHelper.ToEnum(reader["Status"], LocationStatus.Free),
            CreateTime = ConvertHelper.ToDateTime(reader["CreateTime"], DateTime.Now)
        };
    }

    #endregion

    #region 刀模库存管理

    /// <summary>
    /// 获取所有库存
    /// </summary>
    public List<DieInventory> GetAllInventory()
    {
        var sql = @"SELECT i.InventoryID, i.DieID, i.LocationID, i.StorageStatus, i.InStockTime, 
                     i.LastBorrowTime, i.LastReturnTime, i.TotalBorrowCount, i.Remark, i.UpdateTime,
                     l.LocationCode, d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieInventory i
                     LEFT JOIN DM_StorageLocation l ON i.LocationID = l.LocationID
                     LEFT JOIN DM_DieInfo d ON i.DieID = d.DieID
                     ORDER BY i.UpdateTime DESC";
        return ExecuteQuerySafe(sql, MapToDieInventory, "获取所有库存");
    }

    /// <summary>
    /// 根据状态获取库存
    /// </summary>
    public List<DieInventory> GetInventoryByStatus(StorageStatus status)
    {
        var sql = @"SELECT i.InventoryID, i.DieID, i.LocationID, i.StorageStatus, i.InStockTime, 
                     i.LastBorrowTime, i.LastReturnTime, i.TotalBorrowCount, i.Remark, i.UpdateTime,
                     l.LocationCode, d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieInventory i
                     LEFT JOIN DM_StorageLocation l ON i.LocationID = l.LocationID
                     LEFT JOIN DM_DieInfo d ON i.DieID = d.DieID
                     WHERE i.StorageStatus = @Status
                     ORDER BY i.UpdateTime DESC";
        return ExecuteQuerySafe(sql, MapToDieInventory, $"获取库存(Status:{status})", 
            new SqlParameter("@Status", (int)status));
    }

    /// <summary>
    /// 获取在库库存
    /// </summary>
    public List<DieInventory> GetInStockInventory()
    {
        return GetInventoryByStatus(StorageStatus.InStock);
    }

    /// <summary>
    /// 根据ID获取库存
    /// </summary>
    public DieInventory? GetInventoryById(int inventoryId)
    {
        var sql = @"SELECT i.InventoryID, i.DieID, i.LocationID, i.StorageStatus, i.InStockTime, 
                     i.LastBorrowTime, i.LastReturnTime, i.TotalBorrowCount, i.Remark, i.UpdateTime,
                     l.LocationCode, d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieInventory i
                     LEFT JOIN DM_StorageLocation l ON i.LocationID = l.LocationID
                     LEFT JOIN DM_DieInfo d ON i.DieID = d.DieID
                     WHERE i.InventoryID = @InventoryID";
        var inventories = ExecuteQuerySafe(sql, MapToDieInventory, $"获取库存(ID:{inventoryId})", 
            new SqlParameter("@InventoryID", inventoryId));
        return inventories.FirstOrDefault();
    }

    /// <summary>
    /// 根据刀模ID获取库存
    /// </summary>
    public DieInventory? GetInventoryByDieId(int dieId)
    {
        var sql = @"SELECT i.InventoryID, i.DieID, i.LocationID, i.StorageStatus, i.InStockTime, 
                     i.LastBorrowTime, i.LastReturnTime, i.TotalBorrowCount, i.Remark, i.UpdateTime,
                     l.LocationCode, d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieInventory i
                     LEFT JOIN DM_StorageLocation l ON i.LocationID = l.LocationID
                     LEFT JOIN DM_DieInfo d ON i.DieID = d.DieID
                     WHERE i.DieID = @DieID";
        var inventories = ExecuteQuerySafe(sql, MapToDieInventory, $"获取库存(DieID:{dieId})", 
            new SqlParameter("@DieID", dieId));
        return inventories.FirstOrDefault();
    }

    /// <summary>
    /// 更新库存库位
    /// </summary>
    public bool UpdateInventoryLocation(int inventoryId, int? locationId)
    {
        var sql = @"UPDATE DM_DieInventory SET 
                     LocationID = @LocationID,
                     UpdateTime = GETDATE()
                     WHERE InventoryID = @InventoryID";
        return ExecuteNonQuerySafe(sql, $"更新库存库位(InventoryID:{inventoryId})",
            new SqlParameter("@InventoryID", inventoryId),
            new SqlParameter("@LocationID", locationId ?? (object)DBNull.Value)) > 0;
    }

    /// <summary>
    /// 更新库存状态
    /// </summary>
    public bool UpdateInventoryStatus(int inventoryId, StorageStatus status)
    {
        var sql = @"UPDATE DM_DieInventory SET 
                     StorageStatus = @StorageStatus,
                     UpdateTime = GETDATE()
                     WHERE InventoryID = @InventoryID";
        return ExecuteNonQuerySafe(sql, $"更新库存状态(InventoryID:{inventoryId})",
            new SqlParameter("@InventoryID", inventoryId),
            new SqlParameter("@StorageStatus", (int)status)) > 0;
    }

    /// <summary>
    /// 将数据读取器映射为库存对象
    /// </summary>
    private DieInventory MapToDieInventory(SqlDataReader reader)
    {
        return new DieInventory
        {
            InventoryID = ConvertHelper.ToInt(reader["InventoryID"]),
            DieID = ConvertHelper.ToInt(reader["DieID"]),
            LocationID = ConvertHelper.ToNullableInt(reader["LocationID"]),
            StorageStatus = ConvertHelper.ToEnum(reader["StorageStatus"], StorageStatus.InStock),
            InStockTime = ConvertHelper.ToNullableDateTime(reader["InStockTime"]),
            LastBorrowTime = ConvertHelper.ToNullableDateTime(reader["LastBorrowTime"]),
            LastReturnTime = ConvertHelper.ToNullableDateTime(reader["LastReturnTime"]),
            TotalBorrowCount = ConvertHelper.ToInt(reader["TotalBorrowCount"]),
            Remark = ConvertHelper.ToString(reader["Remark"]),
            UpdateTime = ConvertHelper.ToDateTime(reader["UpdateTime"], DateTime.Now),
            LocationCode = ConvertHelper.ToString(reader["LocationCode"]),
            DieCode = ConvertHelper.ToString(reader["DieCode"]),
            CustomerName = ConvertHelper.ToString(reader["CustomerName"]),
            ProductName = ConvertHelper.ToString(reader["ProductName"])
        };
    }

    #endregion

    #region 借用记录管理

    /// <summary>
    /// 获取所有借用记录
    /// </summary>
    public List<DieBorrowRecord> GetAllBorrowRecords()
    {
        var sql = @"SELECT r.BorrowID, r.DieID, r.InventoryID, r.BorrowType, r.BorrowerNo, r.BorrowerName, 
                     r.BorrowDept, r.BorrowTime, r.ExpectedReturnTime, r.ActualReturnTime, r.Purpose, 
                     r.Status, r.ReturnOperatorNo, r.ReturnOperatorName, r.Remark, r.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieBorrowRecord r
                     LEFT JOIN DM_DieInfo d ON r.DieID = d.DieID
                     ORDER BY r.BorrowTime DESC";
        return ExecuteQuerySafe(sql, MapToDieBorrowRecord, "获取所有借用记录");
    }

    /// <summary>
    /// 根据状态获取借用记录
    /// </summary>
    public List<DieBorrowRecord> GetBorrowRecordsByStatus(BorrowStatus status)
    {
        var sql = @"SELECT r.BorrowID, r.DieID, r.InventoryID, r.BorrowType, r.BorrowerNo, r.BorrowerName, 
                     r.BorrowDept, r.BorrowTime, r.ExpectedReturnTime, r.ActualReturnTime, r.Purpose, 
                     r.Status, r.ReturnOperatorNo, r.ReturnOperatorName, r.Remark, r.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieBorrowRecord r
                     LEFT JOIN DM_DieInfo d ON r.DieID = d.DieID
                     WHERE r.Status = @Status
                     ORDER BY r.BorrowTime DESC";
        return ExecuteQuerySafe(sql, MapToDieBorrowRecord, $"获取借用记录(Status:{status})", 
            new SqlParameter("@Status", (int)status));
    }

    /// <summary>
    /// 根据刀模ID获取借用记录
    /// </summary>
    public List<DieBorrowRecord> GetBorrowRecordsByDieId(int dieId)
    {
        var sql = @"SELECT r.BorrowID, r.DieID, r.InventoryID, r.BorrowType, r.BorrowerNo, r.BorrowerName, 
                     r.BorrowDept, r.BorrowTime, r.ExpectedReturnTime, r.ActualReturnTime, r.Purpose, 
                     r.Status, r.ReturnOperatorNo, r.ReturnOperatorName, r.Remark, r.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieBorrowRecord r
                     LEFT JOIN DM_DieInfo d ON r.DieID = d.DieID
                     WHERE r.DieID = @DieID
                     ORDER BY r.BorrowTime DESC";
        return ExecuteQuerySafe(sql, MapToDieBorrowRecord, $"获取借用记录(DieID:{dieId})", 
            new SqlParameter("@DieID", dieId));
    }

    /// <summary>
    /// 搜索借用记录
    /// </summary>
    public List<DieBorrowRecord> SearchBorrowRecords(string? dieCode = null, string? borrowerName = null, 
        DateTime? startDate = null, DateTime? endDate = null)
    {
        var conditions = new List<string>();
        var parameters = new List<SqlParameter>();

        if (!string.IsNullOrEmpty(dieCode))
        {
            conditions.Add("d.DieCode LIKE @DieCode");
            parameters.Add(new SqlParameter("@DieCode", $"%{dieCode}%"));
        }

        if (!string.IsNullOrEmpty(borrowerName))
        {
            conditions.Add("r.BorrowerName LIKE @BorrowerName");
            parameters.Add(new SqlParameter("@BorrowerName", $"%{borrowerName}%"));
        }

        if (startDate.HasValue)
        {
            conditions.Add("r.BorrowTime >= @StartDate");
            parameters.Add(new SqlParameter("@StartDate", startDate.Value));
        }

        if (endDate.HasValue)
        {
            conditions.Add("r.BorrowTime <= @EndDate");
            parameters.Add(new SqlParameter("@EndDate", endDate.Value.AddDays(1).AddSeconds(-1)));
        }

        var baseSql = @"SELECT r.BorrowID, r.DieID, r.InventoryID, r.BorrowType, r.BorrowerNo, r.BorrowerName, 
                         r.BorrowDept, r.BorrowTime, r.ExpectedReturnTime, r.ActualReturnTime, r.Purpose, 
                         r.Status, r.ReturnOperatorNo, r.ReturnOperatorName, r.Remark, r.CreateTime,
                         d.DieCode, d.CustomerName, d.ProductName
                         FROM DM_DieBorrowRecord r
                         LEFT JOIN DM_DieInfo d ON r.DieID = d.DieID";
        
        return Search(baseSql, conditions, parameters, MapToDieBorrowRecord);
    }

    /// <summary>
    /// 根据ID获取借用记录
    /// </summary>
    public DieBorrowRecord? GetBorrowRecordById(int borrowId)
    {
        var sql = @"SELECT r.BorrowID, r.DieID, r.InventoryID, r.BorrowType, r.BorrowerNo, r.BorrowerName, 
                     r.BorrowDept, r.BorrowTime, r.ExpectedReturnTime, r.ActualReturnTime, r.Purpose, 
                     r.Status, r.ReturnOperatorNo, r.ReturnOperatorName, r.Remark, r.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieBorrowRecord r
                     LEFT JOIN DM_DieInfo d ON r.DieID = d.DieID
                     WHERE r.BorrowID = @BorrowID";
        var records = ExecuteQuerySafe(sql, MapToDieBorrowRecord, $"获取借用记录(ID:{borrowId})", 
            new SqlParameter("@BorrowID", borrowId));
        return records.FirstOrDefault();
    }

    /// <summary>
    /// 创建借用记录
    /// </summary>
    public int CreateBorrowRecord(DieBorrowRecord record)
    {
        int resultId = 0;
        bool success = ExecuteInTransaction((connection, transaction) =>
        {
            // 插入借用记录
            var sql = @"INSERT INTO DM_DieBorrowRecord (DieID, InventoryID, BorrowType, BorrowerNo, BorrowerName, 
                         BorrowDept, BorrowTime, ExpectedReturnTime, Purpose, Status, Remark, CreateTime) 
                         VALUES (@DieID, @InventoryID, @BorrowType, @BorrowerNo, @BorrowerName, 
                         @BorrowDept, @BorrowTime, @ExpectedReturnTime, @Purpose, @Status, @Remark, GETDATE());
                         SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@DieID", record.DieID);
            command.Parameters.AddWithValue("@InventoryID", record.InventoryID);
            command.Parameters.AddWithValue("@BorrowType", (int)record.BorrowType);
            command.Parameters.AddWithValue("@BorrowerNo", record.BorrowerNo);
            command.Parameters.AddWithValue("@BorrowerName", record.BorrowerName);
            command.Parameters.AddWithValue("@BorrowDept", record.BorrowDept);
            command.Parameters.AddWithValue("@BorrowTime", record.BorrowTime);
            command.Parameters.AddWithValue("@ExpectedReturnTime", record.ExpectedReturnTime ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Purpose", record.Purpose);
            command.Parameters.AddWithValue("@Status", (int)BorrowStatus.Borrowing);
            command.Parameters.AddWithValue("@Remark", record.Remark);

            var result = command.ExecuteScalar();
            var borrowId = result == DBNull.Value ? 0 : Convert.ToInt32(result);
            resultId = borrowId;

            // 更新库存状态为借出
            var updateSql = @"UPDATE DM_DieInventory SET 
                              StorageStatus = @StorageStatus,
                              LastBorrowTime = @LastBorrowTime,
                              TotalBorrowCount = TotalBorrowCount + 1,
                              UpdateTime = GETDATE()
                              WHERE InventoryID = @InventoryID";

            using var updateCommand = new SqlCommand(updateSql, connection, transaction);
            updateCommand.Parameters.AddWithValue("@StorageStatus", (int)StorageStatus.Borrowed);
            updateCommand.Parameters.AddWithValue("@LastBorrowTime", record.BorrowTime);
            updateCommand.Parameters.AddWithValue("@InventoryID", record.InventoryID);
            updateCommand.ExecuteNonQuery();

            // 更新库位状态为空闲
            var locationSql = @"UPDATE DM_StorageLocation SET Status = @Status 
                                 WHERE LocationID = (SELECT LocationID FROM DM_DieInventory WHERE InventoryID = @InventoryID)";
            using var locationCommand = new SqlCommand(locationSql, connection, transaction);
            locationCommand.Parameters.AddWithValue("@Status", (int)LocationStatus.Free);
            locationCommand.Parameters.AddWithValue("@InventoryID", record.InventoryID);
            locationCommand.ExecuteNonQuery();

            return true;
        }, "创建借用记录");

        return success ? resultId : 0;
    }

    /// <summary>
    /// 归还刀模
    /// </summary>
    public bool ReturnDie(int borrowId, string returnOperatorNo, string returnOperatorName, string? remark = null)
    {
        return ExecuteInTransaction((connection, transaction) =>
        {
            // 获取借用记录
            var record = GetBorrowRecordById(borrowId);
            if (record == null || record.Status == BorrowStatus.Returned)
            {
                ExceptionHelper.HandleException(new BusinessException("借用记录不存在或已归还。"), "归还刀模");
                return false;
            }

            var actualReturnTime = DateTime.Now;

            // 更新借用记录
            var sql = @"UPDATE DM_DieBorrowRecord SET 
                         ActualReturnTime = @ActualReturnTime,
                         Status = @Status,
                         ReturnOperatorNo = @ReturnOperatorNo,
                         ReturnOperatorName = @ReturnOperatorName,
                         Remark = @Remark
                         WHERE BorrowID = @BorrowID";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@ActualReturnTime", actualReturnTime);
            command.Parameters.AddWithValue("@Status", (int)BorrowStatus.Returned);
            command.Parameters.AddWithValue("@ReturnOperatorNo", returnOperatorNo);
            command.Parameters.AddWithValue("@ReturnOperatorName", returnOperatorName);
            command.Parameters.AddWithValue("@Remark", remark ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BorrowID", borrowId);
            command.ExecuteNonQuery();

            // 更新库存状态为在库
            var updateSql = @"UPDATE DM_DieInventory SET 
                              StorageStatus = @StorageStatus,
                              LastReturnTime = @LastReturnTime,
                              UpdateTime = GETDATE()
                              WHERE InventoryID = @InventoryID";

            using var updateCommand = new SqlCommand(updateSql, connection, transaction);
            updateCommand.Parameters.AddWithValue("@StorageStatus", (int)StorageStatus.InStock);
            updateCommand.Parameters.AddWithValue("@LastReturnTime", actualReturnTime);
            updateCommand.Parameters.AddWithValue("@InventoryID", record.InventoryID);
            updateCommand.ExecuteNonQuery();

            return true;
        }, $"归还刀模(BorrowID:{borrowId})");
    }

    /// <summary>
    /// 将数据读取器映射为借用记录对象
    /// </summary>
    private DieBorrowRecord MapToDieBorrowRecord(SqlDataReader reader)
    {
        return new DieBorrowRecord
        {
            BorrowID = ConvertHelper.ToInt(reader["BorrowID"]),
            DieID = ConvertHelper.ToInt(reader["DieID"]),
            InventoryID = ConvertHelper.ToInt(reader["InventoryID"]),
            BorrowType = ConvertHelper.ToEnum(reader["BorrowType"], BorrowType.Internal),
            BorrowerNo = ConvertHelper.ToString(reader["BorrowerNo"]),
            BorrowerName = ConvertHelper.ToString(reader["BorrowerName"]),
            BorrowDept = ConvertHelper.ToString(reader["BorrowDept"]),
            BorrowTime = ConvertHelper.ToDateTime(reader["BorrowTime"], DateTime.MinValue),
            ExpectedReturnTime = ConvertHelper.ToNullableDateTime(reader["ExpectedReturnTime"]),
            ActualReturnTime = ConvertHelper.ToNullableDateTime(reader["ActualReturnTime"]),
            Purpose = ConvertHelper.ToString(reader["Purpose"]),
            Status = ConvertHelper.ToEnum(reader["Status"], BorrowStatus.Borrowing),
            ReturnOperatorNo = ConvertHelper.ToString(reader["ReturnOperatorNo"]),
            ReturnOperatorName = ConvertHelper.ToString(reader["ReturnOperatorName"]),
            Remark = ConvertHelper.ToString(reader["Remark"]),
            CreateTime = ConvertHelper.ToDateTime(reader["CreateTime"], DateTime.Now),
            DieCode = ConvertHelper.ToString(reader["DieCode"]),
            CustomerName = ConvertHelper.ToString(reader["CustomerName"]),
            ProductName = ConvertHelper.ToString(reader["ProductName"])
        };
    }

    #endregion

    #region 报废申请管理

    /// <summary>
    /// 获取所有报废记录
    /// </summary>
    public List<DieScrapRecord> GetAllScrapRecords()
    {
        var sql = @"SELECT s.ScrapID, s.DieID, s.InventoryID, s.ScrapReason, s.ScrapType, s.ApplicantNo, 
                     s.ApplicantName, s.ApplyTime, s.AuditorNo, s.AuditorName, s.AuditTime, 
                     s.AuditStatus, s.AuditRemark, s.ScrapTime, s.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieScrapRecord s
                     LEFT JOIN DM_DieInfo d ON s.DieID = d.DieID
                     ORDER BY s.ApplyTime DESC";
        return ExecuteQuerySafe(sql, MapToDieScrapRecord, "获取所有报废记录");
    }

    /// <summary>
    /// 根据状态获取报废记录
    /// </summary>
    public List<DieScrapRecord> GetScrapRecordsByStatus(ScrapAuditStatus status)
    {
        var sql = @"SELECT s.ScrapID, s.DieID, s.InventoryID, s.ScrapReason, s.ScrapType, s.ApplicantNo, 
                     s.ApplicantName, s.ApplyTime, s.AuditorNo, s.AuditorName, s.AuditTime, 
                     s.AuditStatus, s.AuditRemark, s.ScrapTime, s.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieScrapRecord s
                     LEFT JOIN DM_DieInfo d ON s.DieID = d.DieID
                     WHERE s.AuditStatus = @AuditStatus
                     ORDER BY s.ApplyTime DESC";
        return ExecuteQuerySafe(sql, MapToDieScrapRecord, $"获取报废记录(Status:{status})", 
            new SqlParameter("@AuditStatus", (int)status));
    }

    /// <summary>
    /// 根据ID获取报废记录
    /// </summary>
    public DieScrapRecord? GetScrapRecordById(int scrapId)
    {
        var sql = @"SELECT s.ScrapID, s.DieID, s.InventoryID, s.ScrapReason, s.ScrapType, s.ApplicantNo, 
                     s.ApplicantName, s.ApplyTime, s.AuditorNo, s.AuditorName, s.AuditTime, 
                     s.AuditStatus, s.AuditRemark, s.ScrapTime, s.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieScrapRecord s
                     LEFT JOIN DM_DieInfo d ON s.DieID = d.DieID
                     WHERE s.ScrapID = @ScrapID";
        var records = ExecuteQuerySafe(sql, MapToDieScrapRecord, $"获取报废记录(ID:{scrapId})", 
            new SqlParameter("@ScrapID", scrapId));
        return records.FirstOrDefault();
    }

    /// <summary>
    /// 创建报废记录
    /// </summary>
    public int CreateScrapRecord(DieScrapRecord record)
    {
        var sql = @"INSERT INTO DM_DieScrapRecord (DieID, InventoryID, ScrapReason, ScrapType, ApplicantNo, 
                     ApplicantName, ApplyTime, AuditStatus, CreateTime) 
                     VALUES (@DieID, @InventoryID, @ScrapReason, @ScrapType, @ApplicantNo, 
                     @ApplicantName, @ApplyTime, @AuditStatus, GETDATE());
                     SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var result = ExecuteScalarSafe(sql, "创建报废记录",
            new SqlParameter("@DieID", record.DieID),
            new SqlParameter("@InventoryID", record.InventoryID),
            new SqlParameter("@ScrapReason", record.ScrapReason),
            new SqlParameter("@ScrapType", record.ScrapType),
            new SqlParameter("@ApplicantNo", record.ApplicantNo),
            new SqlParameter("@ApplicantName", record.ApplicantName),
            new SqlParameter("@ApplyTime", record.ApplyTime),
            new SqlParameter("@AuditStatus", (int)ScrapAuditStatus.Pending));

        return result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    /// <summary>
    /// 审核报废记录
    /// </summary>
    public bool AuditScrapRecord(int scrapId, bool approved, string auditorNo, string auditorName, string? auditRemark = null)
    {
        return ExecuteInTransaction((connection, transaction) =>
        {
            var auditTime = DateTime.Now;
            var auditStatus = approved ? ScrapAuditStatus.Approved : ScrapAuditStatus.Rejected;

            // 更新报废申请记录
            var sql = @"UPDATE DM_DieScrapRecord SET 
                         AuditStatus = @AuditStatus,
                         AuditorNo = @AuditorNo,
                         AuditorName = @AuditorName,
                         AuditTime = @AuditTime,
                         AuditRemark = @AuditRemark
                         WHERE ScrapID = @ScrapID";

            using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@AuditStatus", (int)auditStatus);
            command.Parameters.AddWithValue("@AuditorNo", auditorNo);
            command.Parameters.AddWithValue("@AuditorName", auditorName);
            command.Parameters.AddWithValue("@AuditTime", auditTime);
            command.Parameters.AddWithValue("@AuditRemark", auditRemark ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ScrapID", scrapId);
            command.ExecuteNonQuery();

            // 如果审核通过，更新库存状态为报废
            if (approved)
            {
                var record = GetScrapRecordById(scrapId);
                if (record != null)
                {
                    var updateSql = @"UPDATE DM_DieInventory SET 
                                      StorageStatus = @StorageStatus,
                                      UpdateTime = GETDATE()
                                      WHERE InventoryID = @InventoryID";

                    using var updateCommand = new SqlCommand(updateSql, connection, transaction);
                    updateCommand.Parameters.AddWithValue("@StorageStatus", (int)StorageStatus.Scrapped);
                    updateCommand.Parameters.AddWithValue("@InventoryID", record.InventoryID);
                    updateCommand.ExecuteNonQuery();

                    // 更新报废时间
                    var scrapTimeSql = "UPDATE DM_DieScrapRecord SET ScrapTime = @ScrapTime WHERE ScrapID = @ScrapID";
                    using var scrapTimeCommand = new SqlCommand(scrapTimeSql, connection, transaction);
                    scrapTimeCommand.Parameters.AddWithValue("@ScrapTime", auditTime);
                    scrapTimeCommand.Parameters.AddWithValue("@ScrapID", scrapId);
                    scrapTimeCommand.ExecuteNonQuery();
                }
            }

            return true;
        }, $"审核报废记录(ID:{scrapId})");
    }

    /// <summary>
    /// 删除报废记录
    /// </summary>
    public bool DeleteScrapRecord(int scrapId)
    {
        var sql = "DELETE FROM DM_DieScrapRecord WHERE ScrapID = @ScrapID AND AuditStatus = @AuditStatus";
        return ExecuteNonQuerySafe(sql, $"删除报废记录(ID:{scrapId})",
            new SqlParameter("@ScrapID", scrapId),
            new SqlParameter("@AuditStatus", (int)ScrapAuditStatus.Pending)) > 0;
    }

    /// <summary>
    /// 将数据读取器映射为报废记录对象
    /// </summary>
    private DieScrapRecord MapToDieScrapRecord(SqlDataReader reader)
    {
        return new DieScrapRecord
        {
            ScrapID = ConvertHelper.ToInt(reader["ScrapID"]),
            DieID = ConvertHelper.ToInt(reader["DieID"]),
            InventoryID = ConvertHelper.ToInt(reader["InventoryID"]),
            ScrapReason = ConvertHelper.ToString(reader["ScrapReason"]),
            ScrapType = ConvertHelper.ToString(reader["ScrapType"]),
            ApplicantNo = ConvertHelper.ToString(reader["ApplicantNo"]),
            ApplicantName = ConvertHelper.ToString(reader["ApplicantName"]),
            ApplyTime = ConvertHelper.ToDateTime(reader["ApplyTime"], DateTime.MinValue),
            AuditorNo = ConvertHelper.ToString(reader["AuditorNo"]),
            AuditorName = ConvertHelper.ToString(reader["AuditorName"]),
            AuditTime = ConvertHelper.ToNullableDateTime(reader["AuditTime"]),
            AuditStatus = ConvertHelper.ToEnum(reader["AuditStatus"], ScrapAuditStatus.Pending),
            AuditRemark = ConvertHelper.ToString(reader["AuditRemark"]),
            ScrapTime = ConvertHelper.ToNullableDateTime(reader["ScrapTime"]),
            CreateTime = ConvertHelper.ToDateTime(reader["CreateTime"], DateTime.Now),
            DieCode = ConvertHelper.ToString(reader["DieCode"]),
            CustomerName = ConvertHelper.ToString(reader["CustomerName"]),
            ProductName = ConvertHelper.ToString(reader["ProductName"])
        };
    }

    #endregion
}
