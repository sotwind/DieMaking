using DieMaking.Helpers;
using DieMaking.Models;
using Microsoft.Data.SqlClient;

namespace DieMaking.Services;

public class WarehouseService
{
    #region 库位管理

    public List<StorageLocation> GetAllLocations()
    {
        var sql = @"SELECT LocationID, LocationCode, Area, ShelfNo, LayerNo, PositionNo, 
                     Description, Status, CreateTime 
                     FROM DM_StorageLocation 
                     ORDER BY Area, ShelfNo, LayerNo, PositionNo";

        return DbHelper.ExecuteQuery(sql, MapToStorageLocation);
    }

    public List<StorageLocation> GetLocationsByStatus(LocationStatus status)
    {
        var sql = @"SELECT LocationID, LocationCode, Area, ShelfNo, LayerNo, PositionNo, 
                     Description, Status, CreateTime 
                     FROM DM_StorageLocation 
                     WHERE Status = @Status
                     ORDER BY Area, ShelfNo, LayerNo, PositionNo";

        return DbHelper.ExecuteQuery(sql, MapToStorageLocation, new SqlParameter("@Status", (int)status));
    }

    public List<StorageLocation> SearchLocations(string keyword)
    {
        var sql = @"SELECT LocationID, LocationCode, Area, ShelfNo, LayerNo, PositionNo, 
                     Description, Status, CreateTime 
                     FROM DM_StorageLocation 
                     WHERE LocationCode LIKE @Keyword OR Area LIKE @Keyword OR Description LIKE @Keyword
                     ORDER BY Area, ShelfNo, LayerNo, PositionNo";

        return DbHelper.ExecuteQuery(sql, MapToStorageLocation, new SqlParameter("@Keyword", $"%{keyword}%"));
    }

    public StorageLocation? GetLocationById(int locationId)
    {
        var sql = @"SELECT LocationID, LocationCode, Area, ShelfNo, LayerNo, PositionNo, 
                     Description, Status, CreateTime 
                     FROM DM_StorageLocation 
                     WHERE LocationID = @LocationID";

        var locations = DbHelper.ExecuteQuery(sql, MapToStorageLocation, new SqlParameter("@LocationID", locationId));
        return locations.FirstOrDefault();
    }

    public int CreateLocation(StorageLocation location)
    {
        var sql = @"INSERT INTO DM_StorageLocation (LocationCode, Area, ShelfNo, LayerNo, PositionNo, Description, Status, CreateTime) 
                     VALUES (@LocationCode, @Area, @ShelfNo, @LayerNo, @PositionNo, @Description, @Status, GETDATE());
                     SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var result = DbHelper.ExecuteScalar(sql,
            new SqlParameter("@LocationCode", location.LocationCode),
            new SqlParameter("@Area", location.Area),
            new SqlParameter("@ShelfNo", location.ShelfNo),
            new SqlParameter("@LayerNo", location.LayerNo),
            new SqlParameter("@PositionNo", location.PositionNo),
            new SqlParameter("@Description", location.Description),
            new SqlParameter("@Status", (int)location.Status));

        return result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    public bool UpdateLocation(StorageLocation location)
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

        return DbHelper.ExecuteNonQuery(sql,
            new SqlParameter("@LocationID", location.LocationID),
            new SqlParameter("@LocationCode", location.LocationCode),
            new SqlParameter("@Area", location.Area),
            new SqlParameter("@ShelfNo", location.ShelfNo),
            new SqlParameter("@LayerNo", location.LayerNo),
            new SqlParameter("@PositionNo", location.PositionNo),
            new SqlParameter("@Description", location.Description),
            new SqlParameter("@Status", (int)location.Status)) > 0;
    }

    public bool DeleteLocation(int locationId)
    {
        var sql = "DELETE FROM DM_StorageLocation WHERE LocationID = @LocationID";
        return DbHelper.ExecuteNonQuery(sql, new SqlParameter("@LocationID", locationId)) > 0;
    }

    public bool IsLocationCodeExists(string locationCode, int? excludeLocationId = null)
    {
        var sql = excludeLocationId.HasValue
            ? "SELECT COUNT(*) FROM DM_StorageLocation WHERE LocationCode = @LocationCode AND LocationID != @LocationID"
            : "SELECT COUNT(*) FROM DM_StorageLocation WHERE LocationCode = @LocationCode";

        var parameters = new List<SqlParameter> { new SqlParameter("@LocationCode", locationCode) };
        if (excludeLocationId.HasValue)
            parameters.Add(new SqlParameter("@LocationID", excludeLocationId.Value));

        var result = DbHelper.ExecuteScalar(sql, parameters.ToArray());
        return Convert.ToInt32(result) > 0;
    }

    private StorageLocation MapToStorageLocation(SqlDataReader reader)
    {
        return new StorageLocation
        {
            LocationID = Convert.ToInt32(reader["LocationID"]),
            LocationCode = reader["LocationCode"].ToString() ?? "",
            Area = reader["Area"].ToString() ?? "",
            ShelfNo = reader["ShelfNo"].ToString() ?? "",
            LayerNo = reader["LayerNo"].ToString() ?? "",
            PositionNo = reader["PositionNo"].ToString() ?? "",
            Description = reader["Description"].ToString() ?? "",
            Status = (LocationStatus)Convert.ToInt32(reader["Status"]),
            CreateTime = Convert.ToDateTime(reader["CreateTime"])
        };
    }

    #endregion

    #region 刀模库存管理

    public List<DieInventory> GetAllInventory()
    {
        var sql = @"SELECT i.InventoryID, i.DieID, i.LocationID, i.StorageStatus, i.InStockTime, 
                     i.LastBorrowTime, i.LastReturnTime, i.TotalBorrowCount, i.Remark, i.UpdateTime,
                     l.LocationCode, d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieInventory i
                     LEFT JOIN DM_StorageLocation l ON i.LocationID = l.LocationID
                     LEFT JOIN DM_DieInfo d ON i.DieID = d.DieID
                     ORDER BY i.UpdateTime DESC";

        return DbHelper.ExecuteQuery(sql, MapToDieInventory);
    }

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

        return DbHelper.ExecuteQuery(sql, MapToDieInventory, new SqlParameter("@Status", (int)status));
    }

    public List<DieInventory> GetInStockInventory()
    {
        return GetInventoryByStatus(StorageStatus.InStock);
    }

    public DieInventory? GetInventoryById(int inventoryId)
    {
        var sql = @"SELECT i.InventoryID, i.DieID, i.LocationID, i.StorageStatus, i.InStockTime, 
                     i.LastBorrowTime, i.LastReturnTime, i.TotalBorrowCount, i.Remark, i.UpdateTime,
                     l.LocationCode, d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieInventory i
                     LEFT JOIN DM_StorageLocation l ON i.LocationID = l.LocationID
                     LEFT JOIN DM_DieInfo d ON i.DieID = d.DieID
                     WHERE i.InventoryID = @InventoryID";

        var inventories = DbHelper.ExecuteQuery(sql, MapToDieInventory, new SqlParameter("@InventoryID", inventoryId));
        return inventories.FirstOrDefault();
    }

    public DieInventory? GetInventoryByDieId(int dieId)
    {
        var sql = @"SELECT i.InventoryID, i.DieID, i.LocationID, i.StorageStatus, i.InStockTime, 
                     i.LastBorrowTime, i.LastReturnTime, i.TotalBorrowCount, i.Remark, i.UpdateTime,
                     l.LocationCode, d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieInventory i
                     LEFT JOIN DM_StorageLocation l ON i.LocationID = l.LocationID
                     LEFT JOIN DM_DieInfo d ON i.DieID = d.DieID
                     WHERE i.DieID = @DieID";

        var inventories = DbHelper.ExecuteQuery(sql, MapToDieInventory, new SqlParameter("@DieID", dieId));
        return inventories.FirstOrDefault();
    }

    public bool UpdateInventoryLocation(int inventoryId, int? locationId)
    {
        var sql = @"UPDATE DM_DieInventory SET 
                     LocationID = @LocationID,
                     UpdateTime = GETDATE()
                     WHERE InventoryID = @InventoryID";

        return DbHelper.ExecuteNonQuery(sql,
            new SqlParameter("@InventoryID", inventoryId),
            new SqlParameter("@LocationID", locationId ?? (object)DBNull.Value)) > 0;
    }

    public bool UpdateInventoryStatus(int inventoryId, StorageStatus status)
    {
        var sql = @"UPDATE DM_DieInventory SET 
                     StorageStatus = @StorageStatus,
                     UpdateTime = GETDATE()
                     WHERE InventoryID = @InventoryID";

        return DbHelper.ExecuteNonQuery(sql,
            new SqlParameter("@InventoryID", inventoryId),
            new SqlParameter("@StorageStatus", (int)status)) > 0;
    }

    private DieInventory MapToDieInventory(SqlDataReader reader)
    {
        return new DieInventory
        {
            InventoryID = Convert.ToInt32(reader["InventoryID"]),
            DieID = Convert.ToInt32(reader["DieID"]),
            LocationID = reader["LocationID"] != DBNull.Value ? Convert.ToInt32(reader["LocationID"]) : null,
            StorageStatus = (StorageStatus)Convert.ToInt32(reader["StorageStatus"]),
            InStockTime = reader["InStockTime"] != DBNull.Value ? Convert.ToDateTime(reader["InStockTime"]) : null,
            LastBorrowTime = reader["LastBorrowTime"] != DBNull.Value ? Convert.ToDateTime(reader["LastBorrowTime"]) : null,
            LastReturnTime = reader["LastReturnTime"] != DBNull.Value ? Convert.ToDateTime(reader["LastReturnTime"]) : null,
            TotalBorrowCount = Convert.ToInt32(reader["TotalBorrowCount"]),
            Remark = reader["Remark"].ToString() ?? "",
            UpdateTime = Convert.ToDateTime(reader["UpdateTime"]),
            LocationCode = reader["LocationCode"].ToString(),
            DieCode = reader["DieCode"].ToString(),
            CustomerName = reader["CustomerName"].ToString(),
            ProductName = reader["ProductName"].ToString()
        };
    }

    #endregion

    #region 借用记录管理

    public List<DieBorrowRecord> GetAllBorrowRecords()
    {
        var sql = @"SELECT r.BorrowID, r.DieID, r.InventoryID, r.BorrowType, r.BorrowerNo, r.BorrowerName, 
                     r.BorrowDept, r.BorrowTime, r.ExpectedReturnTime, r.ActualReturnTime, r.Purpose, 
                     r.Status, r.ReturnOperatorNo, r.ReturnOperatorName, r.Remark, r.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieBorrowRecord r
                     LEFT JOIN DM_DieInfo d ON r.DieID = d.DieID
                     ORDER BY r.BorrowTime DESC";

        return DbHelper.ExecuteQuery(sql, MapToDieBorrowRecord);
    }

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

        return DbHelper.ExecuteQuery(sql, MapToDieBorrowRecord, new SqlParameter("@Status", (int)status));
    }

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

        return DbHelper.ExecuteQuery(sql, MapToDieBorrowRecord, new SqlParameter("@DieID", dieId));
    }

    public List<DieBorrowRecord> SearchBorrowRecords(string? dieCode = null, string? borrowerName = null, DateTime? startDate = null, DateTime? endDate = null)
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

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

        var sql = $@"SELECT r.BorrowID, r.DieID, r.InventoryID, r.BorrowType, r.BorrowerNo, r.BorrowerName, 
                     r.BorrowDept, r.BorrowTime, r.ExpectedReturnTime, r.ActualReturnTime, r.Purpose, 
                     r.Status, r.ReturnOperatorNo, r.ReturnOperatorName, r.Remark, r.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieBorrowRecord r
                     LEFT JOIN DM_DieInfo d ON r.DieID = d.DieID
                     {whereClause}
                     ORDER BY r.BorrowTime DESC";

        return DbHelper.ExecuteQuery(sql, MapToDieBorrowRecord, parameters.ToArray());
    }

    public DieBorrowRecord? GetBorrowRecordById(int borrowId)
    {
        var sql = @"SELECT r.BorrowID, r.DieID, r.InventoryID, r.BorrowType, r.BorrowerNo, r.BorrowerName, 
                     r.BorrowDept, r.BorrowTime, r.ExpectedReturnTime, r.ActualReturnTime, r.Purpose, 
                     r.Status, r.ReturnOperatorNo, r.ReturnOperatorName, r.Remark, r.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieBorrowRecord r
                     LEFT JOIN DM_DieInfo d ON r.DieID = d.DieID
                     WHERE r.BorrowID = @BorrowID";

        var records = DbHelper.ExecuteQuery(sql, MapToDieBorrowRecord, new SqlParameter("@BorrowID", borrowId));
        return records.FirstOrDefault();
    }

    public int CreateBorrowRecord(DieBorrowRecord record)
    {
        using var connection = DbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
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

            transaction.Commit();
            return borrowId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool ReturnDie(int borrowId, string returnOperatorNo, string returnOperatorName, string? remark = null)
    {
        using var connection = DbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 获取借用记录
            var record = GetBorrowRecordById(borrowId);
            if (record == null || record.Status == BorrowStatus.Returned)
                return false;

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

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private DieBorrowRecord MapToDieBorrowRecord(SqlDataReader reader)
    {
        return new DieBorrowRecord
        {
            BorrowID = Convert.ToInt32(reader["BorrowID"]),
            DieID = Convert.ToInt32(reader["DieID"]),
            InventoryID = Convert.ToInt32(reader["InventoryID"]),
            BorrowType = (BorrowType)Convert.ToInt32(reader["BorrowType"]),
            BorrowerNo = reader["BorrowerNo"].ToString() ?? "",
            BorrowerName = reader["BorrowerName"].ToString() ?? "",
            BorrowDept = reader["BorrowDept"].ToString() ?? "",
            BorrowTime = Convert.ToDateTime(reader["BorrowTime"]),
            ExpectedReturnTime = reader["ExpectedReturnTime"] != DBNull.Value ? Convert.ToDateTime(reader["ExpectedReturnTime"]) : null,
            ActualReturnTime = reader["ActualReturnTime"] != DBNull.Value ? Convert.ToDateTime(reader["ActualReturnTime"]) : null,
            Purpose = reader["Purpose"].ToString() ?? "",
            Status = (BorrowStatus)Convert.ToInt32(reader["Status"]),
            ReturnOperatorNo = reader["ReturnOperatorNo"].ToString() ?? "",
            ReturnOperatorName = reader["ReturnOperatorName"].ToString() ?? "",
            Remark = reader["Remark"].ToString() ?? "",
            CreateTime = Convert.ToDateTime(reader["CreateTime"]),
            DieCode = reader["DieCode"].ToString(),
            CustomerName = reader["CustomerName"].ToString(),
            ProductName = reader["ProductName"].ToString()
        };
    }

    #endregion

    #region 报废申请管理

    public List<DieScrapRecord> GetAllScrapRecords()
    {
        var sql = @"SELECT s.ScrapID, s.DieID, s.InventoryID, s.ScrapReason, s.ScrapType, s.ApplicantNo, 
                     s.ApplicantName, s.ApplyTime, s.AuditorNo, s.AuditorName, s.AuditTime, 
                     s.AuditStatus, s.AuditRemark, s.ScrapTime, s.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieScrapRecord s
                     LEFT JOIN DM_DieInfo d ON s.DieID = d.DieID
                     ORDER BY s.ApplyTime DESC";

        return DbHelper.ExecuteQuery(sql, MapToDieScrapRecord);
    }

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

        return DbHelper.ExecuteQuery(sql, MapToDieScrapRecord, new SqlParameter("@AuditStatus", (int)status));
    }

    public DieScrapRecord? GetScrapRecordById(int scrapId)
    {
        var sql = @"SELECT s.ScrapID, s.DieID, s.InventoryID, s.ScrapReason, s.ScrapType, s.ApplicantNo, 
                     s.ApplicantName, s.ApplyTime, s.AuditorNo, s.AuditorName, s.AuditTime, 
                     s.AuditStatus, s.AuditRemark, s.ScrapTime, s.CreateTime,
                     d.DieCode, d.CustomerName, d.ProductName
                     FROM DM_DieScrapRecord s
                     LEFT JOIN DM_DieInfo d ON s.DieID = d.DieID
                     WHERE s.ScrapID = @ScrapID";

        var records = DbHelper.ExecuteQuery(sql, MapToDieScrapRecord, new SqlParameter("@ScrapID", scrapId));
        return records.FirstOrDefault();
    }

    public int CreateScrapRecord(DieScrapRecord record)
    {
        var sql = @"INSERT INTO DM_DieScrapRecord (DieID, InventoryID, ScrapReason, ScrapType, ApplicantNo, 
                     ApplicantName, ApplyTime, AuditStatus, CreateTime) 
                     VALUES (@DieID, @InventoryID, @ScrapReason, @ScrapType, @ApplicantNo, 
                     @ApplicantName, @ApplyTime, @AuditStatus, GETDATE());
                     SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var result = DbHelper.ExecuteScalar(sql,
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

    public bool AuditScrapRecord(int scrapId, bool approved, string auditorNo, string auditorName, string? auditRemark = null)
    {
        using var connection = DbHelper.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
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

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool DeleteScrapRecord(int scrapId)
    {
        var sql = "DELETE FROM DM_DieScrapRecord WHERE ScrapID = @ScrapID AND AuditStatus = @AuditStatus";
        return DbHelper.ExecuteNonQuery(sql, 
            new SqlParameter("@ScrapID", scrapId),
            new SqlParameter("@AuditStatus", (int)ScrapAuditStatus.Pending)) > 0;
    }

    private DieScrapRecord MapToDieScrapRecord(SqlDataReader reader)
    {
        return new DieScrapRecord
        {
            ScrapID = Convert.ToInt32(reader["ScrapID"]),
            DieID = Convert.ToInt32(reader["DieID"]),
            InventoryID = Convert.ToInt32(reader["InventoryID"]),
            ScrapReason = reader["ScrapReason"].ToString() ?? "",
            ScrapType = reader["ScrapType"].ToString() ?? "",
            ApplicantNo = reader["ApplicantNo"].ToString() ?? "",
            ApplicantName = reader["ApplicantName"].ToString() ?? "",
            ApplyTime = Convert.ToDateTime(reader["ApplyTime"]),
            AuditorNo = reader["AuditorNo"].ToString(),
            AuditorName = reader["AuditorName"].ToString(),
            AuditTime = reader["AuditTime"] != DBNull.Value ? Convert.ToDateTime(reader["AuditTime"]) : null,
            AuditStatus = (ScrapAuditStatus)Convert.ToInt32(reader["AuditStatus"]),
            AuditRemark = reader["AuditRemark"].ToString(),
            ScrapTime = reader["ScrapTime"] != DBNull.Value ? Convert.ToDateTime(reader["ScrapTime"]) : null,
            CreateTime = Convert.ToDateTime(reader["CreateTime"]),
            DieCode = reader["DieCode"].ToString(),
            CustomerName = reader["CustomerName"].ToString(),
            ProductName = reader["ProductName"].ToString()
        };
    }

    #endregion
}
