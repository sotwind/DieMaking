# OpenClaw 三层记忆系统架构技术报告

## 摘要

OpenClaw 采用了一种独特的三层记忆系统架构，旨在解决 AI 助手在会话重启后丢失上下文的根本问题。该架构借鉴了人类记忆的认知模型，将记忆分为工作记忆、短期记忆和长期记忆三个层次，通过文件系统实现持久化存储，确保 AI 助手能够在多次会话之间保持连续性和上下文感知能力。

---

## 1. 什么是三层记忆系统

### 1.1 架构概述

OpenClaw 的三层记忆系统是一种分层的持久化架构，灵感来源于人类认知心理学中的记忆模型：

| 层级 | 对应文件 | 功能描述 | 类比人类记忆 |
|------|----------|----------|--------------|
| **工作记忆 (Working Memory)** | `SESSION-STATE.md` | 当前任务的活跃状态 | 大脑中的当前意识 |
| **短期记忆 (Short-term Memory)** | `memory/YYYY-MM-DD.md` | 每日原始日志记录 | 近期经历和事件 |
| **长期记忆 (Long-term Memory)** | `MEMORY.md` | 精心整理的持久知识 | 深层知识和经验 |

### 1.2 各层详细说明

#### 1.2.1 工作记忆 (SESSION-STATE.md)

工作记忆是 AI 助手的"RAM"，用于存储当前活跃任务的详细信息：

- **作用**：记录当前正在进行的任务状态、关键决策、用户偏好等
- **更新频率**：每条消息中遇到关键信息时立即更新
- **触发条件**：
  - 用户纠正 ("It's X, not Y" / "Actually...")
  - 专有名词 (人名、地名、公司、产品)
  - 用户偏好 (颜色、风格、"我喜欢/不喜欢")
  - 决策 ("Let's do X" / "Go with Y")
  - 具体数值 (数字、日期、ID、URL)

**WAL (Write-Ahead Logging) 协议**：
在回复用户之前，先将关键信息写入 SESSION-STATE.md。这确保了即使在上下文压缩或会话中断的情况下，关键信息也不会丢失。

#### 1.2.2 短期记忆 (memory/YYYY-MM-DD.md)

短期记忆以每日日志的形式存在：

- **作用**：记录当天发生的所有重要事件和对话
- **格式**：Markdown 文件，按日期命名 (如 `2026-03-07.md`)
- **更新频率**：会话期间持续追加
- **读取策略**：每次会话开始时读取今天和昨天的日志

**工作缓冲区 (Working Buffer)**：
当上下文使用率达到 60% 时，系统会进入"危险区域"，此时会启用工作缓冲区 `memory/working-buffer.md`，记录每次对话的摘要，以防止在上下文压缩时丢失信息。

#### 1.2.3 长期记忆 (MEMORY.md)

长期记忆是精心整理的知识库：

- **作用**：存储持久的用户偏好、重要决策、关键信息
- **特点**：经过筛选和整理，不是原始日志的简单堆积
- **更新频率**：定期从每日日志中提炼
- **安全限制**：仅在主会话（私密对话）中加载，不在群组上下文中加载以保护隐私

---

## 2. OpenClaw 中的配置和实现

### 2.1 文件系统布局

```
~/.openclaw/workspace/
├── AGENTS.md              # 操作规则和指南
├── SOUL.md                # AI 助手的身份和个性
├── USER.md                # 用户信息
├── MEMORY.md              # ⭐ 长期记忆
├── SESSION-STATE.md       # ⭐ 工作记忆 (WAL 目标)
├── HEARTBEAT.md           # 定期检查清单
├── TOOLS.md               # 工具配置
└── memory/
    ├── YYYY-MM-DD.md      # ⭐ 每日短期记忆
    └── working-buffer.md  # ⭐ 危险区域日志
```

### 2.2 核心配置参数

在 `~/.openclaw/openclaw.json` 中配置记忆系统：

```json5
{
  agents: {
    defaults: {
      workspace: "/home/admin/.openclaw/workspace",
      compaction: {
        mode: "safeguard",
        triggerTokens: 150000,
        reserveTokensFloor: 100000,
        maxHistoryShare: 0.6,
        memoryFlush: {
          enabled: true,
          softThresholdTokens: 4000,
          systemPrompt: "Session nearing compaction. Store durable memories now.",
          prompt: "Write any lasting notes to memory/YYYY-MM-DD.md; reply with NO_REPLY if nothing to store."
        }
      }
    }
  }
}
```

### 2.3 记忆搜索配置

OpenClaw 支持多种记忆搜索后端：

```json5
{
  agents: {
    defaults: {
      memorySearch: {
        provider: "openai",  // 或 "gemini", "local", "ollama"
        model: "text-embedding-3-small",
        query: {
          hybrid: {
            enabled: true,
            vectorWeight: 0.7,
            textWeight: 0.3,
            mmr: { enabled: true, lambda: 0.7 },
            temporalDecay: { enabled: true, halfLifeDays: 30 }
          }
        }
      }
    }
  }
}
```

### 2.4 实现机制

#### 2.4.1 自动记忆刷新

当会话接近压缩阈值时，OpenClaw 会触发静默的智能体回合：

1. 监控会话 token 使用量
2. 当超过软阈值 (`contextWindow - reserveTokensFloor - softThresholdTokens`) 时
3. 运行静默的"立即写入记忆"指令
4. 使用 `NO_REPLY` 标记确保用户无感知

#### 2.4.2 压缩恢复机制

当检测到上下文被压缩时（通过 `<summary>` 标签或 "truncated" 关键词）：

1. 首先读取 `memory/working-buffer.md`
2. 然后读取 `SESSION-STATE.md`
3. 读取今天和昨天的每日笔记
4. 如果仍缺少上下文，搜索所有来源
5. 从缓冲区提取重要上下文到 SESSION-STATE.md

### 2.5 记忆工具

OpenClaw 提供两个核心记忆工具：

- `memory_search`：语义搜索，返回带文件路径和行号的片段
- `memory_get`：读取特定记忆文件的内容

---

## 3. 架构的优势和劣势

### 3.1 优势

#### 3.1.1 持久性和连续性
- **会话间连续性**：AI 助手在每次重启后都能读取之前的记忆
- **防上下文丢失**：即使发生上下文压缩，关键信息也已持久化到文件
- **可审计性**：所有记忆都是纯文本 Markdown，便于人工审查和编辑

#### 3.1.2 分层管理
- **工作记忆**：快速访问当前任务状态
- **短期记忆**：保留近期详细历史
- **长期记忆**：存储精炼后的持久知识

#### 3.1.3 隐私和安全
- **本地优先**：所有记忆文件存储在本地，无需云服务
- **访问控制**：MEMORY.md 仅在私密会话中加载，防止在群组中泄露敏感信息
- **Git 兼容**：可以通过私有 Git 仓库进行备份和版本控制

#### 3.1.4 可扩展性
- **混合搜索**：结合向量相似度和 BM25 关键词搜索
- **时间衰减**：自动降低旧记忆的权重，突出新信息
- **MMR 重排序**：确保搜索结果多样性，避免重复信息

#### 3.1.5 人机协作
- **可编辑性**：用户可以直接编辑记忆文件
- **透明性**：记忆以人类可读的 Markdown 格式存储
- **渐进式完善**：从原始日志到精炼知识的自然演进

### 3.2 劣势

#### 3.2.1 存储开销
- **文件数量增长**：每日日志会随时间积累，可能达到数百个文件
- **索引维护**：向量索引需要定期更新和重建
- **存储空间**：本地嵌入模型可能占用较大空间（如 0.6GB）

#### 3.2.2 性能考虑
- **搜索延迟**：大型语料库的语义搜索可能有延迟
- **I/O 开销**：频繁的文件读写操作
- **内存占用**：加载大量记忆文件会消耗上下文窗口

#### 3.2.3 一致性挑战
- **同步问题**：多个会话同时写入可能导致冲突
- **过时信息**：长期记忆可能包含已失效的信息，需要定期清理
- **提炼依赖**：从短期记忆到长期记忆的提炼需要智能体主动执行

#### 3.2.4 配置复杂性
- **学习曲线**：用户需要理解三层架构才能有效使用
- **调优需求**：搜索权重、时间衰减等参数需要根据使用场景调整
- **维护工作**：需要定期审查和清理记忆文件

---

## 4. 适用场景分析

### 4.1 高适用场景

#### 4.1.1 长期个人助理
- **场景**：需要记住用户偏好、习惯和长期项目的 AI 助手
- **优势**：能够建立持久的用户画像，提供个性化服务
- **示例**：日程管理、项目跟踪、学习进度记录

#### 4.1.2 多会话复杂任务
- **场景**：需要多天完成的复杂任务
- **优势**：工作记忆确保任务状态不丢失
- **示例**：软件开发、研究报告、内容创作

#### 4.1.3 知识管理
- **场景**：需要积累和检索大量领域知识
- **优势**：混合搜索支持语义和关键词检索
- **示例**：研究助理、技术支持、法律顾问

### 4.2 中等适用场景

#### 4.2.1 群组协作
- **场景**：在 Discord/Slack 等群组中工作
- **考虑**：MEMORY.md 不在群组中加载，需要使用 memory_search 按需检索
- **建议**：将共享知识放入 AGENTS.md 或 USER.md

#### 4.2.2 临时任务
- **场景**：一次性、短期的任务
- **考虑**：三层架构可能过于复杂
- **建议**：主要依赖工作记忆，减少长期记忆写入

### 4.3 低适用场景

#### 4.3.1 无状态服务
- **场景**：简单的问答、单次交互
- **问题**：记忆系统的开销不必要
- **建议**：禁用记忆插件或简化配置

#### 4.3.2 高隐私敏感场景
- **场景**：涉及高度敏感信息的对话
- **问题**：持久化存储可能带来风险
- **建议**：使用 `workspaceAccess: "ro"` 或 `"none"` 禁用记忆写入

---

## 5. 配置建议和最佳实践

### 5.1 初始配置

#### 5.1.1 最小可用配置

对于新用户，建议从以下配置开始：

```json5
{
  agents: {
    defaults: {
      workspace: "~/.openclaw/workspace",
      compaction: {
        mode: "safeguard",
        reserveTokensFloor: 20000
      }
    }
  }
}
```

#### 5.1.2 推荐完整配置

```json5
{
  agents: {
    defaults: {
      workspace: "~/.openclaw/workspace",
      compaction: {
        mode: "safeguard",
        triggerTokens: 150000,
        reserveTokensFloor: 100000,
        memoryFlush: {
          enabled: true,
          softThresholdTokens: 4000
        }
      },
      memorySearch: {
        provider: "openai",
        model: "text-embedding-3-small",
        query: {
          hybrid: {
            enabled: true,
            vectorWeight: 0.7,
            textWeight: 0.3,
            temporalDecay: {
              enabled: true,
              halfLifeDays: 30
            }
          }
        }
      }
    }
  }
}
```

### 5.2 记忆管理最佳实践

#### 5.2.1 工作记忆 (SESSION-STATE.md)

- **立即写入**：收到关键信息后，先写入再回复
- **结构化格式**：
  ```markdown
  # Session State
  **Current Task**: [任务名称]
  **Last Updated**: [时间戳]

  ## Decisions
  - [决策内容]

  ## Preferences
  - [用户偏好]

  ## Context
  - [相关上下文]
  ```

#### 5.2.2 短期记忆 (memory/YYYY-MM-DD.md)

- **每日回顾**：在一天结束时添加 `## Retain` 部分
- **结构化记录**：
  ```markdown
  ## Retain
  - W @Entity: [世界事实]
  - B @Entity: [经历/行为]
  - O(c=0.95) @Entity: [观点/偏好]
  ```

#### 5.2.3 长期记忆 (MEMORY.md)

- **定期提炼**：每周审查每日日志，提取重要信息
- **分类组织**：
  ```markdown
  ## About [User Name]
  ### Key Context
  ### Preferences Learned
  ### Important Dates

  ## Lessons Learned
  ### [Date] - [Topic]

  ## Active Projects
  ```

### 5.3 安全建议

1. **使用私有 Git 仓库**：备份工作区但保持私密
2. **定期清理**：删除过时或敏感的记忆条目
3. **群组聊天限制**：不在群组中加载 MEMORY.md
4. **敏感信息处理**：使用占位符，真实密钥存储在环境变量

### 5.4 性能优化

1. **启用嵌入缓存**：
   ```json5
   memorySearch: {
     cache: { enabled: true, maxEntries: 50000 }
   }
   ```

2. **调整时间衰减**：根据使用频率调整 `halfLifeDays`

3. **限制搜索结果**：
   ```json5
   memorySearch: {
     limits: { maxResults: 6, maxSnippetChars: 700 }
   }
   ```

4. **定期维护**：
   ```bash
   # 清理旧会话
   openclaw sessions cleanup --dry-run
   openclaw sessions cleanup --enforce
   ```

### 5.5 故障排除

#### 5.5.1 记忆搜索无结果
- 检查 `memorySearch.enabled` 是否为 true
- 确认记忆文件路径正确
- 验证嵌入模型 API 密钥

#### 5.5.2 上下文丢失
- 检查 `SESSION-STATE.md` 是否正确更新
- 确认工作缓冲区 `working-buffer.md` 存在
- 验证压缩配置是否合理

#### 5.5.3 记忆文件过大
- 定期审查和清理 MEMORY.md
- 归档旧的每日日志
- 使用 `memory_search` 替代加载完整文件

---

## 6. 总结

OpenClaw 的三层记忆系统是一个精心设计的架构，它通过工作记忆、短期记忆和长期记忆的分层管理，解决了 AI 助手在会话间保持连续性的核心问题。该架构具有以下特点：

1. **分层清晰**：每层有明确的职责和更新频率
2. **持久可靠**：基于文件系统的存储确保数据安全
3. **灵活可配**：支持多种搜索后端和配置选项
4. **隐私优先**：本地存储，支持访问控制
5. **人机协作**：人类可读的格式支持人工编辑

虽然该架构带来了一定的复杂性和存储开销，但对于需要长期连续性和上下文感知的 AI 助手场景，这些投入是值得的。通过遵循最佳实践，用户可以构建一个既强大又安全的记忆系统。

---

## 参考资料

1. OpenClaw 官方文档 - Memory: `/docs/concepts/memory.md`
2. OpenClaw 官方文档 - Agent Workspace: `/docs/concepts/agent-workspace.md`
3. OpenClaw 官方文档 - Session Management: `/docs/reference/session-management-compaction.md`
4. Proactive Agent Skill: `~/.openclaw/workspace/skills/proactive-agent/SKILL.md`
5. Workspace Memory Research: `/docs/experiments/research/memory.md`

---

*报告生成时间：2026-03-07*
*OpenClaw 版本：2026.3.2*
