#!/usr/bin/env node
/**
 * 每日订单统计 - 定时任务
 * 每天早上8点查询各厂区前一天的接单情况，并发送到钉钉群
 */

const oracledb = require('oracledb');
const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// 各厂区数据库配置
const DB_CONFIGS = {
    "集团总部": {
        host: '36.138.130.91',
        port: 1521,
        service: 'dbms',
        user: 'fgrp',
        password: 'kuke.fgrp'
    },
    "临海厂区": {
        host: '36.137.213.189',
        port: 1521,
        service: 'dbms',
        user: 'read',
        password: 'ejsh.read'
    },
    "新昌厂区": {
        host: '36.134.7.141',
        port: 1521,
        service: 'dbms',
        user: 'b0003',
        password: 'kuke.b0003'
    },
    "老厂厂区": {
        host: '36.138.132.30',
        port: 1521,
        service: 'dbms',
        user: 'read',
        password: 'ejsh.read'
    },
    "文森厂区": {
        host: 'db.05.forestpacking.com',
        port: 1521,
        service: 'dbms',
        user: 'read',
        password: 'ejsh.read'
    }
};

// 获取昨天日期
function getYesterday() {
    const d = new Date();
    d.setDate(d.getDate() - 1);
    return d.toISOString().split('T')[0];
}

// 连接数据库并查询
async function queryFactory(factoryName, config, queryDate) {
    let connection;
    try {
        connection = await oracledb.getConnection({
            user: config.user,
            password: config.password,
            connectString: `${config.host}:${config.port}/${config.service}`
        });

        // 查询订单统计
        const sql = `
            SELECT 
                COUNT(*) as 订单数,
                NVL(SUM(NVL(accamt, 0)), 0) as 总金额
            FROM v_ord 
            WHERE status = 'Y'
              AND ptdate >= TO_DATE('${queryDate}', 'YYYY-MM-DD')
              AND ptdate < TO_DATE('${queryDate}', 'YYYY-MM-DD') + 1
        `;
        
        const result = await connection.execute(sql, [], { outFormat: oracledb.OUT_FORMAT_OBJECT });
        return {
            factory: factoryName,
            orders: result.rows[0].订单数,
            amount: parseFloat(result.rows[0].总金额),
            error: null
        };
    } catch (err) {
        return {
            factory: factoryName,
            orders: 0,
            amount: 0,
            error: err.message
        };
    } finally {
        if (connection) {
            try { await connection.close(); } catch {}
        }
    }
}

// 发送消息到钉钉
async function sendToDingTalk(message) {
    try {
        // 使用 message 工具发送
        const { message: msgTool } = require('/opt/openclaw/lib/node_modules/openclaw-tools');
        await msgTool.send({
            channel: 'dingtalk',
            target: '大龙虾测试群',
            message: message
        });
        console.log('✅ 消息已发送到钉钉群');
        return true;
    } catch (e) {
        console.log(`❌ 发送异常: ${e.message}`);
        return false;
    }
}

// 主函数
async function main() {
    console.log('开始执行每日订单统计...\n');
    
    const queryDate = getYesterday();
    const reportDate = queryDate;
    
    console.log(`📊 易捷各厂区接单日报 (${reportDate})`);
    console.log('='.repeat(50));
    
    const results = [];
    let totalOrders = 0;
    let totalAmount = 0;
    
    // 查询各厂区
    for (const [factoryName, config] of Object.entries(DB_CONFIGS)) {
        process.stdout.write(`🏭 ${factoryName}: 查询中... `);
        const result = await queryFactory(factoryName, config, queryDate);
        results.push(result);
        
        if (result.error) {
            console.log(`❌ 查询失败 (${result.error.substring(0, 50)})`);
        } else {
            console.log(`✅ ${result.orders}单, ¥${result.amount.toFixed(2)}`);
            totalOrders += result.orders;
            totalAmount += result.amount;
        }
    }
    
    console.log('='.repeat(50));
    console.log(`📈 合计: ${totalOrders} 单, ¥${totalAmount.toFixed(2)}`);
    console.log(`⏰ 生成时间: ${new Date().toLocaleString('zh-CN')}`);
    
    // 生成报告内容
    const reportLines = [
        `📊 易捷各厂区接单日报 (${reportDate})`,
        '='.repeat(30),
        ...results.map(r => {
            if (r.error) {
                return `🏭 ${r.factory}: 查询失败`;
            }
            return `🏭 ${r.factory}: ${r.orders}单, ¥${r.amount.toFixed(2)}`;
        }),
        '='.repeat(30),
        `📈 合计: ${totalOrders} 单`,
        `💰 总金额: ¥${totalAmount.toFixed(2)}`,
        `⏰ 生成时间: ${new Date().toLocaleString('zh-CN')}`
    ];
    
    const reportText = reportLines.join('\n');
    
    // 保存报告
    const reportsDir = path.join(__dirname, 'reports');
    if (!fs.existsSync(reportsDir)) {
        fs.mkdirSync(reportsDir, { recursive: true });
    }
    
    const reportFile = path.join(reportsDir, `daily_orders_${queryDate}.txt`);
    fs.writeFileSync(reportFile, reportText);
    console.log(`\n报告已保存: ${reportFile}`);
    
    // 保存JSON数据
    const jsonData = {
        date: queryDate,
        generatedAt: new Date().toISOString(),
        factories: results,
        summary: {
            totalOrders: totalOrders,
            totalAmount: totalAmount
        }
    };
    const jsonFile = path.join(reportsDir, `daily_orders_${queryDate}.json`);
    fs.writeFileSync(jsonFile, JSON.stringify(jsonData, null, 2));
    console.log(`JSON数据已保存: ${jsonFile}`);
    
    // 发送到钉钉
    console.log('\n正在发送消息到钉钉群...');
    await sendToDingTalk(reportText);
    
    console.log('\n✅ 任务完成');
}

main().catch(err => {
    console.error('任务失败:', err);
    process.exit(1);
});
