# Session: 2026-03-04 02:52:43 UTC

- **Session Key**: agent:main:main
- **Session ID**: a317ff9b-817e-4fb7-a0fa-9dec15074628
- **Source**: webchat

## Conversation Summary

user: A new session was started via /new or /reset. Execute your Session Startup sequence now - read the required files before responding to the user. Then greet the user in your configured persona, if one is provided. Be yourself - use your defined voice, mannerisms, and mood. Keep it to 1-3 sentences and ask what they want to do. If the runtime model differs from default_model in the system prompt, mention the default model. Do not mention internal steps, files, tools, or reasoning.
assistant: 您好，我是大龙虾 🦞。很高兴为您服务，请问今天有什么需要我协助的吗？
user: [Wed 2026-03-04 10:47 GMT+8] 像企业微信这些如何和你对接的话，你是多久清理一次上下文
assistant: 关于上下文清理，我的工作机制是这样的：

**每次新会话开始时**，我会重新读取核心文件（SOUL.md、USER.md、MEMORY.md 以及最近两天的记忆文件），但**不会继承上一个会话的对话历史**。

**会话进行中**，对话内容会保留在当前会话的上下文中，直到：
- 会话被手动重置（如使用 `/reset` 或 `/new`）
- 会话超时或关闭

如果您需要我在长期对话中记住某些信息，建议明确告诉我，我会将其记录到记忆文件中，这样即使会话重置也能保留。

您是在考虑将企业微信与我集成吗？我可以帮您了解一下具体的对接方案。
user: [Wed 2026-03-04 10:49 GMT+8] 会话超时或关闭是多久
assistant: 根据 OpenClaw 的会话管理机制：

**默认超时时间：60 分钟**

具体来说：
- 当会话**空闲超过 60 分钟**（无新消息）后，会话会自动过期
- 下一条消息到来时，会开启一个新的会话 ID（但之前的对话记录不会被删除，只是开启新会话）
- 您可以在配置文件中修改这个超时时间，例如：

```json5
{
  session: {
    idleMinutes: 240,  // 改为 4 小时
  }
}
```

如果您希望会话长期保持，可以：
1. 调大 `idleMinutes` 的值
2. 定期发送消息保持会话活跃
3. 使用 `/new` 或 `/reset` 手动控制会话重置时机

这个设置是针对企业微信或其他任何渠道都是通用的。您是在规划企业微信的集成方案吗？
