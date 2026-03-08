#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
金蝶云星空WebAPI SDK测试 - 获取原纸库存数据

配置信息:
- APPID: 337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt
- 接口地址: http://36.139.60.189/k3cloud
- 开放平台: https://openapi.open.kingdee.com

金蝶WebAPI使用说明:
- 登录接口: Kingdee.BOS.WebApi.ServicesStub.AuthService.AuthLogin.common.kdsvc
- 查询接口: Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.ExecuteBillQuery.common.kdsvc
"""

import requests
import json
import time

class KingdeeWebAPI:
    """金蝶云星空WebAPI客户端"""
    
    def __init__(self, server_url, app_id, app_secret=None):
        """
        初始化API客户端
        
        Args:
            server_url: 服务器地址，如 http://36.139.60.189/k3cloud
            app_id: 应用ID
            app_secret: 应用密钥（如果有）
        """
        self.server_url = server_url.rstrip('/')
        self.app_id = app_id
        self.app_secret = app_secret
        self.cookies = None
        
    def login(self):
        """
        WebAPI登录认证
        金蝶WebAPI使用表单登录方式
        """
        url = f"{self.server_url}/Kingdee.BOS.WebApi.ServicesStub.AuthService.AuthLogin.common.kdsvc"
        
        headers = {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        }
        
        # 登录参数 - 尝试使用APPID作为认证
        params = {
            'appid': self.app_id,
        }
        
        try:
            response = requests.post(url, json=params, headers=headers, timeout=30)
            print(f"登录响应状态码: {response.status_code}")
            print(f"登录响应内容: {response.text[:500]}")
            
            # 保存cookies
            self.cookies = response.cookies
            
            try:
                result = response.json()
                print(f"登录响应JSON: {json.dumps(result, indent=2, ensure_ascii=False)}")
                
                if result.get('Result', {}).get('ResponseStatus', {}).get('IsSuccess'):
                    print("登录成功!")
                    return True
                else:
                    error = result.get('Result', {}).get('ResponseStatus', {}).get('Errors', [])
                    print(f"登录失败: {error}")
                    return False
            except:
                print("响应不是JSON格式")
                return False
                
        except Exception as e:
            print(f"登录请求异常: {str(e)}")
            return False
    
    def execute_bill_query(self, form_id, filter_string=None, field_keys=None, top=None, skip=None):
        """
        执行单据查询
        
        Args:
            form_id: 表单ID，如 BD_MATERIAL(物料), STK_Inventory(库存)
            filter_string: 过滤条件
            field_keys: 返回字段列表
            top: 返回记录数
            skip: 跳过记录数
        """
        url = f"{self.server_url}/Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.ExecuteBillQuery.common.kdsvc"
        
        headers = {
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        }
        
        params = {
            'FormId': form_id,
        }
        
        if filter_string:
            params['FilterString'] = filter_string
        if field_keys:
            params['FieldKeys'] = field_keys
        if top:
            params['Top'] = top
        if skip:
            params['Skip'] = skip
            
        try:
            response = requests.post(url, json=params, headers=headers, cookies=self.cookies, timeout=30)
            print(f"查询响应状态码: {response.status_code}")
            print(f"查询响应内容: {response.text[:500]}")
            
            try:
                result = response.json()
                return result
            except:
                return {'raw_response': response.text}
        except Exception as e:
            print(f"查询请求异常: {str(e)}")
            return None
    
    def query_material(self, material_name=None, material_code=None):
        """
        查询物料信息
        
        Args:
            material_name: 物料名称（模糊查询）
            material_code: 物料编码
        """
        filter_str = ""
        if material_name:
            filter_str = f"FMaterialName like '%{material_name}%'"
        if material_code:
            if filter_str:
                filter_str += " and "
            filter_str += f"FMaterialNumber = '{material_code}'"
            
        # 物料主数据表单ID: BD_MATERIAL
        result = self.execute_bill_query(
            form_id='BD_MATERIAL',
            filter_string=filter_str,
            field_keys='FMaterialNumber, FMaterialName, FSpecification, FMaterialGroup',
            top=100
        )
        return result
    
    def query_inventory(self, material_code=None, warehouse_code=None):
        """
        查询库存信息
        
        Args:
            material_code: 物料编码
            warehouse_code: 仓库编码
        """
        filter_str = ""
        if material_code:
            filter_str = f"FMaterialID.FNumber = '{material_code}'"
        if warehouse_code:
            if filter_str:
                filter_str += " and "
            filter_str += f"FStockID.FNumber = '{warehouse_code}'"
            
        # 即时库存表单ID: STK_Inventory
        result = self.execute_bill_query(
            form_id='STK_Inventory',
            filter_string=filter_str,
            field_keys='FMaterialID.FNumber, FMaterialID.FName, FStockID.FName, FQty, FAvailableQty',
            top=100
        )
        return result


def test_kingdee_api():
    """测试金蝶WebAPI"""
    
    # 配置信息
    SERVER_URL = "http://36.139.60.189/k3cloud"
    APP_ID = "337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt"
    
    print("=" * 60)
    print("金蝶云星空WebAPI SDK测试")
    print("=" * 60)
    print(f"服务器地址: {SERVER_URL}")
    print(f"APPID: {APP_ID[:20]}...")
    print()
    
    # 创建API客户端
    api = KingdeeWebAPI(SERVER_URL, APP_ID)
    
    # 尝试登录
    print("步骤1: 尝试登录...")
    login_success = api.login()
    
    # 查询物料 - 查找原纸相关物料
    print("\n步骤2: 查询原纸相关物料...")
    material_result = api.query_material(material_name="原纸")
    print(f"物料查询结果: {json.dumps(material_result, indent=2, ensure_ascii=False) if material_result else 'None'}")
    
    # 查询库存
    print("\n步骤3: 查询库存数据...")
    inventory_result = api.query_inventory()
    print(f"库存查询结果: {json.dumps(inventory_result, indent=2, ensure_ascii=False) if inventory_result else 'None'}")
    
    print("\n" + "=" * 60)
    print("测试完成")
    print("=" * 60)


if __name__ == "__main__":
    test_kingdee_api()
