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

// 查询订单接单情况
const orderQuery = `
    SELECT 
        ptdate,
        serial,
        clntcde,
        accamt,
        status,
        agntcde
    FROM v_ord
    WHERE TRUNC(ptdate) = TO_DATE('${dateStr}', 'YYYY-MM-DD')
    AND status = 'Y'
    ORDER BY serial
`;

// 查询所有表
const listTablesQuery = `
    SELECT table_name 
    FROM user_tables 
    WHERE ROWNUM <= 20
    ORDER BY table_name
`;

// 查询订单相关表
const findOrderTablesQuery = `
    SELECT table_name 
    FROM user_tables 
    WHERE table_name LIKE '%ORD%' OR table_name LIKE '%MK%' OR table_name LIKE '%PRC%' OR table_name LIKE '%CT%'
    ORDER BY table_name
`;

async function exploreWensen() {
    let connection;
    try {
        console.log('🦞 温森数据库接单统计');
        console.log('='.repeat(60));
        
        connection = await oracledb.getConnection(wensenConfig);
        console.log('✅ 数据库连接成功');
        
        // 先检查 v_ord 视图是否存在
        let vOrdExists = false;
        try {
            const checkResult = await connection.execute(
                `SELECT view_name FROM user_views WHERE view_name = 'V_ORD'`,
                [],
                { outFormat: oracledb.OUT_FORMAT_OBJECT }
            );
            vOrdExists = checkResult.rows.length > 0;
            console.log(`v_ord 视图：${vOrdExists ? '✅ 存在' : '❌ 不存在'}`);
        } catch (e) {
            console.log(`v_ord 检查失败：${e.message.substring(0, 100)}`);
        }
        
        // 查询订单相关表
        console.log('\n📋 查找订单相关表...');
        const orderTables = await connection.execute(findOrderTablesQuery, [], { outFormat: oracledb.OUT_FORMAT_OBJECT });
        console.log('订单相关表:', orderTables.rows.map(r => r.TABLE_NAME).join(', ') || '无');
        
        // 查询所有表（前 20）
        const allTables = await connection.execute(listTablesQuery, [], { outFormat: oracledb.OUT_FORMAT_OBJECT });
        console.log('所有表 (前 20):', allTables.rows.map(r => r.TABLE_NAME).join(', '));
        
        // 尝试查询 ord_ct 表（订单表）
        console.log('\n📊 尝试查询 ord_ct 表...');
        let ordData = null;
        try {
            ordData = await connection.execute(
                `SELECT * FROM ord_ct WHERE ROWNUM <= 5`,
                [],
                { outFormat: oracledb.OUT_FORMAT_OBJECT }
            );
            console.log(`✅ ord_ct 表存在，列名：${ordData.metaData.map(c => c.name).join(', ')}`);
            console.log('数据预览:');
            ordData.rows.forEach((row, idx) => {
                console.log(`  [${idx + 1}] ${JSON.stringify(row)}`);
            });
        } catch (e) {
            console.log(`❌ ord_ct 表不存在：${e.message.substring(0, 100)}`);
        }
        
        // 尝试查询 mk_prctyp 表（报价表）
        console.log('\n📊 尝试查询 mk_prctyp 表...');
        let mkData = null;
        try {
            mkData = await connection.execute(
                `SELECT * FROM mk_prctyp WHERE ROWNUM <= 5`,
                [],
                { outFormat: oracledb.OUT_FORMAT_OBJECT }
            );
            console.log(`✅ mk_prctyp 表存在，列名：${mkData.metaData.map(c => c.name).join(', ')}`);
        } catch (e) {
            console.log(`❌ mk_prctyp 表不存在：${e.message.substring(0, 100)}`);
        }
        
        // 尝试查询 ord_ct 表按日期统计
        if (ordData) {
            console.log(`\n📈 统计 ${dateStr} 的接单情况...`);
            
            // 检查是否有 ptdate 字段
            const hasPtDate = ordData.metaData.some(c => c.name.toLowerCase().includes('ptdate') || c.name.toLowerCase().includes('date'));
            console.log(`日期字段：${hasPtDate ? '✅ 存在' : '❌ 不存在'}`);
            
            if (hasPtDate) {
                const dailyQuery = `
                    SELECT 
                        COUNT(*) as total_orders,
                        SUM(nvl(accamt, 0)) as total_amount
                    FROM ord_ct
                    WHERE TRUNC(ptdate) = TO_DATE('${dateStr}', 'YYYY-MM-DD')
                `;
                
                try {
                    const dailyResult = await connection.execute(dailyQuery, [], { outFormat: oracledb.OUT_FORMAT_OBJECT });
                    console.log('接单统计结果:');
                    console.log(`  总订单数：${dailyResult.rows[0]?.TOTAL_ORDERS || 0}`);
                    console.log(`  总金额：${dailyResult.rows[0]?.TOTAL_AMOUNT || 0}`);
                } catch (e) {
                    console.log(`统计查询失败：${e.message.substring(0, 100)}`);
                }
            }
            
            // 获取最近 3 条订单数据
            const recentQuery = `
                SELECT * FROM ord_ct 
                WHERE ROWNUM <= 3 
                ORDER BY ${ordData.metaData.some(c => c.name.toLowerCase().includes('date')) ? 'ptdate' : 'serial'} DESC
            `;
            
            const recentData = await connection.execute(recentQuery, [], { outFormat: oracledb.OUT_FORMAT_OBJECT });
            console.log('\n📋 最近 3 条订单数据:');
            recentData.rows.forEach((row, idx) => {
                console.log(`  [${idx + 1}] ${JSON.stringify(row)}`);
            });
        }
        
        return { success: true };
        
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

exploreWensen().catch(console.error);
