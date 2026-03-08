# Session: 2026-03-06 01:39:24 UTC

- **Session Key**: agent:main:dingtalk:group:cidarxtsiyayg0k0kwd6wwdma==
- **Session ID**: a8ef6080-7c4b-4147-b285-3462e6e776fe
- **Source**: dingtalk

## Conversation Summary

assistant: ✅ New session started · model: dashscope-coding/qwen3.5-plus
user: Conversation info (untrusted metadata):
```json
{
  "message_id": "msgzocLS6py7SCkmO0h/UXrIA==",
  "sender_id": "03340431021450571680",
  "conversation_label": "大龙虾测试群 - 陈华来",
  "sender": "03340431021450571680",
  "timestamp": "Thu 2026-03-05 10:12 GMT+8",
  "group_subject": "大龙虾测试群",
  "group_channel": "agent:main:dingtalk:group:cidarxtsiyayg0k0kwd6wwdma==",
  "is_group_chat": true
}
```

Sender (untrusted metadata):
```json
{
  "label": "陈华来",
  "name": "陈华来"
}
```

你现在配置里预设的大模型有哪些，如果用于编程开发的话你建议使用哪个大模型，我如何通过指令让你切换到你推荐的大模型
assistant: 根据当前配置，我来回答你的问题：

## 当前可用模型

从系统配置来看，主要可用的模型别名包括：

| 别名 | 完整路径 | 适用场景 |
|------|----------|----------|
| `qwen3-max-2025-09-23` | dashscope-us/qwen3-max-2025-09-23 | 通用任务、复杂推理 |
| `qwen3.5-plus` | dashscope/qwen3.5-plus 或 dashscope-coding/qwen3.5-plus | **编程开发** ⭐ |

**当前使用：** `dashscope-coding/qwen3.5-plus`

## 编程开发推荐

**推荐使用 `qwen3.5-plus`（dashscope-coding 版本）**，原因：

1. **专为代码优化** - dashscope-coding 前缀表示这是代码专用版本
2. **性能平衡** - 在代码生成、调试、重构任务上表现优秀
3. **成本效益** - 相比 qwen3-max 更经济，适合日常开发

## 如何切换模型

你可以通过以下指令让我切换模型：

```
/model qwen3-max-2025-09-23
```

或

```
/model dashscope-us/qwen3-max-2025-09-23
```

**注意：** 模型切换需要配置支持。当前会话的模型由 `models.json` 配置文件决定，如果配置的 API Key 不支持目标模型，切换会失败。

目前我们已经在用最适合编程的模型了 🦞
