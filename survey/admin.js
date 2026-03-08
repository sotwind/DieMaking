// 后台管理脚本
document.addEventListener('DOMContentLoaded', function() {
    
    // ========== 全局变量 ==========
    let submissions = [];
    const scoreNames = {
        quality: '产品质量',
        service: '服务态度',
        delivery: '配送效率',
        overall: '整体体验'
    };

    // ========== 初始化 ==========
    loadSubmissions();
    setupEventListeners();

    // ========== 数据加载 ==========
    function loadSubmissions() {
        const data = localStorage.getItem('survey_submissions');
        submissions = JSON.parse(data || '[]');
        renderAll();
    }

    function renderAll() {
        renderStats();
        renderScoreBars();
        renderTable();
    }

    // ========== 统计渲染 ==========
    function renderStats() {
        const total = submissions.length;
        const totalImages = submissions.reduce((sum, s) => sum + (s.imageCount || 0), 0);
        
        let totalScoreSum = 0;
        let totalAvgSum = 0;

        submissions.forEach(s => {
            totalScoreSum += s.totalScore || 0;
            totalAvgSum += parseFloat(s.averageScore) || 0;
        });

        const avgOverall = total > 0 ? (totalScoreSum / total).toFixed(1) : 0;
        const avgScore = total > 0 ? (totalAvgSum / total).toFixed(1) : 0;

        document.getElementById('totalSubmissions').textContent = total;
        document.getElementById('avgOverall').textContent = avgOverall;
        document.getElementById('avgScore').textContent = avgScore;
        document.getElementById('totalImages').textContent = totalImages;
    }

    // ========== 评分柱状图 ==========
    function renderScoreBars() {
        if (submissions.length === 0) {
            ['Quality', 'Service', 'Delivery', 'Overall'].forEach(key => {
                document.getElementById(`bar${key}`).style.width = '0%';
                document.getElementById(`val${key}`).textContent = '0';
            });
            return;
        }

        const sums = { quality: 0, service: 0, delivery: 0, overall: 0 };
        
        submissions.forEach(s => {
            if (s.scores) {
                sums.quality += s.scores.quality || 0;
                sums.service += s.scores.service || 0;
                sums.delivery += s.scores.delivery || 0;
                sums.overall += s.scores.overall || 0;
            }
        });

        const avgs = {
            quality: (sums.quality / submissions.length).toFixed(1),
            service: (sums.service / submissions.length).toFixed(1),
            delivery: (sums.delivery / submissions.length).toFixed(1),
            overall: (sums.overall / submissions.length).toFixed(1)
        };

        // 更新柱状图
        ['quality', 'service', 'delivery', 'overall'].forEach(key => {
            const capitalized = key.charAt(0).toUpperCase() + key.slice(1);
            const percentage = avgs[key];
            document.getElementById(`bar${capitalized}`).style.width = `${percentage}%`;
            document.getElementById(`val${capitalized}`).textContent = avgs[key];
        });
    }

    // ========== 表格渲染 ==========
    function renderTable() {
        const tbody = document.getElementById('tableBody');
        const emptyState = document.getElementById('emptyState');
        const table = document.getElementById('dataTable');

        if (submissions.length === 0) {
            tbody.innerHTML = '';
            table.style.display = 'none';
            emptyState.classList.add('show');
            return;
        }

        table.style.display = 'table';
        emptyState.classList.remove('show');

        // 按提交时间倒序排序
        const sorted = [...submissions].sort((a, b) => {
            return new Date(b.submitTime) - new Date(a.submitTime);
        });

        tbody.innerHTML = sorted.map((item, index) => {
            const avgClass = item.averageScore >= 80 ? 'good' : 
                            item.averageScore >= 60 ? 'average' : 'bad';
            
            return `
                <tr>
                    <td>${item.id || 'N/A'}</td>
                    <td>${escapeHtml(item.name || '-')}</td>
                    <td>${formatContact(item)}</td>
                    <td class="score-cell ${getScoreClass(item.scores?.quality)}">${item.scores?.quality || 0}</td>
                    <td class="score-cell ${getScoreClass(item.scores?.service)}">${item.scores?.service || 0}</td>
                    <td class="score-cell ${getScoreClass(item.scores?.delivery)}">${item.scores?.delivery || 0}</td>
                    <td class="score-cell ${getScoreClass(item.scores?.overall)}">${item.scores?.overall || 0}</td>
                    <td class="score-cell"><strong>${item.totalScore || 0}</strong></td>
                    <td class="score-cell ${avgClass}">${item.averageScore || 0}</td>
                    <td><span class="image-count">${item.imageCount || 0} 张</span></td>
                    <td>${formatDate(item.submitTime)}</td>
                    <td>
                        <button class="btn-view" onclick="viewDetail('${item.id}')">查看</button>
                        <button class="btn-delete" onclick="deleteSubmission('${item.id}')">删除</button>
                    </td>
                </tr>
            `;
        }).join('');
    }

    function formatContact(item) {
        const parts = [];
        if (item.phone) parts.push(item.phone);
        if (item.email) parts.push(item.email);
        return parts.join('<br>') || '-';
    }

    function getScoreClass(score) {
        if (!score) return '';
        if (score >= 80) return 'good';
        if (score >= 60) return 'average';
        return 'bad';
    }

    function formatDate(dateStr) {
        if (!dateStr) return '-';
        const date = new Date(dateStr);
        return date.toLocaleString('zh-CN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // ========== 查看详情 ==========
    window.viewDetail = function(id) {
        const item = submissions.find(s => s.id === id);
        if (!item) return;

        const content = document.getElementById('detailContent');
        content.innerHTML = `
            <div class="detail-row">
                <span class="detail-label">提交编号:</span>
                <span class="detail-value">${item.id}</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">姓名:</span>
                <span class="detail-value">${escapeHtml(item.name || '-')}</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">邮箱:</span>
                <span class="detail-value">${item.email || '-'}</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">手机号:</span>
                <span class="detail-value">${item.phone || '-'}</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">各项评分:</span>
                <span class="detail-value">
                    <span class="detail-score">产品质量: ${item.scores?.quality || 0}</span>
                    <span class="detail-score">服务态度: ${item.scores?.service || 0}</span>
                    <span class="detail-score">配送效率: ${item.scores?.delivery || 0}</span>
                    <span class="detail-score">整体体验: ${item.scores?.overall || 0}</span>
                </span>
            </div>
            <div class="detail-row">
                <span class="detail-label">总分:</span>
                <span class="detail-value"><strong>${item.totalScore || 0}</strong> / 400</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">平均分:</span>
                <span class="detail-value"><strong>${item.averageScore || 0}</strong></span>
            </div>
            <div class="detail-row">
                <span class="detail-label">提交时间:</span>
                <span class="detail-value">${formatDate(item.submitTime)}</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">上传图片:</span>
                <span class="detail-value">${item.imageCount || 0} 张</span>
            </div>
            <div class="detail-row">
                <span class="detail-label">意见与建议:</span>
            </div>
            <div class="detail-comment">${escapeHtml(item.comment || '无')}</div>
        `;

        document.getElementById('detailModal').classList.add('active');
    };

    // ========== 删除提交 ==========
    window.deleteSubmission = function(id) {
        if (!confirm('确定要删除这条提交记录吗？此操作不可恢复。')) {
            return;
        }

        submissions = submissions.filter(s => s.id !== id);
        localStorage.setItem('survey_submissions', JSON.stringify(submissions));
        renderAll();
    };

    // ========== 导出数据 ==========
    function exportToExcel() {
        if (submissions.length === 0) {
            alert('暂无数据可导出！');
            return;
        }

        // 生成 CSV 内容
        const headers = ['编号', '姓名', '邮箱', '手机号', '产品质量', '服务态度', 
                        '配送效率', '整体体验', '总分', '平均分', '图片数', '评论', '提交时间'];
        
        const rows = submissions.map(s => [
            s.id || '',
            s.name || '',
            s.email || '',
            s.phone || '',
            s.scores?.quality || 0,
            s.scores?.service || 0,
            s.scores?.delivery || 0,
            s.scores?.overall || 0,
            s.totalScore || 0,
            s.averageScore || 0,
            s.imageCount || 0,
            `"${(s.comment || '').replace(/"/g, '""')}"`,
            s.submitTime || ''
        ]);

        // 添加统计行
        rows.push([]);
        rows.push(['统计', '', '', '', 
            calcAvg('quality'), calcAvg('service'), 
            calcAvg('delivery'), calcAvg('overall'),
            submissions.reduce((sum, s) => sum + (s.totalScore || 0), 0),
            (submissions.reduce((sum, s) => sum + parseFloat(s.averageScore || 0), 0) / submissions.length).toFixed(1),
            submissions.reduce((sum, s) => sum + (s.imageCount || 0), 0),
            '', ''
        ]);

        function calcAvg(key) {
            if (submissions.length === 0) return 0;
            const sum = submissions.reduce((acc, s) => acc + (s.scores?.[key] || 0), 0);
            return (sum / submissions.length).toFixed(1);
        }

        const csvContent = [
            headers.join(','),
            ...rows.map(row => row.join(','))
        ].join('\n');

        // 创建下载
        const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', `调查数据_${new Date().toISOString().slice(0, 10)}.csv`);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    // ========== 清空数据 ==========
    function clearAllData() {
        if (submissions.length === 0) {
            alert('暂无数据可清空！');
            return;
        }

        if (!confirm('确定要清空所有提交数据吗？此操作不可恢复！')) {
            return;
        }

        submissions = [];
        localStorage.removeItem('survey_submissions');
        renderAll();
    }

    // ========== 事件监听 ==========
    function setupEventListeners() {
        // 导出按钮
        document.getElementById('exportBtn').addEventListener('click', exportToExcel);
        
        // 清空按钮
        document.getElementById('clearBtn').addEventListener('click', clearAllData);
        
        // 刷新按钮
        document.getElementById('refreshBtn').addEventListener('click', loadSubmissions);
        
        // 关闭弹窗
        document.querySelector('.close-modal').addEventListener('click', function() {
            document.getElementById('detailModal').classList.remove('active');
        });

        // 点击外部关闭
        document.getElementById('detailModal').addEventListener('click', function(e) {
            if (e.target === this) {
                this.classList.remove('active');
            }
        });
    }

    // ========== 初始化提示 ==========
    console.log('后台管理已加载 ✓');
    console.log(`当前数据：${submissions.length} 条提交记录`);
});
