#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8605 // Unboxing a possibly null value.


using System.Reflection;
using System.Data.SQLite;

namespace SkillBot
{
    class DatabaseUtility
    {
        public string? currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public string databaseName = "users.db";
        public string connectionString = "";

        public async Task InitializeDB()
        {
            string databasePath = Path.Combine(currentDir, databaseName); 
            connectionString = $"Data Source={databasePath}; Version=3;";

            if(!File.Exists(databasePath))
            {
                SQLiteConnection.CreateFile(databasePath);

                // Optional: Connect to the database to perform initial setup (e.g., create tables)
                using (var connection = new SQLiteConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Example of creating a table
                    var createTableCommand = new SQLiteCommand("CREATE TABLE IF NOT EXISTS user_data (userID INTEGER PRIMARY KEY, skillpoints INTEGER)", connection);
                    await createTableCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task AddBranchColumn(string branchName)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();
                string columnName = branchName + "Progress";

                var command = new SQLiteCommand($"ALTER TABLE user_data ADD COLUMN {columnName} INTEGER DEFAULT 0", connection); 
                await command.ExecuteNonQueryAsync(); 
            }
        }

        public async Task AddUserIfNotExists(ulong userId) 
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                var checkCommand = new SQLiteCommand("SELECT COUNT(*) FROM user_data WHERE userID = @userId", connection);
                checkCommand.Parameters.AddWithValue("@userId", userId);
                long userCount = (long)await checkCommand.ExecuteScalarAsync();

                if (userCount == 0) 
                {
                    var insertCommand = new SQLiteCommand("INSERT INTO user_data (userID, skillpoints) VALUES (@userId, 0)", connection); 
                    insertCommand.Parameters.AddWithValue("@userId", userId);
                    await insertCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<int> GetSkillPoints(ulong userId)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                var command = new SQLiteCommand("SELECT skillpoints FROM user_data WHERE userID = @userId", connection);
                command.Parameters.AddWithValue("@userId", userId); 

                object? result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0; 
            }
        }

        public async Task SetSkillPoints(ulong userId, int skillPoints)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                var command = new SQLiteCommand("UPDATE user_data SET skillpoints = @skillPoints WHERE userID = @userId", connection);
                command.Parameters.AddWithValue("@skillPoints", skillPoints);
                command.Parameters.AddWithValue("@userId", userId);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> GetBranchProgress(ulong userId, string branchName)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                string columnName = branchName + "Progress";
                var command = new SQLiteCommand($"SELECT {columnName} FROM user_data WHERE userID = @userId", connection);
                command.Parameters.AddWithValue("@userId", userId); 

                object? result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        public async Task SetBranchProgress(ulong userId, string branchName, int progress)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                string columnName = branchName + "Progress";
                var command = new SQLiteCommand($"UPDATE user_data SET {columnName} = @progress WHERE userID = @userId", connection);
                command.Parameters.AddWithValue("@progress", progress);
                command.Parameters.AddWithValue("@userId", userId);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteBranchColumn(string branchName)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                await connection.OpenAsync();

                string columnName = branchName + "Progress";

                // Important safety check - Make sure the column actually exists
                var columnExistsCommand = new SQLiteCommand($"SELECT COUNT(*) FROM pragma_table_info('user_data') WHERE name = @columnName;", connection);
                columnExistsCommand.Parameters.AddWithValue("@columnName", columnName);
                long columnCount = (long)await columnExistsCommand.ExecuteScalarAsync();

                if (columnCount > 0)
                {
                    var deleteCommand = new SQLiteCommand($"ALTER TABLE user_data DROP COLUMN {columnName}", connection);
                    await deleteCommand.ExecuteNonQueryAsync();
                }
                else
                {
                    // Handle the case where the column doesn't exist - you might want to log this or send a message
                    Console.WriteLine($"Column {columnName} does not exist in the table.");
                }
            }
        }
    }
}