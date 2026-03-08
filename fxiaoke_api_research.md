# 纷享销客OpenAPI研究报告

## 一、概述

### 开放平台地址
- **开放平台**: https://open.fxiaoke.com/
- **开发文档**: https://www.fxiaoke.com/mob/guide/openapi/dist/

### 所在云域名
根据企业所在云不同，API域名也有所不同：

| 云环境 | 域名 |
|--------|------|
| 纷享云 | open.fxiaoke.com |
| 华为云 | open-hwcloud.fxiaoke.com |
| 阿里云 | open-ale.fxiaoke.com |
| 香港华为 | open-ksc.sharecrm.com |
| 法兰克福 | open-hws.fxiaoke.com |
| 北美云 | open-na.sharecrm.com |

---

## 二、认证机制

### 2.1 客户端凭证模式 (OAuth 2.0)

适用于服务端之间的授权（无用户参与），客户端直接向授权服务器提交 "客户端 ID + 客户端密钥"，验证通过后获取访问令牌。

#### 获取Token接口

**请求方式**: POST + application/json

**请求路径**: `https://{云域名}/oauth2.0/token?thirdTraceId={UUID}`

**请求参数**:

| 参数 | 类型 | 是否必填 | 说明 |
|------|------|----------|------|
| appId | String | 是 | 自建应用的appId |
| appSecret | String | 是 | 自建应用的appSecret |
| permanentCode | String | 是 | 永久授权码 |
| grantType | String | 是 | 授权模式，固定值为：app_secret |

**请求示例**:
```json
{
  "appId": "FSAID_xxxxx",
  "permanentCode": "3F9xxxxxCA5",
  "appSecret": "e4d0xxxxxdff",
  "grantType": "app_secret"
}
```

**返回参数**:

| 参数 | 类型 | 说明 |
|------|------|------|
| openUserId | String | 用户的openUserId |
| accessToken | String | 授权凭证token，有效期2小时 |
| expiresIn | Int | 过期时间（秒） |
| appId | String | 自建应用的appId |
| ea | String | 企业账号 |

**返回示例**:
```json
{
  "openUserId": "FSCID_xxxxxxx",
  "accessToken": "BCxxxxxDF2",
  "expiresIn": 7084,
  "appId": "FSAID_xxxxx",
  "ea": "fxxxx1",
  "errorCode": 0,
  "errorMessage": "success",
  "traceId": "E-O.fxxxxx6b"
}
```

### 2.2 Token刷新机制

- **有效期**: 7200秒（2小时）
- **建议刷新时间**: 6600-6650秒之间刷新
- **缓存要求**: 本接口需要缓存至少6600秒
- **错误码**: 20016 表示token过期

---

## 三、API调用公共参数

### 3.1 URL参数

所有接口都需要在URL后面填入一个随机字符串作为 `thirdTraceId`，用于标识每一次请求。

- **格式**: RFC 4122 标准 UUID Version 4
- **示例**: `https://www.fxiaoke.com/cgi/crm/v2/data/get?thirdTraceId=5ea0422d-98e3-49e0-a3cb-9bd5517d1f30`

### 3.2 请求头 Headers

每次接口请求前（获取token的请求除外），都需要在Headers设置以下请求头：

| 请求头 | 类型 | 获取途径 | 示例 |
|--------|------|----------|------|
| authorization | String | 固定值 "Bearer " + accessToken（注意Bearer后有一个空格） | Bearer 1247D6096162AB2FDCEA46D6D7B74B33 |
| x-fs-ea | String | 从获取token接口返回结果中获取 "ea" | 12345 |
| x-fs-userid | String | 在CRM应用中搜索"人员"，点击系统名，在账号信息中找到"员工ID" | FSUID_xxxxx |

---

## 四、CRM核心数据接口

### 4.1 接口通用规范

**请求方式**: POST + application/json

**请求路径**: `https://{云域名}/cgi/crm/v2/data/query?thirdTraceId={UUID}`

### 4.2 通用请求参数

| 参数 | 类型 | 是否必填 | 说明 |
|------|------|----------|------|
| data | Map | 是 | 数据map |
| dataObjectApiName | String | 是 | 对象的api_name |
| find_explicit_total_num | Boolean | 否 | 是否返回总数（默认true） |
| search_query_info | Map | 是 | 查询条件说明 |
| search_query_info.limit | Int | 是 | 分页条数（最大100） |
| search_query_info.offset | Int | 是 | 偏移量（从0开始，必须是limit的整数倍） |
| search_query_info.filters | List | 是 | 过滤条件列表 |
| search_query_info.orders | List | 是 | 排序条件 |
| search_query_info.fieldProjection | List[String] | 是 | 返回字段列表 |

### 4.3 Operator操作符说明

| 参数 | 含义 | 参数 | 含义 |
|------|------|------|------|
| EQ | 等于 | N | 不等于 |
| GT | 大于 | GTE | 大于等于 |
| LT | 小于 | LTE | 小于等于 |
| LIKE | 包含 | NLIKE | 不包含 |
| IS | 为空 | ISN | 不为空 |
| IN | 属于 | NIN | 不属于 |
| BETWEEN | 介于 | NBETWEEN | 不介于 |
| STARTWITH | 开始于 | ENDWITH | 结束于 |
| HASANYOF | 有重叠元素 | NHASANYOF | 没有重叠 |

### 4.4 核心CRM对象接口

#### 客户对象 (AccountObj)

**对象名**: AccountObj

**主要接口**:
- 查询单个客户: `/cgi/crm/v2/data/get`
- 查询客户列表: `/cgi/crm/v2/data/query`
- 创建客户: `/cgi/crm/v2/data/add`
- 修改客户: `/cgi/crm/v2/data/update`
- 变更负责人: `/cgi/crm/v2/data/changeOwner`
- 作废客户: `/cgi/crm/v2/data/invalid`
- 恢复客户: `/cgi/crm/v2/data/recover`

#### 联系人对象 (ContactObj)

**对象名**: ContactObj

**主要接口**:
- 查询单个联系人: `/cgi/crm/v2/data/get`
- 查询联系人列表: `/cgi/crm/v2/data/query`
- 创建联系人: `/cgi/crm/v2/data/add`
- 修改联系人: `/cgi/crm/v2/data/update`

#### 商机对象 (OpportunityObj)

**对象名**: OpportunityObj

**主要接口**:
- 查询单个商机: `/cgi/crm/v2/data/get`
- 查询商机列表: `/cgi/crm/v2/data/query`
- 创建商机: `/cgi/crm/v2/data/add`
- 修改商机: `/cgi/crm/v2/data/update`
- 变更负责人: `/cgi/crm/v2/data/changeOwner`

#### 产品对象 (ProductObj)

**对象名**: ProductObj

**主要接口**:
- 查询单个产品: `/cgi/crm/v2/data/get`
- 查询产品列表: `/cgi/crm/v2/data/query`
- 创建产品: `/cgi/crm/v2/data/add`
- 修改产品: `/cgi/crm/v2/data/update`

#### 库存对象 (StockObj)

**对象名**: StockObj

**主要接口**:
- 查询单个库存: `/cgi/crm/v2/data/get`
- 查询库存列表: `/cgi/crm/v2/data/query`

#### 销售订单对象 (SalesOrderObj)

**对象名**: SalesOrderObj

**主要接口**:
- 查询单个订单: `/cgi/crm/v2/data/get`
- 查询订单列表: `/cgi/crm/v2/data/query`
- 创建订单: `/cgi/crm/v2/data/add`
- 修改订单: `/cgi/crm/v2/data/update`

---

## 五、全局返回码

| 返回码 | 说明 |
|--------|------|
| -2 | 系统错误 |
| -1 | 系统繁忙 |
| 0 | 请求成功 |
| 10001 | 缺少参数appId |
| 10002 | 缺少参数appSecret |
| 10012 | 缺少参数permanentCode |
| 11001 | 参数appId不合法 |
| 11002 | 参数appSecret不合法 |
| 11013 | 参数permanentCode不合法 |
| 12002 | 登录状态错误 |
| 14001 | 接口调用超过限制 |
| 15002 | 参数不合法 |
| 15003 | APP没有访问权限 |
| 20005 | accessToken不存在或者已经过期 |
| 20006 | appId或appSecret错误 |
| 20015 | 永久授权码错误 |
| **20016** | **corpAccessToken不存在或者已经过期** |
| 20020 | 应用没有获取该企业的数据的权限 |
| 20021 | 在当前企业下，该app的状态为停用 |
| 20022 | 企业没有对该app授权 |
| 30002 | 当天访问频次超限（0点重新统计） |
| 30003 | 客户没有购买openapi配额 |
| 30004 | 秒频次超限 |
| 30007 | 部门不存在 |
| 30027 | 员工不存在 |
| 32000 | 参数错误 |
| 50009 | 服务异常，如:超时 |

---

## 六、Python示例代码

### 6.1 完整Python SDK类

```python
"""
纷享销客OpenAPI Python SDK
"""

import requests
import uuid
import time
from datetime import datetime, timedelta
from typing import Dict, List, Optional, Any


class FxiaoKeAPI:
    """纷享销客OpenAPI客户端"""
    
    # 云域名映射
    CLOUD_DOMAINS = {
        'fxiaoke': 'open.fxiaoke.com',
        'huawei': 'open-hwcloud.fxiaoke.com',
        'aliyun': 'open-ale.fxiaoke.com',
        'hk_huawei': 'open-ksc.sharecrm.com',
        'frankfurt': 'open-hws.fxiaoke.com',
        'north_america': 'open-na.sharecrm.com',
    }
    
    # CRM对象API名称映射
    OBJECT_API_NAMES = {
        'account': 'AccountObj',           # 客户
        'contact': 'ContactObj',           # 联系人
        'opportunity': 'OpportunityObj',   # 商机
        'product': 'ProductObj',           # 产品
        'stock': 'StockObj',               # 库存
        'sales_order': 'SalesOrderObj',    # 销售订单
        'high_seas': 'HighSeasObj',        # 公海客户
        'lead': 'LeadObj',                 # 线索
        'contract': 'ContractObj',         # 合同
        'payment': 'PaymentObj',           # 回款
        'refund': 'RefundObj',             # 退款
        'warehouse': 'WarehouseObj',       # 仓库
        'inventory': 'InventoryObj',       # 库存明细
        'spu': 'SPUObj',                   # 商品
        'price_book': 'PriceBookObj',      # 价目表
        'quote': 'QuoteObj',               # 报价单
        'activity': 'ActivityObj',         # 市场活动
        'task': 'TaskObj',                 # 任务
        'schedule': 'ScheduleObj',         # 日程
        'department': 'DepartmentObj',     # 部门
        'user': 'UserObj',                 # 人员
    }
    
    def __init__(self, app_id: str, app_secret: str, permanent_code: str, 
                 cloud: str = 'fxiaoke', user_id: str = None):
        """
        初始化API客户端
        
        Args:
            app_id: 自建应用的appId
            app_secret: 自建应用的appSecret
            permanent_code: 永久授权码
            cloud: 云环境，默认为'fxiaoke'
            user_id: 员工ID（x-fs-userid）
        """
        self.app_id = app_id
        self.app_secret = app_secret
        self.permanent_code = permanent_code
        self.user_id = user_id
        
        # 设置域名
        if cloud in self.CLOUD_DOMAINS:
            self.domain = self.CLOUD_DOMAINS[cloud]
        else:
            self.domain = cloud  # 允许直接传入自定义域名
        
        self.base_url = f"https://{self.domain}"
        
        # Token信息
        self.access_token = None
        self.ea = None
        self.open_user_id = None
        self.token_expire_time = None
        
    def _generate_trace_id(self) -> str:
        """生成UUID v4格式的traceId"""
        return str(uuid.uuid4())
    
    def _get_headers(self) -> Dict[str, str]:
        """获取请求头"""
        if not self.access_token:
            raise ValueError("请先获取access_token")
        
        headers = {
            'Content-Type': 'application/json',
            'authorization': f'Bearer {self.access_token}',
            'x-fs-ea': self.ea,
        }
        
        if self.user_id:
            headers['x-fs-userid'] = self.user_id
            
        return headers
    
    def get_token(self) -> Dict[str, Any]:
        """
        获取access_token
        
        Returns:
            包含access_token等信息的字典
        """
        url = f"{self.base_url}/oauth2.0/token?thirdTraceId={self._generate_trace_id()}"
        
        payload = {
            "appId": self.app_id,
            "appSecret": self.app_secret,
            "permanentCode": self.permanent_code,
            "grantType": "app_secret"
        }
        
        response = requests.post(url, json=payload)
        result = response.json()
        
        if result.get('errorCode') == 0:
            self.access_token = result['accessToken']
            self.ea = result['ea']
            self.open_user_id = result['openUserId']
            # 计算token过期时间（提前100秒过期，用于缓冲）
            expires_in = result.get('expiresIn', 7200)
            self.token_expire_time = datetime.now() + timedelta(seconds=expires_in - 100)
            
        return result
    
    def refresh_token_if_needed(self):
        """检查并刷新token（如果即将过期）"""
        if not self.access_token or not self.token_expire_time:
            return self.get_token()
        
        # 如果token将在5分钟内过期，则刷新
        if datetime.now() >= self.token_expire_time - timedelta(minutes=5):
            return self.get_token()
        
        return {'errorCode': 0, 'message': 'Token still valid'}
    
    def _make_request(self, endpoint: str, data: Dict[str, Any]) -> Dict[str, Any]:
        """
        发送API请求
        
        Args:
            endpoint: API端点
            data: 请求数据
            
        Returns:
            API响应结果
        """
        # 确保token有效
        self.refresh_token_if_needed()
        
        url = f"{self.base_url}{endpoint}?thirdTraceId={self._generate_trace_id()}"
        headers = self._get_headers()
        
        response = requests.post(url, json=data, headers=headers)
        result = response.json()
        
        # 处理token过期错误（20016）
        if result.get('errorCode') == 20016:
            self.get_token()  # 重新获取token
            headers = self._get_headers()
            response = requests.post(url, json=data, headers=headers)
            result = response.json()
            
        return result
    
    def query_data(self, object_api_name: str, 
                   filters: List[Dict] = None,
                   orders: List[Dict] = None,
                   fields: List[str] = None,
                   limit: int = 100,
                   offset: int = 0,
                   return_total: bool = True) -> Dict[str, Any]:
        """
        查询数据列表
        
        Args:
            object_api_name: 对象API名称
            filters: 过滤条件列表
            orders: 排序条件列表
            fields: 返回字段列表
            limit: 分页条数（最大100）
            offset: 偏移量
            return_total: 是否返回总数
            
        Returns:
            查询结果
        """
        search_query_info = {
            "limit": limit,
            "offset": offset,
        }
        
        if filters:
            search_query_info["filters"] = filters
        else:
            search_query_info["filters"] = []
            
        if orders:
            search_query_info["orders"] = orders
        else:
            search_query_info["orders"] = [{"fieldName": "create_time", "isAsc": False}]
            
        if fields:
            search_query_info["fieldProjection"] = fields
        else:
            search_query_info["fieldProjection"] = ["_id", "name"]
        
        data = {
            "data": {
                "dataObjectApiName": object_api_name,
                "find_explicit_total_num": return_total,
                "search_query_info": search_query_info
            }
        }
        
        return self._make_request('/cgi/crm/v2/data/query', data)
    
    def get_data(self, object_api_name: str, object_data_id: str) -> Dict[str, Any]:
        """
        查询单个数据详情
        
        Args:
            object_api_name: 对象API名称
            object_data_id: 数据ID
            
        Returns:
            数据详情
        """
        data = {
            "data": {
                "dataObjectApiName": object_api_name,
                "objectDataId": object_data_id
            }
        }
        
        return self._make_request('/cgi/crm/v2/data/get', data)
    
    def create_data(self, object_api_name: str, data_map: Dict[str, Any]) -> Dict[str, Any]:
        """
        创建数据
        
        Args:
            object_api_name: 对象API名称
            data_map: 数据字段值
            
        Returns:
            创建结果
        """
        data = {
            "data": {
                "dataObjectApiName": object_api_name,
                "objectData": data_map
            }
        }
        
        return self._make_request('/cgi/crm/v2/data/add', data)
    
    def update_data(self, object_api_name: str, object_data_id: str, 
                    data_map: Dict[str, Any]) -> Dict[str, Any]:
        """
        更新数据
        
        Args:
            object_api_name: 对象API名称
            object_data_id: 数据ID
            data_map: 更新的字段值
            
        Returns:
            更新结果
        """
        data = {
            "data": {
                "dataObjectApiName": object_api_name,
                "objectDataId": object_data_id,
                "objectData": data_map
            }
        }
        
        return self._make_request('/cgi/crm/v2/data/update', data)
    
    def delete_data(self, object_api_name: str, object_data_id: str) -> Dict[str, Any]:
        """
        作废数据（逻辑删除）
        
        Args:
            object_api_name: 对象API名称
            object_data_id: 数据ID
            
        Returns:
            删除结果
        """
        data = {
            "data": {
                "dataObjectApiName": object_api_name,
                "objectDataId": object_data_id
            }
        }
        
        return self._make_request('/cgi/crm/v2/data/invalid', data)


# ==================== 便捷方法封装 ====================

class FxiaoKeCRM(FxiaoKeAPI):
    """纷享销客CRM便捷操作类"""
    
    def query_accounts(self, filters: List[Dict] = None, 
                       fields: List[str] = None, 
                       limit: int = 100) -> Dict[str, Any]:
        """查询客户列表"""
        return self.query_data(
            object_api_name=self.OBJECT_API_NAMES['account'],
            filters=filters,
            fields=fields,
            limit=limit
        )
    
    def get_account(self, account_id: str) -> Dict[str, Any]:
        """获取客户详情"""
        return self.get_data(
            object_api_name=self.OBJECT_API_NAMES['account'],
            object_data_id=account_id
        )
    
    def query_contacts(self, filters: List[Dict] = None,
                       fields: List[str] = None,
                       limit: int = 100) -> Dict[str, Any]:
        """查询联系人列表"""
        return self.query_data(
            object_api_name=self.OBJECT_API_NAMES['contact'],
            filters=filters,
            fields=fields,
            limit=limit
        )
    
    def get_contact(self, contact_id: str) -> Dict[str, Any]:
        """获取联系人详情"""
        return self.get_data(
            object_api_name=self.OBJECT_API_NAMES['contact'],
            object_data_id=contact_id
        )
    
    def query_opportunities(self, filters: List[Dict] = None,
                            fields: List[str] = None,
                            limit: int = 100) -> Dict[str, Any]:
        """查询商机列表"""
        return self.query_data(
            object_api_name=self.OBJECT_API_NAMES['opportunity'],
            filters=filters,
            fields=fields,
            limit=limit
        )
    
    def get_opportunity(self, opportunity_id: str) -> Dict[str, Any]:
        """获取商机详情"""
        return self.get_data(
            object_api_name=self.OBJECT_API_NAMES['opportunity'],
            object_data_id=opportunity_id
        )
    
    def query_products(self, filters: List[Dict] = None,
                       fields: List[str] = None,
                       limit: int = 100) -> Dict[str, Any]:
        """查询产品列表"""
        return self.query_data(
            object_api_name=self.OBJECT_API_NAMES['product'],
            filters=filters,
            fields=fields,
            limit=limit
        )
    
    def get_product(self, product_id: str) -> Dict[str, Any]:
        """获取产品详情"""
        return self.get_data(
            object_api_name=self.OBJECT_API_NAMES['product'],
            object_data_id=product_id
        )
    
    def query_stock(self, filters: List[Dict] = None,
                    fields: List[str] = None,
                    limit: int = 100) -> Dict[str, Any]:
        """查询库存列表"""
        return self.query_data(
            object_api_name=self.OBJECT_API_NAMES['stock'],
            filters=filters,
            fields=fields,
            limit=limit
        )
    
    def query_sales_orders(self, filters: List[Dict] = None,
                           fields: List[str] = None,
                           limit: int = 100) -> Dict[str, Any]:
        """查询销售订单列表"""
        return self.query_data(
            object_api_name=self.OBJECT_API_NAMES['sales_order'],
            filters=filters,
            fields=fields,
            limit=limit
        )
    
    def get_sales_order(self, order_id: str) -> Dict[str, Any]:
        """获取销售订单详情"""
        return self.get_data(
            object_api_name=self.OBJECT_API_NAMES['sales_order'],
            object_data_id=order_id
        )


# ==================== 使用示例 ====================

def main():
    """使用示例"""
    
    # 初始化客户端（需要替换为实际的凭证信息）
    # 注意：以下凭证需要用户从纷享销客开放平台获取
    client = FxiaoKeCRM(
        app_id="FSAID_xxxxxxxx",           # 自建应用的appId
        app_secret="xxxxxxxxxxxxxxxx",      # 自建应用的appSecret
        permanent_code="xxxxxxxxxxxxxxxx",  # 永久授权码
        cloud="fxiaoke",                    # 云环境
        user_id="FSUID_xxxxxxxx"            # 员工ID
    )
    
    # 1. 获取access_token
    token_result = client.get_token()
    print("Token获取结果:", token_result)
    
    if token_result.get('errorCode') != 0:
        print("获取Token失败:", token_result.get('errorMessage'))
        return
    
    # 2. 查询客户列表示例
    # 查询所有客户，返回前10条
    accounts = client.query_accounts(
        fields=["_id", "name", "create_time", "owner__r.name"],
        limit=10
    )
    print("客户列表:", accounts)
    
    # 3. 根据条件查询客户
    # 查询客户名称为"测试客户"的客户
    filtered_accounts = client.query_accounts(
        filters=[{
            "operator": "EQ",
            "field_name": "name",
            "field_values": ["测试客户"]
        }],
        fields=["_id", "name", "create_time"],
        limit=10
    )
    print("筛选后的客户:", filtered_accounts)
    
    # 4. 查询联系人列表
    contacts = client.query_contacts(
        fields=["_id", "name", "account_id__r.name", "phone"],
        limit=10
    )
    print("联系人列表:", contacts)
    
    # 5. 查询商机列表
    opportunities = client.query_opportunities(
        fields=["_id", "name", "account_id__r.name", "amount", "stage__r.name"],
        limit=10
    )
    print("商机列表:", opportunities)
    
    # 6. 查询产品列表
    products = client.query_products(
        fields=["_id", "name", "product_code", "price"],
        limit=10
    )
    print("产品列表:", products)
    
    # 7. 查询库存列表
    stock = client.query_stock(
        fields=["_id", "name", "product_id__r.name", "quantity"],
        limit=10
    )
    print("库存列表:", stock)
    
    # 8. 查询销售订单
    orders = client.query_sales_orders(
        fields=["_id", "name", "account_id__r.name", "amount", "order_date"],
        limit=10
    )
    print("销售订单列表:", orders)
    
    # 9. 获取单个客户详情
    if accounts.get('data', {}).get('dataList'):
        first_account_id = accounts['data']['dataList'][0]['_id']
        account_detail = client.get_account(first_account_id)
        print("客户详情:", account_detail)


if __name__ == "__main__":
    main()
```

### 6.2 过滤条件构建示例

```python
# 构建复杂过滤条件示例

# 1. 等于条件
filter_eq = {
    "operator": "EQ",
    "field_name": "name",
    "field_values": ["测试客户"]
}

# 2. 不等于条件
filter_ne = {
    "operator": "N",
    "field_name": "status",
    "field_values": ["已作废"]
}

# 3. 包含条件（模糊查询）
filter_like = {
    "operator": "LIKE",
    "field_name": "name",
    "field_values": ["科技"]
}

# 4. 大于条件（用于数值或日期）
filter_gt = {
    "operator": "GT",
    "field_name": "create_time",
    "field_values": ["1704067200000"]  # 时间戳（毫秒）
}

# 5. 介于条件（用于日期范围）
filter_between = {
    "operator": "BETWEEN",
    "field_name": "create_time",
    "field_values": ["1704067200000", "1706745600000"]
}

# 6. 属于条件（多选）
filter_in = {
    "operator": "IN",
    "field_name": "stage",
    "field_values": ["初步接触", "需求确认", "商务谈判"]
}

# 7. 为空条件
filter_is = {
    "operator": "IS",
    "field_name": "phone",
    "field_values": []
}

# 8. 组合多个条件
complex_filters = [
    {
        "operator": "EQ",
        "field_name": "owner__r.name",
        "field_values": ["张三"]
    },
    {
        "operator": "GTE",
        "field_name": "create_time",
        "field_values": ["1704067200000"]
    },
    {
        "operator": "LIKE",
        "field_name": "name",
        "field_values": ["重要"]
    }
]

# 使用复杂条件查询
result = client.query_accounts(
    filters=complex_filters,
    fields=["_id", "name", "create_time", "owner__r.name"],
    orders=[{"fieldName": "create_time", "isAsc": False}],
    limit=50
)
```

---

## 七、需要用户提供的凭证信息

使用纷享销客OpenAPI前，需要用户准备以下信息：

### 7.1 应用凭证（从纷享销客开放平台获取）

1. **appId**: 自建应用的appId
   - 获取方式：登录纷享销客开放平台 → 应用管理 → 查看应用详情

2. **appSecret**: 自建应用的appSecret
   - 获取方式：登录纷享销客开放平台 → 应用管理 → 查看应用详情

3. **permanentCode**: 永久授权码
   - 获取方式：企业管理员授权应用后获得

### 7.2 企业信息

4. **cloud/域名**: 企业所在的云环境
   - 默认：open.fxiaoke.com（纷享云）
   - 其他云请咨询企业管理员

5. **ea**: 企业账号
   - 获取方式：调用获取token接口后返回

### 7.3 用户信息

6. **user_id (x-fs-userid)**: 员工ID
   - 获取方式：
     1. 登录纷享销客CRM
     2. 点击"CRM"应用
     3. 搜索"人员"
     4. 点击需要查询人员的"系统名"
     5. 在账号信息中找到"员工ID"

---

## 八、注意事项

1. **Token缓存**: 获取token的接口需要缓存至少6600秒，在6650-7200秒之间应重新调用刷新token

2. **错误处理**: 建议根据错误码20016（token过期）增加重试策略

3. **频次限制**: 
   - 当天访问频次超限（0点重新统计）- 错误码30002
   - 秒频次超限 - 错误码30004
   - 需要购买openapi配额 - 错误码30003

4. **分页限制**: 
   - limit最大值为100
   - offset从0开始，必须是limit的整数倍

5. **字段返回**: 如果字段的值为空，则不会返回该字段

6. **错误判断**: 不能使用返回值的message字段做逻辑判断，errorMessage会有变化

---

## 九、参考资料

- [纷享销客开放平台](https://open.fxiaoke.com/)
- [开发文档](https://www.fxiaoke.com/mob/guide/openapi/dist/)
- [全局返回码](https://www.fxiaoke.com/mob/guide/openapi/dist/pages/open-api/guide/code/codes/)
