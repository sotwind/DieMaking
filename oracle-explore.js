const oracledb = require('oracledb');

// 启用 Thick 模式支持旧版本数据库
oracledb.initOracleClient({ libDir: '' });

// 数据库配置
const databases = {
    '新厂新系统': {
        user: 'b0003',
        password: 'kuke.b0003',
        connectString: '(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=36.134.7.141)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=dbms)))'
    },
    '老厂新系统': {
        user: 'read',
        password: 'ejsh.read',
        connectString: '(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=36.138.132.30)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=dbms)))'
    },
    '临海老系统': {
        user: 'read',
        password: 'ejsh.read',
        connectString: '(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=36.137.213.189)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=dbms)))'
    },
    '温森新系统': {
        user: 'read',
        password: 'ejsh.read',
        connectString: '(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=db.05.forestpacking.com)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=dbms)))'
    }
};

// 查询所有表
const listTablesQuery = `
    SELECT table_name 
    FROM user_tables 
    WHERE ROWNUM <= 10
    ORDER BY table_name
`;

// 查询报价相关表
const findOrderQuery = `
    SELECT table_name 
    FROM user_tables 
    WHERE table_name LIKE '%ORD%' OR table_name LIKE '%MK%' OR table_name LIKE '%PRC%'
    ORDER BY table_name
`;

// 查询 v_ord 视图
const checkVOrdQuery = `
    SELECT view_name 
    FROM user_views 
    WHERE view_name = 'V_ORD'
`;

async function exploreDatabase(dbName, config) {
    let connection;
    try {
        console.log(`\n📌 探索数据库：${dbName}`);
        
        connection = await oracledb.getConnection({
            user: config.user,
            password: config.password,
            connectString: config.connectString
        });
        
        console.log(`✅ 连接成功`);
        
        // 检查 v_ord 视图
        let vOrdExists = false;
        try {
            const vOrdResult = await connection.execute(checkVOrdQuery, [], { outFormat: oracledb.OUT_FORMAT_OBJECT });
            vOrdExists = vOrdResult.rows.length > 0;
            console.log(`   v_ord 视图：${vOrdExists ? '✅ 存在' : '❌ 不存在'}`);
        } catch (e) {
            console.log(`   v_ord 视图检查失败：${e.message.substring(0, 100)}`);
        }
        
        // 查询订单相关表
        const orderTables = await connection.execute(findOrderQuery, [], { outFormat: oracledb.OUT_FORMAT_OBJECT });
        console.log(`   订单相关表：${orderTables.rows.map(r => r.TABLE_NAME).join(', ') || '无'}`);
        
        // 查询所有表（前 10 个）
        const allTables = await connection.execute(listTablesQuery, [], { outFormat: oracledb.OUT_FORMAT_OBJECT });
        console.log(`   所有表 (前 10): ${allTables.rows.map(r => r.TABLE_NAME).join(', ')}`);
        
        // 尝试查询 mk_prctyp 表（报价表）
        let mkData = null;
        try {
            mkData = await connection.execute(
                `SELECT * FROM mk_prctyp WHERE ROWNUM <= 3`,
                [],
                { outFormat: oracledb.OUT_FORMAT_OBJECT }
            );
            console.log(`   mk_prctyp 表：✅ 存在，${mkData.rows.length} 条数据`);
        } catch (e) {
            console.log(`   mk_prctyp 表：❌ 不存在 (${e.message.substring(0, 50)})`);
        }
        
        // 尝试查询 ord_ct 表（订单表）
        let ordData = null;
        try {
            ordData = await connection.execute(
                `SELECT * FROM ord_ct WHERE ROWNUM <= 3`,
                [],
                { outFormat: oracledb.OUT_FORMAT_OBJECT }
            );
            console.log(`   ord_ct 表：✅ 存在，${ordData.rows.length} 条数据`);
        } catch (e) {
            console.log(`   ord_ct 表：❌ 不存在 (${e.message.substring(0, 50)})`);
        }
        
        return {
            success: true,
            dbName: dbName,
            vOrdExists: vOrdExists,
            orderTables: orderTables.rows.map(r => r.TABLE_NAME),
            allTables: allTables.rows.map(r => r.TABLE_NAME),
            mkData: mkData ? mkData.rows : null,
            ordData: ordData ? ordData.rows : null,
            mkColumns: mkData ? mkData.metaData : null,
            ordColumns: ordData ? ordData.metaData : null
        };
        
    } catch (error) {
        console.log(`❌ 连接失败：${error.message.substring(0, 200)}`);
        return {
            success: false,
            dbName: dbName,
            error: error.message
        };
    } finally {
        if (connection) {
            try {
                await connection.close();
                console.log(`🔒 连接已关闭`);
            } catch (e) {}
        }
    }
}

async function main() {
    console.log('🦞 Oracle 数据库探索工具');
    console.log('='.repeat(60));
    console.log(`⏰ 查询时间：${new Date().toLocaleString('zh-CN')}`);
    console.log(`📊 目标：探索各厂数据库表结构，为利润统计 SQL 做准备`);
    console.log('='.repeat(60));
    
    const results = [];
    
    for (const [dbName, config] of Object.entries(databases)) {
        const result = await exploreDatabase(dbName, config);
        results.push(result);
        
        // 添加延迟
        await new Promise(resolve => setTimeout(resolve, 2000));
    }
    
    // 输出结果汇总
    console.log('\n' + '='.repeat(60));
    console.log('📋 数据库探索结果汇总');
    console.log('='.repeat(60));
    
    for (const result of results) {
        console.log(`\n📌 ${result.dbName}:`);
        
        if (result.success) {
            console.log(`   ✅ 连接成功`);
            console.log(`   v_ord 视图：${result.vOrdExists ? '✅' : '❌'}`);
            console.log(`   订单相关表：${result.orderTables.join(', ') || '无'}`);
            
            if (result.mkData && result.mkData.length > 0) {
                console.log(`   mk_prctyp 数据预览:`);
                result.mkData.forEach((row, idx) => {
                    console.log(`      [${idx + 1}] ${JSON.stringify(row).substring(0, 200)}`);
                });
            }
            
            if (result.ordData && result.ordData.length > 0) {
                console.log(`   ord_ct 数据预览:`);
                result.ordData.forEach((row, idx) => {
                    console.log(`      [${idx + 1}] ${JSON.stringify(row).substring(0, 200)}`);
                });
            }
        } else {
            console.log(`   ❌ 连接失败：${result.error.substring(0, 150)}`);
        }
    }
    
    // 保存结果到文件
    const fs = require('fs');
    const outputFile = '/home/admin/.openclaw/workspace/oracle-explore-result.json';
    fs.writeFileSync(outputFile, JSON.stringify(results, null, 2));
    console.log(`\n📁 详细结果已保存到：${outputFile}`);
}

main().catch(console.error);
