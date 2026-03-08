#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
金蝶K3Cloud Python SDK 测试脚本
使用正确的初始化参数测试API连接
"""

import sys
import json
sys.path.insert(0, '/home/admin/.openclaw/workspace/kingdee_sdk/sdk_package')

from k3cloud_webapi_sdk.main import K3CloudApiSdk

# ==========================================
# 配置参数
# ==========================================
CONFIG = {
    "app_id": "337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt",
    "app_secret": "",  # 需要从开放平台获取
    "dcid": "66306281bdfb20",  # 数据中心ID
    "server_url": "http://k3.forestpacking.com/k3cloud",  # 外网地址
    "username": "Administrator",
    "lcid": 2052,
    "org_num": 0,
    "request_timeout": 420,
    "connect_timeout": 120
}

# ==========================================
# 测试类
# ==========================================
class KingdeeAPITester:
    def __init__(self):
        self.api_sdk = None
        self.results = []
        
    def log(self, message, level="INFO"):
        """记录日志"""
        prefix = {"INFO": "[INFO]", "ERROR": "[ERROR]", "SUCCESS": "[SUCCESS]", "WARN": "[WARN]"}
        msg = f"{prefix.get(level, '[INFO]')} {message}"
        print(msg)
        self.results.append(msg)
        
    def init_sdk(self):
        """初始化SDK"""
        self.log("=" * 60)
        self.log("开始初始化金蝶SDK")
        self.log("=" * 60)
        
        try:
            # 创建SDK实例
            self.api_sdk = K3CloudApiSdk(CONFIG["server_url"], CONFIG["request_timeout"])
            
            # 检查AppSecret是否配置
            if not CONFIG["app_secret"]:
                self.log("AppSecret未配置！需要从金蝶开放平台获取", "WARN")
                self.log("尝试使用空AppSecret继续...", "WARN")
            
            # 使用InitConfig方法初始化
            self.api_sdk.InitConfig(
                acct_id=CONFIG["dcid"],
                user_name=CONFIG["username"],
                app_id=CONFIG["app_id"],
                app_secret=CONFIG["app_secret"],
                server_url=CONFIG["server_url"],
                lcid=CONFIG["lcid"],
                org_num=CONFIG["org_num"],
                connect_timeout=CONFIG["connect_timeout"],
                request_timeout=CONFIG["request_timeout"]
            )
            
            self.log("SDK初始化成功！", "SUCCESS")
            return True
            
        except Exception as e:
            self.log(f"SDK初始化失败: {str(e)}", "ERROR")
            return False
    
    def test_get_datacenters(self):
        """测试获取数据中心列表"""
        self.log("\n" + "-" * 60)
        self.log("测试1: 获取数据中心列表")
        self.log("-" * 60)
        
        try:
            response = self.api_sdk.GetDataCenters()
            self.log(f"响应结果: {response}", "SUCCESS")
            
            # 解析响应
            try:
                data = json.loads(response)
                if isinstance(data, list) and len(data) > 0:
                    self.log(f"找到 {len(data)} 个数据中心", "SUCCESS")
                    for dc in data:
                        self.log(f"  - 数据中心: {dc}")
                else:
                    self.log("数据中心列表为空或格式异常", "WARN")
            except:
                self.log("响应解析成功，但无法解析为JSON列表")
                
            return True
            
        except Exception as e:
            self.log(f"获取数据中心失败: {str(e)}", "ERROR")
            return False
    
    def test_query_material(self):
        """测试查询物料（查找'原纸'）"""
        self.log("\n" + "-" * 60)
        self.log("测试2: 查询物料（查找'原纸'）")
        self.log("-" * 60)
        
        try:
            # 使用ExecuteBillQuery查询物料
            para = {
                "FormId": "BD_MATERIAL",
                "FieldKeys": "FMaterialId,FNumber,FName,FSpecification,FMaterialGroup_FNumber",
                "FilterString": "FName like '%原纸%'",
                "OrderString": "FNumber",
                "TopRowCount": 0,
                "StartRow": 0,
                "Limit": 100,
                "SubSystemId": ""
            }
            
            response = self.api_sdk.ExecuteBillQuery(para)
            self.log(f"响应结果: {response}", "SUCCESS")
            
            try:
                data = json.loads(response)
                if isinstance(data, list) and len(data) > 0:
                    self.log(f"找到 {len(data)} 条物料记录", "SUCCESS")
                    for item in data[:5]:  # 只显示前5条
                        self.log(f"  - 物料: {item}")
                else:
                    self.log("未找到物料记录或列表为空", "WARN")
            except Exception as e:
                self.log(f"响应解析异常: {str(e)}", "ERROR")
                
            return True
            
        except Exception as e:
            self.log(f"查询物料失败: {str(e)}", "ERROR")
            return False
    
    def test_query_inventory(self):
        """测试查询库存"""
        self.log("\n" + "-" * 60)
        self.log("测试3: 查询库存（原纸库存）")
        self.log("-" * 60)
        
        try:
            # 使用ExecuteBillQuery查询库存
            # STK_Inventory是库存表
            para = {
                "FormId": "STK_Inventory",
                "FieldKeys": "FMaterialId,FMaterialId_FNumber,FMaterialId_FName,FQty,FStockId_FName,FStockLocId_FName",
                "FilterString": "FMaterialId_FName like '%原纸%'",
                "OrderString": "FMaterialId_FNumber",
                "TopRowCount": 0,
                "StartRow": 0,
                "Limit": 100,
                "SubSystemId": ""
            }
            
            response = self.api_sdk.ExecuteBillQuery(para)
            self.log(f"响应结果: {response}", "SUCCESS")
            
            try:
                data = json.loads(response)
                if isinstance(data, list) and len(data) > 0:
                    self.log(f"找到 {len(data)} 条库存记录", "SUCCESS")
                    for item in data[:5]:  # 只显示前5条
                        self.log(f"  - 库存: {item}")
                else:
                    self.log("未找到库存记录或列表为空", "WARN")
            except Exception as e:
                self.log(f"响应解析异常: {str(e)}", "ERROR")
                
            return True
            
        except Exception as e:
            self.log(f"查询库存失败: {str(e)}", "ERROR")
            return False
    
    def test_bill_query_json(self):
        """测试使用BillQuery（JSON格式）查询"""
        self.log("\n" + "-" * 60)
        self.log("测试4: 使用BillQuery(JSON格式)查询物料")
        self.log("-" * 60)
        
        try:
            para = {
                "FormId": "BD_MATERIAL",
                "FieldKeys": "FMaterialId,FNumber,FName",
                "FilterString": "FName like '%原纸%'",
                "Limit": 10
            }
            
            response = self.api_sdk.BillQuery(para)
            self.log(f"响应结果: {response}", "SUCCESS")
            return True
            
        except Exception as e:
            self.log(f"BillQuery查询失败: {str(e)}", "ERROR")
            return False
    
    def run_all_tests(self):
        """运行所有测试"""
        self.log("\n" + "=" * 60)
        self.log("金蝶K3Cloud API 测试开始")
        self.log("=" * 60)
        self.log(f"服务器地址: {CONFIG['server_url']}")
        self.log(f"数据中心ID: {CONFIG['dcid']}")
        self.log(f"应用ID: {CONFIG['app_id']}")
        self.log(f"用户名: {CONFIG['username']}")
        
        # 初始化SDK
        if not self.init_sdk():
            self.log("\nSDK初始化失败，终止测试", "ERROR")
            return False
        
        # 执行各项测试
        tests = [
            ("获取数据中心", self.test_get_datacenters),
            ("查询物料", self.test_query_material),
            ("查询库存", self.test_query_inventory),
            ("BillQuery查询", self.test_bill_query_json)
        ]
        
        results = []
        for name, test_func in tests:
            try:
                result = test_func()
                results.append((name, result))
            except Exception as e:
                self.log(f"测试'{name}'执行异常: {str(e)}", "ERROR")
                results.append((name, False))
        
        # 输出测试总结
        self.log("\n" + "=" * 60)
        self.log("测试总结")
        self.log("=" * 60)
        for name, result in results:
            status = "✓ 通过" if result else "✗ 失败"
            self.log(f"{name}: {status}")
        
        passed = sum(1 for _, r in results if r)
        total = len(results)
        self.log(f"\n总计: {passed}/{total} 项测试通过")
        
        return True


# ==========================================
# 主程序
# ==========================================
if __name__ == "__main__":
    tester = KingdeeAPITester()
    tester.run_all_tests()
    
    # 输出完整日志
    print("\n\n" + "=" * 60)
    print("完整测试日志:")
    print("=" * 60)
    for line in tester.results:
        print(line)
