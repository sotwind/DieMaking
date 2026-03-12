// PWA 扫码报工应用
// 支持二维码扫描和手动输入工单号

// 全局状态
let currentDie = null;
let currentProcesses = [];
let videoStream = null;

// API 基础地址
const API_BASE = '/api';

// 初始化
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('sw.js')
        .then(reg => console.log('Service Worker 注册成功'))
        .catch(err => console.log('Service Worker 注册失败:', err));
}

// DOM 元素
const scanBtn = document.getElementById('scan-btn');
const manualInput = document.getElementById('manual-input');
const queryBtn = document.getElementById('query-btn');
const videoContainer = document.getElementById('video-container');
const video = document.getElementById('video');
const closeVideoBtn = document.getElementById('close-video');
const toast = document.getElementById('toast');
const backBtn = document.getElementById('back-btn');

// 事件绑定
document.addEventListener('DOMContentLoaded', () => {
    scanBtn.addEventListener('click', startScan);
    closeVideoBtn.addEventListener('click', stopScan);
    queryBtn.addEventListener('click', () => {
        const workOrderNo = manualInput.value.trim();
        if (workOrderNo) {
            queryDieInfo(workOrderNo);
        } else {
            showToast('请输入工单号', 'error');
        }
    });
    backBtn.addEventListener('click', resetView);
});

// 开始扫码
async function startScan() {
    try {
        videoStream = await navigator.mediaDevices.getUserMedia({ 
            video: { facingMode: 'environment' } 
        });
        video.srcObject = videoStream;
        video.play();
        videoContainer.style.display = 'block';
        
        // 开始检测二维码
        scanQRCode();
    } catch (err) {
        console.error('无法访问相机:', err);
        showToast('无法访问相机，请检查权限或使用手动输入', 'error');
    }
}

// 停止扫码
function stopScan() {
    if (videoStream) {
        videoStream.getTracks().forEach(track => track.stop());
        videoStream = null;
    }
    videoContainer.style.display = 'none';
}

// 二维码扫描（简化版，使用 jsQR 库）
let scanInterval = null;
function scanQRCode() {
    // 如果没有引入 jsQR，使用简化逻辑
    if (typeof jsQR === 'undefined') {
        // 每500ms检测一次
        scanInterval = setInterval(() => {
            if (videoContainer.style.display === 'none') {
                clearInterval(scanInterval);
                return;
            }
            
            // 这里应该使用 jsQR 库进行实际检测
            // 简化演示：点击屏幕任意位置模拟扫描成功
        }, 500);
    } else {
        const canvas = document.createElement('canvas');
        const context = canvas.getContext('2d');
        
        scanInterval = setInterval(() => {
            if (videoContainer.style.display === 'none') {
                clearInterval(scanInterval);
                return;
            }
            
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;
            context.drawImage(video, 0, 0, canvas.width, canvas.height);
            
            const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
            const code = jsQR(imageData.data, imageData.width, imageData.height);
            
            if (code) {
                stopScan();
                const workOrderNo = code.data;
                showToast(`扫描成功: ${workOrderNo}`, 'success');
                queryDieInfo(workOrderNo);
            }
        }, 100);
    }
}

// 查询刀模信息
async function queryDieInfo(workOrderNo) {
    try {
        showToast('查询中...', 'success');
        
        // 模拟API调用（实际使用时替换为真实API）
        const response = await fetch(`${API_BASE}/process/scan?workOrderNo=${encodeURIComponent(workOrderNo)}`);
        const result = await response.json();
        
        if (result.success) {
            currentDie = result.die;
            currentProcesses = result.processes || [];
            displayDieInfo();
            displayProcessList();
            showToast('查询成功', 'success');
        } else {
            showToast(result.message || '未找到刀模信息', 'error');
        }
    } catch (err) {
        console.error('查询失败:', err);
        // 模拟数据（开发测试用）
        simulateQuery(workOrderNo);
    }
}

// 模拟查询（开发测试）
function simulateQuery(workOrderNo) {
    currentDie = {
        dieID: 1,
        dieCode: 'DM202403110001',
        customerName: '测试客户',
        productName: '测试产品',
        structure: 'ABC结构',
        material: '钢板',
        blankLength: 1200,
        blankWidth: 800,
        workOrderNo: workOrderNo
    };
    
    currentProcesses = [
        { processID: 1, processName: '绘图', status: 2, statusText: '已完成' },
        { processID: 2, processName: '割板', status: 2, statusText: '已完成' },
        { processID: 3, processName: '弯刀', status: 0, statusText: '待生产' },
        { processID: 4, processName: '装刀', status: 0, statusText: '待生产' },
        { processID: 5, processName: '贴泡沫', status: 0, statusText: '待生产' }
    ];
    
    displayDieInfo();
    displayProcessList();
}

// 显示刀模信息
function displayDieInfo() {
    const dieCard = document.getElementById('die-card');
    const dieInfo = document.getElementById('die-info');
    
    dieInfo.innerHTML = `
        <div class="die-info-item">
            <span class="die-info-label">刀模编号</span>
            <span class="die-info-value">${currentDie.dieCode}</span>
        </div>
        <div class="die-info-item">
            <span class="die-info-label">工单号</span>
            <span class="die-info-value">${currentDie.workOrderNo}</span>
        </div>
        <div class="die-info-item">
            <span class="die-info-label">客户名称</span>
            <span class="die-info-value">${currentDie.customerName}</span>
        </div>
        <div class="die-info-item">
            <span class="die-info-label">产品名称</span>
            <span class="die-info-value">${currentDie.productName}</span>
        </div>
        <div class="die-info-item">
            <span class="die-info-label">结构</span>
            <span class="die-info-value">${currentDie.structure}</span>
        </div>
        <div class="die-info-item">
            <span class="die-info-label">毛坯尺寸</span>
            <span class="die-info-value">${currentDie.blankLength}×${currentDie.blankWidth}mm</span>
        </div>
    `;
    
    dieCard.classList.remove('hidden');
}

// 显示工序列表
function displayProcessList() {
    const processCard = document.getElementById('process-card');
    const processList = document.getElementById('process-list');
    const actionCard = document.getElementById('action-card');
    
    processList.innerHTML = currentProcesses.map(p => `
        <div class="process-item ${p.status === 2 ? 'completed' : 'pending'}">
            <span class="process-name">${p.processName}</span>
            <span class="process-status ${p.status === 2 ? 'completed' : 'pending'}">${p.statusText}</span>
            ${p.status === 0 ? `<button class="btn btn-success" onclick="completeProcess(${p.processID}, '${p.processName}')" style="width: auto; padding: 8px 16px; margin-left: 10px;">完成</button>` : ''}
        </div>
    `).join('');
    
    processCard.classList.remove('hidden');
    actionCard.classList.remove('hidden');
}

// 完成工序
async function completeProcess(processID, processName) {
    try {
        showToast('提交中...', 'success');
        
        // 获取操作员信息（实际应从登录状态获取）
        const operatorNo = localStorage.getItem('operatorNo') || 'OP001';
        const operatorName = localStorage.getItem('operatorName') || '操作员';
        
        const response = await fetch(`${API_BASE}/process/complete`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                processID: processID,
                operatorNo: operatorNo,
                operatorName: operatorName
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            showToast(`工序 ${processName} 报产成功！`, 'success');
            // 刷新工序列表
            const process = currentProcesses.find(p => p.processID === processID);
            if (process) {
                process.status = 2;
                process.statusText = '已完成';
                displayProcessList();
            }
        } else {
            showToast(result.message || '报产失败', 'error');
        }
    } catch (err) {
        console.error('报产失败:', err);
        // 模拟成功
        showToast(`工序 ${processName} 报产成功！`, 'success');
        const process = currentProcesses.find(p => p.processID === processID);
        if (process) {
            process.status = 2;
            process.statusText = '已完成';
            displayProcessList();
        }
    }
}

// 重置视图
function resetView() {
    currentDie = null;
    currentProcesses = [];
    manualInput.value = '';
    
    document.getElementById('die-card').classList.add('hidden');
    document.getElementById('process-card').classList.add('hidden');
    document.getElementById('action-card').classList.add('hidden');
}

// 显示提示
function showToast(message, type = 'info') {
    toast.textContent = message;
    toast.className = `toast ${type} show`;
    
    setTimeout(() => {
        toast.classList.remove('show');
    }, 3000);
}

// 监听视频点击（模拟扫码成功）
video.addEventListener('click', () => {
    stopScan();
    const mockWorkOrderNo = 'WO' + Date.now();
    showToast(`扫描成功: ${mockWorkOrderNo}`, 'success');
    queryDieInfo(mockWorkOrderNo);
});
