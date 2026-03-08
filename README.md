# 刀模管理系统

一款专业的纸箱刀模全生命周期管理软件，涵盖刀模信息、生产工序、仓库库存、借用归还等业务流程。

## 功能特性

### 刀模管理
- ✅ 刀模信息录入与维护
- ✅ 多维度刀模查询与筛选
- ✅ 刀模审核流程管理
- ✅ 刀模工序配置
- ✅ 尺寸与工艺信息管理

### 生产管理
- ✅ 可视化生产看板
- ✅ 实时生产进度跟踪
- ✅ 工序报产与完工记录
- ✅ 工序依赖关系控制
- ✅ 生产统计分析

### 仓库管理
- ✅ 库位管理与规划
- ✅ 刀模入库/出库管理
- ✅ 刀模领用与归还
- ✅ 借用记录追踪
- ✅ 报废申请与审核
- ✅ 库存状态实时监控

### 报表统计
- ✅ 完工统计（按刀模/客户/日期）
- ✅ 工序统计分析
- ✅ 库存汇总与明细
- ✅ 库位分布统计
- ✅ 借用记录统计
- ✅ 数据导出Excel

### 系统管理
- ✅ 用户权限管理
- ✅ 系统参数配置
- ✅ 操作日志审计
- ✅ 数据备份与恢复
- ✅ 个性化设置

## 技术栈

| 技术 | 版本 | 说明 |
|------|------|------|
| .NET | 10.0 | 开发框架 |
| Windows Forms | - | UI框架 |
| SQL Server | 2016+ | 数据库 |
| C# | 12.0 | 编程语言 |

## 快速开始

### 环境要求

- Windows 10/11 或 Windows Server 2016+
- .NET 10.0 Desktop Runtime
- SQL Server 2016 或更高版本

### 安装步骤

1. **安装 .NET 运行时**
   ```
   下载地址：https://dotnet.microsoft.com/download/dotnet
   ```

2. **安装 SQL Server**
   - 可使用 SQL Server Express 免费版
   - 或使用现有的 SQL Server 实例

3. **部署应用程序**
   ```
   1. 将程序文件复制到目标目录（如 C:\Program Files\DieMaking\）
   2. 编辑 App.config 配置数据库连接字符串
   3. 创建桌面快捷方式
   ```

4. **启动程序**
   ```
   双击 DieMaking.exe 启动程序
   首次启动会自动初始化数据库
   ```

5. **登录系统**
   ```
   用户名：admin
   密码：admin123
   ```

### 默认登录信息

- **用户名**：admin
- **密码**：admin123
- **建议**：首次登录后立即修改默认密码

## 项目结构

```
DieMaking/
├── Forms/              # 窗体界面
│   ├── Common/         # 通用窗体
│   ├── Die/            # 刀模管理
│   ├── Production/     # 生产管理
│   ├── Report/         # 报表统计
│   ├── System/         # 系统管理
│   ├── Warehouse/      # 仓库管理
│   ├── LoginForm.cs    # 登录窗体
│   └── MainForm.cs     # 主窗体
├── Helpers/            # 辅助类
│   ├── DbHelper.cs     # 数据库访问
│   ├── ConfigHelper.cs # 配置管理
│   └── ...
├── Models/             # 数据模型
│   ├── DieInfo.cs      # 刀模模型
│   ├── User.cs         # 用户模型
│   └── ...
├── Services/           # 业务服务
│   ├── DieService.cs   # 刀模服务
│   ├── UserService.cs  # 用户服务
│   └── ...
├── docs/               # 文档目录
│   ├── 开发文档.md
│   ├── 部署文档.md
│   └── 用户操作手册.md
├── App.config          # 配置文件
├── DieMaking.csproj    # 项目文件
└── Program.cs          # 程序入口
```

## 数据库表结构

| 表名 | 说明 |
|------|------|
| DM_User | 用户表 |
| DM_DieInfo | 刀模信息表 |
| DM_DieProcess | 刀模工序表 |
| DM_DieCompletion | 完工记录表 |
| DM_DieInventory | 库存表 |
| DM_StorageLocation | 库位表 |
| DM_DieBorrowRecord | 借用记录表 |
| DM_DieScrapRecord | 报废记录表 |
| DM_OperationLog | 操作日志表 |
| DM_SystemConfig | 系统配置表 |

详细表结构请参考 [开发文档](docs/开发文档.md)

## 文档

- [开发文档](docs/开发文档.md) - 项目架构、技术栈、API接口、开发规范
- [部署文档](docs/部署文档.md) - 系统要求、安装步骤、配置说明、问题排查
- [用户操作手册](docs/用户操作手册.md) - 功能说明、操作指南、常见问题

## 截图展示

### 登录界面
![登录界面](screenshots/login.png)

### 主界面
![主界面](screenshots/main.png)

### 刀模列表
![刀模列表](screenshots/die-list.png)

### 生产看板
![生产看板](screenshots/production-board.png)

### 报表统计
![报表统计](screenshots/report.png)

## 开发团队

**开发单位**：纸箱报价系统开发团队

**版本**：v1.0.0

**更新日期**：2024年

## 版权信息

Copyright © 2024 纸箱报价系统. All rights reserved.

本软件仅供内部使用，未经许可不得复制、传播或用于商业用途。

## 技术支持

如有问题或建议，请联系系统管理员或技术支持团队。

---

**注意**：使用前请仔细阅读[用户操作手册](docs/用户操作手册.md)，了解系统的各项功能和操作方法。
