/**
 * 刀模管理系统 PWA - 扫码功能
 */

// 扫码相关状态
const ScanState = {
    video: null,
    canvas: null,
    canvasContext: null,
    stream: null,
    isScanning: false,
    scanInterval: null
};

// DOM 元素
const ScanElements = {
    video: document.getElementById('scanner-video'),
    canvas: document.getElementById('scanner-canvas'),
    startBtn: document.getElementById('start-scan-btn'),
    stopBtn: document.getElementById('stop-scan-btn'),
    container: document.getElementById('scanner-container')
};

// 初始化扫码功能
function initScanner() {
    ScanState.video = ScanElements.video;
    ScanState.canvas = ScanElements.canvas;
    ScanState.canvasContext = ScanState.canvas.getContext('2d', { willReadFrequently: true });
    
    // 绑定按钮事件
    ScanElements.startBtn?.addEventListener('click', startScan);
    ScanElements.stopBtn?.addEventListener('click', stopScan);
}

// 开始扫码
async function startScan() {
    if (ScanState.isScanning) return;
    
    try {
        // 请求摄像头权限
        const constraints = {
            video: {
                facingMode: 'environment', // 后置摄像头
                width: { ideal: 1280 },
                height: { ideal: 720 }
            }
        };
        
        ScanState.stream = await navigator.mediaDevices.getUserMedia(constraints);
        ScanState.video.srcObject = ScanState.stream;
        
        // 等待视频加载
        await new Promise((resolve) => {
            ScanState.video.onloadedmetadata = () => {
                resolve();
            };
        });
        
        await ScanState.video.play();
        
        // 设置画布尺寸
        ScanState.canvas.width = ScanState.video.videoWidth;
        ScanState.canvas.height = ScanState.video.videoHeight;
        
        ScanState.isScanning = true;
        
        // 更新按钮状态
        ScanElements.startBtn?.classList.add('hidden');
        ScanElements.stopBtn?.classList.remove('hidden');
        
        // 开始扫描循环
        startScanLoop();
        
        showToast('扫码已启动，请将二维码对准扫描框');
        
    } catch (error) {
        console.error('启动扫码失败:', error);
        
        let errorMessage = '无法访问摄像头';
        if (error.name === 'NotAllowedError') {
            errorMessage = '请允许使用摄像头权限';
        } else if (error.name === 'NotFoundError') {
            errorMessage = '未找到摄像头设备';
        } else if (error.name === 'NotReadableError') {
            errorMessage = '摄像头被其他应用占用';
        }
        
        showToast(errorMessage);
    }
}

// 停止扫码
function stopScan() {
    if (!ScanState.isScanning) return;
    
    ScanState.isScanning = false;
    
    // 停止扫描循环
    if (ScanState.scanInterval) {
        clearTimeout(ScanState.scanInterval);
        ScanState.scanInterval = null;
    }
    
    // 停止视频流
    if (ScanState.stream) {
        ScanState.stream.getTracks().forEach(track => track.stop());
        ScanState.stream = null;
    }
    
    ScanState.video.srcObject = null;
    
    // 更新按钮状态
    ScanElements.startBtn?.classList.remove('hidden');
    ScanElements.stopBtn?.classList.add('hidden');
}

// 扫描循环
function startScanLoop() {
    if (!ScanState.isScanning) return;
    
    // 检查视频是否准备好
    if (ScanState.video.readyState === ScanState.video.HAVE_ENOUGH_DATA) {
        // 绘制视频帧到画布
        ScanState.canvasContext.drawImage(
            ScanState.video,
            0, 0,
            ScanState.canvas.width,
            ScanState.canvas.height
        );
        
        // 获取图像数据
        const imageData = ScanState.canvasContext.getImageData(
            0, 0,
            ScanState.canvas.width,
            ScanState.canvas.height
        );
        
        // 使用 jsQR 解码
        const code = jsQR(
            imageData.data,
            imageData.width,
            imageData.height,
            {
                inversionAttempts: 'attemptBoth'
            }
        );
        
        // 如果识别到二维码
        if (code) {
            handleScanResult(code.data);
            return; // 识别成功后停止循环
        }
    }
    
    // 继续扫描
    ScanState.scanInterval = setTimeout(startScanLoop, 100); // 100ms 扫描一次
}

// 处理扫码结果
async function handleScanResult(data) {
    console.log('扫码结果:', data);
    
    // 停止扫码
    stopScan();
    
    // 解析工单号（直接使用字符串，无需复杂解析）
    const workOrderNo = data.trim();
    
    if (!workOrderNo) {
        showToast('无法识别工单号');
        return;
    }
    
    // 显示识别结果
    showToast(`识别到工单号: ${workOrderNo}`);
    
    // 自动填充到手动输入框
    const manualInput = document.getElementById('manual-workorder');
    if (manualInput) {
        manualInput.value = workOrderNo;
    }
    
    // 自动查询
    await queryDieByWorkOrder(workOrderNo);
}

// 震动反馈（如果支持）
function vibrate() {
    if ('vibrate' in navigator) {
        navigator.vibrate(200);
    }
}

// 播放提示音
function playBeep() {
    try {
        const audioContext = new (window.AudioContext || window.webkitAudioContext)();
        const oscillator = audioContext.createOscillator();
        const gainNode = audioContext.createGain();
        
        oscillator.connect(gainNode);
        gainNode.connect(audioContext.destination);
        
        oscillator.frequency.value = 800;
        oscillator.type = 'sine';
        
        gainNode.gain.setValueAtTime(0.3, audioContext.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(0.01, audioContext.currentTime + 0.1);
        
        oscillator.start(audioContext.currentTime);
        oscillator.stop(audioContext.currentTime + 0.1);
    } catch (e) {
        console.log('播放提示音失败:', e);
    }
}

// 页面加载完成后初始化扫码
document.addEventListener('DOMContentLoaded', initScanner);

// 页面可见性变化时暂停/恢复扫码
document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
        // 页面隐藏时停止扫码
        if (ScanState.isScanning) {
            stopScan();
        }
    }
});
