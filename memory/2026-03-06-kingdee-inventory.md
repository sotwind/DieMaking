# Session: 2026-03-06 20:19:21 UTC

- **Session Key**: agent:main:dingtalk:direct:03340431021450571680
- **Session ID**: 512c6bb1-1669-44d2-a5e0-4e009502f70b
- **Source**: dingtalk

## Conversation Summary

assistant: ✅ New session started · model: dashscope-coding/kimi-k2.5
user: Conversation info (untrusted metadata):
```json
{
  "message_id": "msgRM5ZdumiIMP1mfOTugr3Wg==",
  "sender_id": "03340431021450571680",
  "sender": "陈华来",
  "timestamp": "Fri 2026-03-06 20:57 GMT+8"
}
```

Sender (untrusted metadata):
```json
{
  "label": "陈华来 (03340431021450571680)",
  "id": "03340431021450571680",
  "name": "陈华来"
}
```

你可以访问金蝶云星空erp吗？ 有什么比较方便的方法，比方说有没有现成的mcp或者skills
assistant: 我找到了一个相关的 MCP 服务器，让我再搜索一下是否有直接访问金蝶云星空 ERP 的 MCP 或 API 方案：
assistant: 让我再搜索一下金蝶云星空的 API 文档和相关的开源项目：
assistant: 根据我的搜索，目前**没有现成的 MCP Server 可以直接访问金蝶云星空 ERP**。不过有几个方案可以供您参考：

---

## 现有资源

### 1. 金蝶开发者社区搜索 MCP（有限功能）
- **GitHub**: `qiliang517/kingdee-search-mcp`
- **功能**: 只能搜索金蝶开发者社区的知识库
- **用途**: 查找文档和解决方案，**无法操作 ERP 数据**

### 2. K3Cloud WebAPI SDK（多语言）
GitHub 上有多个开源的 K3Cloud API 封装库：

| 语言 | 项目 | 说明 |
|------|------|------|
| Go | `yrzs/k3cloud` | Golang 版 SDK |
| C# | `1609676823/YiKdWebClient` | 支持多种认证方式 |
| Java | `cashlifei/Kingdee-K3Cloud-Web-Api` | Java 版封装 |
| Node.js | `freedomw1987/node-k3cloud` | Node.js 版 |
| Ruby | `zevinto/k3cloud-sdk` | Ruby Gem |

---

## 推荐方案

### 方案一：自建 MCP Server（推荐）

基于现有的 K3Cloud WebAPI SDK，我可以帮您创建一个专门的 MCP Server。金蝶云星空提供标准的 WebAPI 接口，包括：

- **登录认证**（第三方授权、API签名、集成密钥、用户名密码）
- **单据查询**（查看、保存、提交、审核等）
- **基础资料管理**（客户、供应商、物料等）
- **报表查询**

**优势**: 完全定制化，符合您的具体业务需求

### 方案二：使用 HTTP Request Skill

如果调用频率不高，可以直接使用 HTTP 请求工具调用金蝶 WebAPI。

---

## 下一步

请问您：

1. **主要想实现什么功能？**（查询单据、创建订单、同步库存等）
2. **使用什么编程语言？**（方便选择 SDK）
3. **是否需要我帮您开发一个 MCP Server？**

如果您确定需要，我可以基于金蝶云星空的 WebAPI 文档为您创建一个完整的 MCP Server。
user: Conversation info (untrusted metadata):
```json
{
  "message_id": "msgHs9JVNfH6oI4OreXyrZ6zg==",
  "sender_id": "03340431021450571680",
  "sender": "陈华来",
  "timestamp": "Fri 2026-03-06 21:05 GMT+8"
}
```

Sender (untrusted metadata):
```json
{
  "label": "陈华来 (03340431021450571680)",
  "id": "03340431021450571680",
  "name": "陈华来"
}
```

主要可能查查库存用吧
assistant: 明白了，查询库存是金蝶云星空中比较常见的需求。让我先了解一下金蝶云星空 WebAPI 的具体用法，然后给您一个实用的方案：
assistant: 很好，我找到了详细的资料。让我为您总结一下查询库存的方案：

---

## 金蝶云星空库存查询方案

### 核心接口
金蝶云星空使用 `ExecuteBillQuery` 接口进行单据查询，库存相关的表单 ID 主要有：

| 表单用途 | FormId |
|---------|--------|
| 物料基础资料 | `BD_MATERIAL` |
| 库存余额表 | `STK_Inventory` |
| 物料收发明细 | `STK_InOutStockDetail` |

---

### 方案一：快速实现（Python + HTTP）

如果您想快速验证，可以直接用 Python 调用 WebAPI：

```python
import requests
import json

class K3CloudClient:
    def __init__(self, server_url, acct_id, username, password, lcid=2052):
        self.server_url = server_url.rstrip('/') + '/'
        self.acct_id = acct_id
        self.username = username
        self.password = password
        self.lcid = lcid
        self.cookie = None
    
    def login(self):
        """登录获取 Session"""
        url = f"{self.server_url}Kingdee.BOS.WebApi.ServicesStub.AuthService.ValidateUser.common.kdsvc"
        data = {
            "acctid": self.acct_id,
            "username": self.username,
            "password": self.password,
            "lcid": self.lcid
        }
        resp = requests.post(url, json=data)
        if resp.json().get('LoginResultType') == 1:
            self.cookie = resp.cookies
            return True
        return False
    
    def query_inventory(self, material_number=None, warehouse=None):
        """查询库存余额"""
        url = f"{self.server_url}Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.ExecuteBillQuery.common.kdsvc"
        
        # 构建查询条件
        filter_string = ""
        if material_number:
            filter_string += f"FMaterialId.FNumber='{material_number}'"
        if warehouse:
            if filter_string:
                filter_string += " and "
            filter_string += f"FStockId.FNumber='{warehouse}'"
        
        data = {
            "FormId": "STK_Inventory",  # 库存余额表
            "FieldKeys": "FMaterialId.FNumber,FMaterialId.FName,FStockId.FNumber,FStockId.FName,FQty,FBaseQty",
            "FilterString": filter_string,
            "OrderString": "FMaterialId.FNumber",
            "TopRowCount": 100
        }
        
        resp = requests.post(url, json=data, cookies=self.cookie)
        return resp.json()

# 使用示例
client = K3CloudClient(
    server_url="http://your-k3-server/k3cloud",
    acct_id="您的账套ID",
    username="用户名",
    password="密码"
)

if client.login():
    # 查询某个物料的库存
    result = client.query_inventory(material_number="WL001")
    print(json.dumps(result, indent=2, ensure_ascii=False))
```

---

### 方案二：创建 MCP Server（推荐）

我可以为您创建一个专门的 MCP Server，提供以下工具：

- `query_stock_balance` - 查询库存余额
- `query_material_info` - 查询物料信息
- `query_stock_inout` - 查询收发存明细

**需要您提供的信息：**
1. 金蝶服务器地址（私有云/公有云）
2. 认证方式（用户名密码 / 第三方授权 / API签名）
3. 账套ID（数据中心标识）

---

### 方案三：使用现有 SDK

| 语言 | 推荐项目 | 特点 |
|------|---------|------|
| Go | `yrzs/k3cloud` | 简洁易用 |
| C# | `YiKdWebClient` | 功能最全，支持多种认证 |
| Node.js | `node-k3cloud` | 适合前端集成 |

---

## 建议

如果只是偶尔查询库存，**方案一**的 Python 脚本最轻量。

如果需要频繁使用或集成到工作流中，建议采用**方案二**创建 MCP Server，这样可以通过自然语言直接查询，比如：
> "查一下物料 WL001 在 A01 仓库的库存"

您希望采用哪种方案？如果选择方案二，请告诉我您的金蝶环境信息，我可以立即开始开发。
