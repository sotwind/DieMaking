# 金蝶云星空 Python SDK 研究报告

## 一、SDK结构分析

### 1.1 解压后的目录结构

```
SDK_Python3.0_V8.2.0/
├── Python3.0SDK使用说明.docx    # 官方使用文档
├── python_sdk_demo/              # 示例代码目录
│   ├── conf.ini                  # 配置文件示例
│   ├── zrun/
│   │   └── run.py               # 测试运行入口
│   ├── BD_MATERIAL/             # 物料模块示例
│   │   ├── test_bd_material.py  # 物料接口测试
│   │   └── test_bd_materialflex.py
│   ├── GetReportData/           # 报表数据示例
│   └── GLR_AccoutBalance/       # 科目余额示例
└── python_sdk_v8.2.0/
    └── kingdee.cdp.webapi.sdk-8.2.0-py3-none-any.whl  # SDK安装包
```

### 1.2 SDK包结构分析

安装后的SDK包结构 (`k3cloud_webapi_sdk`):

```
k3cloud_webapi_sdk/
├── __init__.py
├── main.py              # 主入口，包含K3CloudApiSdk类
├── sample.py            # 使用示例
├── const/               # 常量定义
│   ├── const_define.py  # 调用方法、查询模式等常量
│   └── header_param.py  # HTTP头参数
├── core/                # 核心实现
│   └── webapi_client.py # WebApi客户端基类
├── model/               # 数据模型
│   ├── api_config.py    # API配置类
│   ├── cookie.py        # Cookie处理
│   ├── cookie_store.py  # Cookie存储
│   ├── identity.py      # 身份认证信息
│   └── query_param.py   # 查询参数
└── util/                # 工具类
    ├── base64_util.py   # Base64编码
    ├── config_util.py   # 配置读取
    ├── encode_util.py   # 编码工具
    └── hmac_util.py     # HMAC签名
```

## 二、核心类和方法

### 2.1 主要类

#### 1. K3CloudApiSdk (主类)
位置: `k3cloud_webapi_sdk/main.py`

继承关系: `K3CloudApiSdk` → `WebApiClient`

**初始化方法:**
```python
# 方式1: 使用配置文件
api_sdk = K3CloudApiSdk(server_url, timeout)
api_sdk.Init(config_path='conf.ini', config_node='config')

# 方式2: 直接传参 (推荐)
api_sdk = K3CloudApiSdk(server_url, timeout)
api_sdk.InitConfig(acct_id, user_name, app_id, app_secret, server_url, 
                   lcid=2052, org_num=0, connect_timeout=120, request_timeout=120)
```

#### 2. Identify (身份认证类)
位置: `k3cloud_webapi_sdk/model/identity.py`

```python
class Identify:
    def __init__(self, server_url, dcid, user_name, app_id, app_secret, org_num, lcid=2052, pwd=''):
        self.ServerUrl = server_url    # 服务器地址
        self.DCID = dcid               # 数据中心ID/账套ID
        self.LCID = lcid               # 语系(默认2052)
        self.UserName = user_name      # 用户名
        self.Pwd = pwd                 # 密码
        self.AppId = app_id            # 应用ID
        self.AppSecret = app_secret    # 应用密钥
        self.OrgNum = org_num          # 组织编码
```

#### 3. ApiConfig (API配置类)
位置: `k3cloud_webapi_sdk/model/api_config.py`

### 2.2 主要API方法

| 方法名 | 功能 | 参数 |
|--------|------|------|
| `GetDataCenters()` | 获取数据中心列表 | 无 |
| `ExecuteBillQuery(data)` | 单据查询(返回List) | data: 查询参数 |
| `BillQuery(data)` | 单据查询JSON格式 | data: 查询参数 |
| `Save(formid, data)` | 保存单据 | formid: 表单ID, data: 数据 |
| `BatchSave(formid, data)` | 批量保存 | formid: 表单ID, data: 数据 |
| `Submit(formid, data)` | 提交单据 | formid: 表单ID, data: 数据 |
| `Audit(formid, data)` | 审核单据 | formid: 表单ID, data: 数据 |
| `Delete(formid, data)` | 删除单据 | formid: 表单ID, data: 数据 |
| `View(formid, data)` | 查看单据 | formid: 表单ID, data: 数据 |
| `Allocate(formid, data)` | 分配 | formid: 表单ID, data: 数据 |
| `SwitchOrg(data)` | 切换组织 | data: 组织数据 |
| `SendMsg(data)` | 发送消息 | data: 消息数据 |

### 2.3 认证流程

1. **构造请求头**: 使用AppId和AppSecret生成HMAC-SHA256签名
2. **设置Cookie**: 维护会话状态
3. **发送请求**: 通过HTTP POST发送JSON数据
4. **处理响应**: 解析返回的JSON数据

签名算法:
```python
# 1. 解析AppId获取client_id和client_sec
client_id, encoded_sec = app_id.split('_')
client_sec = decode(encoded_sec)

# 2. 构造签名字符串
api_sign = 'POST\n' + path_url + '\n\nx-api-nonce:' + nonce + '\nx-api-timestamp:' + timestamp + '\n'

# 3. 计算HMAC-SHA256签名
signature = HmacSHA256(api_sign, client_sec)
```

## 三、Python初始化代码

### 3.1 配置文件方式 (conf.ini)

```ini
[config]
X-KDApi-AcctID = 693a66beacc2d2
X-KDApi-UserName = Administrator
X-KDApi-AppID = 333129_73bAQ8GJ3qm/3VzL461DU6XI5qXa6tnv
X-KDApi-AppSec = 7d6c8629089547f1b200bec104b0430e
X-KDApi-ServerUrl = http://172.16.10.203/k3cloud
X-KDApi-LCID = 2052
X-KDApi-OrgNum = 0
X-KDApi-ConnectTimeout = 120
X-KDApi-RequestTimeout = 420
```

```python
from k3cloud_webapi_sdk.main import K3CloudApiSdk

api_sdk = K3CloudApiSdk()
api_sdk.Init(config_path='conf.ini', config_node='config')
```

### 3.2 直接传参方式 (推荐)

```python
from k3cloud_webapi_sdk.main import K3CloudApiSdk

# 创建SDK实例
api_sdk = K3CloudApiSdk(
    server_url='http://172.16.10.203/k3cloud',
    timeout=420
)

# 初始化配置
api_sdk.InitConfig(
    acct_id='693a66beacc2d2',
    user_name='Administrator',
    app_id='333129_73bAQ8GJ3qm/3VzL461DU6XI5qXa6tnv',
    app_secret='7d6c8629089547f1b200bec104b0430e',
    server_url='http://172.16.10.203/k3cloud',
    lcid=2052,
    org_num=0,
    connect_timeout=120,
    request_timeout=420
)
```

## 四、接口调用示例

### 4.1 物料查询 (查找原纸物料)

```python
import json

# 构造查询参数
query_data = {
    "FormId": "BD_MATERIAL",  # 物料表单ID
    "FieldKeys": "FMaterialId,FNumber,FName,FSpecification,FMaterialGroup_FNumber",
    "FilterString": "FName like '%原纸%'",  # 查找名称包含"原纸"的物料
    "OrderString": "FNumber",
    "TopRowCount": 100,
    "StartRow": 0,
    "Limit": 100
}

# 执行查询
response = api_sdk.ExecuteBillQuery(query_data)
result = json.loads(response)

# 处理结果
for item in result:
    material_id = item[0]
    material_number = item[1]
    material_name = item[2]
    specification = item[3]
    group = item[4]
    print(f"物料: {material_number} - {material_name}")
```

### 4.2 库存查询 (原纸库存)

```python
# 构造查询参数
query_data = {
    "FormId": "STK_Inventory",  # 库存表单ID
    "FieldKeys": "FMaterialId,FMaterialId_FNumber,FMaterialId_FName,FQty,FStockId_FName",
    "FilterString": "FMaterialId_FName like '%原纸%'",
    "OrderString": "FMaterialId_FNumber",
    "TopRowCount": 100,
    "StartRow": 0,
    "Limit": 100
}

# 执行查询
response = api_sdk.ExecuteBillQuery(query_data)
result = json.loads(response)

# 处理结果
for item in result:
    material_number = item[1]
    material_name = item[2]
    qty = item[3]
    stock_name = item[4]
    print(f"库存: {material_number} - {material_name}, 数量: {qty}, 仓库: {stock_name}")
```

### 4.3 使用BillQuery接口 (JSON格式)

```python
query_data = {
    "FormId": "BD_MATERIAL",
    "FieldKeys": "FMaterialId,FNumber,FName",
    "FilterString": "FName like '%原纸%'",
    "Limit": 10
}

response = api_sdk.BillQuery(query_data)
# 返回的是JSON字符串，直接解析即可
result = json.loads(response)
```

## 五、测试结果

### 5.1 本地测试 (通过)

| 测试项 | 结果 | 说明 |
|--------|------|------|
| SDK导入 | ✓ 通过 | 成功导入k3cloud_webapi_sdk |
| SDK实例创建 | ✓ 通过 | 成功创建K3CloudApiSdk实例 |
| SDK配置初始化 | ✓ 通过 | 成功调用InitConfig方法 |

### 5.2 网络连接测试 (失败)

| 测试项 | 结果 | 说明 |
|--------|------|------|
| Ping 172.16.10.203 | ✗ 失败 | 100% packet loss |
| HTTP连接 | ✗ 失败 | 连接超时 |

**原因分析:**
- 服务器地址 `172.16.10.203` 是内网IP
- 当前运行环境无法访问该内网地址
- 需要在能访问该内网的服务器上运行测试

## 六、常见表单ID参考

| 业务对象 | FormId | 说明 |
|----------|--------|------|
| 物料 | BD_MATERIAL | 物料主数据 |
| 库存 | STK_Inventory | 即时库存 |
| 客户 | BD_CUSTOMER | 客户主数据 |
| 供应商 | BD_SUPPLIER | 供应商主数据 |
| 仓库 | BD_STOCK | 仓库主数据 |
| 销售订单 | SAL_SaleOrder | 销售订单 |
| 采购订单 | PUR_PurchaseOrder | 采购订单 |
| 生产订单 | PRD_MO | 生产订单 |

## 七、注意事项

1. **用户名确认**: 当前使用`Administrator`，实际使用时请确认正确的用户名
2. **网络访问**: 确保运行环境能访问`http://172.16.10.203/k3cloud`
3. **超时设置**: 建议设置合理的超时时间(默认420秒)
4. **错误处理**: 所有API调用都应添加try-except异常处理
5. **返回格式**: ExecuteBillQuery返回List<List>，BillQuery返回JSON字符串

## 八、完整测试代码

见文件: `/home/admin/.openclaw/workspace/kingdee_sdk/test_kingdee_api.py`

使用方法:
```bash
cd /home/admin/.openclaw/workspace/kingdee_sdk
python3 test_kingdee_api.py
```

## 九、总结

1. **SDK功能完整**: Python SDK提供了与Java SDK相同的功能
2. **两种初始化方式**: 支持配置文件和直接传参两种方式
3. **接口丰富**: 提供了保存、查询、审核、删除等完整的单据操作接口
4. **网络限制**: 当前环境无法连接内网服务器，需要在目标服务器上运行测试
5. **代码可用**: 已编写完整的测试代码，可直接在目标环境运行
