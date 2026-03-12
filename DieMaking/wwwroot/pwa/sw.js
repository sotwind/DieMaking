// Service Worker - 支持离线访问
const CACHE_NAME = 'diemaking-pwa-v1';
const urlsToCache = [
    '/pwa/',
    '/pwa/index.html',
    '/pwa/app.js',
    '/pwa/manifest.json',
    '/pwa/icons/icon-192x192.png'
];

// 安装时缓存资源
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('缓存资源');
                return cache.addAll(urlsToCache);
            })
    );
});

// 激活时清理旧缓存
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    if (cacheName !== CACHE_NAME) {
                        console.log('删除旧缓存:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
});

// 拦截请求，优先从缓存获取
self.addEventListener('fetch', event => {
    event.respondWith(
        caches.match(event.request)
            .then(response => {
                // 缓存命中，返回缓存
                if (response) {
                    return response;
                }
                // 缓存未命中，发起网络请求
                return fetch(event.request);
            })
    );
});
