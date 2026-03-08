using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;

namespace DieMaking.Helpers;

/// <summary>
/// 批量操作帮助类 - 提供高性能的批量数据导入和更新功能
/// </summary>
public static class BulkOperationHelper
{
    /// <summary>
    /// 批量插入数据（使用SqlBulkCopy）
    /// </summary>
    public static BulkOperationResult BulkInsert<T>(List<T> data, string tableName, Dictionary<string, string>? columnMappings = null, int batchSize = 5000)
    {
        var result = new BulkOperationResult();
        var stopwatch = Stopwatch.StartNew();

        if (data == null || data.Count == 0)
        {
            result.Message = "没有数据需要导入";
            return result;
        }

        try
        {
            // 创建DataTable
            var dataTable = CreateDataTableFromList(data, columnMappings);

            using var connection = DbHelper.CreateConnection();
            connection.Open();

            using var bulkCopy = new SqlBulkCopy(connection)
            {
                DestinationTableName = tableName,
                BatchSize = batchSize,
                BulkCopyTimeout = 300 // 5分钟超时
            };

            // 设置列映射
            if (columnMappings != null)
            {
                foreach (var mapping in columnMappings)
                {
                    bulkCopy.ColumnMappings.Add(mapping.Key, mapping.Value);
                }
            }
            else
            {
                // 自动映射
                foreach (DataColumn column in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }
            }

            // 执行批量插入
            bulkCopy.WriteToServer(dataTable);

            stopwatch.Stop();

            result.Success = true;
            result.RowCount = data.Count;
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Message = $"成功导入 {data.Count} 条记录，耗时 {stopwatch.ElapsedMilliseconds}ms";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Message = $"导入失败：{ex.Message}";
            ExceptionHelper.HandleException(ex, "批量导入数据");
        }

        return result;
    }

    /// <summary>
    /// 异步批量插入数据
    /// </summary>
    public static async Task<BulkOperationResult> BulkInsertAsync<T>(List<T> data, string tableName, Dictionary<string, string>? columnMappings = null, int batchSize = 5000, IProgress<BulkProgress>? progress = null)
    {
        var result = new BulkOperationResult();
        var stopwatch = Stopwatch.StartNew();

        if (data == null || data.Count == 0)
        {
            result.Message = "没有数据需要导入";
            return result;
        }

        try
        {
            // 分批处理以支持进度报告
            var totalRows = data.Count;
            var processedRows = 0;
            var batchIndex = 0;

            using var connection = DbHelper.CreateConnection();
            await connection.OpenAsync();

            while (processedRows < totalRows)
            {
                var batchData = data.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                var dataTable = CreateDataTableFromList(batchData, columnMappings);

                using var bulkCopy = new SqlBulkCopy(connection)
                {
                    DestinationTableName = tableName,
                    BatchSize = batchSize,
                    BulkCopyTimeout = 300
                };

                // 设置列映射
                if (columnMappings != null)
                {
                    foreach (var mapping in columnMappings)
                    {
                        bulkCopy.ColumnMappings.Add(mapping.Key, mapping.Value);
                    }
                }
                else
                {
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }
                }

                // 执行批量插入
                await bulkCopy.WriteToServerAsync(dataTable);

                processedRows += batchData.Count;
                batchIndex++;

                // 报告进度
                progress?.Report(new BulkProgress
                {
                    TotalRows = totalRows,
                    ProcessedRows = processedRows,
                    PercentComplete = (int)((double)processedRows / totalRows * 100),
                    CurrentBatch = batchIndex,
                    Message = $"已导入 {processedRows}/{totalRows} 条记录"
                });
            }

            stopwatch.Stop();

            result.Success = true;
            result.RowCount = totalRows;
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Message = $"成功导入 {totalRows} 条记录，耗时 {stopwatch.ElapsedMilliseconds}ms";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Message = $"导入失败：{ex.Message}";
            ExceptionHelper.HandleException(ex, "批量导入数据");
        }

        return result;
    }

    /// <summary>
    /// 批量更新数据（使用表值参数）
    /// </summary>
    public static BulkOperationResult BulkUpdate<T>(List<T> data, string tableName, string keyColumn, List<string> updateColumns, int batchSize = 1000)
    {
        var result = new BulkOperationResult();
        var stopwatch = Stopwatch.StartNew();

        if (data == null || data.Count == 0)
        {
            result.Message = "没有数据需要更新";
            return result;
        }

        try
        {
            var totalRows = data.Count;
            var processedRows = 0;

            using var connection = DbHelper.CreateConnection();
            connection.Open();

            // 分批处理
            for (int i = 0; i < data.Count; i += batchSize)
            {
                var batch = data.Skip(i).Take(batchSize).ToList();
                
                // 构建批量更新SQL
                var updateSql = BuildBatchUpdateSql(tableName, keyColumn, updateColumns, batch.Count);
                var parameters = CreateUpdateParameters(batch, keyColumn, updateColumns);

                using var command = new SqlCommand(updateSql, connection);
                command.Parameters.AddRange(parameters.ToArray());
                command.ExecuteNonQuery();

                processedRows += batch.Count;
            }

            stopwatch.Stop();

            result.Success = true;
            result.RowCount = totalRows;
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Message = $"成功更新 {totalRows} 条记录，耗时 {stopwatch.ElapsedMilliseconds}ms";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Message = $"更新失败：{ex.Message}";
            ExceptionHelper.HandleException(ex, "批量更新数据");
        }

        return result;
    }

    /// <summary>
    /// 使用MERGE语句批量更新或插入
    /// </summary>
    public static BulkOperationResult BulkMerge<T>(List<T> data, string tableName, string keyColumn, List<string> updateColumns, List<string>? insertColumns = null, int batchSize = 1000)
    {
        var result = new BulkOperationResult();
        var stopwatch = Stopwatch.StartNew();

        if (data == null || data.Count == 0)
        {
            result.Message = "没有数据需要处理";
            return result;
        }

        try
        {
            var totalRows = data.Count;
            var processedRows = 0;
            var insertedCount = 0;
            var updatedCount = 0;

            using var connection = DbHelper.CreateConnection();
            connection.Open();

            // 创建临时表
            var tempTableName = $"#TempMerge_{Guid.NewGuid().ToString("N")[0..8]}";
            var createTempSql = BuildCreateTempTableSql(tableName, tempTableName);
            using (var createCmd = new SqlCommand(createTempSql, connection))
            {
                createCmd.ExecuteNonQuery();
            }

            // 分批处理
            for (int i = 0; i < data.Count; i += batchSize)
            {
                var batch = data.Skip(i).Take(batchSize).ToList();
                
                // 插入到临时表
                var dataTable = CreateDataTableFromList(batch);
                using (var bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = tempTableName;
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                    }
                    bulkCopy.WriteToServer(dataTable);
                }

                // 执行MERGE
                var mergeSql = BuildMergeSql(tableName, tempTableName, keyColumn, updateColumns, insertColumns);
                using (var mergeCmd = new SqlCommand(mergeSql, connection))
                {
                    var mergeResult = mergeCmd.ExecuteScalar();
                    if (mergeResult != null)
                    {
                        var counts = mergeResult.ToString()!.Split(',');
                        if (counts.Length >= 2)
                        {
                            insertedCount += int.Parse(counts[0]);
                            updatedCount += int.Parse(counts[1]);
                        }
                    }
                }

                // 清空临时表
                using (var truncateCmd = new SqlCommand($"TRUNCATE TABLE {tempTableName}", connection))
                {
                    truncateCmd.ExecuteNonQuery();
                }

                processedRows += batch.Count;
            }

            // 删除临时表
            using (var dropCmd = new SqlCommand($"DROP TABLE {tempTableName}", connection))
            {
                dropCmd.ExecuteNonQuery();
            }

            stopwatch.Stop();

            result.Success = true;
            result.RowCount = totalRows;
            result.InsertedCount = insertedCount;
            result.UpdatedCount = updatedCount;
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Message = $"成功处理 {totalRows} 条记录（插入 {insertedCount}，更新 {updatedCount}），耗时 {stopwatch.ElapsedMilliseconds}ms";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Message = $"处理失败：{ex.Message}";
            ExceptionHelper.HandleException(ex, "批量合并数据");
        }

        return result;
    }

    /// <summary>
    /// 批量删除数据
    /// </summary>
    public static BulkOperationResult BulkDelete<TKey>(List<TKey> keys, string tableName, string keyColumn, int batchSize = 1000)
    {
        var result = new BulkOperationResult();
        var stopwatch = Stopwatch.StartNew();

        if (keys == null || keys.Count == 0)
        {
            result.Message = "没有数据需要删除";
            return result;
        }

        try
        {
            var totalRows = keys.Count;

            using var connection = DbHelper.CreateConnection();
            connection.Open();

            // 分批处理
            for (int i = 0; i < keys.Count; i += batchSize)
            {
                var batch = keys.Skip(i).Take(batchSize).ToList();
                var placeholders = string.Join(",", batch.Select((_, idx) => $"@p{idx}"));
                var deleteSql = $"DELETE FROM {tableName} WHERE {keyColumn} IN ({placeholders})";

                using var command = new SqlCommand(deleteSql, connection);
                for (int j = 0; j < batch.Count; j++)
                {
                    command.Parameters.AddWithValue($"@p{j}", batch[j]!);
                }
                command.ExecuteNonQuery();
            }

            stopwatch.Stop();

            result.Success = true;
            result.RowCount = totalRows;
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            result.Message = $"成功删除 {totalRows} 条记录，耗时 {stopwatch.ElapsedMilliseconds}ms";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Message = $"删除失败：{ex.Message}";
            ExceptionHelper.HandleException(ex, "批量删除数据");
        }

        return result;
    }

    #region 辅助方法

    /// <summary>
    /// 从列表创建DataTable
    /// </summary>
    private static DataTable CreateDataTableFromList<T>(List<T> data, Dictionary<string, string>? columnMappings = null)
    {
        var dataTable = new DataTable();
        var properties = typeof(T).GetProperties();

        // 创建列
        foreach (var prop in properties)
        {
            var columnName = columnMappings?.ContainsKey(prop.Name) == true 
                ? columnMappings[prop.Name] 
                : prop.Name;
            
            var propertyType = prop.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            dataTable.Columns.Add(columnName, underlyingType);
        }

        // 填充数据
        foreach (var item in data)
        {
            var row = dataTable.NewRow();
            foreach (var prop in properties)
            {
                var columnName = columnMappings?.ContainsKey(prop.Name) == true 
                    ? columnMappings[prop.Name] 
                    : prop.Name;
                var value = prop.GetValue(item);
                row[columnName] = value ?? DBNull.Value;
            }
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    /// <summary>
    /// 构建批量更新SQL
    /// </summary>
    private static string BuildBatchUpdateSql(string tableName, string keyColumn, List<string> updateColumns, int batchSize)
    {
        var cases = new List<string>();
        foreach (var col in updateColumns)
        {
            var caseStatements = new List<string>();
            for (int i = 0; i < batchSize; i++)
            {
                caseStatements.Add($"WHEN @{keyColumn}{i} THEN @{col}{i}");
            }
            cases.Add($"{col} = CASE {keyColumn} {string.Join(" ", caseStatements)} END");
        }

        var keyList = string.Join(",", Enumerable.Range(0, batchSize).Select(i => $"@{keyColumn}{i}"));
        return $"UPDATE {tableName} SET {string.Join(", ", cases)} WHERE {keyColumn} IN ({keyList})";
    }

    /// <summary>
    /// 创建更新参数
    /// </summary>
    private static List<SqlParameter> CreateUpdateParameters<T>(List<T> data, string keyColumn, List<string> updateColumns)
    {
        var parameters = new List<SqlParameter>();
        var properties = typeof(T).GetProperties();

        for (int i = 0; i < data.Count; i++)
        {
            var item = data[i];
            
            // 主键参数
            var keyProp = properties.FirstOrDefault(p => p.Name == keyColumn);
            if (keyProp != null)
            {
                parameters.Add(new SqlParameter($"@{keyColumn}{i}", keyProp.GetValue(item) ?? DBNull.Value));
            }

            // 更新列参数
            foreach (var col in updateColumns)
            {
                var prop = properties.FirstOrDefault(p => p.Name == col);
                if (prop != null)
                {
                    parameters.Add(new SqlParameter($"@{col}{i}", prop.GetValue(item) ?? DBNull.Value));
                }
            }
        }

        return parameters;
    }

    /// <summary>
    /// 构建创建临时表SQL
    /// </summary>
    private static string BuildCreateTempTableSql(string sourceTableName, string tempTableName)
    {
        return $@"
            SELECT * INTO {tempTableName} 
            FROM {sourceTableName} 
            WHERE 1=0;
            
            ALTER TABLE {tempTableName} DROP COLUMN IF EXISTS RowNum;
        ";
    }

    /// <summary>
    /// 构建MERGE SQL
    /// </summary>
    private static string BuildMergeSql(string targetTable, string sourceTable, string keyColumn, List<string> updateColumns, List<string>? insertColumns = null)
    {
        var insertCols = insertColumns ?? updateColumns.Concat(new[] { keyColumn }).ToList();
        var updateSet = string.Join(", ", updateColumns.Select(c => $"target.{c} = source.{c}"));
        var insertColList = string.Join(", ", insertCols);
        var insertValueList = string.Join(", ", insertCols.Select(c => $"source.{c}"));

        return $@"
            MERGE {targetTable} AS target
            USING {sourceTable} AS source
            ON target.{keyColumn} = source.{keyColumn}
            WHEN MATCHED THEN
                UPDATE SET {updateSet}
            WHEN NOT MATCHED THEN
                INSERT ({insertColList})
                VALUES ({insertValueList})
            OUTPUT $action INTO @MergeActions;
            
            DECLARE @InsertedCount INT = (SELECT COUNT(*) FROM @MergeActions WHERE Action = 'INSERT');
            DECLARE @UpdatedCount INT = (SELECT COUNT(*) FROM @MergeActions WHERE Action = 'UPDATE');
            SELECT CAST(@InsertedCount AS VARCHAR) + ',' + CAST(@UpdatedCount AS VARCHAR);
        ";
    }

    #endregion
}

/// <summary>
/// 批量操作结果
/// </summary>
public class BulkOperationResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }
    
    /// <summary>处理行数</summary>
    public int RowCount { get; set; }
    
    /// <summary>插入行数</summary>
    public int InsertedCount { get; set; }
    
    /// <summary>更新行数</summary>
    public int UpdatedCount { get; set; }
    
    /// <summary>执行时间（毫秒）</summary>
    public long ExecutionTimeMs { get; set; }
    
    /// <summary>消息</summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 批量操作进度
/// </summary>
public class BulkProgress
{
    /// <summary>总行数</summary>
    public int TotalRows { get; set; }
    
    /// <summary>已处理行数</summary>
    public int ProcessedRows { get; set; }
    
    /// <summary>完成百分比</summary>
    public int PercentComplete { get; set; }
    
    /// <summary>当前批次</summary>
    public int CurrentBatch { get; set; }
    
    /// <summary>消息</summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>预计剩余时间（秒）</summary>
    public int EstimatedSecondsRemaining { get; set; }
}

/// <summary>
/// 批量操作进度对话框
/// </summary>
public class BulkProgressDialog : Form
{
    private ProgressBar _progressBar = null!;
    private Label _lblStatus = null!;
    private Label _lblPercent = null!;
    private Button _btnCancel = null!;
    private CancellationTokenSource? _cancellationTokenSource;

    public BulkProgressDialog()
    {
        InitializeComponent();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? CancellationToken.None;

    private void InitializeComponent()
    {
        this.Text = "批量操作进度";
        this.Size = new Size(500, 200);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;

        var lblTitle = new Label
        {
            Text = "正在处理数据，请稍候...",
            Location = new Point(20, 20),
            Size = new Size(460, 25),
            Font = new Font(UIStyleHelper.FontName, 11, FontStyle.Bold)
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(20, 60),
            Size = new Size(460, 25),
            Minimum = 0,
            Maximum = 100,
            Style = ProgressBarStyle.Continuous
        };

        _lblPercent = new Label
        {
            Text = "0%",
            Location = new Point(20, 95),
            Size = new Size(100, 25),
            Font = new Font(UIStyleHelper.FontName, 10, FontStyle.Bold)
        };

        _lblStatus = new Label
        {
            Text = "准备开始...",
            Location = new Point(120, 95),
            Size = new Size(360, 25),
            Font = new Font(UIStyleHelper.FontName, 9)
        };

        _btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(200, 130),
            Size = new Size(100, 35)
        };
        _btnCancel.Click += (s, e) =>
        {
            _cancellationTokenSource?.Cancel();
            _btnCancel.Enabled = false;
            _lblStatus.Text = "正在取消...";
        };

        this.Controls.Add(lblTitle);
        this.Controls.Add(_progressBar);
        this.Controls.Add(_lblPercent);
        this.Controls.Add(_lblStatus);
        this.Controls.Add(_btnCancel);
    }

    /// <summary>
    /// 更新进度
    /// </summary>
    public void UpdateProgress(BulkProgress progress)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action<BulkProgress>(UpdateProgress), progress);
            return;
        }

        _progressBar.Value = Math.Min(progress.PercentComplete, 100);
        _lblPercent.Text = $"{progress.PercentComplete}%";
        _lblStatus.Text = progress.Message;

        if (progress.PercentComplete >= 100)
        {
            _btnCancel.Text = "完成";
            _btnCancel.Enabled = true;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        base.OnFormClosing(e);
    }
}
