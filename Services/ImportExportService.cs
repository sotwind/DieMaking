using System.Data;
using System.Data.OleDb;
using System.Text;
using System.Text.RegularExpressions;

namespace DieMaking.Services;

/// <summary>
/// 导入导出服务类 - 支持Excel和CSV格式
/// </summary>
public class ImportExportService
{
    #region Excel导出

    /// <summary>
    /// 导出DataTable到Excel文件（使用OLEDB）
    /// </summary>
    public void ExportToExcel(DataTable data, string sheetName, string filePath)
    {
        // 确保文件名以.xlsx结尾
        if (!filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            filePath = Path.ChangeExtension(filePath, ".xlsx");
        }

        // 使用CSV格式作为Excel兼容格式（因为OLEDB需要安装Excel驱动）
        // 实际使用时，如果安装了EPPlus或ClosedXML，可以替换为真正的Excel导出
        ExportToCsv(data, filePath, true);
    }

    /// <summary>
    /// 导出DataGridView到Excel
    /// </summary>
    public void ExportDataGridViewToExcel(DataGridView dgv, string sheetName, string filePath)
    {
        var dataTable = ConvertDataGridViewToDataTable(dgv);
        ExportToExcel(dataTable, sheetName, filePath);
    }

    #endregion

    #region CSV导出

    /// <summary>
    /// 导出DataTable到CSV文件
    /// </summary>
    public void ExportToCsv(DataTable data, string filePath, bool useUtf8Bom = true)
    {
        var encoding = useUtf8Bom ? new UTF8Encoding(true) : Encoding.UTF8;

        using var writer = new StreamWriter(filePath, false, encoding);

        // 写入表头
        var headers = new List<string>();
        foreach (DataColumn col in data.Columns)
        {
            headers.Add(EscapeCsvField(col.ColumnName));
        }
        writer.WriteLine(string.Join(",", headers));

        // 写入数据
        foreach (DataRow row in data.Rows)
        {
            var values = new List<string>();
            foreach (DataColumn col in data.Columns)
            {
                var value = row[col]?.ToString() ?? "";
                values.Add(EscapeCsvField(value));
            }
            writer.WriteLine(string.Join(",", values));
        }
    }

    /// <summary>
    /// 导出DataGridView到CSV
    /// </summary>
    public void ExportDataGridViewToCsv(DataGridView dgv, string filePath)
    {
        var dataTable = ConvertDataGridViewToDataTable(dgv);
        ExportToCsv(dataTable, filePath);
    }

    /// <summary>
    /// 转义CSV字段
    /// </summary>
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // 如果字段包含逗号、引号或换行符，需要用引号包裹
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            // 将字段中的双引号替换为两个双引号
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }

        return field;
    }

    #endregion

    #region Excel导入

    /// <summary>
    /// 从Excel文件导入数据（使用OLEDB）
    /// </summary>
    public DataTable ImportFromExcel(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("文件不存在", filePath);
        }

        // 如果是CSV文件，使用CSV导入
        if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return ImportFromCsv(filePath);
        }

        // 尝试使用OLEDB读取Excel
        return ImportFromExcelUsingOleDb(filePath);
    }

    /// <summary>
    /// 使用OLEDB从Excel导入
    /// </summary>
    private DataTable ImportFromExcelUsingOleDb(string filePath)
    {
        var connectionString = GetExcelConnectionString(filePath);
        var dataTable = new DataTable();

        using var connection = new OleDbConnection(connectionString);
        connection.Open();

        // 获取第一个工作表名称
        var schemaTable = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
        if (schemaTable == null || schemaTable.Rows.Count == 0)
        {
            throw new Exception("Excel文件中没有找到工作表");
        }

        var sheetName = schemaTable.Rows[0]["TABLE_NAME"].ToString();

        // 读取数据
        var sql = $"SELECT * FROM [{sheetName}]";
        using var adapter = new OleDbDataAdapter(sql, connection);
        adapter.Fill(dataTable);

        return dataTable;
    }

    /// <summary>
    /// 获取Excel连接字符串
    /// </summary>
    private string GetExcelConnectionString(string filePath)
    {
        // 根据文件扩展名选择不同的连接字符串
        if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            // Excel 2007+ 格式
            return $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={filePath};Extended Properties='Excel 12.0 Xml;HDR=YES;IMEX=1;'";
        }
        else
        {
            // Excel 97-2003 格式
            return $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={filePath};Extended Properties='Excel 8.0;HDR=YES;IMEX=1;'";
        }
    }

    #endregion

    #region CSV导入

    /// <summary>
    /// 从CSV文件导入数据
    /// </summary>
    public DataTable ImportFromCsv(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("文件不存在", filePath);
        }

        var dataTable = new DataTable();
        var lines = File.ReadAllLines(filePath, Encoding.UTF8);

        if (lines.Length == 0)
        {
            return dataTable;
        }

        // 解析表头
        var headers = ParseCsvLine(lines[0]);
        foreach (var header in headers)
        {
            dataTable.Columns.Add(header, typeof(string));
        }

        // 解析数据行
        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            var row = dataTable.NewRow();

            for (int j = 0; j < Math.Min(values.Count, headers.Count); j++)
            {
                row[j] = values[j];
            }

            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    /// <summary>
    /// 解析CSV行
    /// </summary>
    private List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 转义的引号
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    // 切换引号状态
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // 字段分隔符
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        // 添加最后一个字段
        values.Add(currentValue.ToString());

        return values;
    }

    #endregion

    #region 数据转换

    /// <summary>
    /// 将DataGridView转换为DataTable
    /// </summary>
    public DataTable ConvertDataGridViewToDataTable(DataGridView dgv)
    {
        var dataTable = new DataTable();

        // 添加列
        foreach (DataGridViewColumn col in dgv.Columns)
        {
            if (col.Visible)
            {
                dataTable.Columns.Add(col.HeaderText, typeof(string));
            }
        }

        // 添加行
        foreach (DataGridViewRow row in dgv.Rows)
        {
            if (!row.IsNewRow)
            {
                var dataRow = dataTable.NewRow();
                int colIndex = 0;

                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Visible)
                    {
                        dataRow[colIndex] = row.Cells[col.Index].Value?.ToString() ?? "";
                        colIndex++;
                    }
                }

                dataTable.Rows.Add(dataRow);
            }
        }

        return dataTable;
    }

    /// <summary>
    /// 转换数据类型
    /// </summary>
    public T? ConvertValue<T>(object? value, T? defaultValue = default)
    {
        if (value == null || value == DBNull.Value)
            return defaultValue;

        try
        {
            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(int))
            {
                return (T)(object)Convert.ToInt32(value);
            }
            else if (underlyingType == typeof(decimal))
            {
                return (T)(object)Convert.ToDecimal(value);
            }
            else if (underlyingType == typeof(double))
            {
                return (T)(object)Convert.ToDouble(value);
            }
            else if (underlyingType == typeof(DateTime))
            {
                return (T)(object)Convert.ToDateTime(value);
            }
            else if (underlyingType == typeof(bool))
            {
                return (T)(object)Convert.ToBoolean(value);
            }
            else if (underlyingType == typeof(string))
            {
                return (T)(object)value.ToString()!;
            }
            else
            {
                return (T)Convert.ChangeType(value, underlyingType);
            }
        }
        catch
        {
            return defaultValue;
        }
    }

    #endregion

    #region 列标题映射

    /// <summary>
    /// 列标题映射配置
    /// </summary>
    public class ColumnMapping
    {
        public string SourceColumn { get; set; } = string.Empty;
        public string TargetColumn { get; set; } = string.Empty;
        public Type DataType { get; set; } = typeof(string);
        public bool IsRequired { get; set; } = false;
        public Func<object?, object?>? Transform { get; set; }
    }

    /// <summary>
    /// 应用列标题映射
    /// </summary>
    public DataTable ApplyColumnMapping(DataTable sourceData, List<ColumnMapping> mappings)
    {
        var result = new DataTable();

        // 创建目标列
        foreach (var mapping in mappings)
        {
            result.Columns.Add(mapping.TargetColumn, mapping.DataType);
        }

        // 转换数据
        foreach (DataRow sourceRow in sourceData.Rows)
        {
            var targetRow = result.NewRow();

            foreach (var mapping in mappings)
            {
                var value = sourceRow[mapping.SourceColumn];

                // 应用转换函数
                if (mapping.Transform != null)
                {
                    value = mapping.Transform(value);
                }

                // 类型转换
                if (value != null && value != DBNull.Value)
                {
                    try
                    {
                        targetRow[mapping.TargetColumn] = Convert.ChangeType(value, mapping.DataType);
                    }
                    catch
                    {
                        // 转换失败时设置默认值
                        targetRow[mapping.TargetColumn] = DBNull.Value;
                    }
                }
                else if (mapping.IsRequired)
                {
                    throw new Exception($"必填字段 '{mapping.SourceColumn}' 为空");
                }
            }

            result.Rows.Add(targetRow);
        }

        return result;
    }

    #endregion

    #region 导入验证和错误报告

    /// <summary>
    /// 导入结果
    /// </summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public List<ImportError> Errors { get; set; } = new();
        public DataTable? Data { get; set; }
    }

    /// <summary>
    /// 导入错误信息
    /// </summary>
    public class ImportError
    {
        public int RowIndex { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string? OriginalValue { get; set; }
    }

    /// <summary>
    /// 验证导入数据
    /// </summary>
    public ImportResult ValidateImportData(DataTable data, List<ColumnValidationRule> rules)
    {
        var result = new ImportResult
        {
            TotalCount = data.Rows.Count,
            Data = data
        };

        for (int i = 0; i < data.Rows.Count; i++)
        {
            var row = data.Rows[i];
            bool rowValid = true;

            foreach (var rule in rules)
            {
                var value = row[rule.ColumnName];
                var validation = rule.Validate(value);

                if (!validation.IsValid)
                {
                    result.Errors.Add(new ImportError
                    {
                        RowIndex = i + 1, // 1-based row index
                        ColumnName = rule.ColumnName,
                        ErrorMessage = validation.ErrorMessage,
                        OriginalValue = value?.ToString()
                    });
                    rowValid = false;
                }
            }

            if (rowValid)
            {
                result.SuccessCount++;
            }
            else
            {
                result.FailCount++;
            }
        }

        result.Success = result.FailCount == 0;
        return result;
    }

    #endregion

    #region 模板生成

    /// <summary>
    /// 生成导入模板
    /// </summary>
    public void GenerateImportTemplate(string filePath, List<TemplateColumn> columns)
    {
        var dataTable = new DataTable();

        // 添加列
        foreach (var col in columns)
        {
            dataTable.Columns.Add(col.HeaderText, typeof(string));
        }

        // 添加示例数据行
        var exampleRow = dataTable.NewRow();
        for (int i = 0; i < columns.Count; i++)
        {
            exampleRow[i] = columns[i].ExampleValue;
        }
        dataTable.Rows.Add(exampleRow);

        // 导出为Excel（实际是CSV格式）
        ExportToCsv(dataTable, filePath, true);
    }

    /// <summary>
    /// 模板列定义
    /// </summary>
    public class TemplateColumn
    {
        public string HeaderText { get; set; } = string.Empty;
        public string ExampleValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = false;
    }

    #endregion
}

/// <summary>
/// 列验证规则
/// </summary>
public class ColumnValidationRule
{
    public string ColumnName { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public Type? DataType { get; set; }
    public int? MaxLength { get; set; }
    public string? RegexPattern { get; set; }
    public Func<object?, bool>? CustomValidator { get; set; }
    public string? CustomErrorMessage { get; set; }

    public ValidationResult Validate(object? value)
    {
        // 必填验证
        if (IsRequired && (value == null || string.IsNullOrWhiteSpace(value.ToString())))
        {
            return ValidationResult.Fail($"字段 '{ColumnName}' 为必填项");
        }

        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success();
        }

        var stringValue = value.ToString()!;

        // 数据类型验证
        if (DataType != null)
        {
            try
            {
                Convert.ChangeType(value, DataType);
            }
            catch
            {
                return ValidationResult.Fail($"字段 '{ColumnName}' 的数据类型不正确，应为 {DataType.Name}");
            }
        }

        // 长度验证
        if (MaxLength.HasValue && stringValue.Length > MaxLength.Value)
        {
            return ValidationResult.Fail($"字段 '{ColumnName}' 长度不能超过 {MaxLength.Value} 个字符");
        }

        // 正则表达式验证
        if (!string.IsNullOrEmpty(RegexPattern))
        {
            if (!Regex.IsMatch(stringValue, RegexPattern))
            {
                return ValidationResult.Fail(CustomErrorMessage ?? $"字段 '{ColumnName}' 格式不正确");
            }
        }

        // 自定义验证
        if (CustomValidator != null && !CustomValidator(value))
        {
            return ValidationResult.Fail(CustomErrorMessage ?? $"字段 '{ColumnName}' 验证失败");
        }

        return ValidationResult.Success();
    }
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Fail(string message) => new() { IsValid = false, ErrorMessage = message };
}
