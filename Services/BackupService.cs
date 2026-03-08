using DieMaking.Helpers;
using Microsoft.Data.SqlClient;
using System.IO.Compression;

namespace DieMaking.Services;

/// <summary>
/// 备份类型
/// </summary>
public enum BackupType
{
    /// <summary>完整备份</summary>
    Full = 0,
    /// <summary>差异备份</summary>
    Differential = 1,
    /// <summary>事务日志备份</summary>
    Log = 2
}

/// <summary>
/// 备份状态
/// </summary>
public enum BackupStatus
{
    /// <summary>进行中</summary>
    InProgress = 0,
    /// <summary>成功</summary>
    Success = 1,
    /// <summary>失败</summary>
    Failed = 2,
    /// <summary>已取消</summary>
    Cancelled = 3
}

/// <summary>
/// 备份服务 - 提供数据库备份、恢复和管理功能
/// </summary>
public class BackupService
{
    private readonly string _backupBasePath;
    private readonly int _retentionCount;

    /// <summary>
    /// 备份进度事件
    /// </summary>
    public event EventHandler<BackupProgressEventArgs>? BackupProgress;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="backupBasePath">备份基础路径</param>
    /// <param name="retentionCount">保留的备份数量</param>
    public BackupService(string? backupBasePath = null, int retentionCount = 10)
    {
        _backupBasePath = backupBasePath ?? GetDefaultBackupPath();
        _retentionCount = retentionCount;

        // 确保备份目录存在
        if (!Directory.Exists(_backupBasePath))
        {
            Directory.CreateDirectory(_backupBasePath);
        }
    }

    #region 手动备份

    /// <summary>
    /// 执行手动备份
    /// </summary>
    /// <param name="backupName">备份名称（可选）</param>
    /// <param name="backupType">备份类型</param>
    /// <param name="compress">是否压缩</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>备份结果</returns>
    public async Task<BackupResult> BackupAsync(string? backupName = null, BackupType backupType = BackupType.Full,
        bool compress = true, CancellationToken cancellationToken = default)
    {
        var result = new BackupResult();
        var startTime = DateTime.Now;
        var backupFileName = GenerateBackupFileName(backupName, backupType);
        var backupPath = Path.Combine(_backupBasePath, backupFileName);

        try
        {
            // 记录备份开始
            var recordId = await RecordBackupStartAsync(backupFileName, backupPath, backupType, cancellationToken);
            result.BackupId = recordId;

            OnBackupProgress(new BackupProgressEventArgs
            {
                Stage = BackupStage.Starting,
                Message = "开始备份...",
                PercentComplete = 0
            });

            // 执行SQL备份命令
            var databaseName = GetDatabaseName();
            var backupSql = GenerateBackupSql(databaseName, backupPath, backupType);

            OnBackupProgress(new BackupProgressEventArgs
            {
                Stage = BackupStage.BackingUp,
                Message = "正在执行数据库备份...",
                PercentComplete = 30
            });

            await using var connection = await DbHelper.CreateAndOpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(backupSql, connection);
            command.CommandTimeout = 3600; // 1小时超时
            await command.ExecuteNonQueryAsync(cancellationToken);

            // 如果需要压缩
            if (compress && File.Exists(backupPath))
            {
                OnBackupProgress(new BackupProgressEventArgs
                {
                    Stage = BackupStage.Compressing,
                    Message = "正在压缩备份文件...",
                    PercentComplete = 70
                });

                var compressedPath = await CompressBackupAsync(backupPath, cancellationToken);

                // 删除原始文件
                File.Delete(backupPath);
                backupPath = compressedPath;
            }

            // 获取备份文件信息
            var fileInfo = new FileInfo(backupPath);
            var endTime = DateTime.Now;

            // 更新备份记录
            await UpdateBackupRecordAsync(recordId, endTime, fileInfo.Length, BackupStatus.Success, null, cancellationToken);

            // 清理旧备份
            await CleanupOldBackupsAsync(cancellationToken);

            OnBackupProgress(new BackupProgressEventArgs
            {
                Stage = BackupStage.Completed,
                Message = "备份完成",
                PercentComplete = 100
            });

            result.Success = true;
            result.BackupPath = backupPath;
            result.BackupSize = fileInfo.Length;
            result.Duration = endTime - startTime;
            result.Message = $"备份成功: {backupFileName}";
        }
        catch (OperationCanceledException)
        {
            OnBackupProgress(new BackupProgressEventArgs
            {
                Stage = BackupStage.Cancelled,
                Message = "备份已取消",
                PercentComplete = 0
            });

            result.Success = false;
            result.Message = "备份已取消";

            // 清理未完成的文件
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
        catch (Exception ex)
        {
            OnBackupProgress(new BackupProgressEventArgs
            {
                Stage = BackupStage.Error,
                Message = $"备份失败: {ex.Message}",
                PercentComplete = 0,
                Error = ex
            });

            result.Success = false;
            result.Message = $"备份失败: {ex.Message}";
            result.ErrorMessage = ex.Message;

            // 更新备份记录为失败
            if (result.BackupId > 0)
            {
                await UpdateBackupRecordAsync(result.BackupId, DateTime.Now, 0, BackupStatus.Failed, ex.Message, cancellationToken);
            }

            // 清理失败的文件
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }

        return result;
    }

    /// <summary>
    /// 同步执行备份
    /// </summary>
    public BackupResult Backup(string? backupName = null, BackupType backupType = BackupType.Full, bool compress = true)
    {
        return BackupAsync(backupName, backupType, compress).GetAwaiter().GetResult();
    }

    #endregion

    #region 自动备份

    /// <summary>
    /// 执行自动定时备份
    /// </summary>
    /// <param name="schedule">备份计划（Cron表达式）</param>
    /// <param name="backupType">备份类型</param>
    /// <param name="compress">是否压缩</param>
    /// <returns>备份任务</returns>
    public async Task<BackupResult> ScheduledBackupAsync(string schedule, BackupType backupType = BackupType.Full, bool compress = true)
    {
        // 检查是否需要执行备份
        if (!ShouldRunScheduledBackup(schedule))
        {
            return new BackupResult
            {
                Success = true,
                Message = "不在备份计划时间内，跳过本次备份"
            };
        }

        return await BackupAsync($"Auto_{DateTime.Now:yyyyMMdd_HHmmss}", backupType, compress);
    }

    /// <summary>
    /// 检查是否应该执行定时备份
    /// </summary>
    private bool ShouldRunScheduledBackup(string schedule)
    {
        // 简化版：解析Cron表达式判断是否应该执行
        // 实际项目中可以使用 NCrontab 库

        // 默认每天凌晨2点执行
        var now = DateTime.Now;
        return now.Hour == 2 && now.Minute < 5;
    }

    #endregion

    #region 数据恢复

    /// <summary>
    /// 从备份文件恢复数据库
    /// </summary>
    /// <param name="backupFilePath">备份文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>恢复结果</returns>
    public async Task<RestoreResult> RestoreAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        var result = new RestoreResult();
        var startTime = DateTime.Now;

        try
        {
            OnBackupProgress(new BackupProgressEventArgs
            {
                Stage = BackupStage.Starting,
                Message = "开始恢复数据库...",
                PercentComplete = 0
            });

            // 检查备份文件
            if (!File.Exists(backupFilePath))
            {
                // 尝试在备份目录中查找
                var fullPath = Path.Combine(_backupBasePath, backupFilePath);
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException("备份文件不存在", backupFilePath);
                }
                backupFilePath = fullPath;
            }

            // 如果是压缩文件，先解压
            var actualBackupPath = backupFilePath;
            if (Path.GetExtension(backupFilePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                OnBackupProgress(new BackupProgressEventArgs
                {
                    Stage = BackupStage.Decompressing,
                    Message = "正在解压备份文件...",
                    PercentComplete = 10
                });

                actualBackupPath = await DecompressBackupAsync(backupFilePath, cancellationToken);
            }

            OnBackupProgress(new BackupProgressEventArgs
            {
                Stage = BackupStage.Restoring,
                Message = "正在恢复数据库...",
                PercentComplete = 30
            });

            // 执行恢复
            var databaseName = GetDatabaseName();
            await PerformRestoreAsync(databaseName, actualBackupPath, cancellationToken);

            // 清理解压的临时文件
            if (actualBackupPath != backupFilePath && File.Exists(actualBackupPath))
            {
                File.Delete(actualBackupPath);
            }

            var endTime = DateTime.Now;

            OnBackupProgress(new BackupProgressEventArgs
            {
                Stage = BackupStage.Completed,
                Message = "数据库恢复完成",
                PercentComplete = 100
            });

            result.Success = true;
            result.Duration = endTime - startTime;
            result.Message = "数据库恢复成功";
        }
        catch (Exception ex)
        {
            OnBackupProgress(new BackupProgressEventArgs
            {
                Stage = BackupStage.Error,
                Message = $"恢复失败: {ex.Message}",
                PercentComplete = 0,
                Error = ex
            });

            result.Success = false;
            result.Message = $"恢复失败: {ex.Message}";
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 同步恢复数据库
    /// </summary>
    public RestoreResult Restore(string backupFilePath)
    {
        return RestoreAsync(backupFilePath).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 从备份记录恢复
    /// </summary>
    public async Task<RestoreResult> RestoreFromRecordAsync(int backupId, CancellationToken cancellationToken = default)
    {
        var record = await GetBackupRecordAsync(backupId, cancellationToken);
        if (record == null)
        {
            return new RestoreResult
            {
                Success = false,
                Message = "备份记录不存在"
            };
        }

        return await RestoreAsync(record.BackupPath, cancellationToken);
    }

    #endregion

    #region 备份文件管理

    /// <summary>
    /// 获取备份列表
    /// </summary>
    public List<BackupRecord> GetBackupList()
    {
        try
        {
            var sql = @"
                SELECT BackupID, BackupFileName, BackupPath, BackupSize, BackupType,
                       StartTime, EndTime, Status, ErrorMessage, CreatedBy, Remark
                FROM DM_BackupRecord
                ORDER BY StartTime DESC";

            return DbHelper.ExecuteQuery(sql, reader => new BackupRecord
            {
                BackupId = Convert.ToInt32(reader["BackupID"]),
                BackupFileName = reader["BackupFileName"].ToString()!,
                BackupPath = reader["BackupPath"].ToString()!,
                BackupSize = reader["BackupSize"] != DBNull.Value ? Convert.ToInt64(reader["BackupSize"]) : 0,
                BackupType = (BackupType)Convert.ToInt32(reader["BackupType"]),
                StartTime = Convert.ToDateTime(reader["StartTime"]),
                EndTime = reader["EndTime"] != DBNull.Value ? Convert.ToDateTime(reader["EndTime"]) : null,
                Status = (BackupStatus)Convert.ToInt32(reader["Status"]),
                ErrorMessage = reader["ErrorMessage"].ToString(),
                CreatedBy = reader["CreatedBy"].ToString(),
                Remark = reader["Remark"].ToString()
            });
        }
        catch
        {
            // 如果表不存在，从文件系统读取
            return GetBackupListFromFileSystem();
        }
    }

    /// <summary>
    /// 从文件系统获取备份列表
    /// </summary>
    private List<BackupRecord> GetBackupListFromFileSystem()
    {
        var records = new List<BackupRecord>();

        if (!Directory.Exists(_backupBasePath))
        {
            return records;
        }

        var files = Directory.GetFiles(_backupBasePath, "*.bak")
            .Concat(Directory.GetFiles(_backupBasePath, "*.zip"))
            .OrderByDescending(f => new FileInfo(f).CreationTime);

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            records.Add(new BackupRecord
            {
                BackupFileName = Path.GetFileName(file),
                BackupPath = file,
                BackupSize = fileInfo.Length,
                StartTime = fileInfo.CreationTime,
                Status = BackupStatus.Success
            });
        }

        return records;
    }

    /// <summary>
    /// 删除备份
    /// </summary>
    public async Task<bool> DeleteBackupAsync(int backupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await GetBackupRecordAsync(backupId, cancellationToken);
            if (record == null)
            {
                return false;
            }

            // 删除文件
            if (File.Exists(record.BackupPath))
            {
                File.Delete(record.BackupPath);
            }

            // 删除记录
            var sql = "DELETE FROM DM_BackupRecord WHERE BackupID = @BackupID";
            await DbHelper.ExecuteNonQueryAsync(sql, new SqlParameter("@BackupID", backupId));

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 清理旧备份（保留最近N个）
    /// </summary>
    public async Task CleanupOldBackupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $@"
                SELECT BackupID, BackupPath FROM (
                    SELECT BackupID, BackupPath,
                           ROW_NUMBER() OVER (ORDER BY StartTime DESC) as RowNum
                    FROM DM_BackupRecord
                    WHERE Status = 1
                ) AS RankedBackups
                WHERE RowNum > @RetentionCount";

            var oldBackups = await DbHelper.ExecuteQueryAsync(sql,
                reader => new
                {
                    BackupId = Convert.ToInt32(reader["BackupID"]),
                    BackupPath = reader["BackupPath"].ToString()!
                },
                new SqlParameter("@RetentionCount", _retentionCount));

            foreach (var backup in oldBackups)
            {
                // 删除文件
                if (File.Exists(backup.BackupPath))
                {
                    File.Delete(backup.BackupPath);
                }

                // 删除记录
                await DbHelper.ExecuteNonQueryAsync(
                    "DELETE FROM DM_BackupRecord WHERE BackupID = @BackupID",
                    new SqlParameter("@BackupID", backup.BackupId));
            }
        }
        catch
        {
            // 忽略错误
        }
    }

    /// <summary>
    /// 获取备份统计信息
    /// </summary>
    public BackupStatistics GetBackupStatistics()
    {
        try
        {
            var sql = @"
                SELECT 
                    COUNT(*) as TotalCount,
                    COUNT(CASE WHEN Status = 1 THEN 1 END) as SuccessCount,
                    COUNT(CASE WHEN Status = 2 THEN 1 END) as FailedCount,
                    SUM(CASE WHEN Status = 1 THEN BackupSize ELSE 0 END) as TotalSize,
                    MAX(StartTime) as LastBackupTime
                FROM DM_BackupRecord";

            return DbHelper.ExecuteQuery(sql, reader => new BackupStatistics
            {
                TotalCount = Convert.ToInt32(reader["TotalCount"]),
                SuccessCount = Convert.ToInt32(reader["SuccessCount"]),
                FailedCount = Convert.ToInt32(reader["FailedCount"]),
                TotalSize = reader["TotalSize"] != DBNull.Value ? Convert.ToInt64(reader["TotalSize"]) : 0,
                LastBackupTime = reader["LastBackupTime"] != DBNull.Value ? Convert.ToDateTime(reader["LastBackupTime"]) : null
            }).FirstOrDefault() ?? new BackupStatistics();
        }
        catch
        {
            return new BackupStatistics();
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 生成备份文件名
    /// </summary>
    private string GenerateBackupFileName(string? backupName, BackupType backupType)
    {
        var databaseName = GetDatabaseName();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var typeSuffix = backupType switch
        {
            BackupType.Differential => "_diff",
            BackupType.Log => "_log",
            _ => ""
        };

        var name = string.IsNullOrEmpty(backupName) ? $"{databaseName}_{timestamp}" : backupName;
        return $"{name}{typeSuffix}.bak";
    }

    /// <summary>
    /// 生成备份SQL
    /// </summary>
    private string GenerateBackupSql(string databaseName, string backupPath, BackupType backupType)
    {
        var typeClause = backupType switch
        {
            BackupType.Differential => "WITH DIFFERENTIAL",
            _ => ""
        };

        return $@"
            BACKUP DATABASE [{databaseName}]
            TO DISK = @BackupPath
            {typeClause}
            WITH FORMAT, COMPRESSION, STATS = 10";
    }

    /// <summary>
    /// 执行恢复操作
    /// </summary>
    private async Task PerformRestoreAsync(string databaseName, string backupPath, CancellationToken cancellationToken)
    {
        // 构建master连接字符串
        var builder = new SqlConnectionStringBuilder(DbHelper.ConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        // 设置单用户模式并恢复
        var restoreSql = $@"
            ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            RESTORE DATABASE [{databaseName}]
            FROM DISK = @BackupPath
            WITH REPLACE, RECOVERY;
            ALTER DATABASE [{databaseName}] SET MULTI_USER;";

        await using var command = new SqlCommand(restoreSql, connection);
        command.Parameters.AddWithValue("@BackupPath", backupPath);
        command.CommandTimeout = 3600; // 1小时超时
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// 压缩备份文件
    /// </summary>
    private async Task<string> CompressBackupAsync(string backupPath, CancellationToken cancellationToken)
    {
        var zipPath = Path.ChangeExtension(backupPath, ".zip");

        await Task.Run(() =>
        {
            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(backupPath, Path.GetFileName(backupPath));
        }, cancellationToken);

        return zipPath;
    }

    /// <summary>
    /// 解压备份文件
    /// </summary>
    private async Task<string> DecompressBackupAsync(string zipPath, CancellationToken cancellationToken)
    {
        var extractPath = Path.Combine(
            Path.GetDirectoryName(zipPath)!,
            $"temp_{Guid.NewGuid():N}.bak");

        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase));
            if (entry != null)
            {
                entry.ExtractToFile(extractPath, true);
            }
        }, cancellationToken);

        return extractPath;
    }

    /// <summary>
    /// 记录备份开始
    /// </summary>
    private async Task<int> RecordBackupStartAsync(string fileName, string path, BackupType type, CancellationToken cancellationToken)
    {
        try
        {
            var sql = @"
                INSERT INTO DM_BackupRecord (BackupFileName, BackupPath, BackupType, StartTime, Status, CreatedBy)
                OUTPUT INSERTED.BackupID
                VALUES (@FileName, @Path, @Type, GETDATE(), 0, @CreatedBy)";

            var result = await DbHelper.ExecuteScalarAsync(sql,
                new SqlParameter("@FileName", fileName),
                new SqlParameter("@Path", path),
                new SqlParameter("@Type", (int)type),
                new SqlParameter("@CreatedBy", Environment.UserName));

            return result != null ? Convert.ToInt32(result) : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 更新备份记录
    /// </summary>
    private async Task UpdateBackupRecordAsync(int backupId, DateTime endTime, long size, BackupStatus status,
        string? errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            var sql = @"
                UPDATE DM_BackupRecord
                SET EndTime = @EndTime, BackupSize = @Size, Status = @Status, ErrorMessage = @ErrorMessage
                WHERE BackupID = @BackupID";

            await DbHelper.ExecuteNonQueryAsync(sql,
                new SqlParameter("@BackupID", backupId),
                new SqlParameter("@EndTime", endTime),
                new SqlParameter("@Size", size),
                new SqlParameter("@Status", (int)status),
                new SqlParameter("@ErrorMessage", errorMessage ?? (object)DBNull.Value));
        }
        catch
        {
            // 忽略错误
        }
    }

    /// <summary>
    /// 获取备份记录
    /// </summary>
    private async Task<BackupRecord?> GetBackupRecordAsync(int backupId, CancellationToken cancellationToken)
    {
        try
        {
            var sql = @"
                SELECT BackupID, BackupFileName, BackupPath, BackupSize, BackupType,
                       StartTime, EndTime, Status, ErrorMessage, CreatedBy, Remark
                FROM DM_BackupRecord
                WHERE BackupID = @BackupID";

            var results = await DbHelper.ExecuteQueryAsync(sql,
                reader => new BackupRecord
                {
                    BackupId = Convert.ToInt32(reader["BackupID"]),
                    BackupFileName = reader["BackupFileName"].ToString()!,
                    BackupPath = reader["BackupPath"].ToString()!,
                    BackupSize = reader["BackupSize"] != DBNull.Value ? Convert.ToInt64(reader["BackupSize"]) : 0,
                    BackupType = (BackupType)Convert.ToInt32(reader["BackupType"]),
                    StartTime = Convert.ToDateTime(reader["StartTime"]),
                    EndTime = reader["EndTime"] != DBNull.Value ? Convert.ToDateTime(reader["EndTime"]) : null,
                    Status = (BackupStatus)Convert.ToInt32(reader["Status"]),
                    ErrorMessage = reader["ErrorMessage"].ToString(),
                    CreatedBy = reader["CreatedBy"].ToString(),
                    Remark = reader["Remark"].ToString()
                },
                new SqlParameter("@BackupID", backupId));

            return results.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取数据库名称
    /// </summary>
    private string GetDatabaseName()
    {
        var builder = new SqlConnectionStringBuilder(DbHelper.ConnectionString);
        return builder.InitialCatalog;
    }

    /// <summary>
    /// 获取默认备份路径
    /// </summary>
    private string GetDefaultBackupPath()
    {
        // 尝试从配置读取
        try
        {
            var sql = "SELECT ConfigValue FROM DM_SystemConfig WHERE ConfigKey = 'BackupPath'";
            var result = DbHelper.ExecuteScalar(sql);
            if (result != null && result != DBNull.Value)
            {
                var path = result.ToString()!;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }
        catch
        {
            // 使用默认路径
        }

        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DieMaking", "Backup");

        if (!Directory.Exists(defaultPath))
        {
            Directory.CreateDirectory(defaultPath);
        }

        return defaultPath;
    }

    /// <summary>
    /// 触发备份进度事件
    /// </summary>
    private void OnBackupProgress(BackupProgressEventArgs args)
    {
        BackupProgress?.Invoke(this, args);
    }

    #endregion
}

#region 事件和数据模型

/// <summary>
/// 备份阶段
/// </summary>
public enum BackupStage
{
    Starting,
    BackingUp,
    Compressing,
    Decompressing,
    Restoring,
    Completed,
    Cancelled,
    Error
}

/// <summary>
/// 备份进度事件参数
/// </summary>
public class BackupProgressEventArgs : EventArgs
{
    /// <summary>当前阶段</summary>
    public BackupStage Stage { get; set; }

    /// <summary>进度消息</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>完成百分比</summary>
    public int PercentComplete { get; set; }

    /// <summary>错误信息</summary>
    public Exception? Error { get; set; }
}

/// <summary>
/// 备份结果
/// </summary>
public class BackupResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>消息</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>备份ID</summary>
    public int BackupId { get; set; }

    /// <summary>备份路径</summary>
    public string BackupPath { get; set; } = string.Empty;

    /// <summary>备份大小（字节）</summary>
    public long BackupSize { get; set; }

    /// <summary>耗时</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 恢复结果
/// </summary>
public class RestoreResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>消息</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>耗时</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 备份记录
/// </summary>
public class BackupRecord
{
    /// <summary>备份ID</summary>
    public int BackupId { get; set; }

    /// <summary>备份文件名</summary>
    public string BackupFileName { get; set; } = string.Empty;

    /// <summary>备份路径</summary>
    public string BackupPath { get; set; } = string.Empty;

    /// <summary>备份大小</summary>
    public long BackupSize { get; set; }

    /// <summary>备份类型</summary>
    public BackupType BackupType { get; set; }

    /// <summary>开始时间</summary>
    public DateTime StartTime { get; set; }

    /// <summary>结束时间</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>状态</summary>
    public BackupStatus Status { get; set; }

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>创建人</summary>
    public string? CreatedBy { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    /// <summary>备份大小文本</summary>
    public string BackupSizeText => FormatFileSize(BackupSize);

    /// <summary>状态文本</summary>
    public string StatusText => Status switch
    {
        BackupStatus.InProgress => "进行中",
        BackupStatus.Success => "成功",
        BackupStatus.Failed => "失败",
        BackupStatus.Cancelled => "已取消",
        _ => "未知"
    };

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F2} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F2} KB";
        return $"{bytes} B";
    }
}

/// <summary>
/// 备份统计信息
/// </summary>
public class BackupStatistics
{
    /// <summary>总备份数</summary>
    public int TotalCount { get; set; }

    /// <summary>成功数</summary>
    public int SuccessCount { get; set; }

    /// <summary>失败数</summary>
    public int FailedCount { get; set; }

    /// <summary>总大小</summary>
    public long TotalSize { get; set; }

    /// <summary>最后备份时间</summary>
    public DateTime? LastBackupTime { get; set; }

    /// <summary>成功率</summary>
    public double SuccessRate => TotalCount > 0 ? (double)SuccessCount / TotalCount * 100 : 0;
}

#endregion
