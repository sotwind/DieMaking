# OpenClaw 子代理机制深度研究报告

## 1. OpenClaw 原生支持的子代理功能

### 1.1 核心子代理工具

OpenClaw 提供两个主要的子代理相关工具：

#### `sessions_spawn` 工具
- **功能**：启动后台子代理运行（`deliver: false`，全局队列：`subagent`）
- **会话键格式**：`agent:<agentId>:subagent:<uuid>`
- **参数**：
  - `task` (必需)：任务描述
  - `label`：可选标签
  - `agentId`：目标代理ID（需授权）
  - `model`：覆盖子代理模型
  - `thinking`：覆盖思考级别
  - `runTimeoutSeconds`：运行超时（默认继承配置）
  - `thread`：是否绑定线程（默认 `false`）
  - `mode`：`run`（一次性）或 `session`（持久会话）
  - `cleanup`：`delete` 或 `keep`（默认 `keep`）
  - `sandbox`：`inherit` 或 `require`（默认 `inherit`）

#### `subagents` 工具
- **功能**：管理当前会话的子代理运行
- **动作**：
  - `list`：列出子代理
  - `kill`：终止子代理
  - `steer`：向子代理发送指令
  - `send`：向子代理发送消息

### 1.2 嵌套子代理（Nested Sub-Agents）

OpenClaw 支持多级嵌套子代理：

| 深度 | 会话键格式 | 角色 | 能否生成子代理 |
|------|-----------|------|---------------|
| 0 | `agent:<id>:main` | 主代理 | 始终可以 |
| 1 | `agent:<id>:subagent:<uuid>` | 子代理（协调器） | 仅当 `maxSpawnDepth >= 2` |
| 2 | `agent:<id>:subagent:<uuid>:subagent:<uuid>` | 子-子代理（工作节点） | 永不 |

**配置示例**：
```json5
{
  agents: {
    defaults: {
      subagents: {
        maxSpawnDepth: 2,          // 允许子代理生成子代理（默认：1）
        maxChildrenPerAgent: 5,    // 每个代理会话的最大活跃子代理数（默认：5）
        maxConcurrent: 8,          // 全局并发限制（默认：8）
        runTimeoutSeconds: 900,    // 默认超时
      },
    },
  },
}
```

### 1.3 ACP（Agent Client Protocol）代理

OpenClaw 还支持通过 ACP 运行外部编码工具：

- **支持的 harness**：Pi、Claude Code、Codex、OpenCode、Gemini CLI、Kimi
- **会话键格式**：`agent:<agentId>:acp:<uuid>`
- **与原生子代理的区别**：
  - ACP 运行在外部 harness 运行时
  - 子代理运行在 OpenClaw 原生运行时
  - ACP 目前不支持沙盒（在主机上运行）

## 2. sessions_spawn 和 subagents 工具的能力边界

### 2.1 sessions_spawn 能力边界

**支持的功能**：
- ✅ 启动后台子代理任务
- ✅ 指定不同的模型和思考级别
- ✅ 线程绑定（Discord、Telegram 等）
- ✅ 超时控制
- ✅ 沙盒继承/要求
- ✅ 任务标签

**限制**：
- ❌ 不接受频道传递参数（`target`、`channel`、`to`、`threadId` 等）
- ❌ 非阻塞调用，立即返回 `{ status: "accepted", runId, childSessionKey }`
- ❌ 子代理上下文仅注入 `AGENTS.md` + `TOOLS.md`（无 `SOUL.md`、`IDENTITY.md` 等）
- ❌ 最大嵌套深度为 5（推荐深度 2）
- ❌ 网关重启时，待处理的"announce back"工作会丢失

### 2.2 subagents 工具能力边界

**支持的功能**：
- ✅ 列出当前会话的子代理
- ✅ 终止特定子代理（级联终止其子代理）
- ✅ 向子代理发送指令
- ✅ 查看子代理日志和状态

**限制**：
- ❌ 仅管理当前请求者会话的子代理
- ❌ 无法直接管理其他会话的子代理
- ❌ 深度 2 子代理无法使用会话工具

### 2.3 工具策略（Tool Policy）

**默认子代理工具权限**：
- 获得所有工具，**除了**会话工具和系统工具
- 当 `maxSpawnDepth >= 2` 时，深度 1 的协调器子代理额外获得 `sessions_spawn`、`subagents`、`sessions_list`、`sessions_history`

**深度 2 叶子工作节点**：
- ❌ 无会话工具
- ❌ 无法生成子代理

## 3. 单实例 OpenClaw 通过子代理实现"多代理"效果

### 3.1 架构设计

单实例 OpenClaw 可以通过以下方式实现多代理效果：

```
┌─────────────────────────────────────────────────────────────┐
│                    OpenClaw Gateway                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │  Main Agent │  │  Agent A    │  │  Agent B    │         │
│  │  (主代理)    │  │ (子代理1)   │  │ (子代理2)   │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
│         │                │                │                │
│         └────────────────┴────────────────┘                │
│                          │                                 │
│                   ┌──────┴──────┐                         │
│                   │ Sub-agent   │                         │
│                   │   Queue     │                         │
│                   │  (lane:     │                         │
│                   │  subagent)  │                         │
│                   └─────────────┘                         │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 多代理隔离机制

**工作空间隔离**：
- 每个子代理有自己的会话上下文
- 独立的工具调用历史
- 独立的模型和思考级别配置

**并发控制**：
- 子代理使用专用的 `subagent` 队列通道
- 并发限制：`agents.defaults.subagents.maxConcurrent`（默认 8）
- 每个代理会话最多 `maxChildrenPerAgent` 个活跃子代理（默认 5）

**沙盒隔离**：
- 子代理可以运行在独立的 Docker 容器中
- 支持 `session`、`agent`、`shared` 三种作用域
- 可以限制文件系统访问（`workspaceAccess: none/ro/rw`）

### 3.3 实际使用场景

**场景 1：并行研究任务**
```javascript
// 主代理同时启动多个研究子代理
const tasks = [
  "研究 OpenClaw 的子代理机制",
  "研究 OpenClaw 的会话管理",
  "研究 OpenClaw 的沙盒功能"
];

// 每个任务在独立的子代理中并行执行
for (const task of tasks) {
  sessions_spawn({ task, model: "openai/gpt-5.2-mini" });
}
```

**场景 2：协调器模式（Orchestrator Pattern）**
```javascript
// 深度 1：协调器子代理
sessions_spawn({
  task: "协调以下子任务：1. 数据分析 2. 报告生成 3. 结果验证",
  maxSpawnDepth: 2  // 允许此子代理生成工作节点
});

// 深度 2：工作节点子代理（由协调器生成）
// 每个工作节点执行具体任务并返回结果给协调器
```

**场景 3：线程绑定持久会话**
```javascript
// Discord/Telegram 线程绑定
sessions_spawn({
  task: "长期项目跟踪",
  thread: true,        // 绑定到线程
  mode: "session"      // 持久会话模式
});
// 后续该线程的消息都会路由到同一个子代理会话
```

## 4. 与真正的多实例 OpenClaw 部署的区别

### 4.1 单实例 + 子代理 vs 多实例部署

| 特性 | 单实例 + 子代理 | 多实例部署 |
|------|----------------|-----------|
| **进程隔离** | 共享同一 Gateway 进程 | 独立的 Gateway 进程 |
| **配置隔离** | 共享同一配置文件 | 独立的配置文件（`OPENCLAW_CONFIG_PATH`） |
| **状态隔离** | 共享状态目录 | 独立状态目录（`OPENCLAW_STATE_DIR`） |
| **端口使用** | 单一端口集 | 独立端口（需间隔 20+ 端口） |
| **资源隔离** | 共享资源（CPU/内存） | 独立资源分配 |
| **故障域** | 单点故障 | 故障隔离（一个实例崩溃不影响其他） |
| **配置复杂度** | 较低 | 较高 |
| **适用场景** | 轻量级多任务、并行处理 | 强隔离、高可用、多租户 |

### 4.2 多实例部署配置

```bash
# 主实例
openclaw --profile main gateway --port 18789

# 救援实例（完全隔离）
openclaw --profile rescue gateway --port 19001
```

**隔离检查清单**：
- `OPENCLAW_CONFIG_PATH` — 每个实例独立的配置文件
- `OPENCLAW_STATE_DIR` — 每个实例独立的会话、凭证、缓存
- `agents.defaults.workspace` — 每个实例独立的工作空间根目录
- `gateway.port` — 每个实例唯一的端口
- 派生端口（browser/canvas）不能重叠

### 4.3 何时选择哪种方案

**选择单实例 + 子代理**：
- 需要并行处理多个任务
- 任务之间需要共享上下文或数据
- 资源有限，无法运行多个 Gateway 实例
- 需要快速协调和通信

**选择多实例部署**：
- 需要强隔离（如不同用户/租户）
- 需要高可用性（救援机器人场景）
- 需要不同的全局配置
- 需要独立的资源限制

## 5. 实际使用中的最佳实践建议

### 5.1 子代理使用最佳实践

**1. 模型选择策略**
```json5
{
  agents: {
    defaults: {
      subagents: {
        model: "openai/gpt-5.2-mini",  // 子代理使用 cheaper 模型
      },
      model: "anthropic/claude-opus-4-6",  // 主代理使用高质量模型
    },
  },
}
```

**2. 超时和清理配置**
```json5
{
  agents: {
    defaults: {
      subagents: {
        runTimeoutSeconds: 900,        // 15分钟超时
        archiveAfterMinutes: 60,       // 1小时后自动归档
      },
    },
  },
}
```

**3. 嵌套深度控制**
- 默认 `maxSpawnDepth: 1`（子代理不能生成子代理）
- 仅在需要协调器模式时启用 `maxSpawnDepth: 2`
- 永远不要超过深度 5（系统限制）

**4. 避免轮询，使用推送机制**
```markdown
❌ 不要这样做：
- 子代理完成后，主代理不断轮询检查状态

✅ 应该这样做：
- 子代理完成后自动 announce 结果回主代理
- 主代理等待推送通知
```

### 5.2 安全最佳实践

**1. 沙盒配置**
```json5
{
  agents: {
    defaults: {
      sandbox: {
        mode: "non-main",      // 非主会话使用沙盒
        scope: "session",      // 每个会话独立容器
        workspaceAccess: "none", // 沙盒工作空间隔离
      },
    },
  },
}
```

**2. 工具限制**
```json5
{
  tools: {
    subagents: {
      tools: {
        deny: ["gateway", "cron", "discord"],  // 子代理不能使用的工具
      },
    },
  },
}
```

**3. 代理间通信控制**
```json5
{
  tools: {
    agentToAgent: {
      enabled: false,  // 默认禁用代理间消息
      allow: ["home", "work"],  // 明确允许的代理
    },
  },
}
```

### 5.3 性能优化建议

**1. 并发控制**
- 根据系统资源调整 `maxConcurrent`（默认 8）
- 根据任务类型调整 `maxChildrenPerAgent`（默认 5）

**2. 上下文管理**
- 子代理上下文仅包含 `AGENTS.md` + `TOOLS.md`，比主代理轻量
- 使用 `cleanup: "delete"` 及时清理完成的子代理

**3. 成本优化**
- 为子代理配置 cheaper 模型
- 设置合理的超时避免长时间运行
- 使用 `archiveAfterMinutes` 自动清理旧会话

### 5.4 调试和监控

**1. 查看子代理状态**
```bash
/subagents list
/subagents info <id>
/subagents log <id>
```

**2. 检查沙盒状态**
```bash
openclaw sandbox explain
docker ps --filter "name=openclaw-sbx-"
```

**3. 监控日志**
```bash
tail -f ~/.openclaw/logs/gateway.log | grep -E "routing|sandbox|tools|subagent"
```

### 5.5 常见陷阱

**1. 子代理无法生成子代理**
- 检查 `maxSpawnDepth` 配置
- 深度 2 子代理永远无法生成子代理

**2. 沙盒冲突**
- 如果请求者会话已沙盒化，ACP 会话会被阻止
- 沙盒化会话不能生成 ACP 会话

**3. 上下文丢失**
- 网关重启会丢失待处理的 announce
- 子代理没有 `SOUL.md` 等人格文件

**4. 资源泄漏**
- 未配置 `archiveAfterMinutes` 会导致会话堆积
- 未清理的 Docker 容器会占用资源

## 6. 总结

OpenClaw 的子代理机制提供了一种在单实例内实现"伪多代理"架构的强大方式：

1. **核心能力**：`sessions_spawn` 和 `subagents` 工具提供了完整的子代理生命周期管理
2. **隔离级别**：支持会话隔离、沙盒隔离、工具策略隔离
3. **扩展性**：支持最多 5 级嵌套（推荐 2 级），适合协调器模式
4. **与多实例对比**：子代理适合轻量级并行任务，多实例适合强隔离场景
5. **最佳实践**：合理配置模型、超时、沙盒，避免过度嵌套，使用推送而非轮询

子代理机制让 OpenClaw 能够在保持单实例简单性的同时，实现复杂的多任务协调和并行处理能力。
