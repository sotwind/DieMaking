// 调查问卷后端服务器
const http = require('http');
const fs = require('fs');
const path = require('path');
const { URL } = require('url');

const PORT = 3000;
const DATA_FILE = path.join(__dirname, 'data.json');
const UPLOAD_DIR = path.join(__dirname, 'uploads');

// 确保上传目录存在
if (!fs.existsSync(UPLOAD_DIR)) {
    fs.mkdirSync(UPLOAD_DIR, { recursive: true });
}

// MIME 类型映射
const MIME_TYPES = {
    '.html': 'text/html',
    '.css': 'text/css',
    '.js': 'application/javascript',
    '.json': 'application/json',
    '.png': 'image/png',
    '.jpg': 'image/jpeg',
    '.jpeg': 'image/jpeg',
    '.gif': 'image/gif',
    '.webp': 'image/webp',
    '.svg': 'image/svg+xml',
    '.ico': 'image/x-icon'
};

// 读取数据
function readData() {
    try {
        if (fs.existsSync(DATA_FILE)) {
            return JSON.parse(fs.readFileSync(DATA_FILE, 'utf8'));
        }
    } catch (e) {
        console.error('读取数据失败:', e);
    }
    return [];
}

// 保存数据
function saveData(data) {
    fs.writeFileSync(DATA_FILE, JSON.stringify(data, null, 2));
}

// 生成 ID
function generateId() {
    return 'SURVEY_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
}

// 解析 multipart/form-data
function parseMultipart(req, boundary) {
    return new Promise((resolve, reject) => {
        let body = [];
        req.on('data', chunk => body.push(chunk));
        req.on('end', () => {
            const buffer = Buffer.concat(body);
            const parts = buffer.toString('binary').split('--' + boundary);
            const result = { fields: {}, files: [] };

            parts.forEach(part => {
                if (!part.trim()) return;
                
                const headerMatch = part.match(/Content-Disposition: form-data; name="([^"]+)"(?:; filename="([^"]*)")?/);
                if (!headerMatch) return;

                const name = headerMatch[1];
                const filename = headerMatch[2];
                const content = part.split('\r\n\r\n').slice(1).join('\r\n\r\n').replace(/\r\n$/, '');

                if (filename) {
                    result.files.push({ name, filename, content: Buffer.from(content, 'binary') });
                } else {
                    result.fields[name] = content;
                }
            });

            resolve(result);
        });
        req.on('error', reject);
    });
}

// 创建服务器
const server = http.createServer(async (req, res) => {
    const parsedUrl = new URL(req.url, `http://localhost:${PORT}`);
    const pathname = parsedUrl.pathname;

    // CORS 头
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, DELETE, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

    // 处理 OPTIONS 预检请求
    if (req.method === 'OPTIONS') {
        res.writeHead(200);
        res.end();
        return;
    }

    console.log(`${req.method} ${pathname}`);

    // API 路由
    if (pathname === '/api/submit' && req.method === 'POST') {
        try {
            const contentType = req.headers['content-type'] || '';
            const boundary = contentType.split('boundary=')[1];

            let data;
            if (boundary) {
                data = await parseMultipart(req, boundary);
            } else {
                // JSON 格式
                let body = '';
                req.on('data', chunk => body += chunk);
                req.on('end', async () => {
                    try {
                        const jsonData = JSON.parse(body);
                        const submissions = readData();
                        
                        const submission = {
                            id: generateId(),
                            ...jsonData,
                            submitTime: new Date().toISOString()
                        };

                        submissions.push(submission);
                        saveData(submissions);

                        res.writeHead(200, { 'Content-Type': 'application/json' });
                        res.end(JSON.stringify({ success: true, id: submission.id }));
                    } catch (e) {
                        res.writeHead(400, { 'Content-Type': 'application/json' });
                        res.end(JSON.stringify({ error: e.message }));
                    }
                });
                return;
            }

            // 处理上传的文件
            const imageInfos = [];
            for (const file of data.files) {
                if (file.name === 'image' || file.name.startsWith('image[')) {
                    const ext = path.extname(file.filename) || '.jpg';
                    const filename = `${Date.now()}_${Math.random().toString(36).substr(2, 9)}${ext}`;
                    const filepath = path.join(UPLOAD_DIR, filename);
                    
                    fs.writeFileSync(filepath, file.content);
                    imageInfos.push({
                        name: file.filename,
                        filename: filename,
                        size: file.content.length,
                        type: `image/${ext.replace('.', '')}`
                    });
                }
            }

            // 保存提交数据
            const submissions = readData();
            const submission = {
                id: generateId(),
                name: data.fields.name || '匿名',
                email: data.fields.email || '',
                phone: data.fields.phone || '',
                scores: {
                    quality: parseInt(data.fields.score1) || 0,
                    service: parseInt(data.fields.score2) || 0,
                    delivery: parseInt(data.fields.score3) || 0,
                    overall: parseInt(data.fields.score4) || 0
                },
                comment: data.fields.comment || '',
                images: imageInfos,
                imageCount: imageInfos.length,
                submitTime: new Date().toISOString()
            };

            submission.totalScore = Object.values(submission.scores).reduce((a, b) => a + b, 0);
            submission.averageScore = (submission.totalScore / 4).toFixed(1);

            submissions.push(submission);
            saveData(submissions);

            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ success: true, id: submission.id }));

        } catch (e) {
            console.error('提交错误:', e);
            res.writeHead(500, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: e.message }));
        }
        return;
    }

    // 获取所有提交
    if (pathname === '/api/submissions' && req.method === 'GET') {
        const submissions = readData();
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify(submissions));
        return;
    }

    // 删除提交
    if (pathname.startsWith('/api/submissions/') && req.method === 'DELETE') {
        const id = pathname.split('/').pop();
        let submissions = readData();
        const initialLength = submissions.length;
        submissions = submissions.filter(s => s.id !== id);
        
        if (submissions.length < initialLength) {
            saveData(submissions);
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ success: true }));
        } else {
            res.writeHead(404, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ error: 'Not found' }));
        }
        return;
    }

    // 静态文件服务
    let filePath = pathname === '/' ? '/index.html' : pathname;
    filePath = path.join(__dirname, filePath);

    const ext = path.extname(filePath).toLowerCase();
    const mimeType = MIME_TYPES[ext] || 'application/octet-stream';

    fs.readFile(filePath, (err, content) => {
        if (err) {
            if (err.code === 'ENOENT') {
                res.writeHead(404);
                res.end('文件不存在');
            } else {
                res.writeHead(500);
                res.end('服务器错误');
            }
        } else {
            res.writeHead(200, { 'Content-Type': mimeType });
            res.end(content);
        }
    });
});

server.listen(PORT, '0.0.0.0', () => {
    console.log(`
╔════════════════════════════════════════════════════════╗
║          调查问卷服务器已启动                          ║
╠════════════════════════════════════════════════════════╣
║  前端页面：http://localhost:${PORT}                     ║
║  后台管理：http://localhost:${PORT}/admin.html          ║
║  API 接口：http://localhost:${PORT}/api/submit          ║
║  数据文件：${DATA_FILE}                    ║
║  上传目录：${UPLOAD_DIR}                      ║
╚════════════════════════════════════════════════════════╝
    `);
});
