const oracledb = require('oracledb');

// 数据库连接配置 - 易捷集团
const DB_CONFIG = {
    user: 'fgrp',
    password: 'kuke.fgrp',
    connectString: '(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=36.138.130.91)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=dbms)))'
};

async function describeTable(conn, tableName) {
    const result = await conn.execute(
        `SELECT column_name, data_type, data_length 
         FROM user_tab_columns 
         WHERE table_name = :table
         ORDER BY column_id`,
        [tableName],
        { outFormat: oracledb.OUT_FORMAT_OBJECT }
    );
    return result.rows;
}

async function executeQuery(conn, sql, params = [], options = { outFormat: oracledb.OUT_FORMAT_OBJECT }) {
    try {
        const result = await conn.execute(sql, params, options);
        return {
            success: true,
            rows: result.rows,
            columns: result.metaData.map(col => col.name)
        };
    } catch (err) {
        return {
            success: false,
            error: err.message
        };
    }
}

async function main() {
    console.log('='.repeat(80));
    console.log('oders: Oracle 数据库查询工具');
    console.log('ders: 查询易捷集团数据库');
    console.log('='.repeat(80));
    console.log(`ders: 查询时间：${new Date().toLocaleString('zh-CN')}\n`);

    let connection;
    const results = {};

    try {
        console.log('ders: 连接数据库...');
        connection = await oracledb.getConnection(DB_CONFIG);
        console.log('ders: ✓ 连接成功\n');

        // 1. 查询 PB_DEPT_MEMBER 表结构
        console.log('ders: >>> 查询 PB_DEPT_MEMBER 表结构...');
        let result = await describeTable(connection, 'PB_DEPT_MEMBER');
        results.pb_dept_member_structure = {
            table: 'PB_DEPT_MEMBER',
            success: true,
            columns: result.map(col => ({
                column_name: col.COLUMN_NAME,
                data_type: col.DATA_TYPE,
                data_length: col.DATA_LENGTH
            }))
        };
        console.log('oders: 字段列表:');
        results.pb_dept_member_structure.columns.forEach(col => {
            console.log(`oders:   ${col.column_name} - ${col.data_type}(${col.data_length})`);
        });
        console.log();

        // 2. 查询 PB_DEPT 表结构
        console.log('oders: >>> 查询 PB_DEPT 表结构...');
        result = await describeTable(connection, 'PB_DEPT');
        results.pb_dept_structure = {
            table: 'PB_DEPT',
            success: true,
            columns: result.map(col => ({
                column_name: col.COLUMN_NAME,
                data_type: col.DATA_TYPE,
                data_length: col.DATA_LENGTH
            }))
        };
        console.log('oders: 字段列表:');
        results.pb_dept_structure.columns.forEach(col => {
            console.log(`oders:   ${col.column_name} - ${col.data_type}(${col.data_length})`);
        });
        console.log();

        // 3. 执行修复后的 SQL 查询
        console.log('oders: >>> 执行修复后的 SQL 查询...');
        const fixedSql = `SELECT m.user_cde as EMPCDE, m.dept_cde as TEMCDE, m.dept_cde as TEMCDE2, 
                                 m.user_nme as EMPNME, d.dept_nme as TEMNME
                          FROM pb_dept_member m
                          LEFT JOIN pb_dept d ON m.dept_cde = d.dept_cde
                          WHERE m.isactive = 'Y'
                          ORDER BY d.dept_nme, m.user_nme`;
        result = await executeQuery(connection, fixedSql);
        results.fixed_query = {
            sql: fixedSql,
            ...result
        };
        if (result.success) {
            console.log(`oders: 查询成功，返回 ${result.rows.length} 条数据`);
            if (result.rows.length > 0) {
                console.log('oders: 列名:', result.columns.join(', '));
                console.log('oders: 前5条数据:');
                result.rows.slice(0, 5).forEach((row, idx) => {
                    console.log(`oders:   [${idx + 1}] ${JSON.stringify(row)}`);
                });
            }
        } else {
            console.log(`oders: 查询失败: ${result.error}`);
        }
        console.log();

        // 4. 查询昨天（2026-03-04）到今天（2026-03-05）的订单数量
        console.log('oders: >>> 查询昨天（2026-03-04）到今天（2026-03-05）的订单数量...');
        const sqlOrders = `SELECT COUNT(*) as cnt FROM ord_bas 
                           WHERE created >= TO_DATE(:dateFrom, 'YYYY-MM-DD') 
                             AND created < TO_DATE(:dateTo, 'YYYY-MM-DD') 
                             AND isactive = 'Y'`;
        result = await executeQuery(connection, sqlOrders, ['2026-03-04', '2026-03-05']);
        results.order_count = {
            sql: sqlOrders,
            date_from: '2026-03-04',
            date_to: '2026-03-05',
            ...result
        };
        if (result.success && result.rows.length > 0) {
            console.log(`oders: 昨天（2026-03-04）到今天（2026-03-05）的订单数量: ${result.rows[0].CNT}`);
        } else {
            console.log(`oders: 查询失败: ${result.error}`);
        }
        console.log();

        // 输出 JSON 结果
        console.log('oders: ' + '='.repeat(80));
        console.log('oders: 📤 JSON 输出:');
        console.log('oders: ' + '='.repeat(80));
        console.log(JSON.stringify(results, null, 2));

    } catch (error) {
        console.log(`oders: ❌ 执行出错：${error.message}`);
    } finally {
        if (connection) {
            try {
                await connection.close();
                console.log('\noders: 🔒 连接已关闭');
            } catch (e) {}
        }
    }
}

main().catch(console.error);
