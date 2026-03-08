using System.Data;

namespace DieMaking.Forms.Common;

/// <summary>
/// 导入预览窗体
/// </summary>
public class ImportPreviewForm : Form
{
    private DataGridView _dgvPreview = null!;
    private Label _lblInfo = null!;
    private DataTable _data = null!;

    public ImportPreviewForm(DataTable data, string title)
    {
        _data = data;
        InitializeComponent();
        this.Text = title;
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(900, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.WindowState = FormWindowState.Maximized;

        // 顶部信息面板
        var panelTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(10)
        };

        _lblInfo = new Label
        {
            Location = new Point(10, 10),
            Size = new Size(600, 40),
            Font = new Font("微软雅黑", 10, FontStyle.Bold),
            ForeColor = Color.DarkBlue
        };

        var btnConfirm = new Button
        {
            Text = "确认导入",
            Location = new Point(650, 15),
            Size = new Size(100, 30)
        };
        btnConfirm.Click += (s, e) =>
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        };

        var btnCancel = new Button
        {
            Text = "取消",
            Location = new Point(760, 15),
            Size = new Size(100, 30),
            DialogResult = DialogResult.Cancel
        };
        btnCancel.Click += (s, e) =>
        {
            this.Close();
        };

        panelTop.Controls.Add(_lblInfo);
        panelTop.Controls.Add(btnConfirm);
        panelTop.Controls.Add(btnCancel);

        // 数据预览表格
        _dgvPreview = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.AliceBlue }
        };

        this.Controls.Add(_dgvPreview);
        this.Controls.Add(panelTop);
    }

    private void LoadData()
    {
        _lblInfo.Text = $"共 {_data.Rows.Count} 条数据，{_data.Columns.Count} 个字段\n请确认数据无误后点击【确认导入】按钮";

        // 设置列
        _dgvPreview.Columns.Clear();
        foreach (DataColumn col in _data.Columns)
        {
            _dgvPreview.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = col.ColumnName,
                HeaderText = col.ColumnName,
                DataPropertyName = col.ColumnName
            });
        }

        // 添加数据行
        foreach (DataRow row in _data.Rows)
        {
            var rowIndex = _dgvPreview.Rows.Add();
            for (int i = 0; i < _data.Columns.Count; i++)
            {
                _dgvPreview.Rows[rowIndex].Cells[i].Value = row[i]?.ToString();
            }
        }
    }
}
