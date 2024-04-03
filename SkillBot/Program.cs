#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;


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

        // Server Name and ID
        public static string serverName = "";
        public static ulong serverID = 0;

        // Static instance of the JsonUtility class 
        public static JsonUtility jsonUtility = new JsonUtility();

        // Static instance of DatabaseUtility
        public static DatabaseUtility databaseUtility = new DatabaseUtility();

        static async Task Main(string[] args)
        {
            // Create folders
            if(!Directory.Exists(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }
            
            if(!Directory.Exists(treeDir))
            {
                Directory.CreateDirectory(treeDir);
            }

            // Load config from JSON
            await jsonUtility.ReadConfig();

            serverID = jsonUtility.ServerID;
            Console.WriteLine(serverID);

            // Discord bot configuration (using a placeholder token, can use jsonUtility to get token from config file)
            DiscordConfiguration config = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonUtility.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
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

            // Connect the bot 
            await Client.ConnectAsync();

            // Keep the program running
            await Task.Delay(-1);
        }

        // Event handler for when the client is connected
        private static async Task ClientReady(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            Console.WriteLine("Client Ready!");

            await Task.Delay(5000);
            
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

                Console.WriteLine(ID + " Initialized!");

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
                                Console.WriteLine($"Adding user {member.DisplayName} to server {guild.Name}");
                            }
                        }

                        if(!String.IsNullOrEmpty(guild.Name))
                        {
                            Console.WriteLine($"Success!, Server Name: {guild.Name} Initialized!");
                            success = true; 
                        } 

                        else if(!success)
                        {
                            Console.WriteLine($"Error getting guild (attempt {retries + 1})");
                            await Task.Delay(2000); // Delay between retries
                            retries++;
                        }
                    }

                    if (!success) 
                    {
                        Console.WriteLine("Failed to initialize guild after 5 retries.");
                    }
                    else
                    {
                        await jsonUtility.WriteTree(ID, serverList[ID].tree);  
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to initialize users... {e.Message}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to initialize server {ID}.");
                Console.WriteLine(e.Message);
            }
        }
    }
}