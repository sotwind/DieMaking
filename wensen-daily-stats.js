const oracledb = require('oracledb');

// 温森数据库配置
const wensenConfig = {
    user: 'read',
    password: 'ejsh.read',
    connectString: '(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=db.05.forestpacking.com)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=dbms)))'
};

// 昨天日期
const yesterday = new Date();
yesterday.setDate(yesterday.getDate() - 1);
const dateStr = yesterday.toISOString().split('T')[0];
console.log(`统计日期：${dateStr}`);

async function queryWensenOrders() {
    let connection;
    try {
        console.log('🦞 温森数据库接单统计');
        console.log('='.repeat(60));
        
        connection = await oracledb.getConnection(wensenConfig);
        console.log('✅ 数据库连接成功');
        
        // 查询昨天的订单
        console.log(`\n📊 查询 ${dateStr} 的接单情况...`);
        
        // 统计总数和金额（尝试多个金额字段）
        const statsQuery = `
            SELECT 
                COUNT(*) as total_orders,
                SUM(nvl(calprc, 0)) as calprc_total,
                SUM(nvl(credit, 0)) as credit_total,
                SUM(nvl(accnum, 0)) as accnum_total
            FROM ord_ct
            WHERE TO_CHAR(created, 'YYYY-MM-DD') = '${dateStr}'
            AND isactive = 'Y'
        `;
        
        const statsResult = await connection.execute(statsQuery, [], { outFormat: oracledb.OUT_FORMAT_OBJECT });
        const row = statsResult.rows[0];
        
        console.log(`\n📈 接单统计结果:`);
        console.log(`  总订单数：${row.TOTAL_ORDERS}`);
        console.log(`  CALPRC 总额：¥${row.CALPRC_TOTAL?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) || '0.00'}`);
        console.log(`  CREDIT 总额：¥${row.CREDIT_TOTAL?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) || '0.00'}`);
        console.log(`  ACCNUM 总额：¥${row.ACCNUM_TOTAL?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) || '0.00'}`);
        
        // 查询昨天的订单明细（前 3 条）
        console.log(`\n📋 昨天订单明细（前 3 条）:`);
        
        const detailQuery = `
            SELECT 
                serial,
                clntcde,
                dptnme,
                agntcde,
                calprc,
                credit,
                accnum,
                created,
                matcde,
                prdtyp,
                osizel,
                osizew,
                ordtyp,
                meters
            FROM ord_ct
            WHERE TO_CHAR(created, 'YYYY-MM-DD') = '${dateStr}'
            AND isactive = 'Y'
            ORDER BY created DESC
        `;
        
        const detailResult = await connection.execute(detailQuery, [], { 
            outFormat: oracledb.OUT_FORMAT_OBJECT,
            maxRows: 3
        });
        
        if (detailResult.rows.length > 0) {
            detailResult.rows.forEach((row, idx) => {
                console.log(`\n  [${idx + 1}]`);
                console.log(`      单号：${row.SERIAL}`);
                console.log(`      客户：${row.CLNTCDE}`);
                console.log(`      部门：${row.DPTNME || '未分配'}`);
                console.log(`      业务员：${row.AGNTCDE || '未分配'}`);
                console.log(`      面积：${row.METERS || 0} 米`);
                console.log(`      材质：${row.MATCDE || '-'}`);
                console.log(`      产品类型：${row.PRDTYP || '-'}`);
                console.log(`      订单类型：${row.ORDTYP || '-'}`);
                console.log(`      尺寸：${row.OSIZEL || 0} x ${row.OSIZEW || 0} mm`);
                console.log(`      时间：${new Date(row.CREATED).toLocaleString('zh-CN')}`);
            });
        }
        
        // 按业务员统计
        console.log(`\n📊 按业务员统计 (Top 10):`);
        
        const agentQuery = `
            SELECT 
                agntcde,
                COUNT(*) as order_count,
                SUM(nvl(credit, 0)) as total_credit
            FROM ord_ct
            WHERE TO_CHAR(created, 'YYYY-MM-DD') = '${dateStr}'
            AND isactive = 'Y'
            AND agntcde IS NOT NULL
            GROUP BY agntcde
            ORDER BY order_count DESC
        `;
        
        const agentResult = await connection.execute(agentQuery, [], { 
            outFormat: oracledb.OUT_FORMAT_OBJECT,
            maxRows: 10
        });
        
        if (agentResult.rows.length > 0) {
            agentResult.rows.forEach((row, idx) => {
                console.log(`  ${idx + 1}. ${row.AGNTCDE}: ${row.ORDER_COUNT}单，¥${row.TOTAL_CREDIT?.toLocaleString('zh-CN', { minimumFractionDigits: 2 })}`);
            });
        }
        
        // 按产品类型统计
        console.log(`\n📦 按产品类型统计:`);
        
        const productQuery = `
            SELECT 
                prdtyp,
                COUNT(*) as order_count,
                SUM(nvl(credit, 0)) as total_credit
            FROM ord_ct
            WHERE TO_CHAR(created, 'YYYY-MM-DD') = '${dateStr}'
            AND isactive = 'Y'
            AND prdtyp IS NOT NULL
            GROUP BY prdtyp
            ORDER BY order_count DESC
        `;
        
        const productResult = await connection.execute(productQuery, [], { outFormat: oracledb.OUT_FORMAT_OBJECT });
        
        if (productResult.rows.length > 0) {
            productResult.rows.forEach((row, idx) => {
                console.log(`  ${idx + 1}. ${row.PRDTYP}: ${row.ORDER_COUNT}单，¥${row.TOTAL_CREDIT?.toLocaleString('zh-CN', { minimumFractionDigits: 2 })}`);
            });
        }
        
        // 输出汇总报告
        console.log('\n' + '='.repeat(60));
        console.log('📋 温森接单统计报告');
        console.log('='.repeat(60));
        console.log(`统计日期：${dateStr}`);
        console.log(`总订单数：${row.TOTAL_ORDERS} 单`);
        console.log(`信用额度总额：¥${row.CREDIT_TOTAL?.toLocaleString('zh-CN', { minimumFractionDigits: 2 }) || '0.00'}`);
        console.log(`业务员数：${agentResult.rows.length} 人`);
        console.log(`产品种类：${productResult.rows.length} 种`);
        console.log('='.repeat(60));
        
        return {
            success: true,
            date: dateStr,
            totalOrders: row.TOTAL_ORDERS,
            totalCredit: row.CREDIT_TOTAL
        };
        
    } catch (error) {
        console.log(`❌ 错误：${error.message}`);
        return { success: false, error: error.message };
    } finally {
        if (connection) {
            try {
                await connection.close();
                console.log('\n🔒 连接已关闭');
            } catch (e) {}
        }
    }
}

queryWensenOrders().catch(console.error);
