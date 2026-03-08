const oracledb = require('oracledb');

// 初始化 Oracle Client (Thick 模式) - 支持旧版本数据库
const ORACLE_CLIENT_PATH = '/home/admin/oracle_instantclient/instantclient_21_1';
console.log('🔧 初始化 Oracle Thick 模式...');
console.log(`📂 Oracle Client 路径：${ORACLE_CLIENT_PATH}`);

try {
    oracledb.initOracleClient({ libDir: ORACLE_CLIENT_PATH });
    console.log('✅ Oracle Thick 模式初始化成功');
} catch (err) {
    console.log(`⚠️  Thick 模式初始化失败：${err.message}`);
    console.log('📝 将尝试使用 Thin 模式（可能不支持旧版数据库）');
}

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

// 利润统计 SQL
const profitQuery = `
    SELECT 
        t.ptdate as PTDATE,
        t.serial as SERIAL,
        c.clntnme as CLIENT,
        p.prdtnme as PRODUCT,
        e.empnme as SALESMAN,
        d.dptnme as DEPT,
        nvl(sum(t.accamt),0) as QUOTE_AMOUNT,
        nvl(sum(s.sell_amt),0) as SELL_AMOUNT,
        nvl(sum(s.sell_amt),0) - nvl(sum(t.accamt),0) as PROFIT,
        case when nvl(sum(t.accamt),0) = 0 then 0
             else (nvl(sum(s.sell_amt),0) - nvl(sum(t.accamt),0)) / nvl(sum(t.accamt),0) * 100
        end as PROFIT_RATE
    FROM v_ord t
    LEFT JOIN pb_clnt c ON t.clntcde = c.clntcde
    LEFT JOIN prd_base p ON t.prdtcde = p.prdtcde
    LEFT JOIN pb_dept_member e ON t.agntcde = e.mobile
    LEFT JOIN pb_dept d ON e.dptcde = d.dptcde
    LEFT JOIN sell_detail s ON t.serial = s.order_no
    WHERE t.status = 'Y'
    GROUP BY t.ptdate, t.serial, c.clntnme, p.prdtnme, e.empnme, d.dptnme
    ORDER BY t.ptdate DESC
`;

// 简化查询（备用）
const simpleQuery = `
    SELECT 
        ptdate,
        serial,
        clntcde,
        prdtcde,
        accamt,
        status
    FROM v_ord
    WHERE status = 'Y'
    ORDER BY ptdate DESC
`;

async function queryDatabase(dbName, config) {
    let connection;
    try {
        console.log(`\n📌 连接数据库：${dbName} (${config.connectString.substring(50, 80)}...)`);
        
        connection = await oracledb.getConnection({
            user: config.user,
            password: config.password,
            connectString: config.connectString
        });
        
        console.log(`✅ 连接成功`);
        
        // 先测试简单查询
        let result;
        try {
            result = await connection.execute(
                simpleQuery,
                [],
                { 
                    outFormat: oracledb.OUT_FORMAT_OBJECT,
                    maxRows: 3
                }
            );
        } catch (e) {
            console.log(`⚠️  简单查询失败，尝试基础表查询：${e.message.substring(0, 100)}`);
            
            // 尝试查询基础表
            result = await connection.execute(
                `SELECT table_name FROM user_tables WHERE ROWNUM <= 3`,
                [],
                { outFormat: oracledb.OUT_FORMAT_OBJECT }
            );
        }
        
        return {
            success: true,
            dbName: dbName,
            rows: result.rows,
            columns: result.metaData
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
    console.log('🦞 Oracle 数据库查询工具');
    console.log('='.repeat(60));
    console.log(`⏰ 查询时间：${new Date().toLocaleString('zh-CN')}`);
    console.log(`📊 目标：从每个厂拉取 3 条数据验证 SQL`);
    console.log('='.repeat(60));
    
    const results = [];
    
    for (const [dbName, config] of Object.entries(databases)) {
        const result = await queryDatabase(dbName, config);
        results.push(result);
        
        // 添加延迟避免连接过快
        await new Promise(resolve => setTimeout(resolve, 1000));
    }
    
    // 输出结果
    console.log('\n' + '='.repeat(60));
    console.log('📋 查询结果汇总');
    console.log('='.repeat(60));
    
    for (const result of results) {
        console.log(`\n📌 ${result.dbName}:`);
        
        if (result.success) {
            console.log(`   ✅ 查询成功，返回 ${result.rows.length} 条数据`);
            
            if (result.rows.length > 0) {
                console.log(`   列名：${result.columns.map(c => c.name).join(', ')}`);
                console.log(`   数据预览:`);
                result.rows.forEach((row, idx) => {
                    console.log(`      [${idx + 1}] ${JSON.stringify(row)}`);
                });
            }
        } else {
            console.log(`   ❌ 查询失败：${result.error.substring(0, 150)}`);
        }
    }
    
    // 输出 JSON 结果
    console.log('\n' + '='.repeat(60));
    console.log('📤 JSON 输出:');
    console.log('='.repeat(60));
    console.log(JSON.stringify(results, null, 2));
}

main().catch(console.error);
