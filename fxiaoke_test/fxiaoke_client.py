"""
纷享销客OpenAPI Python客户端

提供纷享销客CRM API的封装，包括：
- Token获取和自动刷新
- 客户数据查询
- 错误处理和重试机制
"""

import logging
import os
import uuid
from datetime import datetime, timedelta
from typing import Any, Dict, List, Optional

import requests

# 配置日志
logger = logging.getLogger(__name__)


class FxiaoKeError(Exception):
    """纷享销客API错误"""

    def __init__(self, message: str, error_code: int = None, trace_id: str = None):
        super().__init__(message)
        self.error_code = error_code
        self.trace_id = trace_id

    def __str__(self):
        msg = super().__str__()
        if self.error_code:
            msg += f" (错误码: {self.error_code})"
        if self.trace_id:
            msg += f" (TraceId: {self.trace_id})"
        return msg


class TokenExpiredError(FxiaoKeError):
    """Token过期错误"""
    pass


class FxiaoKeClient:
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

    # Token过期错误码
    TOKEN_EXPIRED_CODES = {20005, 20016}
    # Token建议提前刷新时间（秒）
    TOKEN_REFRESH_BUFFER = 300  # 5分钟

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
        self.access_token: Optional[str] = None
        self.ea: Optional[str] = None
        self.open_user_id: Optional[str] = None
        self.token_expire_time: Optional[datetime] = None

        logger.info(f"FxiaoKeClient初始化完成，域名: {self.domain}")

    @classmethod
    def from_env(cls) -> 'FxiaoKeClient':
        """从环境变量创建客户端"""
        required_vars = ['FXIAOKE_APP_ID', 'FXIAOKE_APP_SECRET', 'FXIAOKE_PERMANENT_CODE']
        missing = [v for v in required_vars if not os.getenv(v)]
        if missing:
            raise ValueError(f"缺少必需的环境变量: {', '.join(missing)}")

        return cls(
            app_id=os.getenv('FXIAOKE_APP_ID'),
            app_secret=os.getenv('FXIAOKE_APP_SECRET'),
            permanent_code=os.getenv('FXIAOKE_PERMANENT_CODE'),
            cloud=os.getenv('FXIAOKE_CLOUD', 'fxiaoke'),
            user_id=os.getenv('FXIAOKE_USER_ID')
        )

    def _generate_trace_id(self) -> str:
        """生成UUID v4格式的traceId"""
        return str(uuid.uuid4())

    def _get_headers(self) -> Dict[str, str]:
        """获取请求头"""
        if not self.access_token:
            raise FxiaoKeError("请先获取access_token")

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

        Raises:
            FxiaoKeError: API调用失败时抛出
        """
        url = f"{self.base_url}/oauth2.0/token?thirdTraceId={self._generate_trace_id()}"

        payload = {
            "appId": self.app_id,
            "appSecret": self.app_secret,
            "permanentCode": self.permanent_code,
            "grantType": "app_secret"
        }

        logger.debug("正在获取access_token...")

        try:
            response = requests.post(url, json=payload, timeout=30)
            response.raise_for_status()
        except requests.RequestException as e:
            logger.error(f"获取token请求失败: {e}")
            raise FxiaoKeError(f"获取token请求失败: {e}")

        result = response.json()
        error_code = result.get('errorCode')

        if error_code != 0:
            error_msg = result.get('errorMessage', '未知错误')
            trace_id = result.get('traceId')
            logger.error(f"获取token失败: {error_msg} (错误码: {error_code})")
            raise FxiaoKeError(error_msg, error_code=error_code, trace_id=trace_id)

        # 保存token信息
        self.access_token = result['accessToken']
        self.ea = result['ea']
        self.open_user_id = result['openUserId']

        # 计算token过期时间（提前TOKEN_REFRESH_BUFFER秒过期，用于缓冲）
        expires_in = result.get('expiresIn', 7200)
        self.token_expire_time = datetime.now() + timedelta(
            seconds=expires_in - self.TOKEN_REFRESH_BUFFER
        )

        logger.info(
            f"获取token成功，openUserId: {self.open_user_id}, "
            f"ea: {self.ea}, 将在 {self.token_expire_time} 后过期"
        )

        return result

    def _ensure_token_valid(self):
        """确保token有效，如果无效则刷新"""
        if not self.access_token or not self.token_expire_time:
            logger.debug("Token不存在，正在获取...")
            self.get_token()
            return

        if datetime.now() >= self.token_expire_time:
            logger.debug("Token即将过期，正在刷新...")
            self.get_token()

    def _make_request(self, endpoint: str, data: Dict[str, Any],
                      retry_on_token_expired: bool = True) -> Dict[str, Any]:
        """
        发送API请求

        Args:
            endpoint: API端点
            data: 请求数据
            retry_on_token_expired: 是否在token过期时重试

        Returns:
            API响应结果

        Raises:
            FxiaoKeError: API调用失败时抛出
        """
        # 确保token有效
        self._ensure_token_valid()

        url = f"{self.base_url}{endpoint}?thirdTraceId={self._generate_trace_id()}"
        headers = self._get_headers()

        logger.debug(f"发送请求: {endpoint}")

        try:
            response = requests.post(url, json=data, headers=headers, timeout=30)
            response.raise_for_status()
        except requests.RequestException as e:
            logger.error(f"请求失败: {e}")
            raise FxiaoKeError(f"请求失败: {e}")

        result = response.json()
        error_code = result.get('errorCode')

        # 处理token过期错误
        if error_code in self.TOKEN_EXPIRED_CODES and retry_on_token_expired:
            logger.warning(f"Token过期(错误码: {error_code})，正在重新获取并重试...")
            self.access_token = None  # 清除过期token
            self.get_token()  # 重新获取token
            # 递归调用，但不允许再次重试
            return self._make_request(endpoint, data, retry_on_token_expired=False)

        if error_code != 0:
            error_msg = result.get('errorMessage', '未知错误')
            trace_id = result.get('traceId')
            logger.error(f"API错误: {error_msg} (错误码: {error_code})")
            raise FxiaoKeError(error_msg, error_code=error_code, trace_id=trace_id)

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
            查询结果字典
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
            数据详情字典
        """
        data = {
            "data": {
                "dataObjectApiName": object_api_name,
                "objectDataId": object_data_id
            }
        }

        return self._make_request('/cgi/crm/v2/data/get', data)

    def query_accounts(self, filters: List[Dict] = None,
                       fields: List[str] = None,
                       limit: int = 100,
                       offset: int = 0) -> Dict[str, Any]:
        """
        查询客户列表

        Args:
            filters: 过滤条件列表
            fields: 返回字段列表，如["_id", "name", "create_time"]
            limit: 分页条数（最大100）
            offset: 偏移量

        Returns:
            客户列表查询结果
        """
        logger.info(f"查询客户列表，limit={limit}, offset={offset}")

        if fields is None:
            fields = ["_id", "name", "create_time", "owner__r.name"]

        return self.query_data(
            object_api_name=self.OBJECT_API_NAMES['account'],
            filters=filters,
            fields=fields,
            limit=limit,
            offset=offset
        )

    def get_account(self, account_id: str) -> Dict[str, Any]:
        """
        获取客户详情

        Args:
            account_id: 客户ID

        Returns:
            客户详情
        """
        logger.info(f"获取客户详情: {account_id}")
        return self.get_data(
            object_api_name=self.OBJECT_API_NAMES['account'],
            object_data_id=account_id
        )
