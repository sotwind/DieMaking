// 调查问卷交互脚本
document.addEventListener('DOMContentLoaded', function() {
    
    // ========== 配置常量 ==========
    const MAX_FILES = 20;  // 最大图片数量
    const MAX_CHARS = 500; // 最大字符数
    const SCORE_LABELS = {
        0: '未评分',
        1: '非常差',
        2: '很差',
        3: '差',
        4: '较差',
        5: '一般',
        6: '尚可',
        7: '良好',
        8: '好',
        9: '很好',
        10: '优秀'
    };

    // ========== 图片上传功能 ==========
    const uploadArea = document.getElementById('uploadArea');
    const imageUpload = document.getElementById('imageUpload');
    const previewContainer = document.getElementById('previewContainer');
    const imageCount = document.getElementById('imageCount');
    let uploadedFiles = [];

    // 拖拽上传
    uploadArea.addEventListener('dragover', function(e) {
        e.preventDefault();
        uploadArea.classList.add('dragover');
    });

    uploadArea.addEventListener('dragleave', function(e) {
        e.preventDefault();
        uploadArea.classList.remove('dragover');
    });

    uploadArea.addEventListener('drop', function(e) {
        e.preventDefault();
        uploadArea.classList.remove('dragover');
        const files = e.dataTransfer.files;
        handleFiles(files);
    });

    // 点击上传
    imageUpload.addEventListener('change', function(e) {
        handleFiles(e.target.files);
    });

    function handleFiles(files) {
        const imageFiles = Array.from(files).filter(file => file.type.startsWith('image/'));
        
        if (uploadedFiles.length + imageFiles.length > MAX_FILES) {
            alert(`最多只能上传 ${MAX_FILES} 张图片！当前已选择 ${uploadedFiles.length} 张。`);
            return;
        }

        imageFiles.forEach(file => {
            if (uploadedFiles.length < MAX_FILES) {
                uploadedFiles.push(file);
                displayPreview(file);
            }
        });

        updateImageCount();
        imageUpload.value = '';
    }

    function displayPreview(file) {
        const reader = new FileReader();
        reader.onload = function(e) {
            const previewItem = document.createElement('div');
            previewItem.className = 'preview-item';
            previewItem.innerHTML = `
                <img src="${e.target.result}" alt="预览图片">
                <button type="button" class="remove-btn" onclick="removeImage(this)">&times;</button>
            `;
            previewContainer.appendChild(previewItem);
        };
        reader.readAsDataURL(file);
    }

    function updateImageCount() {
        imageCount.textContent = uploadedFiles.length;
    }

    // 删除图片（全局函数）
    window.removeImage = function(btn) {
        const previewItem = btn.parentElement;
        previewItem.remove();
        uploadedFiles.pop(); // 简化处理，删除最后一个
        updateImageCount();
    };

    // ========== 评分功能 ==========
    const scoreInputs = ['score1', 'score2', 'score3', 'score4'];
    const scoreLabels = ['score1Label', 'score2Label', 'score3Label', 'score4Label'];
    const scoreNames = ['产品质量', '服务态度', '配送效率', '整体体验'];

    // 绑定输入框和滑块
    scoreInputs.forEach((inputId, index) => {
        const input = document.getElementById(inputId);
        const range = document.querySelector(`.score-range[data-target="${inputId}"]`);
        const label = document.getElementById(scoreLabels[index]);

        // 输入框变化
        input.addEventListener('input', function() {
            let value = parseInt(this.value) || 0;
            if (value < 0) value = 0;
            if (value > 100) value = 100;
            
            this.value = value;
            range.value = value;
            updateScoreLabel(label, value);
            updateTotalScore();
        });

        // 滑块变化
        range.addEventListener('input', function() {
            const value = this.value;
            input.value = value;
            updateScoreLabel(label, parseInt(value));
            updateTotalScore();
        });

        // 失去焦点时验证
        input.addEventListener('blur', function() {
            let value = parseInt(this.value) || 0;
            if (value < 0) {
                value = 0;
                this.value = 0;
                range.value = 0;
            }
            if (value > 100) {
                value = 100;
                this.value = 100;
                range.value = 100;
            }
            updateScoreLabel(label, value);
            updateTotalScore();
        });
    });

    function updateScoreLabel(labelElement, value) {
        if (value === 0) {
            labelElement.textContent = '未评分';
            labelElement.style.color = '#999';
        } else if (value < 60) {
            labelElement.textContent = `不及格 (${value}分)`;
            labelElement.style.color = '#ff4444';
        } else if (value < 70) {
            labelElement.textContent = `及格 (${value}分)`;
            labelElement.style.color = '#ff9900';
        } else if (value < 80) {
            labelElement.textContent = `良好 (${value}分)`;
            labelElement.style.color = '#44cc44';
        } else if (value < 90) {
            labelElement.textContent = `优秀 (${value}分)`;
            labelElement.style.color = '#667eea';
        } else {
            labelElement.textContent = `卓越 (${value}分)`;
            labelElement.style.color = '#764ba2';
        }
    }

    function updateTotalScore() {
        let total = 0;
        let count = 0;

        scoreInputs.forEach(inputId => {
            const value = parseInt(document.getElementById(inputId).value) || 0;
            total += value;
            if (value > 0) count++;
        });

        const avg = count > 0 ? (total / count).toFixed(1) : 0;

        document.getElementById('totalScore').textContent = total;
        document.getElementById('avgScore').textContent = avg;
    }

    // ========== 字符计数 ==========
    const commentTextarea = document.getElementById('comment');
    const charCount = document.getElementById('charCount');

    commentTextarea.addEventListener('input', function() {
        const currentLength = this.value.length;
        charCount.textContent = currentLength;
        
        if (currentLength > MAX_CHARS) {
            charCount.style.color = '#ff4444';
            this.value = this.value.substring(0, MAX_CHARS);
            charCount.textContent = MAX_CHARS;
        } else {
            charCount.style.color = '#999';
        }
    });

    // ========== 表单提交 ==========
    const surveyForm = document.getElementById('surveyForm');
    const successModal = document.getElementById('successModal');
    const closeModal = document.getElementById('closeModal');
    const closeBtn = document.querySelector('.close-modal');
    const submitInfo = document.getElementById('submitInfo');

    surveyForm.addEventListener('submit', function(e) {
        e.preventDefault();

        // 验证必填项
        const name = document.getElementById('name').value.trim();
        const scores = scoreInputs.map(id => parseInt(document.getElementById(id).value) || 0);

        if (!name) {
            alert('请填写姓名！');
            document.getElementById('name').focus();
            return;
        }

        // 验证所有评分都已填写
        for (let i = 0; i < scores.length; i++) {
            if (scores[i] === 0) {
                alert(`请完成"${scoreNames[i]}"的评分！`);
                document.getElementById(scoreInputs[i]).focus();
                return;
            }
        }

        const comment = document.getElementById('comment').value.trim();
        if (!comment) {
            alert('请填写意见与建议！');
            document.getElementById('comment').focus();
            return;
        }

        // 收集表单数据
        const totalScore = scores.reduce((a, b) => a + b, 0);
        const avgScore = (totalScore / scores.length).toFixed(1);

        const formData = {
            id: generateId(),
            name: name,
            email: document.getElementById('email').value.trim(),
            phone: document.getElementById('phone').value.trim(),
            scores: {
                quality: scores[0],      // 产品质量
                service: scores[1],      // 服务态度
                delivery: scores[2],     // 配送效率
                overall: scores[3]       // 整体体验
            },
            totalScore: totalScore,
            averageScore: avgScore,
            comment: comment,
            images: uploadedFiles.map(file => ({
                name: file.name,
                size: formatFileSize(file.size),
                type: file.type
            })),
            imageCount: uploadedFiles.length,
            submitTime: new Date().toISOString(),
            submitTimeFormatted: formatDate(new Date())
        };

        // 保存到本地存储（模拟后台）
        saveToLocalStorage(formData);

        // 发送到后台 API（如果存在）
        sendToServer(formData);

        // 显示成功提示
        showSuccessModal(formData);

        console.log('提交的数据:', formData);
    });

    function generateId() {
        return 'SURVEY_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }

    function formatFileSize(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    }

    function formatDate(date) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        return `${year}-${month}-${day} ${hours}:${minutes}`;
    }

    function saveToLocalStorage(data) {
        const key = 'survey_submissions';
        let submissions = JSON.parse(localStorage.getItem(key) || '[]');
        submissions.push(data);
        localStorage.setItem(key, JSON.stringify(submissions));
    }

    function sendToServer(formData) {
        // 发送到后台 API
        fetch('/api/submit', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(formData)
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                console.log('服务器保存成功:', data.id);
            }
        })
        .catch(error => {
            console.error('发送到服务器失败:', error);
            console.log('数据已保存到本地存储');
        });
    }

    function showSuccessModal(data) {
        submitInfo.innerHTML = `
            <p><strong>提交编号：</strong>${data.id}</p>
            <p><strong>提交时间：</strong>${data.submitTimeFormatted}</p>
            <p><strong>总分：</strong>${data.totalScore} / 400</p>
            <p><strong>平均分：</strong>${data.averageScore}</p>
            <p><strong>上传图片：</strong>${data.imageCount} 张</p>
        `;
        successModal.classList.add('active');
    }

    // 关闭弹窗
    closeModal.addEventListener('click', function() {
        successModal.classList.remove('active');
    });

    closeBtn.addEventListener('click', function() {
        successModal.classList.remove('active');
    });

    successModal.addEventListener('click', function(e) {
        if (e.target === successModal) {
            successModal.classList.remove('active');
        }
    });

    // ========== 表单重置 ==========
    surveyForm.addEventListener('reset', function() {
        setTimeout(() => {
            uploadedFiles = [];
            previewContainer.innerHTML = '';
            updateImageCount();
            
            // 重置所有评分
            scoreInputs.forEach((inputId, index) => {
                const input = document.getElementById(inputId);
                const range = document.querySelector(`.score-range[data-target="${inputId}"]`);
                const label = document.getElementById(scoreLabels[index]);
                
                input.value = '';
                range.value = 0;
                label.textContent = '未评分';
                label.style.color = '#999';
            });
            
            document.getElementById('totalScore').textContent = '0';
            document.getElementById('avgScore').textContent = '0';
            charCount.textContent = '0';
        }, 10);
    });

    // ========== 初始化 ==========
    console.log('调查问卷已加载完成 ✓');
    console.log('功能：最多 20 张图片上传、4 项百分制评分、自动计算总分和平均分');
});
