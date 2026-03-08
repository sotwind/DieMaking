# 钉钉消息重复发送问题分析与修复方案

## 问题描述

用户报告有两个重复发送消息的问题：
1. **子代理重复发送**：有几个子代理就会重复几次消息
2. **定时任务重复发送**：定时任务会重复两次

## 问题分析

### 问题 1：子代理重复发送

**根本原因**：
- 子代理在完成时会自动 **announce** 其结果回到请求者聊天频道（钉钉）
- 这是 OpenClaw 的设计行为，子代理默认会将完成结果发送到父会话的频道
- 当多个子代理同时运行时，每个子代理都会独立地向钉钉发送完成通知

**技术细节**：
- 子代理会话继承了父会话的 `deliveryContext`（包含 `channel: "dingtalk"`）
- 子代理完成时，OpenClaw 会自动执行 "announce" 步骤，将结果发送到钉钉
- 根据文档："Sub-agents are background agent runs spawned from an existing agent run. They run in their own session and, when finished, **announce** their result back to the requester chat channel."

### 问题 2：定时任务重复发送

**根本原因**：
- 定时任务（cron）配置了 `sessionTarget: "main"`，在主会话中运行
- 如果任务的输出包含消息发送逻辑，可能会导致重复发送
- 需要检查定时任务的 payload 和实际执行逻辑

## 修复方案

### 方案 1：子代理使用 ANNOUNCE_SKIP

在子代理任务的最后，回复 `ANNOUNCE_SKIP` 来抑制公告：

```python
# 子代理任务的最后
if __name__ == "__main__":
    # ... 执行任务 ...
    
    # 抑制公告，避免重复发送消息
    print("ANNOUNCE_SKIP")
```

### 方案 2：使用 streamTo 参数

使用 `sessions_spawn` 时添加 `streamTo: "parent"` 参数：

```json
{
  "task": "执行任务...",
  "streamTo": "parent"
}
```

这样子代理的进度会流式传输回父会话，而不是直接发送到频道。

### 方案 3：定时任务禁用发送

对于定时任务，使用 `--no-deliver` 参数禁用消息发送：

```bash
# 编辑现有任务，禁用发送
openclaw cron edit <job-id> --no-deliver

# 或者创建新任务时禁用发送
openclaw cron add --no-deliver ...
```

### 方案 4：修改定时任务配置

编辑 `~/.openclaw/cron/jobs.json`，修改任务的 `sessionTarget` 或添加 `delivery` 配置：

```json
{
  "id": "...",
  "name": "daily_orders_report",
  "delivery": {
    "mode": "none"
  }
}
```

## 建议的最佳实践

1. **子代理任务**：
   - 如果子代理只是执行后台任务，不需要向用户报告，使用 `ANNOUNCE_SKIP`
   - 如果需要报告，确保父会话统一处理子代理结果，避免每个子代理都直接发送消息

2. **定时任务**：
   - 使用 `--no-deliver` 或 `--announce` 明确控制消息发送行为
   - 在任务脚本内部控制消息发送逻辑，而不是依赖 OpenClaw 的自动发送

3. **消息路由**：
   - 理解 OpenClaw 的 `deliveryContext` 继承机制
   - 子代理默认继承父会话的频道配置
   - 使用 `streamTo: "parent"` 将结果路由回父会话而不是直接发送到频道

## 相关文档

- [Sub-Agents 文档](/opt/openclaw/docs/tools/subagents.md)
- [Cron CLI 文档](/opt/openclaw/docs/cli/cron.md)
- [Cron Jobs 文档](/opt/openclaw/docs/automation/cron-jobs.md)

## 关键引用

> "Reply exactly `ANNOUNCE_SKIP` during the announce step to stay silent."
> — OpenClaw 文档

> "Sub-agents are background agent runs spawned from an existing agent run. They run in their own session and, when finished, **announce** their result back to the requester chat channel."
> — Sub-Agents 文档

> "Disable delivery for an isolated job: `openclaw cron edit <job-id> --no-deliver`"
> — Cron CLI 文档
