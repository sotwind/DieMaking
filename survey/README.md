# 调查问卷系统

功能完整的在线调查问卷系统，支持多图片上传、百分制评分、后台数据管理和统计分析。

## 📁 文件结构

```
survey/
├── index.html          # 调查问卷前端页面
├── styles.css          # 前端样式
├── script.js           # 前端交互逻辑
├── admin.html          # 后台管理页面
├── admin-styles.css    # 后台样式
├── admin.js            # 后台管理逻辑
└── README.md           # 说明文档
```

## ✨ 功能特性

### 前端调查页面

#### 1. 基本信息收集
- 姓名（必填）
- 邮箱（可选）
- 手机号（可选）

#### 2. 图片上传 🖼️
- **最多支持 20 张图片**
- 支持拖拽上传
- 支持点击选择
- 实时预览
- 可删除已上传图片
- 支持 JPG、PNG、GIF、WebP 格式
- 显示已上传图片数量

#### 3. 百分制评分 ⭐
- **4 个评分维度**（每项满分 100 分）：
  - 产品质量
  - 服务态度
  - 配送效率
  - 整体体验
- 数字输入框 + 滑动条双模式
- 实时显示评分等级（不及格/及格/良好/优秀/卓越）
- 自动计算总分（满分 400）
- 自动计算平均分

#### 4. 评论功能
- 多行文本输入
- 字符计数（最多 500 字）
- 必填验证

#### 5. 提交反馈
- 表单验证
- 提交成功弹窗
- 显示提交编号和时间
- 本地存储数据

### 后台管理页面 (`admin.html`)

#### 1. 统计概览 📊
- 总提交数
- 平均总分
- 平均单项分
- 上传图片总数

#### 2. 维度分析 📈
- 4 个维度的平均分柱状图
- 可视化数据展示

#### 3. 数据表格 📄
- 所有提交记录列表
- 按时间倒序排序
- 评分颜色标识（红/橙/绿）
- 查看详情
- 单条删除

#### 4. 数据导出 📥
- 导出为 CSV/Excel 格式
- 包含所有字段
- 自动添加统计行

#### 5. 数据管理
- 刷新数据
- 清空所有数据

## 🚀 使用方法

### 本地运行

```bash
cd /home/admin/.openclaw/workspace/survey

# 方法 1：直接用浏览器打开
open index.html

# 方法 2：使用 Python 简单服务器
python3 -m http.server 8000
# 访问调查问卷：http://localhost:8000/index.html
# 访问后台管理：http://localhost:8000/admin.html
```

### 部署到服务器

将 `survey/` 文件夹上传到任何 Web 服务器即可使用。

## 📊 数据存储

### 当前实现
- 使用浏览器 `localStorage` 存储
- 数据保存在用户浏览器本地
- 适合演示和小规模使用

### 查看数据
1. 打开浏览器开发者工具（F12）
2. 进入 Application/存储 标签
3. 查看 Local Storage 中的 `survey_submissions`

### 后台访问
访问 `admin.html` 页面即可查看所有提交的数据和统计信息。

## 🔧 扩展建议

### 添加后端 API

创建 Node.js 后端服务：

```javascript
// server.js
const express = require('express');
const multer = require('multer');
const cors = require('cors');
const app = express();

const upload = multer({ dest: 'uploads/' });

app.use(cors());
app.use(express.json());

// 存储提交
app.post('/api/submit', upload.array('images', 20), (req, res) => {
    const data = {
        ...req.body,
        images: req.files,
        submitTime: new Date().toISOString()
    };
    // 保存到数据库
    res.json({ success: true, id: generateId() });
});

// 获取所有提交
app.get('/api/submissions', (req, res) => {
    // 从数据库查询
    res.json(submissions);
});

// 获取统计
app.get('/api/stats', (req, res) => {
    // 计算平均分等统计
    res.json(stats);
});

app.listen(3000);
```

### 修改前端 API 调用

在 `script.js` 中替换 `sendToServer` 函数：

```javascript
async function sendToServer(formData) {
    const response = await fetch('/api/submit', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(formData)
    });
    return await response.json();
}
```

在 `admin.js` 中替换 `loadSubmissions` 函数：

```javascript
async function loadSubmissions() {
    const response = await fetch('/api/submissions');
    submissions = await response.json();
    renderAll();
}
```

### 数据库方案

- **SQLite**: 轻量级，适合小规模
- **MySQL/PostgreSQL**: 生产环境
- **MongoDB**: 灵活文档存储

## 📱 响应式设计

- 适配手机、平板、电脑
- 触摸友好的滑动评分
- 移动端优化的表格

## 🎨 自定义配置

### 修改图片数量限制

在 `script.js` 中修改：
```javascript
const MAX_FILES = 20;  // 改为需要的数量
```

### 修改评分维度

在 `index.html` 中添加/修改评分项，在 `script.js` 中更新 `scoreInputs` 数组。

### 修改主题色

在 `styles.css` 中修改渐变色：
```css
background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
```

## 📋 数据字段说明

```javascript
{
    id: "SURVEY_1234567890_abc123",      // 唯一标识
    name: "张三",                         // 姓名
    email: "test@example.com",           // 邮箱
    phone: "13800138000",                // 手机号
    scores: {
        quality: 95,                     // 产品质量
        service: 88,                     // 服务态度
        delivery: 92,                    // 配送效率
        overall: 90                      // 整体体验
    },
    totalScore: 365,                     // 总分
    averageScore: "91.3",                // 平均分
    comment: "很好的体验...",             // 评论
    images: [...],                       // 图片信息
    imageCount: 5,                       // 图片数量
    submitTime: "2026-02-28T10:30:00Z"  // 提交时间
}
```

## 🔐 安全建议

生产环境使用时：

1. **添加身份验证** - 后台管理需要登录
2. **文件上传验证** - 检查文件类型和大小
3. **输入 sanitization** - 防止 XSS 攻击
4. **HTTPS** - 加密传输
5. **限流** - 防止刷数据
6. **数据库备份** - 定期备份数据

## 📝 更新日志

### v2.0
- ✅ 图片上传数量提升至 20 张
- ✅ 评分改为 4 维度百分制
- ✅ 新增后台管理页面
- ✅ 新增数据统计和图表
- ✅ 新增数据导出功能
- ✅ 新增平均分自动计算

### v1.0
- 基础调查表单
- 五星评分
- 图片上传（5 张）

## 📄 许可证

MIT License - 可自由使用和修改
