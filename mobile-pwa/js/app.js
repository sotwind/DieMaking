/**
 * 刀模管理系统 PWA - 主应用逻辑
 */

// 全局状态
const AppState = {
    currentUser: null,
    currentDie: null,
    currentProcesses: [],
    isScanning: false
};

// API 基础地址
const API_BASE_URL = '/api';

// DOM 元素
const Elements = {
    loginPage: document.getElementById('login-page'),
    mainPage: document.getElementById('main-page'),
    loginForm: document.getElementById('login-form'),
    operatorNoInput: document.getElementById('operator-no'),
    operatorNameInput: document.getElementById('operator-name'),
    currentUserSpan: document.getElementById('current-user'),
    logoutBtn: document.getElementById('logout-btn'),
    dieInfoCard: document.getElementById('die-info-card'),
    processSection: document.getElementById('process-section'),
    processList: document.getElementById('process-list'),
    reportResult: document.getElementById('report-result'),
    resultIcon: document.getElementById('result-icon'),
    resultMessage: document.getElementById('result-message'),
    continueBtn: document.getElementById('continue-btn'),
    manualWorkOrderInput: document.getElementById('manual-workorder'),
    queryBtn: document.getElementById('query-btn')
};

// 初始化
function init() {
    // 检查登录状态
    checkLoginStatus();
    
    // 绑定事件
    bindEvents();
    
    // 注册 Service Worker
    registerServiceWorker();
}

// 检查登录状态
function checkLoginStatus() {
    const savedUser = localStorage.getItem('currentUser');
    if (savedUser) {
        AppState.currentUser = JSON.parse(savedUser);
        showMainPage();
    } else {
        showLoginPage();
    }
}

// 绑定事件
function bindEvents() {
    // 登录表单
    Elements.loginForm?.addEventListener('submit', handleLogin);
    
    // 退出按钮
    Elements.logoutBtn?.addEventListener('click', handleLogout);
    
    // 继续按钮
    Elements.continueBtn?.addEventListener('click', hideResult);
    
    // 查询按钮
    Elements.queryBtn?.addEventListener('click', handleManualQuery);
    
    // 手动输入框回车
    Elements.manualWorkOrderInput?.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            handleManualQuery();
        }
    });
}

// 处理登录
function handleLogin(e) {
    e.preventDefault();
    
    const operatorNo = Elements.operatorNoInput.value.trim();
    const operatorName = Elements.operatorNameInput.value.trim();
    
    if (!operatorNo || !operatorName) {
        showToast('请输入工号和姓名');
        return;
    }
    
    // 保存用户信息
    AppState.currentUser = {
        operatorNo,
        operatorName
    };
    localStorage.setItem('currentUser', JSON.stringify(AppState.currentUser));
    
    showMainPage();
}

// 处理退出
function handleLogout() {
    AppState.currentUser = null;
    AppState.currentDie = null;
    AppState.currentProcesses = [];
    localStorage.removeItem('currentUser');
    
    // 清空表单
    Elements.operatorNoInput.value = '';
    Elements.operatorNameInput.value = '';
    Elements.manualWorkOrderInput.value = '';
    
    hideDieInfo();
    showLoginPage();
}

// 显示登录页面
function showLoginPage() {
    Elements.loginPage?.classList.remove('hidden');
    Elements.mainPage?.classList.add('hidden');
}

// 显示主页面
function showMainPage() {
    Elements.loginPage?.classList.add('hidden');
    Elements.mainPage?.classList.remove('hidden');
    
    if (AppState.currentUser) {
        Elements.currentUserSpan.textContent = AppState.currentUser.operatorName;
    }
}

// 处理手动查询
async function handleManualQuery() {
    const workOrderNo = Elements.manualWorkOrderInput.value.trim();
    if (!workOrderNo) {
        showToast('请输入工单号');
        return;
    }
    
    await queryDieByWorkOrder(workOrderNo);
}

// 查询刀模
async function queryDieByWorkOrder(workOrderNo) {
    try {
        showToast('查询中...');
        
        // 调用 API 查询刀模
        const response = await fetch(`${API_BASE_URL}/die/by-workorder?workOrderNo=${encodeURIComponent(workOrderNo)}`);
        
        if (!response.ok) {
            if (response.status === 404) {
                showResult(false, `未找到工单号为 ${workOrderNo} 的刀模`);
                return;
            }
            throw new Error('查询失败');
        }
        
        const die = await response.json();
        AppState.currentDie = die;
        
        // 显示刀模信息
        displayDieInfo(die);
        
        // 加载工序列表
        await loadProcesses(die.dieId);
        
    } catch (error) {
        console.error('查询失败:', error);
        showResult(false, '查询失败，请检查网络连接');
    }
}

// 显示刀模信息
function displayDieInfo(die) {
    document.getElementById('die-code').textContent = die.dieCode || '-';
    document.getElementById('work-order-no').textContent = die.workOrderNo || die.externalOrderID || '-';
    document.getElementById('customer-name').textContent = die.customerName || '-';
    document.getElementById('product-name').textContent = die.productName || '-';
    
    // 状态标签
    const statusBadge = document.getElementById('die-status');
    statusBadge.textContent = die.statusText || '待生产';
    statusBadge.className = 'status-badge ' + (die.status === 2 ? 'status-completed' : 'status-pending');
    
    Elements.dieInfoCard?.classList.remove('hidden');
}

// 隐藏刀模信息
function hideDieInfo() {
    Elements.dieInfoCard?.classList.add('hidden');
    Elements.processSection?.classList.add('hidden');
    AppState.currentDie = null;
    AppState.currentProcesses = [];
}

// 加载工序列表
async function loadProcesses(dieId) {
    try {
        const response = await fetch(`${API_BASE_URL}/die/${dieId}/processes`);
        
        if (!response.ok) {
            throw new Error('加载工序失败');
        }
        
        const processes = await response.json();
        AppState.currentProcesses = processes;
        
        renderProcessList(processes);
        
    } catch (error) {
        console.error('加载工序失败:', error);
        showToast('加载工序列表失败');
    }
}

// 渲染工序列表
function renderProcessList(processes) {
    Elements.processList.innerHTML = '';
    
    if (!processes || processes.length === 0) {
        Elements.processSection?.classList.add('hidden');
        return;
    }
    
    processes.forEach(process => {
        const processItem = createProcessElement(process);
        Elements.processList.appendChild(processItem);
    });
    
    Elements.processSection?.classList.remove('hidden');
}

// 创建工序元素
function createProcessElement(process) {
    const div = document.createElement('div');
    div.className = 'process-item' + (process.status === 2 ? ' completed' : '');
    div.dataset.processId = process.processID;
    
    const isCompleted = process.status === 2;
    
    div.innerHTML = `
        <div class="process-info">
            <div class="process-name">${process.processName}</div>
            <div class="process-meta">
                ${process.operatorName ? `操作人: ${process.operatorName}` : '待生产'}
                ${process.completeTime ? ` | 完成时间: ${formatDate(process.completeTime)}` : ''}
            </div>
        </div>
        <div class="process-status">
            ${isCompleted ? '✓ 已完成' : '待生产'}
        </div>
        <div class="process-action">
            <button class="btn-report" ${isCompleted ? 'disabled' : ''} onclick="handleReport(${process.processID}, '${process.processName}')">
                ${isCompleted ? '已完成' : '报工'}
            </button>
        </div>
    `;
    
    return div;
}

// 处理报工
async function handleReport(processId, processName) {
    if (!AppState.currentUser) {
        showToast('请先登录');
        return;
    }
    
    try {
        showToast('提交中...');
        
        const response = await fetch(`${API_BASE_URL}/process/${processId}/complete`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                operatorNo: AppState.currentUser.operatorNo,
                operatorName: AppState.currentUser.operatorName
            })
        });
        
        if (!response.ok) {
            throw new Error('报工失败');
        }
        
        showResult(true, `${processName} 报工成功！`);
        
        // 刷新工序列表
        if (AppState.currentDie) {
            await loadProcesses(AppState.currentDie.dieId);
        }
        
    } catch (error) {
        console.error('报工失败:', error);
        showResult(false, '报工失败，请重试');
    }
}

// 显示结果
function showResult(success, message) {
    Elements.resultIcon.textContent = success ? '✓' : '✗';
    Elements.resultIcon.className = 'result-icon ' + (success ? 'success' : 'error');
    Elements.resultMessage.textContent = message;
    Elements.resultMessage.className = 'result-message ' + (success ? 'success' : 'error');
    
    Elements.reportResult?.classList.remove('hidden');
}

// 隐藏结果
function hideResult() {
    Elements.reportResult?.classList.add('hidden');
    Elements.manualWorkOrderInput.value = '';
    hideDieInfo();
}

// 显示提示
function showToast(message) {
    // 移除旧的提示
    const oldToast = document.querySelector('.toast');
    if (oldToast) {
        oldToast.remove();
    }
    
    // 创建新提示
    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.textContent = message;
    document.body.appendChild(toast);
    
    // 3秒后自动移除
    setTimeout(() => {
        toast.remove();
    }, 3000);
}

// 格式化日期
function formatDate(dateString) {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return `${date.getMonth() + 1}/${date.getDate()} ${date.getHours()}:${date.getMinutes().toString().padStart(2, '0')}`;
}

// 注册 Service Worker
function registerServiceWorker() {
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/mobile/sw.js')
            .then(registration => {
                console.log('Service Worker 注册成功:', registration);
            })
            .catch(error => {
                console.log('Service Worker 注册失败:', error);
            });
    }
}

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', init);

// 导出全局函数供 HTML 调用
window.handleReport = handleReport;
