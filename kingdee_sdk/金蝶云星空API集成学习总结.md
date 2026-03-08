# 金蝶云星空OpenAPI集成学习总结

## 一、项目背景

**日期**: 2026-03-07  
**目标**: 通过金蝶云星空OpenAPI获取原纸库存数据

## 二、连接信息

| 配置项 | 值 | 说明 |
|--------|-----|------|
| 服务器地址 | http://k3.forestpacking.com/k3cloud | 外网访问地址 |
| 数据中心ID | 66306281bdfb20 | 数据中心标识 |
| 用户名 | Administrator | 系统管理员账号 |
| AppId | 337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt | 应用ID |
| AppSecret | 285bc45ae50f4c1e9388b00b44070d46 | 应用密钥 |
| 超时设置 | 420秒 | 请求超时时间 |

## 三、SDK信息

- **SDK版本**: Python3.0 V8.2.0
- **SDK路径**: `/home/admin/SDK_Python3.0_V8.2.0.zip`
- **解压位置**: `~/.openclaw/workspace/kingdee_sdk/`

### SDK核心组件

| 组件 | 文件 | 功能 |
|------|------|------|
| 主API类 | `k3cloud_webapi_sdk/main.py` | K3CloudApiSdk类 |
| HTTP客户端 | `k3cloud_webapi_sdk/client.py` | WebApiClient |
| 签名工具 | `k3cloud_webapi_sdk/sign.py` | HMAC-SHA256签名 |
| 异常处理 | `k3cloud_webapi_sdk/exceptions.py` | 错误处理 |

## 四、认证机制

### 4.1 认证方式
- **算法**: HMAC-SHA256
- **参数**: AppId + AppSecret + 时间戳 + 随机数
- **传输**: HTTP Header中携带签名信息

### 4.2 AppId解析
```
格式: {client_id}_{encoded_client_sec}
示例: 337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt
      └─ClientID─┘ └───────Encoded Secret───────┘
```

## 五、关键API接口

### 5.1 单据查询接口
```python
# BillQuery - 推荐用于查询
query_para = {
    "FormId": "BD_MATERIAL",           # 表单ID
    "FieldKeys": "FMaterialId,FNumber,FName,FSpecification",
    "FilterString": "FName like '%原纸%'",  # 过滤条件
    "Limit": 1000                       # 限制条数
}
response = api_sdk.BillQuery(query_para)
```

### 5.2 常用表单ID

| 表单 | FormId | 说明 |
|------|--------|------|
| 物料 | BD_MATERIAL | 物料主数据 |
| 即时库存 | STK_Inventory | 库存查询 |
| 仓库 | BD_STOCK | 仓库基础资料 |

### 5.3 常用字段

**物料表 (BD_MATERIAL)**
- `FMaterialId` - 物料内码
- `FNumber` - 物料编码
- `FName` - 物料名称
- `FSpecification` - 规格型号

**库存表 (STK_Inventory)**
- `FMaterialID` - 物料内码（注意大小写）
- `FStockID` - 仓库内码
- `FQty` - 库存数量
- `FBaseQty` - 基本单位数量

## 六、测试结果

### 6.1 测试状态
| 测试项 | 状态 | 结果 |
|--------|------|------|
| SDK初始化 | ✅ 成功 | 实例创建完成 |
| 登录/认证 | ✅ 成功 | 认证通过 |
| 查询物料 | ✅ 成功 | 1000个原纸物料 |
| 查询库存 | ✅ 成功 | 424条库存记录 |

### 6.2 数据统计
- **物料总数**: 1000个（瓦楞原纸）
- **库存记录**: 424条
- **有库存物料**: 73个
- **总库存量**: 304,590.00

### 6.3 库存Top 10

| 物料编码 | 规格型号 | 库存数量 |
|---------|---------|---------|
| 01.02.00.090.1.2200 | 90克一等品 2200mm | 9,600.00 |
| 01.02.00.090.1.2000 | 90克一等品 2000mm | 9,180.00 |
| 01.02.00.090.1.2100 | 90克一等品 2100mm | 8,662.00 |
| 01.02.00.090.1.1950 | 90克一等品 1950mm | 8,433.00 |
| 01.02.00.090.1.2050 | 90克一等品 2050mm | 7,938.00 |
| 01.02.00.100.1.1250 | 100克一等品 1250mm | 7,800.00 |
| 01.02.00.090.1.1900 | 90克一等品 1900mm | 7,688.00 |
| 01.02.00.090.1.2150 | 90克一等品 2150mm | 7,500.00 |
| 01.02.00.090.1.1850 | 90克一等品 1850mm | 6,900.00 |
| 01.02.00.095.1.2100 | 95克一等品 2100mm | 6,300.00 |

## 七、遇到的问题及解决方案

### 问题1: SDK初始化参数名错误
```
现象: InitConfig() got an unexpected keyword argument 'app_sec'
原因: 参数名错误
解决: 使用正确的参数名 'app_secret'
```

### 问题2: 查询字段不存在
```
现象: 元数据中标识为FMaterialGroup_FNumber的字段不存在
原因: 字段名错误或不存在
解决: 使用基本字段（FMaterialId, FNumber, FName, FSpecification）
```

### 问题3: 库存表字段名错误
```
现象: 元数据中标识为FMaterialId_FNumber的字段不存在
原因: 库存表字段名与物料表不同
解决: 库存表STK_Inventory中使用 FMaterialID（不是FMaterialId_FNumber）
```

## 八、完整代码示例

```python
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
金蝶K3Cloud API - 原纸库存查询
"""

import sys
import json

# 添加SDK路径
sys.path.insert(0, '/home/admin/.openclaw/workspace/kingdee_sdk/k3cloud_webapi_sdk')
from main import K3CloudApiSdk

# 配置信息
CONFIG = {
    "AppId": "337602_241BWwjKyODY5bxGWZ0t3/XqyJ28xBOt",
    "AppSecret": "285bc45ae50f4c1e9388b00b44070d46",
    "dCID": "66306281bdfb20",
    "ServerUrl": "http://k3.forestpacking.com/k3cloud",
    "UserName": "Administrator",
    "LCID": 2052
}

def init_sdk():
    """初始化SDK"""
    api_sdk = K3CloudApiSdk(CONFIG["ServerUrl"])
    api_sdk.InitConfig(
        acct_id=CONFIG["dCID"],
        user_name=CONFIG["UserName"],
        app_id=CONFIG["AppId"],
        app_secret=CONFIG["AppSecret"],
        server_url=CONFIG["ServerUrl"],
        lcid=CONFIG["LCID"]
    )
    return api_sdk

def query_materials(api_sdk):
    """查询原纸物料"""
    query_para = {
        "FormId": "BD_MATERIAL",
        "FieldKeys": "FMaterialId,FNumber,FName,FSpecification",
        "FilterString": "FName like '%原纸%'",
        "Limit": 1000
    }
    response = api_sdk.BillQuery(query_para)
    return json.loads(response)

def query_inventory(api_sdk, material_ids):
    """查询库存"""
    id_list = ",".join([str(mid) for mid in material_ids])
    query_para = {
        "FormId": "STK_Inventory",
        "FieldKeys": "FMaterialID,FStockID,FQty,FBaseQty",
        "FilterString": f"FMaterialID in ({id_list})",
        "Limit": 1000
    }
    response = api_sdk.BillQuery(query_para)
    return json.loads(response)

def main():
    # 初始化
    api_sdk = init_sdk()
    print("✅ SDK初始化成功")
    
    # 查询物料
    materials = query_materials(api_sdk)
    print(f"✅ 查询到 {len(materials)} 个原纸物料")
    
    # 查询库存
    if materials:
        material_ids = [m[0] for m in materials]
        inventory = query_inventory(api_sdk, material_ids)
        print(f"✅ 查询到 {len(inventory)} 条库存记录")
        
        # 计算总库存
        total_qty = sum([float(item[2]) for item in inventory if item[2]])
        print(f"📊 总库存量: {total_qty:,.2f}")

if __name__ == "__main__":
    main()
```

## 九、相关文件

| 文件路径 | 说明 |
|---------|------|
| `/home/admin/kingdee_complete_test.py` | 完整测试脚本 |
| `/home/admin/kingdee_complete_report.json` | 测试报告(JSON) |
| `/home/admin/.openclaw/workspace/kingdee_sdk/` | SDK目录 |

## 十、参考资源

- **开放平台**: https://openapi.open.kingdee.com
- **SDK下载**: https://openapi.open.kingdee.com/ApiSdkCenter
- **API文档**: https://openapi.open.kingdee.com/ApiDoc

## 十一、总结

本次成功完成了金蝶云星空OpenAPI的集成测试：

1. ✅ 成功解析SDK结构和认证机制
2. ✅ 成功使用Python SDK连接金蝶系统
3. ✅ 成功查询到1000个原纸物料
4. ✅ 成功获取424条库存记录
5. ✅ 总库存量：304,590.00

**关键成功因素**:
- 正确配置AppId和AppSecret
- 理解HMAC-SHA256签名机制
- 使用正确的表单ID和字段名
- 处理好字段大小写（如FMaterialID vs FMaterialId）

---

**记录时间**: 2026-03-07  
**记录人**: 大龙虾
