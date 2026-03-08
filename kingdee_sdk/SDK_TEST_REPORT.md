# 金蝶K3Cloud Python SDK 测试报告

## 测试时间
2026-03-07

## SDK信息
- **SDK版本**: Python3.0 V8.2.0
- **SDK路径**: `/home/admin/.openclaw/workspace/kingdee_sdk/`
- **Wheel文件**: `kingdee.cdp.webapi.sdk-8.2.0-py3-none-any.whl`

---

## 1. SDK结构分析

### 1.1 目录结构
```
SDK_Python3.0_V8.2.0/
├── Python3.0SDK使用说明.docx    # 使用说明文档
├── python_sdk_v8.2.0/           # SDK安装包
│   └── kingdee.cdp.webapi.sdk-8.2.0-py3-none-any.whl
└── python_sdk_demo/             # 示例代码
    ├── conf.ini                 # 配置文件示例
    ├── zrun/run.py              # 测试运行器
    ├── BD_MATERIAL/             # 物料接口示例
    ├── GLR_AccoutBalance/       # 科目余额示例
    └── GetReportData/           # 报表查询示例
```

### 1.2 SDK核心模块

| 模块 | 功能 |
|------|------|
| `k3cloud_webapi_sdk.main.K3CloudApiSdk` | 主API类 |
| `k3cloud_webapi_sdk.core.webapi_client.WebApiClient` | HTTP客户端 |
| `k3cloud_webapi_sdk.model.api_config.ApiConfig` | 配置模型 |
| `k3cloud_webapi_sdk.util.config_util` | 配置工具 |
| `k3cloud_webapi_sdk.util.hmac_util` | HMAC签名工具 |
| `k3cloud_webapi_sdk.util.encode_util` | 编码工具 |

### 1.3 主要API方法

| 方法 | 功能 |
|------|------|
| `Init()` | 从配置文件初始化 |
| `InitConfig()` | 直接传参初始化 |
| `GetDataCenters()` | 获取数据中心列表 |
| `ExecuteBillQuery()` | 单据查询（数组返回） |
| `BillQuery()` | 单据查询（JSON返回） |
| `Save()` | 保存单据 |
| `Submit()` | 提交单据 |
| `Audit()` | 审核单据 |
| `View()` | 查看单据 |
| `Delete()` | 删除单据 |

---

## 2. 认证机制分析

### 2.1 认证流程

金蝶OpenAPI使用**HMAC-SHA256**签名认证机制：

1. **AppId格式**: `{client_id}_{encoded_client_sec}`
   - 示例: `337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt`
   - client_id: `337602`
   - encoded_client_sec: `241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt`

2. **请求头参数**:
   - `X-Api-ClientID`: 客户端ID
   - `X-Api-Auth-Version`: 认证版本 (2.0)
   - `X-Api-Timestamp`: 时间戳
   - `X-Api-Nonce`: 随机数
   - `X-Api-Signature`: HMAC-SHA256签名
   - `X-KD-AppKey`: 应用ID
   - `X-KD-AppData`: Base64编码的账套数据
   - `X-KD-Signature`: 应用签名

3. **签名算法**:
   - 使用HMAC-SHA256对请求路径和时间戳进行签名
   - 密钥来自AppSecret

### 2.2 初始化参数

```python
api_sdk.InitConfig(
    acct_id="66306281bdfb20",           # 数据中心ID (dCID)
    user_name="Administrator",          # 用户名
    app_id="337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt",  # 应用ID
    app_secret="",                       # 应用密钥 (必需)
    server_url="http://k3.forestpacking.com/k3cloud",  # 服务器地址
    lcid=2052,                          # 语言代码
    org_num=0,                          # 组织编码
    connect_timeout=120,                # 连接超时
    request_timeout=420                 # 请求超时
)
```

---

## 3. 测试结果

### 3.1 测试环境
- **服务器**: http://k3.forestpacking.com/k3cloud
- **数据中心ID**: 66306281bdfb20
- **应用ID**: 337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt
- **用户名**: Administrator

### 3.2 测试结果摘要

| 测试项 | 状态 | 说明 |
|--------|------|------|
| SDK初始化 | ❌ 失败 | 缺少AppSecret |
| 获取数据中心 | ❌ 失败 | SDK未正确初始化 |
| 查询物料 | ❌ 失败 | SDK未正确初始化 |
| 查询库存 | ❌ 失败 | SDK未正确初始化 |
| BillQuery | ❌ 失败 | SDK未正确初始化 |

### 3.3 错误信息

```
SDK初始化失败，缺少必填授权项：应用密钥
```

所有API调用返回:
```
拒绝请求，请先正确初始化!
```

---

## 4. 问题分析

### 4.1 根本原因
**缺少AppSecret（应用密钥）**

金蝶OpenAPI认证需要两个关键参数：
1. **AppId**: 应用ID，已配置
2. **AppSecret**: 应用密钥，**未配置**

### 4.2 AppSecret获取方式

根据金蝶开放平台文档，AppSecret需要通过以下方式获取：

1. **登录金蝶开放平台**
   - 网址: https://open.kingdee.com/
   - 使用企业账号登录

2. **进入应用管理**
   - 找到对应的应用（AppId: 337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt）

3. **获取AppSecret**
   - 在应用详情页查看或重置AppSecret
   - AppSecret通常是一串32位的字符串

4. **配置权限**
   - 确保应用有权限访问数据中心（66306281bdfb20）
   - 确保应用有权限调用相关API接口

---

## 5. Python初始化代码示例

### 5.1 方式一：直接传参初始化（推荐）

```python
from k3cloud_webapi_sdk.main import K3CloudApiSdk

# 创建SDK实例
api_sdk = K3CloudApiSdk(
    server_url="http://k3.forestpacking.com/k3cloud",
    timeout=420
)

# 初始化配置
api_sdk.InitConfig(
    acct_id="66306281bdfb20",
    user_name="Administrator",
    app_id="337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt",
    app_secret="YOUR_APP_SECRET_HERE",  # 需要替换为实际的AppSecret
    server_url="http://k3.forestpacking.com/k3cloud",
    lcid=2052,
    org_num=0,
    connect_timeout=120,
    request_timeout=420
)
```

### 5.2 方式二：配置文件初始化

创建 `conf.ini`:
```ini
[config]
X-KDApi-AcctID = 66306281bdfb20
X-KDApi-UserName = Administrator
X-KDApi-AppID = 337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt
X-KDApi-AppSec = YOUR_APP_SECRET_HERE
X-KDApi-ServerUrl = http://k3.forestpacking.com/k3cloud
X-KDApi-LCID = 2052
X-KDApi-ConnectTimeout = 120
X-KDApi-RequestTimeout = 420
```

Python代码:
```python
api_sdk = K3CloudApiSdk("http://k3.forestpacking.com/k3cloud")
api_sdk.Init(config_path='conf.ini', config_node='config')
```

---

## 6. 接口调用示例

### 6.1 查询物料（查找"原纸"）

```python
para = {
    "FormId": "BD_MATERIAL",
    "FieldKeys": "FMaterialId,FNumber,FName,FSpecification",
    "FilterString": "FName like '%原纸%'",
    "OrderString": "FNumber",
    "TopRowCount": 0,
    "StartRow": 0,
    "Limit": 100
}

response = api_sdk.ExecuteBillQuery(para)
data = json.loads(response)
for item in data:
    print(f"物料编码: {item[1]}, 物料名称: {item[2]}")
```

### 6.2 查询库存

```python
para = {
    "FormId": "STK_Inventory",
    "FieldKeys": "FMaterialId,FMaterialId_FNumber,FMaterialId_FName,FQty",
    "FilterString": "FMaterialId_FName like '%原纸%'",
    "Limit": 100
}

response = api_sdk.ExecuteBillQuery(para)
data = json.loads(response)
for item in data:
    print(f"物料: {item[2]}, 库存数量: {item[3]}")
```

### 6.3 获取数据中心列表

```python
response = api_sdk.GetDataCenters()
data = json.loads(response)
for dc in data:
    print(f"数据中心: {dc}")
```

---

## 7. 下一步建议

### 7.1 立即需要完成

1. **获取AppSecret**
   - 登录金蝶开放平台: https://open.kingdee.com/
   - 找到应用ID为 `337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt` 的应用
   - 获取或重置AppSecret

2. **验证网络连通性**
   ```bash
   curl -I http://k3.forestpacking.com/k3cloud/
   ```

3. **验证数据中心权限**
   - 确认应用有权限访问数据中心 `66306281bdfb20`

### 7.2 获取AppSecret后

1. 更新测试代码中的 `CONFIG["app_secret"]`
2. 重新运行测试脚本
3. 验证各项API调用是否正常

### 7.3 常见问题排查

| 问题 | 可能原因 | 解决方案 |
|------|----------|----------|
| SDK初始化失败 | AppSecret为空 | 配置正确的AppSecret |
| 认证失败 | AppId或AppSecret错误 | 检查开放平台配置 |
| 网络超时 | 服务器不可达 | 检查网络连接和防火墙 |
| 无权限访问 | 应用未授权 | 在开放平台配置权限 |
| 数据中心不存在 | dCID错误 | 确认数据中心ID正确 |

---

## 8. 附录

### 8.1 测试脚本位置
```
/home/admin/.openclaw/workspace/kingdee_sdk/test_kingdee_api.py
```

### 8.2 SDK包位置
```
/home/admin/.openclaw/workspace/kingdee_sdk/sdk_package/
```

### 8.3 相关文档
- 金蝶开放平台: https://open.kingdee.com/
- SDK使用说明: `SDK_Python3.0_V8.2.0/Python3.0SDK使用说明.docx`
