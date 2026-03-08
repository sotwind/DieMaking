using System.ComponentModel;

namespace DieMaking.Helpers;

/// <summary>
/// 虚拟模式 DataGridView - 支持大数据量的高效显示
/// </summary>
public class VirtualDataGridView : DataGridView
{
    private IList? _virtualDataSource;
    private Type? _itemType;
    private readonly Dictionary<int, object?> _cellValueCache = new();
    private int _cacheSize = 100;
    private int _firstCachedRow = -1;
    private int _lastCachedRow = -1;

    /// <summary>
    /// 虚拟数据源
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IList? VirtualDataSource
    {
        get => _virtualDataSource;
        set
        {
            _virtualDataSource = value;
            _itemType = value?.GetType().GetGenericArguments().FirstOrDefault();
            _cellValueCache.Clear();
            
            if (value != null)
            {
                this.RowCount = value.Count;
                this.VirtualMode = true;
            }
            else
            {
                this.RowCount = 0;
                this.VirtualMode = false;
            }
            
            Invalidate();
        }
    }

    /// <summary>
    /// 缓存大小
    /// </summary>
    [DefaultValue(100)]
    public int CacheSize
    {
        get => _cacheSize;
        set
        {
            _cacheSize = value;
            _cellValueCache.Clear();
        }
    }

    /// <summary>
    /// 是否启用异步加载
    /// </summary>
    [DefaultValue(true)]
    public bool EnableAsyncLoading { get; set; } = true;

    /// <summary>
    /// 数据加载中事件
    /// </summary>
    public event EventHandler<VirtualDataLoadingEventArgs>? DataLoading;

    /// <summary>
    /// 数据加载完成事件
    /// </summary>
    public event EventHandler<VirtualDataLoadedEventArgs>? DataLoaded;

    /// <summary>
    /// 获取单元格值事件（允许外部自定义）
    /// </summary>
    public event EventHandler<GetCellValueEventArgs>? GetCellValueOverride;

    public VirtualDataGridView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        // 启用虚拟模式
        this.VirtualMode = true;
        
        // 基础性能设置
        this.AutoGenerateColumns = false;
        this.AllowUserToAddRows = false;
        this.AllowUserToDeleteRows = false;
        this.AllowUserToResizeRows = false;
        this.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
        this.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        this.ReadOnly = true;
        
        // 启用双缓冲
        EnableDoubleBuffering();

        // 订阅虚拟模式事件
        this.CellValueNeeded += VirtualDataGridView_CellValueNeeded;
        this.CellValuePushed += VirtualDataGridView_CellValuePushed;
        this.NewRowNeeded += VirtualDataGridView_NewRowNeeded;
        this.RowValidated += VirtualDataGridView_RowValidated;
        this.RowDirtyStateNeeded += VirtualDataGridView_RowDirtyStateNeeded;
        this.CancelRowEdit += VirtualDataGridView_CancelRowEdit;
        this.UserDeletingRow += VirtualDataGridView_UserDeletingRow;
        this.Scroll += VirtualDataGridView_Scroll;
    }

    /// <summary>
    /// 启用双缓冲
    /// </summary>
    private void EnableDoubleBuffering()
    {
        var property = typeof(DataGridView).GetProperty("DoubleBuffered",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        property?.SetValue(this, true, null);
    }

    /// <summary>
    /// 异步加载数据
    /// </summary>
    public async Task LoadDataAsync<T>(Func<Task<List<T>>> dataLoader, IProgress<LoadingProgress>? progress = null)
    {
        DataLoading?.Invoke(this, new VirtualDataLoadingEventArgs { IsLoading = true });

        try
        {
            progress?.Report(new LoadingProgress { PercentComplete = 0, Message = "正在加载数据..." });

            var data = await dataLoader();

            progress?.Report(new LoadingProgress { PercentComplete = 50, Message = "正在绑定数据..." });

            // 在UI线程更新数据
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    VirtualDataSource = data;
                }));
            }
            else
            {
                VirtualDataSource = data;
            }

            progress?.Report(new LoadingProgress { PercentComplete = 100, Message = "加载完成" });

            DataLoaded?.Invoke(this, new VirtualDataLoadedEventArgs 
            { 
                RowCount = data.Count, 
                Success = true 
            });
        }
        catch (Exception ex)
        {
            DataLoaded?.Invoke(this, new VirtualDataLoadedEventArgs 
            { 
                RowCount = 0, 
                Success = false, 
                ErrorMessage = ex.Message 
            });
        }
        finally
        {
            DataLoading?.Invoke(this, new VirtualDataLoadingEventArgs { IsLoading = false });
        }
    }

    /// <summary>
    /// 同步加载数据
    /// </summary>
    public void LoadData<T>(List<T> data)
    {
        DataLoading?.Invoke(this, new VirtualDataLoadingEventArgs { IsLoading = true });

        try
        {
            VirtualDataSource = data;

            DataLoaded?.Invoke(this, new VirtualDataLoadedEventArgs 
            { 
                RowCount = data.Count, 
                Success = true 
            });
        }
        catch (Exception ex)
        {
            DataLoaded?.Invoke(this, new VirtualDataLoadedEventArgs 
            { 
                RowCount = 0, 
                Success = false, 
                ErrorMessage = ex.Message 
            });
        }
        finally
        {
            DataLoading?.Invoke(this, new VirtualDataLoadingEventArgs { IsLoading = false });
        }
    }

    /// <summary>
    /// 获取指定行的数据对象
    /// </summary>
    public T? GetRowData<T>(int rowIndex) where T : class
    {
        if (_virtualDataSource == null || rowIndex < 0 || rowIndex >= _virtualDataSource.Count)
            return null;

        return _virtualDataSource[rowIndex] as T;
    }

    /// <summary>
    /// 获取选中的数据对象
    /// </summary>
    public List<T> GetSelectedRowsData<T>() where T : class
    {
        var result = new List<T>();
        foreach (DataGridViewRow row in this.SelectedRows)
        {
            var data = GetRowData<T>(row.Index);
            if (data != null)
            {
                result.Add(data);
            }
        }
        return result;
    }

    /// <summary>
    /// 刷新指定行
    /// </summary>
    public void RefreshRow(int rowIndex)
    {
        if (rowIndex >= 0 && rowIndex < this.RowCount)
        {
            // 清除该行的缓存
            for (int i = 0; i < this.Columns.Count; i++)
            {
                _cellValueCache.Remove(rowIndex * 1000 + i);
            }
            this.InvalidateRow(rowIndex);
        }
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public void ClearCache()
    {
        _cellValueCache.Clear();
        _firstCachedRow = -1;
        _lastCachedRow = -1;
    }

    #region 虚拟模式事件处理

    private void VirtualDataGridView_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (_virtualDataSource == null || e.RowIndex < 0 || e.RowIndex >= _virtualDataSource.Count)
        {
            e.Value = null;
            return;
        }

        // 检查是否有外部自定义获取值
        if (GetCellValueOverride != null)
        {
            var args = new GetCellValueEventArgs
            {
                RowIndex = e.RowIndex,
                ColumnIndex = e.ColumnIndex,
                ColumnName = this.Columns[e.ColumnIndex].DataPropertyName ?? "",
                RowData = _virtualDataSource[e.RowIndex]
            };
            GetCellValueOverride(this, args);
            if (args.Handled)
            {
                e.Value = args.Value;
                return;
            }
        }

        // 使用缓存键
        var cacheKey = e.RowIndex * 1000 + e.ColumnIndex;

        // 检查缓存
        if (_cellValueCache.TryGetValue(cacheKey, out var cachedValue))
        {
            e.Value = cachedValue;
            return;
        }

        // 获取数据对象
        var item = _virtualDataSource[e.RowIndex];
        if (item == null)
        {
            e.Value = null;
            return;
        }

        // 获取列绑定的属性名
        var column = this.Columns[e.ColumnIndex];
        var propertyName = column.DataPropertyName;

        if (string.IsNullOrEmpty(propertyName))
        {
            e.Value = null;
            return;
        }

        // 获取属性值
        var property = item.GetType().GetProperty(propertyName);
        if (property != null)
        {
            var value = property.GetValue(item);
            
            // 应用格式
            if (column.DefaultCellStyle.Format != null && value != null)
            {
                try
                {
                    value = string.Format($"{{0:{column.DefaultCellStyle.Format}}}", value);
                }
                catch { }
            }

            e.Value = value;

            // 添加到缓存
            if (_cellValueCache.Count < _cacheSize * this.Columns.Count)
            {
                _cellValueCache[cacheKey] = value;
            }
        }
        else
        {
            e.Value = null;
        }
    }

    private void VirtualDataGridView_CellValuePushed(object? sender, DataGridViewCellValueEventArgs e)
    {
        // 处理单元格值推送（编辑模式）
        if (_virtualDataSource == null || e.RowIndex < 0 || e.RowIndex >= _virtualDataSource.Count)
            return;

        var item = _virtualDataSource[e.RowIndex];
        if (item == null) return;

        var column = this.Columns[e.ColumnIndex];
        var propertyName = column.DataPropertyName;

        if (string.IsNullOrEmpty(propertyName)) return;

        var property = item.GetType().GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            try
            {
                var value = Convert.ChangeType(e.Value, property.PropertyType);
                property.SetValue(item, value);

                // 更新缓存
                var cacheKey = e.RowIndex * 1000 + e.ColumnIndex;
                _cellValueCache[cacheKey] = value;
            }
            catch { }
        }
    }

    private void VirtualDataGridView_NewRowNeeded(object? sender, DataGridViewRowEventArgs e)
    {
        // 新行需要时触发
    }

    private void VirtualDataGridView_RowValidated(object? sender, DataGridViewCellEventArgs e)
    {
        // 行验证完成
    }

    private void VirtualDataGridView_RowDirtyStateNeeded(object? sender, QuestionEventArgs e)
    {
        // 检查行是否需要保存
        e.Response = false;
    }

    private void VirtualDataGridView_CancelRowEdit(object? sender, QuestionEventArgs e)
    {
        // 取消行编辑
    }

    private void VirtualDataGridView_UserDeletingRow(object? sender, DataGridViewRowCancelEventArgs e)
    {
        // 用户删除行
    }

    private void VirtualDataGridView_Scroll(object? sender, ScrollEventArgs e)
    {
        // 滚动时预加载数据
        if (e.Type == ScrollEventType.ThumbPosition || e.Type == ScrollEventType.EndScroll)
        {
            // 清除旧缓存，准备新缓存
            if (_cellValueCache.Count > _cacheSize * this.Columns.Count * 2)
            {
                _cellValueCache.Clear();
            }
        }
    }

    #endregion

    protected override void OnColumnAdded(DataGridViewColumnEventArgs e)
    {
        base.OnColumnAdded(e);
        
        // 设置默认列样式
        e.Column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        e.Column.HeaderCell.Style.Font = new Font(this.Font, FontStyle.Bold);
    }
}

/// <summary>
/// 虚拟数据加载事件参数
/// </summary>
public class VirtualDataLoadingEventArgs : EventArgs
{
    /// <summary>是否正在加载</summary>
    public bool IsLoading { get; set; }
}

/// <summary>
/// 虚拟数据加载完成事件参数
/// </summary>
public class VirtualDataLoadedEventArgs : EventArgs
{
    /// <summary>行数</summary>
    public int RowCount { get; set; }
    
    /// <summary>是否成功</summary>
    public bool Success { get; set; }
    
    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 获取单元格值事件参数
/// </summary>
public class GetCellValueEventArgs : EventArgs
{
    /// <summary>行索引</summary>
    public int RowIndex { get; set; }
    
    /// <summary>列索引</summary>
    public int ColumnIndex { get; set; }
    
    /// <summary>列名</summary>
    public string ColumnName { get; set; } = string.Empty;
    
    /// <summary>行数据对象</summary>
    public object? RowData { get; set; }
    
    /// <summary>值</summary>
    public object? Value { get; set; }
    
    /// <summary>是否已处理</summary>
    public bool Handled { get; set; }
}

/// <summary>
/// 加载进度
/// </summary>
public class LoadingProgress
{
    /// <summary>完成百分比</summary>
    public int PercentComplete { get; set; }
    
    /// <summary>消息</summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>已加载行数</summary>
    public int LoadedRows { get; set; }
    
    /// <summary>总行数</summary>
    public int TotalRows { get; set; }
}

/// <summary>
/// 带加载进度的虚拟DataGridView
/// </summary>
public class VirtualDataGridViewWithProgress : VirtualDataGridView
{
    private Panel? _loadingPanel;
    private ProgressBar? _loadingProgressBar;
    private Label? _loadingLabel;

    public VirtualDataGridViewWithProgress()
    {
        InitializeLoadingPanel();
        this.DataLoading += VirtualDataGridViewWithProgress_DataLoading;
        this.DataLoaded += VirtualDataGridViewWithProgress_DataLoaded;
    }

    private void InitializeLoadingPanel()
    {
        _loadingPanel = new Panel
        {
            BackColor = Color.FromArgb(200, 255, 255, 255),
            BorderStyle = BorderStyle.FixedSingle,
            Size = new Size(300, 80),
            Visible = false
        };

        _loadingProgressBar = new ProgressBar
        {
            Location = new Point(20, 20),
            Size = new Size(260, 20),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30
        };

        _loadingLabel = new Label
        {
            Text = "正在加载数据...",
            Location = new Point(20, 50),
            Size = new Size(260, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _loadingPanel.Controls.Add(_loadingProgressBar);
        _loadingPanel.Controls.Add(_loadingLabel);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (_loadingPanel != null && !this.Controls.Contains(_loadingPanel))
        {
            this.Controls.Add(_loadingPanel);
            _loadingPanel.Location = new Point(
                (this.Width - _loadingPanel.Width) / 2,
                (this.Height - _loadingPanel.Height) / 2
            );
            _loadingPanel.BringToFront();
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (_loadingPanel != null)
        {
            _loadingPanel.Location = new Point(
                (this.Width - _loadingPanel.Width) / 2,
                (this.Height - _loadingPanel.Height) / 2
            );
        }
    }

    private void VirtualDataGridViewWithProgress_DataLoading(object? sender, VirtualDataLoadingEventArgs e)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => ShowLoading(e.IsLoading)));
        }
        else
        {
            ShowLoading(e.IsLoading);
        }
    }

    private void VirtualDataGridViewWithProgress_DataLoaded(object? sender, VirtualDataLoadedEventArgs e)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => ShowLoading(false)));
        }
        else
        {
            ShowLoading(false);
        }
    }

    private void ShowLoading(bool show)
    {
        if (_loadingPanel != null)
        {
            _loadingPanel.Visible = show;
            if (show)
            {
                _loadingPanel.BringToFront();
            }
        }
    }

    /// <summary>
    /// 更新加载进度
    /// </summary>
    public void UpdateLoadingProgress(LoadingProgress progress)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action<LoadingProgress>(UpdateLoadingProgress), progress);
            return;
        }

        if (_loadingLabel != null)
        {
            _loadingLabel.Text = progress.Message;
        }

        if (_loadingProgressBar != null && progress.TotalRows > 0)
        {
            _loadingProgressBar.Style = ProgressBarStyle.Continuous;
            _loadingProgressBar.Maximum = progress.TotalRows;
            _loadingProgressBar.Value = Math.Min(progress.LoadedRows, progress.TotalRows);
        }
    }
}
