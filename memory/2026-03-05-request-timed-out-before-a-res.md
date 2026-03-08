# Session: 2026-03-05 05:19:32 UTC

- **Session Key**: agent:main:dingtalk:direct:03340431021450571680
- **Session ID**: 416c0d0a-19d9-4a1b-ad36-ae06a2fc4d36
- **Source**: dingtalk

## Conversation Summary

user: Conversation info (untrusted metadata):
```json
{
  "timestamp": "Thu 2026-03-05 13:11 GMT+8"
}
```

现在呢，有别的任务在运行吗？ 有别的主任务以及子任务在运行吗？ 另外，你最多可以并发几个主任务
assistant: 当前任务状态：

**主任务：** 1 个正在运行
- 当前会话：`agent:main:dingtalk:direct:03340431021450571680`（与您的钉钉直聊会话）
- 模型：qwen3.5-plus
- 最后活跃时间：刚刚

**子任务：** 0 个
- 目前没有活跃的子代理在运行
- 过去 30 分钟内也没有子任务记录

---

**关于并发限制：**

OpenClaw 的并发能力主要取决于以下因素：

1. **主会话（Main Sessions）**：每个独立的聊天渠道（如不同的钉钉用户、Telegram 联系人等）都会有一个独立的主会话。理论上可以有多个主会话同时运行，数量取决于：
   - 网关配置和渠道数量
   - 系统资源（CPU/内存）
   - 模型 API 的速率限制

2. **子代理（Sub-agents）**：通过 `sessions_spawn` 创建的子任务。并发数量取决于：
   - 系统资源配置
   - 子代理允许列表设置
   - 模型配额和速率限制

**实际建议：**
- 日常使用中，同时运行 3-5 个子代理通常是安全的
- 如果需要更高并发，建议监控资源使用情况
- 对于 CPU/内存密集型任务，适当降低并发数

需要我帮您检查系统资源状态或配置详情吗？
