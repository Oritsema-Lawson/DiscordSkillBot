#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

using System;
using System.IO;
using System.Runtime.InteropServices;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace SkillBot
{
    class Program
    {
        // Public properties to access the Discord client and CommandsNext extension
        public static DiscordClient? Client { get; private set; }
        private static CommandsNextExtension? Commands { get; set; }

        // Static reference to the main Skill tree object
        public static Tree? mainTree = new Tree(){ treeName = "MainTree" }; 

        // Server Name and ID
        public static string serverName = "";
        public static ulong serverID = 0;

        // Static instance of the JsonUtility class 
        public static JsonUtility jsonUtility = new JsonUtility();

        // Static instance of DatabaseUtility
        public static DatabaseUtility databaseUtility = new DatabaseUtility();

        static async Task Main(string[] args)
        {
            // Load config from JSON
            await jsonUtility.ReadConfig();

            // Initialise Database
            await databaseUtility.InitializeDB();

            // Load mainTree from the JSON file
            mainTree = await jsonUtility.ReadTree(mainTree);

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
                mainTree.existingUsers.Add(e.Member.Id);
                await databaseUtility.AddUserIfNotExists(e.Member.Id);
                await jsonUtility.WriteTree(mainTree);   
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
            Console.WriteLine("ClientReady!");

            await Task.Delay(5000);

            try 
            {
                var guild = await sender.GetGuildAsync(serverID);
                
                // Add all existing members
                
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Error getting guild: {ex.Message}");
            }

            await Task.Run(async () => 
            {
                bool success = false;
                int retries = 0;

                while (!success && retries < 5) // Retry a few times with spacing
                {
                    var guild = await sender.GetGuildAsync(serverID); 
                    
                    foreach (var member in guild.Members.Values)  
                    {
                        if(!mainTree.existingUsers.Contains(member.Id))
                        {
                            mainTree.existingUsers.Add(member.Id);
                            await databaseUtility.AddUserIfNotExists(member.Id);  
                            Console.WriteLine($"Adding user {member.DisplayName}");
                        }
                    }

                    if(!String.IsNullOrEmpty(guild.Name))
                    {
                        Console.WriteLine($"Success!, Server Name: {guild.Name}");
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
                    await jsonUtility.WriteTree(mainTree);  
                }
            });
        }
    }
}