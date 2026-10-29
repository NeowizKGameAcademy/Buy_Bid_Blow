using System;
using System.Text.RegularExpressions;
using BuyBidBlow.Systems.Account;
using MySqlConnector;

namespace BuyBidBlow.Sandbox.Login
{
    // MariaDB(MySQL 호환) 계정 저장소. 드라이버는 MySqlConnector(Assets/Plugins/MySqlConnector, netstandard2.1)다.
    // 기존 Plugins/MySQL/MySql.Data.dll(6.2.5)은 MariaDB 10.10+에서 접속 단계(SHOW COLLATION의 NULL Id)에서 실패해 쓰지 않는다.
    // 호출마다 연결을 열고 닫는다(드라이버 풀링 사용). 스레드 안전.
    public sealed class MariaDbAccountRepository : IAccountRepository
    {
        readonly string connectionString;
        readonly string table;
        readonly bool autoCreateTable;
        readonly object schemaLock = new object();
        volatile bool schemaReady;

        public MariaDbAccountRepository(LoginDbSettings settings)
        {
            if (!Regex.IsMatch(settings.table ?? "", "^[A-Za-z0-9_]{1,64}$")) throw new ArgumentException("table 이름은 영문·숫자·밑줄만 쓸 수 있습니다: " + settings.table);
            table = settings.table;
            autoCreateTable = settings.autoCreateTable;
            // MySqlConnector는 항상 utf8mb4로 통신하므로 문자셋 옵션이 필요 없다.
            var builder = new MySqlConnectionStringBuilder
            {
                Server = settings.host.Trim(),
                Port = (uint)settings.port,
                Database = settings.database.Trim(),
                UserID = settings.user ?? "",
                Password = settings.password ?? "",
                SslMode = ParseSslMode(settings.sslMode),
                ConnectionTimeout = (uint)Math.Max(1, settings.connectTimeoutSeconds),
                Pooling = true
            };
            connectionString = builder.ConnectionString;
        }

        public AccountRecord FindByLoginId(string loginId)
        {
            using (var connection = Open())
            using (var command = new MySqlCommand($"SELECT account_id, login_id, nickname, password_hash FROM `{table}` WHERE login_id = @id LIMIT 1", connection))
            {
                command.Parameters.AddWithValue("@id", loginId);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new AccountRecord(Convert.ToInt64(reader.GetValue(0)), reader.GetString(1), reader.GetString(2), reader.GetString(3));
                }
            }
        }

        public bool TryInsert(string loginId, string nickname, string passwordHash, out long accountId)
        {
            accountId = 0;
            using (var connection = Open())
            {
                using (var insert = new MySqlCommand($"INSERT INTO `{table}` (login_id, nickname, password_hash) VALUES (@id, @nickname, @hash)", connection))
                {
                    insert.Parameters.AddWithValue("@id", loginId);
                    insert.Parameters.AddWithValue("@nickname", nickname);
                    insert.Parameters.AddWithValue("@hash", passwordHash);
                    try { insert.ExecuteNonQuery(); }
                    catch (MySqlException e) when (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry) { return false; }
                    accountId = insert.LastInsertedId;
                    return true;
                }
            }
        }

        public void MarkSignedIn(long accountId)
        {
            using (var connection = Open())
            using (var command = new MySqlCommand($"UPDATE `{table}` SET last_login_at = NOW() WHERE account_id = @accountId", connection))
            {
                command.Parameters.AddWithValue("@accountId", accountId);
                command.ExecuteNonQuery();
            }
        }

        // 접속 확인용(에디터 메뉴). 서버 버전과 계정 수를 돌려준다.
        public string Probe()
        {
            using (var connection = Open())
            {
                string version;
                using (var command = new MySqlCommand("SELECT VERSION()", connection)) version = Convert.ToString(command.ExecuteScalar());
                using (var command = new MySqlCommand($"SELECT COUNT(*) FROM `{table}`", connection))
                    return $"{version} · {table} {Convert.ToInt64(command.ExecuteScalar())}개 계정";
            }
        }

        static MySqlSslMode ParseSslMode(string value) =>
            Enum.TryParse(value ?? "", true, out MySqlSslMode mode) ? mode : MySqlSslMode.Preferred;

        MySqlConnection Open()
        {
            var connection = new MySqlConnection(connectionString);
            try { connection.Open(); EnsureSchema(connection); return connection; }
            catch { connection.Dispose(); throw; }
        }

        void EnsureSchema(MySqlConnection connection)
        {
            if (schemaReady || !autoCreateTable) return;
            lock (schemaLock)
            {
                if (schemaReady) return;
                // Dev/Sandbox/Login/accounts.sql과 같은 정의다. 바꾸면 둘 다 고친다.
                using (var command = new MySqlCommand(
                    $"CREATE TABLE IF NOT EXISTS `{table}` (" +
                    "account_id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY, " +
                    "login_id VARCHAR(32) NOT NULL, " +
                    "nickname VARCHAR(64) NOT NULL, " +
                    "password_hash VARCHAR(255) NOT NULL, " +
                    "created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, " +
                    "last_login_at DATETIME NULL, " +
                    "UNIQUE KEY uq_login_id (login_id)" +
                    ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4", connection))
                    command.ExecuteNonQuery();
                schemaReady = true;
            }
        }
    }
}
