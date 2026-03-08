# 金蝶云星空OpenAPI SDK测试报告

## 测试时间
2026-03-07

## 测试目标
使用金蝶云星空OpenAPI SDK测试获取原纸库存数据

## 提供的凭证信息
- **APPID**: 337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt
- **接口地址**: http://36.139.60.189/k3cloud
- **开放平台登录账号**: 13732300223
- **开放平台登录密码**: Zuifeng#3
- **开放平台网址**: https://openapi.open.kingdee.com

---

## 测试过程

### 第一步：登录金蝶开放平台

**结果**: ❌ 登录失败

**问题描述**:
1. 访问 https://openapi.open.kingdee.com 成功
2. 点击登录后跳转到 https://passport.kingdee.com/passport/#/login
3. 输入账号密码后，需要勾选《金蝶中国用户使用协议》和《金蝶中国隐私政策》
4. 协议复选框无法通过自动化方式点击，导致无法完成登录

**错误信息**:
```
请勾选《金蝶中国用户使用协议》和《金蝶中国隐私政策》
```

---

### 第二步：直接调用WebAPI接口

**发现**:
通过接口探测，发现该金蝶服务器支持以下WebAPI端点:
- `Kingdee.BOS.WebApi.ServicesStub.AuthService.AuthLogin.common.kdsvc` (状态码: 200)
- `Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.ExecuteBillQuery.common.kdsvc` (状态码: 200)

**登录测试**:

尝试了多种登录方式:

1. **使用APPID登录**:
```json
{
    "appid": "337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt"
}
```
**结果**: ❌ Unknown Error

2. **使用账套ID+用户名登录**:
```json
{
    "acctid": "337602",
    "username": "Administrator",
    "password": "",
    "lcid": 2052
}
```
**结果**: ❌ Unknown Error

---

### 第三步：查询接口测试

在未登录的情况下尝试查询接口:

**物料查询** (BD_MATERIAL):
```json
{
    "FormId": "BD_MATERIAL",
    "FilterString": "FMaterialName like '%原纸%'",
    "FieldKeys": "FMaterialNumber, FMaterialName, FSpecification",
    "Top": 100
}
```

**结果**: ❌ 会话信息已丢失，请重新登录
```json
{
    "Result": {
        "ResponseStatus": {
            "ErrorCode": 500,
            "IsSuccess": false,
            "Errors": [{
                "Message": "会话信息已丢失，请重新登录"
            }]
        }
    }
}
```

---

## 遇到的问题和解决方案

### 问题1: 开放平台登录协议勾选问题
- **描述**: 金蝶开放平台登录页面需要勾选用户协议，但该复选框无法通过常规自动化方式点击
- **影响**: 无法登录开放平台下载SDK和查看API文档

### 问题2: WebAPI登录认证失败
- **描述**: 尝试使用APPID直接调用WebAPI登录接口，返回"Unknown Error"
- **可能原因**:
  1. APPID格式不正确或缺少必要的认证参数
  2. 需要使用金蝶官方SDK进行签名计算
  3. 需要先登录开放平台获取AccessToken

### 问题3: 缺少必要的认证信息
- **描述**: 查询接口需要有效的会话信息，但无法获取到有效的登录凭证
- **可能原因**:
  1. 需要提供账套ID、用户名、密码进行登录
  2. 需要第三方登录授权流程

---

## 建议的解决方案

### 方案1: 使用金蝶官方SDK
1. 手动登录开放平台 https://openapi.open.kingdee.com
2. 进入SDK中心下载Python SDK
3. 使用SDK提供的认证方法进行登录
4. SDK会自动处理签名计算和会话管理

### 方案2: 获取正确的登录凭证
需要确认以下信息:
- 账套ID (AcctID)
- 正确的用户名和密码（不是开放平台账号，而是金蝶云星空系统账号）
- 是否需要额外的认证密钥

### 方案3: 联系金蝶技术支持
- 确认APPID的使用方式
- 获取正确的API调用示例
- 确认服务器是否已正确配置OpenAPI

---

## 测试代码示例

已编写测试代码保存在以下文件:
- `/home/admin/.openclaw/workspace/kingdee_test/kingdee_api_test.py` - 基于OpenAPI的测试代码
- `/home/admin/.openclaw/workspace/kingdee_test/kingdee_api_test_v2.py` - 基于WebAPI的测试代码

### 代码结构说明

```python
class KingdeeWebAPI:
    def __init__(self, server_url, app_id, app_secret=None):
        # 初始化API客户端
        
    def login(self):
        # WebAPI登录认证
        # 调用: Kingdee.BOS.WebApi.ServicesStub.AuthService.AuthLogin.common.kdsvc
        
    def execute_bill_query(self, form_id, filter_string=None, field_keys=None):
        # 执行单据查询
        # 调用: Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.ExecuteBillQuery.common.kdsvc
        
    def query_material(self, material_name=None, material_code=None):
        # 查询物料信息 (FormId: BD_MATERIAL)
        
    def query_inventory(self, material_code=None, warehouse_code=None):
        # 查询库存信息 (FormId: STK_Inventory)
```

### 关键API端点

| 功能 | 端点 |
|------|------|
| 登录认证 | `/Kingdee.BOS.WebApi.ServicesStub.AuthService.AuthLogin.common.kdsvc` |
| 单据查询 | `/Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.ExecuteBillQuery.common.kdsvc` |

### 关键表单ID

| 业务对象 | FormId |
|----------|--------|
| 物料主数据 | BD_MATERIAL |
| 即时库存 | STK_Inventory |

---

## 下一步建议

1. **手动下载SDK**: 请手动登录开放平台，下载Python SDK
2. **获取系统账号**: 确认金蝶云星空系统的登录账号（不是开放平台账号）
3. **参考官方文档**: 查看SDK中的示例代码，了解正确的认证方式
4. **测试连接**: 使用SDK提供的工具测试服务器连接

---

## 总结

- **SDK下载**: ❌ 未完成（无法登录开放平台）
- **API接口调用**: ⚠️ 部分成功（接口可访问，但认证失败）
- **原纸库存数据获取**: ❌ 未成功（需要有效的登录凭证）

**主要障碍**: 缺少有效的登录认证方式，需要金蝶官方SDK或正确的系统登录凭证。
