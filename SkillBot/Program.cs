#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8601 // Possible null reference assignment.

using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;

namespace SkillBot
{
    class Program
    {
        public static string? currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string dbDir = Path.Combine(currentDir, "Databases");
        public static string treeDir = Path.Combine(currentDir, "Trees");
        
        public static List<ulong>? serverIDs;

        public static ServerList serverList = new ServerList();

        public static Dictionary<ulong, string> Servers = new Dictionary<ulong, string>();

        // Public properties to access the Discord client and CommandsNext extension
        public static DiscordClient? Client { get; private set; }
        private static CommandsNextExtension? Commands { get; set; }

        // Server ID
        public static ulong CurrentServerID = 0;

        // Static instance of the JsonUtility class 
        public static JsonUtility jsonUtility = new JsonUtility();

        // Static instance of DatabaseUtility
        public static DatabaseUtility databaseUtility = new DatabaseUtility();

        static async Task Main(string[] args)
        {
            // Load config from JSON
            await jsonUtility.ReadConfig();

            // Create folders for trees and databases
            if(!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }
            
            if(!Directory.Exists(treeDir))
            {
                Directory.CreateDirectory(treeDir);
            }

            // Discord bot configuration
            DiscordConfiguration config = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonUtility.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                MinimumLogLevel = LogLevel.Information
            };

            // CommandsNext configuration
            CommandsNextConfiguration commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { jsonUtility.Prefix },
                EnableMentionPrefix = true,
                EnableDms = false,
                EnableDefaultHelp = false
            };

            // Initialize the Discord client
            Client = new DiscordClient(config);

            // Event handler for when the bot is ready to use
            Client.Ready += ClientReady;
            
            // Adds user to database when a user joins
            Client.GuildMemberAdded += async (client, e) => 
            {
                //mainTree.existingUsers.Add(e.Member.Id);
                await databaseUtility.AddUserIfNotExists(e.Member.Id, e.Guild.Id);
                await jsonUtility.WriteTree(e.Guild.Id, serverList[e.Guild.Id].tree);   
            };

            Client.GuildCreated += async (client, e) =>
            {
                await initServer(e.Guild.Id);
            };
            
            // Set up CommandsNext and register command classes
            Commands = Client.UseCommandsNext(commandsConfig);
            Commands.RegisterCommands<UtilityCommands>();
            Commands.RegisterCommands<SkillCommands>();
            
            var slash = Client.UseSlashCommands();
            slash.RegisterCommands<SkillSlashCommands>();

            // Connect the bot 
            await Client.ConnectAsync();
            
            
            // Keep the program running
            await Task.Delay(-1);
        }

        // Event handler for when the client is connected
        private static async Task ClientReady(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Console.WriteLine("[PROGRAM] Client Ready!");

            await Task.Delay(2000);
            
            serverIDs = Client.Guilds.Keys.ToList();

            foreach (ulong ID in serverIDs)
            {
                await initServer(ID);
            }

        }

        private static async Task initServer(ulong ID)
        {
            try
            {
                await databaseUtility.InitializeDB(ID);
                serverList.Add(ID);

                var serverData = serverList[ID];
                Tree tempTree = await jsonUtility.ReadTree(ID);
                serverData.tree = tempTree;

                serverList[ID] = serverData; 

                Console.WriteLine("[PROGRAM] Server Initializing..." + ID);

                try
                {
                    bool success = false;
                    int retries = 0;

                    while (!success && retries < 5) // Retry a few times with spacing
                    {
                        var guild = await Client.GetGuildAsync(ID); 
                        
                        foreach (var member in guild.Members.Values)  
                        {
                            if(!serverList[ID].tree.existingUsers.Contains(member.Id))
                            {
                                serverList[ID].tree.existingUsers.Add(member.Id);
                                await databaseUtility.AddUserIfNotExists(member.Id, ID);  
                                Console.WriteLine($"[PROGRAM] Adding user {member.DisplayName} to server {guild.Name}");
                            }
                        }

                        if(!String.IsNullOrEmpty(guild.Name))
                        {
                            Console.WriteLine($"[PROGRAM] Success!, Server: {guild.Name} Initialized!");
                            success = true; 
                        } 

                        else if(!success)
                        {
                            Console.WriteLine($"[PROGRAM] Error getting guild (attempt {retries + 1})");
                            await Task.Delay(2000); // Delay between retries
                            retries++;
                        }
                    }

                    if (!success) 
                    {
                        Console.WriteLine("[PROGRAM] Failed to initialize guild after 5 retries.");
                    }
                    else
                    {
                        await jsonUtility.WriteTree(ID, serverList[ID].tree);  
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[PROGRAM] Failed to initialize users... {e.Message}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[PROGRAM] Failed to initialize server {ID}.");
                Console.WriteLine(e.Message);
            }
        }
    }
}