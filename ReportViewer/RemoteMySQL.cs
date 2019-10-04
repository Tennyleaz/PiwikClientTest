using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ReportViewer
{
    class RemoteMySQL
    {
        private readonly string server, user, password, name, connStr;
        private readonly int port;

        public RemoteMySQL(string server, int port, string name, string user, string password)
        {
            this.server = server;
            this.port = port;
            this.name = name;
            this.user = user;
            this.password = password;
            connStr = $"server={server};user={user};database={name};port={port};password={password}";
        }

        public async Task<List<VerifyLog>> DoWorkAsync()
        {
            List<VerifyLog> logs = new List<VerifyLog>();
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();

                /*for (int i=1; i<10; i++)
                {
                    string sql = $@"
                        create table ad_archive_2019_{i.ToString("D2")} as
                        select idsite, matomo_log_visit.idvisit, INET_NTOA(CONV(HEX(location_ip), 16, 10)) AS ip, matomo_log_visit.location_country as country, tableVisitID.name as actionName, idaction, tableVisitID.server_time
                        from matomo_log_visit
                        inner join
	                        (select idvisit, idaction_url, server_time, tableActionID.name, tableActionID.idaction
	                        from matomo_log_link_visit_action
	                        inner join 
		                        (select matomo_log_action.idaction, matomo_log_action.name
		                        from matomo_log_action
		                        where matomo_log_action.name LIKE '%/Ad/%' or matomo_log_action.idaction = 15289) as tableActionID
	                        on tableActionID.idaction = matomo_log_link_visit_action.idaction_url
                            where server_time >= '2019-{(i).ToString("D2")}-01 00:00:00' and server_time < '2019-{(i+1).ToString("D2")}-01 00:00:00') as tableVisitID
                        on matomo_log_visit.idvisit = tableVisitID.idvisit";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        cmd.ExecuteNonQuery();
                }*/

                // Perform database operations
                HashSet<string> tableSet = new HashSet<string>();
                using (MySqlCommand cmd = new MySqlCommand("show tables;", conn))
                {
                    var reader = cmd.ExecuteReader();
                    while (await reader.ReadAsync())
                        tableSet.Add(reader[0].ToString());
                    reader.Close();
                    reader.Dispose();
                }

                List<DateTime> unProcessedDates = new List<DateTime>();
                string sql = "CREATE TEMPORARY TABLE IF NOT EXISTS tableLogs as select * from ad_archive_2019_01";
                DateTime startDate = new DateTime(2019, 1, 1);
                DateTime thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime endDate = thisMonth.AddDays(-1);
                for (DateTime dt = startDate; dt <= endDate; dt = dt.AddMonths(1))
                {
                    string tableName = $@"ad_archive_{dt.Year}_{dt.Month.ToString("D2")}";
                    sql += $@" union select * from {tableName}";
                    if (!tableSet.Contains(tableName))
                        unProcessedDates.Add(dt);
                }

                // 本月的報告還沒彙整，先做成臨時表格
                string thisMonthTempTable = $"temp_{thisMonth.Year}_{thisMonth.Month.ToString("D2")}";
                string sqlTemp = GenerateTempTableSQL(thisMonth, thisMonthTempTable);
                //using (MySqlCommand cmd = new MySqlCommand(sqlTemp, conn))
                //    await cmd.ExecuteNonQueryAsync();

                // 記得加上本月臨時表格
                //sql += " union select * from " + thisMonthTempTable;

                // 如果有剩下沒產生的月份，產生永久表格
                foreach (DateTime d in unProcessedDates)
                {
                    string sql2 = GenerateCreateTableSQL(d);
                    using (MySqlCommand cmd = new MySqlCommand(sql2, conn))
                        await cmd.ExecuteNonQueryAsync();
                }

                // 讀取全部的紀錄，產生 tableLogs 臨時表格
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    await cmd.ExecuteNonQueryAsync();

                // 點擊廣告的訪客 tableAd
                string sqlAdTable = @"
                    CREATE TEMPORARY TABLE IF NOT EXISTS tableAd AS
                    (select * from tableLogs
                    where idaction != 15289);";
                using (MySqlCommand cmd1 = new MySqlCommand(sqlAdTable, conn))
                    await cmd1.ExecuteNonQueryAsync();

                // 實際註冊的訪客 tableRegistered
                string sqlRegisteredTable = @"
                    CREATE TEMPORARY TABLE IF NOT EXISTS tableRegistered AS
                    (select * from tableLogs
                    where idaction = 15289 and ip != '218.161.53.0' and ip != '10.10.12.0' and ip != '10.10.8.0');";
                using (MySqlCommand cmd2 = new MySqlCommand(sqlRegisteredTable, conn))
                    await cmd2.ExecuteNonQueryAsync();

                // 尋找相同的 ip，僅限同一天內的
                string finalQuery = @"
                    select tableAd.idsite, tableRegistered.idvisit, tableRegistered.ip, tableRegistered.country, tableAd.actionName, tableRegistered.server_time as registedTime, tableAd.server_time as adClickTime
                    from tableRegistered
                    inner join tableAd
                    on tableRegistered.ip = tableAd.ip
                    where DATE(tableRegistered.server_time) = DATE(tableAd.server_time)
                    ORDER BY registedTime";
                
                using (MySqlCommand cmd3 = new MySqlCommand(finalQuery, conn))
                {
                    var reader = cmd3.ExecuteReader();
                    while (await reader.ReadAsync())
                    {
                        if (reader.FieldCount >= 7)
                        {
                            VerifyLog verifyLog = new VerifyLog();
                            verifyLog.idsite = (uint)reader[0];
                            verifyLog.idvisit = (ulong)reader[1];
                            verifyLog.ip = reader[2].ToString();
                            verifyLog.country = reader[3].ToString();
                            verifyLog.actionName = reader[4].ToString();
                            verifyLog.registeredTime = (DateTime)reader[5];
                            verifyLog.adClickTime = (DateTime)reader[6];
                            // 只留一組註冊的 idvisit
                            if (!logs.Exists(x => x.idvisit == verifyLog.idvisit))
                                logs.Add(verifyLog);
                        }
                    }
                    reader.Close();
                    reader.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
            conn.Dispose();
            Console.WriteLine("Done.");
            return logs;
        }

        private string GenerateCreateTableSQL(DateTime givenMonth)
        {
            DateTime end = givenMonth.AddMonths(1);
            string sql = $@"
create table ad_archive_{givenMonth.Year}_{givenMonth.Month.ToString("D2")} as
 select idsite, matomo_log_visit.idvisit, INET_NTOA(CONV(HEX(location_ip), 16, 10)) AS ip, matomo_log_visit.location_country as country, tableVisitID.name as actionName, idaction, tableVisitID.server_time
 from matomo_log_visit
 inner join
 (select idvisit, idaction_url, server_time, tableActionID.name, tableActionID.idaction
 from matomo_log_link_visit_action
 inner join 
 (select matomo_log_action.idaction, matomo_log_action.name
 from matomo_log_action
 where matomo_log_action.name LIKE '%/Ad/%' or matomo_log_action.idaction = 15289) as tableActionID
 on tableActionID.idaction = matomo_log_link_visit_action.idaction_url
 where server_time >= '{givenMonth.Year}-{givenMonth.Month.ToString("D2")}-01 00:00:00' and server_time < '{end.Year}-{end.Month.ToString("D2")}-01 00:00:00') as tableVisitID
 on matomo_log_visit.idvisit = tableVisitID.idvisit";
            return sql;
        }

        private string GenerateTempTableSQL(DateTime givenMonth, string tempTableName)
        {
            DateTime end = givenMonth.AddMonths(1);
            string sql = $@"
CREATE TEMPORARY TABLE IF NOT EXISTS {tempTableName} as
 select idsite, matomo_log_visit.idvisit, INET_NTOA(CONV(HEX(location_ip), 16, 10)) AS ip, matomo_log_visit.location_country as country, tableVisitID.name as actionName, idaction, tableVisitID.server_time
 from matomo_log_visit
 inner join
 (select idvisit, idaction_url, server_time, tableActionID.name, tableActionID.idaction
 from matomo_log_link_visit_action
 inner join 
 (select matomo_log_action.idaction, matomo_log_action.name
 from matomo_log_action
 where matomo_log_action.name LIKE '%/Ad/%' or matomo_log_action.idaction = 15289) as tableActionID
 on tableActionID.idaction = matomo_log_link_visit_action.idaction_url
 where server_time >= '{givenMonth.Year}-{givenMonth.Month.ToString("D2")}-01 00:00:00' and server_time < '{end.Year}-{end.Month.ToString("D2")}-01 00:00:00') as tableVisitID
 on matomo_log_visit.idvisit = tableVisitID.idvisit";
            return sql;
        }
    }
}
