# OpenClaw 钉钉消息重复发送问题分析报告

## 问题概述

用户报告了两个重复发送消息的问题：
1. **子代理消息重复**：有几个子代理就会重复几次消息
2. **定时任务重复**：定时任务会重复两次

## 根本原因分析

### 问题 1：子代理消息重复

**原因**：子代理继承了父会话的 `deliveryContext`

从代码分析中发现：

1. 在 `subagent-spawn.ts` 中创建子代理时，`requesterOrigin` 被设置为父会话的 delivery context：
```typescript
const requesterOrigin = normalizeDeliveryContext({
  channel: ctx.agentChannel,
  accountId: ctx.agentAccountId,
  to: ctx.agentTo,
  threadId: ctx.agentThreadId,
});
```

2. 子代理注册时保存了这个 `requesterOrigin`：
```typescript
registerSubagentRun({
  runId: params.runId,
  childSessionKey,
  requesterSessionKey: requesterInternalKey,
  requesterOrigin,  // <-- 这里保存了父会话的发送目标
  // ...
});
```

3. 当子代理完成任务后，`subagent-announce.ts` 中的 `runSubagentAnnounceFlow` 函数会使用这个 `requesterOrigin` 来发送完成通知：
```typescript
const delivery = await deliverSubagentAnnouncement({
  requesterSessionKey: targetRequesterSessionKey,
  requesterOrigin,  // <-- 使用保存的 delivery context
  // ...
});
```

4. 问题在于：如果子代理也使用了 `message` 工具发送消息，它会默认使用会话的 delivery context，导致消息被发送到父会话的频道。

### 问题 2：定时任务重复

**原因**：定时任务的双重发送机制

从 `cron/isolated-agent/run.ts` 和 `delivery-dispatch.ts` 分析：

1. 定时任务执行后，会通过 `dispatchCronDelivery` 发送结果
2. 在 `delivery-dispatch.ts` 中有两种发送路径：
   - `deliverViaDirect`: 直接发送
   - `deliverViaAnnounce`: 通过子代理通知流程发送

3. 代码显示：
```typescript
const useDirectDelivery =
  params.deliveryPayloadHasStructuredContent || params.resolvedDelivery.threadId != null;
if (useDirectDelivery) {
  const directResult = await deliverViaDirect(params.resolvedDelivery);
} else {
  const announceResult = await deliverViaAnnounce(params.resolvedDelivery);
}
```

4. 问题可能出在：
   - 定时任务的 `delivery` 配置和 `payload` 配置可能同时存在
   - 或者 `deliverViaAnnounce` 流程中又有额外的发送逻辑

## 详细调查发现

### 子代理 deliveryContext 继承链

1. **父会话** (dingtalk:group:cidarxtsiyayg0k0kwd6wwdma==)
   - deliveryContext: {channel: "dingtalk", to: "cidarxTSIyayg0k0kWd6WWDMA==", accountId: "default"}

2. **子代理创建** (subagent-spawn.ts)
   - 继承父会话的 channel/to/accountId
   - 保存到子代理的 session entry 中

3. **子代理消息发送** (message-tool.ts)
   - 如果没有明确指定 target，会使用 session 的 deliveryContext
   - 导致消息被发送到父会话的钉钉群

### 定时任务重复执行原因

查看 `cron/jobs.json`：
```json
{
  "id": "59d5b7b9-9b2c-41bc-af03-c2ce8338e84f",
  "name": "daily_orders_report",
  "schedule": {"kind": "cron", "expr": "0 8 * * *", "tz": "Asia/Shanghai"},
  "sessionTarget": "main",
  "payload": {"kind": "systemEvent", "text": "run_daily_orders_report"}
}
```

问题可能：
1. `sessionTarget: "main"` 导致任务在主会话中执行
2. 同时存在 `delivery` 配置和 `payload.deliver` 配置
3. 或者 cron 的 announce 流程和 direct 流程同时触发

## 修复方案

### 方案 1：防止子代理消息路由到父会话频道

**方法 A：在子代理系统提示中明确禁止**

在 `subagent-spawn.ts` 的 `buildSubagentSystemPrompt` 中已包含：
```markdown
## What You DON'T Do
- NO user conversations (that's main agent's job)
- NO external messages (email, tweets, etc.) unless explicitly tasked with a specific recipient/channel
- Only use the `message` tool when explicitly instructed to contact a specific external recipient; otherwise return plain text and let the main agent deliver it
```

但需要确保子代理严格遵守。

**方法 B：在子代理创建时清除 deliveryContext**

修改 `subagent-spawn.ts`，在创建子代理会话时不传递 delivery context：

```typescript
// 修改前：
const requesterOrigin = normalizeDeliveryContext({
  channel: ctx.agentChannel,
  accountId: ctx.agentAccountId,
  to: ctx.agentTo,
  threadId: ctx.agentThreadId,
});

// 修改后：
// 子代理不应该继承父会话的 delivery context
// 只在 announce 流程中使用 requesterOrigin，不保存到子代理 session
```

**方法 C：在子代理会话中禁用 message 工具的默认路由**

修改 `message-tool.ts`，对于子代理会话，要求必须显式指定 target：

```typescript
// 在 createMessageTool 中
requireExplicitTarget: isSubagentSession(options?.agentSessionKey)
```

### 方案 2：防止定时任务重复执行

**方法 A：检查 cron 配置**

确保 `cron/jobs.json` 中没有重复的 `delivery` 配置：

```json
{
  "id": "...",
  "name": "daily_orders_report",
  "delivery": {
    "mode": "announce",  // 或者 "none"，但不要和 payload.deliver 同时设置
    "channel": "dingtalk",
    "to": "..."
  },
  "payload": {
    "kind": "agentTurn",
    "message": "...",
    // 不要同时设置 deliver: true
  }
}
```

**方法 B：修改 delivery-dispatch.ts**

确保不会同时走 direct 和 announce 两条路径：

```typescript
// 在 dispatchCronDelivery 中
if (params.deliveryRequested && !params.skipMessagingToolDelivery) {
  // 只能选择一条路径
  const useDirectDelivery = /* ... */;
  if (useDirectDelivery) {
    await deliverViaDirect(params.resolvedDelivery);
  } else {
    await deliverViaAnnounce(params.resolvedDelivery);
  }
}
```

## 推荐的修复步骤

### 立即修复（配置层面）

1. **检查定时任务配置**：
   ```bash
   cat ~/.openclaw/cron/jobs.json
   ```
   确保没有同时设置 `delivery.mode` 和 `payload.deliver`

2. **检查子代理创建参数**：
   在创建子代理时，确保 `expectsCompletionMessage` 设置正确，避免重复通知

### 代码修复（需要修改 OpenClaw 源码）

1. **修改 `subagent-spawn.ts`**：
   - 可选：不在子代理 session 中保存 deliveryContext
   - 或者：在子代理系统提示中更强调不要发送消息

2. **修改 `message-tool.ts`**：
   - 对于子代理会话，默认要求显式 target

3. **修改 `cron/delivery-dispatch.ts`**：
   - 添加防止重复发送的逻辑
   - 确保 direct 和 announce 路径互斥

## 配置文件修复示例

### 修复定时任务重复

```json
{
  "version": 1,
  "jobs": [
    {
      "id": "59d5b7b9-9b2c-41bc-af03-c2ce8338e84f",
      "name": "daily_orders_report",
      "description": "每天早上8点发送各厂区前一天接单情况",
      "enabled": true,
      "schedule": {
        "kind": "cron",
        "expr": "0 8 * * *",
        "tz": "Asia/Shanghai"
      },
      "sessionTarget": "isolated",
      "wakeMode": "now",
      "payload": {
        "kind": "agentTurn",
        "message": "生成前一天各厂区接单情况报告"
      },
      "delivery": {
        "mode": "announce",
        "channel": "dingtalk",
        "to": "cidarxTSIyayg0k0kWd6WWDMA=="
      }
    }
  ]
}
```

关键修改：
- `sessionTarget`: "isolated" - 使用隔离会话，避免影响主会话
- `delivery.mode`: "announce" - 只通过 announce 方式发送结果
- 不要在 `payload` 中设置 `deliver: true`

### 修复子代理消息重复

在创建子代理时，确保：
```typescript
// 如果不需要子代理发送消息到父频道，设置：
expectsCompletionMessage: true  // 让子代理通过 announce 流程返回结果
// 而不是让子代理自己发送消息
```

## 验证修复

1. **验证定时任务**：
   ```bash
   openclaw cron list
   openclaw cron run <job-id> --dry-run
   ```

2. **验证子代理**：
   - 创建测试子代理
   - 检查子代理是否继承了 deliveryContext
   - 验证子代理发送消息时是否要求显式 target

## 总结

- **子代理重复**：主要是因为子代理继承了父会话的 deliveryContext，导致消息默认发送到父会话频道
- **定时任务重复**：可能是因为同时配置了 delivery 和 payload.deliver，或者 direct/announce 两条路径都触发

建议优先通过配置修复，如果问题仍然存在，再考虑修改 OpenClaw 源码。
