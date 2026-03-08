#!/usr/bin/env python3
"""
纷享销客API连接测试脚本

测试内容：
1. 从环境变量读取凭证
2. 测试获取access_token
3. 测试查询客户列表

使用方法：
    export FXIAOKE_APP_ID="your_app_id"
    export FXIAOKE_APP_SECRET="your_app_secret"
    export FXIAOKE_PERMANENT_CODE="your_permanent_code"
    export FXIAOKE_USER_ID="your_user_id"  # 可选
    python test_connection.py
"""

import logging
import os
import sys
from typing import Any, Dict

from fxiaoke_client import FxiaoKeClient, FxiaoKeError

# 配置日志
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)
logger = logging.getLogger(__name__)


def print_separator(title: str = ""):
    """打印分隔线"""
    if title:
        print(f"\n{'=' * 60}")
        print(f"  {title}")
        print(f"{'=' * 60}")
    else:
        print(f"\n{'=' * 60}")


def check_environment() -> bool:
    """检查环境变量是否配置正确"""
    print_separator("环境变量检查")

    required_vars = ['FXIAOKE_APP_ID', 'FXIAOKE_APP_SECRET', 'FXIAOKE_PERMANENT_CODE']
    optional_vars = ['FXIAOKE_USER_ID', 'FXIAOKE_CLOUD']

    all_ok = True

    for var in required_vars:
        value = os.getenv(var)
        if value:
            # 只显示前10个字符，保护敏感信息
            masked = value[:10] + "..." if len(value) > 10 else value
            print(f"  ✓ {var}: {masked}")
        else:
            print(f"  ✗ {var}: 未设置")
            all_ok = False

    for var in optional_vars:
        value = os.getenv(var)
        if value:
            print(f"  ○ {var}: {value}")
        else:
            print(f"  ○ {var}: 未设置（使用默认值）")

    return all_ok


def test_get_token(client: FxiaoKeClient) -> Dict[str, Any]:
    """测试获取access_token"""
    print_separator("测试1: 获取Access Token")

    try:
        result = client.get_token()

        print(f"  ✓ Token获取成功!")
        print(f"    - openUserId: {result.get('openUserId')}")
        print(f"    - accessToken: {result.get('accessToken', '')[:20]}...")
        print(f"    - expiresIn: {result.get('expiresIn')}秒")
        print(f"    - ea (企业账号): {result.get('ea')}")
        print(f"    - traceId: {result.get('traceId')}")

        return {"success": True, "data": result}

    except FxiaoKeError as e:
        print(f"  ✗ Token获取失败!")
        print(f"    - 错误信息: {e}")
        return {"success": False, "error": str(e)}


def test_query_accounts(client: FxiaoKeClient) -> Dict[str, Any]:
    """测试查询客户列表"""
    print_separator("测试2: 查询客户列表")

    try:
        # 查询前10条客户数据
        result = client.query_accounts(
            fields=["_id", "name", "create_time", "owner__r.name"],
            limit=10
        )

        data = result.get('data', {})
        data_list = data.get('dataList', [])
        total_count = data.get('totalNumber', 0)

        print(f"  ✓ 查询成功!")
        print(f"    - 总记录数: {total_count}")
        print(f"    - 本次返回: {len(data_list)}条")

        if data_list:
            print(f"\n  客户数据预览:")
            for i, item in enumerate(data_list[:5], 1):  # 只显示前5条
                name = item.get('name', 'N/A')
                owner = item.get('owner__r', {}).get('name', 'N/A')
                create_time = item.get('create_time', 'N/A')
                print(f"    {i}. {name} (负责人: {owner}, 创建时间: {create_time})")

            if len(data_list) > 5:
                print(f"    ... 还有 {len(data_list) - 5} 条数据")

        return {"success": True, "data": result}

    except FxiaoKeError as e:
        print(f"  ✗ 查询失败!")
        print(f"    - 错误信息: {e}")
        return {"success": False, "error": str(e)}


def test_token_refresh(client: FxiaoKeClient) -> Dict[str, Any]:
    """测试Token自动刷新机制"""
    print_separator("测试3: Token自动刷新机制")

    try:
        # 强制清除token，模拟过期
        original_token = client.access_token
        client.access_token = None

        print("  - 模拟Token失效，重新查询客户列表...")

        # 再次查询，应该自动获取新token
        result = client.query_accounts(limit=1)

        print(f"  ✓ Token自动刷新成功!")
        print(f"    - 新Token: {client.access_token[:20]}...")

        return {"success": True, "data": result}

    except FxiaoKeError as e:
        print(f"  ✗ Token刷新失败!")
        print(f"    - 错误信息: {e}")
        return {"success": False, "error": str(e)}


def main():
    """主函数"""
    print_separator("纷享销客API连接测试")
    print("  本脚本用于测试纷享销客OpenAPI连接")
    print("  请确保已正确配置环境变量")

    # 检查环境变量
    if not check_environment():
        print("\n  ✗ 环境变量检查失败，请配置必需的环境变量后重试")
        print("\n  必需的环境变量:")
        print("    - FXIAOKE_APP_ID")
        print("    - FXIAOKE_APP_SECRET")
        print("    - FXIAOKE_PERMANENT_CODE")
        sys.exit(1)

    # 创建客户端
    print_separator("初始化客户端")
    try:
        client = FxiaoKeClient.from_env()
        print(f"  ✓ 客户端初始化成功")
        print(f"    - API域名: {client.domain}")
        print(f"    - 用户ID: {client.user_id or '未设置'}")
    except ValueError as e:
        print(f"  ✗ 客户端初始化失败: {e}")
        sys.exit(1)

    # 运行测试
    results = []

    # 测试1: 获取Token
    result1 = test_get_token(client)
    results.append(("获取Token", result1))

    # 测试2: 查询客户列表
    result2 = test_query_accounts(client)
    results.append(("查询客户列表", result2))

    # 测试3: Token自动刷新
    result3 = test_token_refresh(client)
    results.append(("Token自动刷新", result3))

    # 打印测试总结
    print_separator("测试总结")

    total = len(results)
    passed = sum(1 for _, r in results if r["success"])

    for name, result in results:
        status = "✓ 通过" if result["success"] else "✗ 失败"
        print(f"  {status} - {name}")

    print(f"\n  总计: {passed}/{total} 项测试通过")

    if passed == total:
        print("\n  🎉 所有测试通过！API连接正常。")
        return 0
    else:
        print("\n  ⚠️ 部分测试失败，请检查配置和API状态。")
        return 1


if __name__ == "__main__":
    sys.exit(main())
