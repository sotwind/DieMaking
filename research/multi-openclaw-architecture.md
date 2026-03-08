# 多OpenClaw协同工作架构设计研究报告

## 概述

本报告基于OpenClaw官方架构文档、源代码分析和分布式系统原理，深入研究多OpenClaw实例协同工作的架构设计。

---

## 1. 多OpenClaw实例协同工作的原理

### 1.1 核心架构概念

OpenClaw采用**Gateway-Node架构模式**，这是理解多实例协作的基础：

```
┌─────────────────────────────────────────────────────────────────┐
│                        Gateway (控制平面)                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  Session    │  │   Agent     │  │    Channel Router       │  │
│  │  Manager    │  │   Runtime   │  │    (多通道消息路由)       │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│         │                │                    │                 │
│         └────────────────┴────────────────────┘                 │
│                          │                                      │
│                    WebSocket Protocol                           │
│                    (统一控制协议)                                │
└──────────────────────────┬──────────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           │               │               │
           ▼               ▼               ▼
    ┌────────────┐  ┌────────────┐  ┌────────────┐
    │  Operator  │  │   Node     │  │   Node     │
    │  (CLI/UI)  │  │ (iOS/Mac)  │  │ (Android)  │
    └────────────┘  └────────────┘  └────────────┘
```

### 1.2 协作原理

**1.2.1 统一WebSocket协议**

所有客户端（CLI、Web UI、macOS应用、iOS/Android节点）通过WebSocket连接到Gateway，并在握手时声明其**角色(role)**和**作用域(scope)**：

- **Operator角色**: 控制平面客户端，拥有配置、会话、代理运行等权限
- **Node角色**: 能力主机，暴露命令表面（如`canvas.*`、`camera.*`、`system.run`）

**1.2.2 设备配对与认证**

```json
// 连接握手示例
{
  "role": "node",  // 或 "operator"
  "scopes": ["operator.read", "operator.write"],
  "caps": ["camera", "canvas", "screen", "location"],
  "commands": ["camera.snap", "canvas.navigate"],
  "device": {
    "id": "device_fingerprint",
    "publicKey": "...",
    "signature": "..."
  }
}
```

**1.2.3 任务分发机制**

当Agent需要执行工具调用时：
1. Gateway接收消息并运行Agent
2. Agent决定调用Node工具
3. Gateway通过WebSocket向Node发送`node.invoke` RPC
4. Node执行命令并返回结果
5. Gateway将结果返回给原始通道

---

## 2. 常见的协作模式

### 2.1 主从模式 (Master-Worker)

**架构特点：**
- 一个主Gateway负责消息路由和Agent运行
- 多个Node作为工作节点执行具体任务

**典型场景：**
```
┌─────────────────┐
│  Gateway主机    │ ← 运行Agent、管理会话、连接消息通道
│  (VPS/服务器)   │
└────────┬────────┘
         │ WebSocket
    ┌────┴────┬────────┬────────┐
    ▼         ▼        ▼        ▼
┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐
│Mac节点 │ │iOS节点│ │Android│ │Linux  │
│(开发)  │ │(移动) │ │(移动) │ │(构建) │
└───────┘ └───────┘ └───────┘ └───────┘
```

**适用场景：**
- 开发环境：Mac作为Node执行本地命令
- 移动场景：iOS/Android提供相机、位置、通知访问
- CI/CD：专用构建节点执行系统命令

### 2.2 多Gateway隔离模式

**架构特点：**
- 同一主机运行多个独立的Gateway实例
- 每个实例有独立的配置、状态目录、端口

**实现方式：**
```bash
# 主实例
openclaw --profile main gateway --port 18789

# 救援/隔离实例
openclaw --profile rescue gateway --port 19001
```

**配置隔离清单：**
- `OPENCLAW_CONFIG_PATH` — 独立配置文件
- `OPENCLAW_STATE_DIR` — 独立会话、凭证、缓存
- `agents.defaults.workspace` — 独立工作空间
- `gateway.port` — 独立端口

**适用场景：**
- 需要严格隔离的生产/测试环境
- 救援机器人（主机器人故障时使用）
- 多租户场景（不同用户/团队完全隔离）

### 2.3 远程访问模式

**架构特点：**
- Gateway运行在持久化主机（VPS/家用服务器）
- 客户端通过SSH隧道或Tailnet连接

**连接方式：**

**SSH隧道：**
```bash
ssh -N -L 18789:127.0.0.1:18789 user@gateway-host
```

**Tailscale Serve：**
```bash
# Gateway保持loopback绑定，通过Tailscale暴露
# 客户端通过Tailnet安全访问
```

**适用场景：**
- 笔记本经常休眠，需要Agent始终在线
- 多设备访问同一个Agent状态
- 远程团队协作

### 2.4 任务分发模式 (Subagent)

**架构特点：**
- 主Agent可以生成子Agent（Subagent）处理独立任务
- 子Agent在独立的会话中运行，完成后结果返回主会话

**生命周期钩子：**
- `subagent_spawning`: 子Agent创建前
- `subagent_spawned`: 子Agent已创建
- `subagent_delivery_target`: 确定子Agent结果投递目标
- `subagent_ended`: 子Agent结束

**工作流程：**
```
主会话 ──spawn──► 子Agent会话 ──run──► 任务执行
   ▲                                      │
   └──────────结果返回────────────────────┘
```

**适用场景：**
- 复杂任务分解（研究、代码生成）
- 并行处理多个独立任务
- 隔离不同任务的上下文

---

## 3. 多OpenClaw协作的优缺点分析

### 3.1 优点

| 优点 | 说明 |
|------|------|
| **高可用性** | Gateway在VPS上持续运行，不受客户端设备休眠影响 |
| **能力扩展** | 通过Node模式将手机、平板等设备的能力集成到Agent |
| **负载分散** | 耗时的系统命令可在专用Node上执行，不阻塞主Gateway |
| **环境隔离** | 多Gateway实例实现完全隔离，适合多团队/多项目 |
| **安全增强** | 执行权限可在Node本地控制，支持allowlist和审批流程 |
| **灵活部署** | 支持从单设备到多设备、从本地到云的多种部署拓扑 |

### 3.2 缺点

| 缺点 | 说明 |
|------|------|
| **网络依赖** | Node与Gateway之间的连接需要稳定的网络 |
| **配置复杂性** | 多实例需要管理多个配置文件、端口、认证 |
| **状态一致性** | 会话状态集中在Gateway，Node无状态但依赖Gateway可用 |
| **审批延迟** | 跨主机的执行请求需要等待Node端的用户审批 |
| **调试困难** | 分布式架构下问题定位更复杂 |
| **资源开销** | 每个Gateway实例都需要独立的内存和端口资源 |

---

## 4. 适用场景分析

### 4.1 推荐使用多OpenClaw的场景

**场景1：跨设备个人助手**
- Gateway运行在VPS或家用服务器
- Mac/iOS/Android作为Node连接
- 在任何设备上都能访问统一的Agent状态

**场景2：开发团队协作**
- 共享的Gateway实例连接团队Slack/Discord
- 每个开发者有自己的Node用于本地命令执行
- 代码审查、构建任务在专用Node上执行

**场景3：多环境隔离**
- 生产环境Gateway（严格权限控制）
- 测试环境Gateway（宽松权限）
- 开发环境Gateway（本地调试）

**场景4：复杂任务自动化**
- 主Agent负责任务协调
- 子Agent并行处理独立子任务
- 结果汇总后统一回复

### 4.2 单实例即可满足的场景

- 个人单设备使用
- 简单的消息自动回复
- 不需要跨设备能力访问
- 对可用性要求不高的场景

---

## 5. 实现多OpenClaw协作的技术方案

### 5.1 基础部署方案

#### 方案A：单Gateway + 多Node

```yaml
# Gateway配置 (~/.openclaw/openclaw.json)
{
  "gateway": {
    "port": 18789,
    "bind": "loopback",  # 或 "lan" 如果需要局域网访问
    "auth": {
      "token": "secure-token"
    }
  },
  "agents": {
    "defaults": {
      "workspace": "~/workspace"
    }
  }
}
```

**Node连接（Mac/Linux）：**
```bash
# 设置Gateway地址
export OPENCLAW_GATEWAY_HOST="gateway.example.com"
export OPENCLAW_GATEWAY_TOKEN="secure-token"

# 启动Node
openclaw node run --display-name "Mac-Dev-Node"
```

**Node连接（iOS/Android）：**
- 使用OpenClaw移动应用
- 扫描配对码或手动输入Gateway地址

#### 方案B：多Gateway实例

```bash
# 创建隔离的配置目录
mkdir -p ~/.openclaw-main ~/.openclaw-rescue

# 主实例
OPENCLAW_CONFIG_PATH=~/.openclaw-main/openclaw.json \
OPENCLAW_STATE_DIR=~/.openclaw-main \
openclaw gateway --port 18789

# 救援实例（端口间隔至少20，避免浏览器/CDP端口冲突）
OPENCLAW_CONFIG_PATH=~/.openclaw-rescue/openclaw.json \
OPENCLAW_STATE_DIR=~/.openclaw-rescue \
openclaw gateway --port 19001
```

### 5.2 安全加固方案

**5.2.1 执行权限控制**

```json
// Node本地配置 (~/.openclaw/exec-approvals.json)
{
  "version": 1,
  "defaults": {
    "security": "allowlist",
    "ask": "on-miss"
  },
  "agents": {
    "main": {
      "security": "allowlist",
      "allowlist": [
        {"pattern": "/usr/bin/git"},
        {"pattern": "/usr/bin/docker"}
      ]
    }
  }
}
```

**5.2.2 TLS加密**

```json
// Gateway TLS配置
{
  "gateway": {
    "tls": {
      "cert": "/path/to/cert.pem",
      "key": "/path/to/key.pem"
    }
  }
}
```

**5.2.3 设备绑定认证**

```json
// 使用设备密钥对替代Bearer Token
{
  "device": {
    "id": "device_fingerprint",
    "publicKey": "...",
    "signature": "..."
  }
}
```

### 5.3 高可用方案

**5.3.1 服务监控**

```bash
# 使用systemd管理Gateway服务
systemctl --user enable --now openclaw-gateway.service

# 或使用launchd (macOS)
openclaw gateway install
```

**5.3.2 自动重启策略**

```bash
# 使用supervisor或类似工具
# 配置自动重启和日志轮转
```

**5.3.3 备份策略**

```bash
# 定期备份会话和配置
rsync -av ~/.openclaw/sessions/ backup/sessions/
rsync -av ~/.openclaw/openclaw.json backup/config/
```

### 5.4 扩展方案

**5.4.1 自定义Node能力**

```typescript
// 通过插件注册自定义Node命令
api.registerHook("gateway_start", async (event, ctx) => {
  // 注册自定义能力
});
```

**5.4.2 插件化扩展**

```typescript
// 自定义插件实现多Gateway协调
export default {
  id: "multi-gateway-coordinator",
  register: (api) => {
    api.registerHook("subagent_spawning", async (event, ctx) => {
      // 根据负载选择目标Gateway
    });
  }
};
```

---

## 6. 架构演进方向

基于OpenClaw的Clawnet重构计划，未来多实例协作可能演进为：

### 6.1 统一协议 (Clawnet)
- 合并WS控制平面和Bridge传输为单一协议
- 明确的角色和作用域分离
- 统一的配对和认证流程

### 6.2 中心化审批
- 审批流程从Node转移到Gateway
- 所有Operator客户端都能接收审批请求
- 支持跨网络审批

### 6.3 设备身份管理
- 稳定的设备ID（基于密钥指纹）
- 可读的设备别名（如`scarlet-claw`、`saltwave`）
- 统一的设备注册表

---

## 7. 总结

多OpenClaw协同工作通过Gateway-Node架构实现了灵活的能力扩展和部署拓扑。核心要点：

1. **Gateway是控制平面**：负责消息路由、Agent运行、会话管理
2. **Node是能力扩展**：提供设备特定功能（相机、位置、系统命令）
3. **统一协议**：WebSocket协议支持角色声明和双向通信
4. **安全优先**：本地执行权限控制、审批流程、TLS加密
5. **灵活部署**：从单设备到多设备、从本地到云的多种模式

选择协作模式时应考虑：可用性需求、安全要求、设备能力、网络环境等因素。

---

## 参考文档

- OpenClaw Gateway Protocol: `/opt/openclaw/docs/gateway/protocol.md`
- Clawnet Refactor: `/opt/openclaw/docs/refactor/clawnet.md`
- Node Documentation: `/opt/openclaw/docs/nodes/index.md`
- Multiple Gateways: `/opt/openclaw/docs/gateway/multiple-gateways.md`
- Remote Access: `/opt/openclaw/docs/gateway/remote.md`
