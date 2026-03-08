# 纷享销客API测试脚本

本项目提供纷享销客OpenAPI的Python客户端和测试脚本，用于测试API连接并获取客户数据。

## 功能特性

- ✅ 自动获取和刷新access_token
- ✅ 查询客户列表
- ✅ 完整的错误处理（包括token过期自动重试）
- ✅ 详细的日志记录
- ✅ 符合PEP8规范的代码
- ✅ 环境变量管理敏感信息

## 目录结构

```
fxiaoke_test/
├── fxiaoke_client.py    # 纷享销客API客户端类
├── test_connection.py   # 连接测试脚本
├── requirements.txt     # Python依赖
└── README.md           # 使用说明
```

## 安装依赖

```bash
cd /home/admin/.openclaw/workspace/fxiaoke_test
pip install -r requirements.txt
```

## 配置环境变量

在运行测试脚本前，需要配置以下环境变量：

### 必需的环境变量

| 变量名 | 说明 | 获取方式 |
|--------|------|----------|
| `FXIAOKE_APP_ID` | 自建应用的appId | 纷享销客开放平台 → 应用管理 |
| `FXIAOKE_APP_SECRET` | 自建应用的appSecret | 纷享销客开放平台 → 应用管理 |
| `FXIAOKE_PERMANENT_CODE` | 永久授权码 | 企业管理员授权应用后获得 |

### 可选的环境变量

| 变量名 | 说明 | 默认值 |
|--------|------|--------|
| `FXIAOKE_USER_ID` | 员工ID（x-fs-userid） | 无 |
| `FXIAOKE_CLOUD` | 云环境 | `fxiaoke` |

### 云环境选项

| 值 | 说明 |
|----|------|
| `fxiaoke` | 纷享云（默认） |
| `huawei` | 华为云 |
| `aliyun` | 阿里云 |
| `hk_huawei` | 香港华为 |
| `frankfurt` | 法兰克福 |
| `north_america` | 北美云 |

### 配置示例

```bash
# 设置环境变量
export FXIAOKE_APP_ID="FSAID_xxxxxxxx"
export FXIAOKE_APP_SECRET="xxxxxxxxxxxxxxxx"
export FXIAOKE_PERMANENT_CODE="xxxxxxxxxxxxxxxx"
export FXIAOKE_USER_ID="FSUID_xxxxxxxx"  # 可选
export FXIAOKE_CLOUD="fxiaoke"  # 可选，默认纷享云
```

## 运行测试

```bash
python test_connection.py
```

### 测试内容

1. **环境变量检查** - 验证必需的环境变量是否已配置
2. **获取Access Token** - 测试OAuth认证流程
3. **查询客户列表** - 测试CRM数据查询接口
4. **Token自动刷新** - 测试token过期自动重试机制

### 预期输出

```
============================================================
  纷享销客API连接测试
============================================================
  本脚本用于测试纷享销客OpenAPI连接
  请确保已正确配置环境变量

============================================================
  环境变量检查
============================================================
  ✓ FXIAOKE_APP_ID: FSAID_xxxxx...
  ✓ FXIAOKE_APP_SECRET: xxxxxxxxxx...
  ✓ FXIAOKE_PERMANENT_CODE: xxxxxxxxxx...
  ○ FXIAOKE_USER_ID: 未设置（使用默认值）
  ○ FXIAOKE_CLOUD: 未设置（使用默认值）

============================================================
  初始化客户端
============================================================
  ✓ 客户端初始化成功
    - API域名: open.fxiaoke.com
    - 用户ID: 未设置

============================================================
  测试1: 获取Access Token
============================================================
2024-01-01 12:00:00 - INFO - 获取token成功，openUserId: FSCID_xxxxx...
  ✓ Token获取成功!
    - openUserId: FSCID_xxxxxxxx
    - accessToken: BCxxxxxxxx...
    - expiresIn: 7200秒
    - ea (企业账号): xxxxx
    - traceId: E-O.xxxxxxx

============================================================
  测试2: 查询客户列表
============================================================
2024-01-01 12:00:01 - INFO - 查询客户列表，limit=10, offset=0
  ✓ 查询成功!
    - 总记录数: 150
    - 本次返回: 10条

  客户数据预览:
    1. 测试客户1 (负责人: 张三, 创建时间: 1704067200000)
    2. 测试客户2 (负责人: 李四, 创建时间: 1703980800000)
    ...

============================================================
  测试3: Token自动刷新机制
============================================================
  - 模拟Token失效，重新查询客户列表...
  ✓ Token自动刷新成功!
    - 新Token: BCxxxxxxxx...

============================================================
  测试总结
============================================================
  ✓ 通过 - 获取Token
  ✓ 通过 - 查询客户列表
  ✓ 通过 - Token自动刷新

  总计: 3/3 项测试通过

  🎉 所有测试通过！API连接正常。
```

## 使用客户端类

### 基础用法

```python
from fxiaoke_client import FxiaoKeClient

# 从环境变量创建客户端
client = FxiaoKeClient.from_env()

# 获取token
token_result = client.get_token()
print(f"Token: {token_result['accessToken']}")

# 查询客户列表
accounts = client.query_accounts(
    fields=["_id", "name", "create_time", "owner__r.name"],
    limit=10
)
print(accounts)
```

### 手动配置客户端

```python
from fxiaoke_client import FxiaoKeClient

client = FxiaoKeClient(
    app_id="FSAID_xxxxxxxx",
    app_secret="xxxxxxxxxxxxxxxx",
    permanent_code="xxxxxxxxxxxxxxxx",
    cloud="fxiaoke",  # 云环境
    user_id="FSUID_xxxxxxxx"  # 可选
)

# 获取token
client.get_token()

# 查询客户
accounts = client.query_accounts(limit=10)
```

### 带过滤条件的查询

```python
# 查询特定名称的客户
accounts = client.query_accounts(
    filters=[{
        "operator": "EQ",
        "field_name": "name",
        "field_values": ["测试客户"]
    }],
    fields=["_id", "name", "create_time"],
    limit=10
)

# 模糊查询
accounts = client.query_accounts(
    filters=[{
        "operator": "LIKE",
        "field_name": "name",
        "field_values": ["科技"]
    }],
    limit=10
)
```

### 错误处理

```python
from fxiaoke_client import FxiaoKeClient, FxiaoKeError

client = FxiaoKeClient.from_env()

try:
    client.get_token()
    accounts = client.query_accounts(limit=10)
except FxiaoKeError as e:
    print(f"API错误: {e}")
    print(f"错误码: {e.error_code}")
    print(f"TraceId: {e.trace_id}")
```

## 注意事项

1. **不要提交真实凭证到代码仓库**
   - 使用环境变量管理敏感信息
   - 已将 `.env` 添加到 `.gitignore`

2. **Token有效期**
   - access_token有效期为2小时（7200秒）
   - 客户端会在token过期前5分钟自动刷新
   - 如果token过期，API调用会自动重试

3. **错误码处理**
   - `20005`: accessToken不存在或已过期
   - `20016`: corpAccessToken不存在或已过期
   - `30002`: 当天访问频次超限（0点重新统计）
   - `30003`: 客户没有购买openapi配额
   - `30004`: 秒频次超限

4. **分页限制**
   - limit最大值为100
   - offset从0开始，必须是limit的整数倍

## 获取凭证信息

### 1. 获取appId和appSecret

1. 登录纷享销客开放平台: https://open.fxiaoke.com/
2. 进入「应用管理」
3. 查看自建应用详情，获取appId和appSecret

### 2. 获取permanentCode（永久授权码）

1. 企业管理员登录纷享销客
2. 进入「应用管理」→「授权管理」
3. 授权应用后获得permanentCode

### 3. 获取user_id（员工ID）

1. 登录纷享销客CRM
2. 点击「CRM」应用
3. 搜索「人员」
4. 点击需要查询人员的「系统名」
5. 在账号信息中找到「员工ID」

## 参考文档

- [纷享销客开放平台](https://open.fxiaoke.com/)
- [开发文档](https://www.fxiaoke.com/mob/guide/openapi/dist/)
- [全局返回码](https://www.fxiaoke.com/mob/guide/openapi/dist/pages/open-api/guide/code/codes/)

## 许可证

MIT License
