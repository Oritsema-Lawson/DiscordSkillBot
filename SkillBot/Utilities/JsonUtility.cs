#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.

using Newtonsoft.Json;
using DSharpPlus;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;


namespace SkillBot
{
    internal class JsonUtility
    {
        // Properties to store configuration values
        public string? Token { get; private set; } 
        public string? Prefix { get; private set; }

        // Gets program directory
        public string? treeDirectory = Program.treeDir;

        // Reads bot token and prefix from the "config.json" file
        public async Task ReadConfig()
        {
            string _path = Path.Combine(Program.currentDir, "config.json");

            if(!File.Exists(_path))
            {
                StreamWriter sw = File.CreateText(_path);
                sw.Dispose();
            }

            using (StreamReader reader = new StreamReader(_path))
            {
                string json = await reader.ReadToEndAsync();
                
                if(String.IsNullOrEmpty(json))
                {
                    Console.WriteLine("[JSONUTILITY] Please set token in config before relaunching.");

                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Formatting = Formatting.Indented; 

                    using (StreamWriter sw = new StreamWriter(_path, false))
                    using (JsonTextWriter writer = new JsonTextWriter(sw))
                    {
                        writer.Indentation = 4; 
                        writer.IndentChar = ' '; 

                        ConfigStructure config = new ConfigStructure() {
                            token = "$TOKEN",
                            prefix = "$PREFIX"
                        };

                        serializer.Serialize(writer, config);
                        await writer.FlushAsync();
                        await Task.Delay(1000);
                        await writer.CloseAsync();
                        Environment.Exit(0);
                    }
                }
                ConfigStructure? data = JsonConvert.DeserializeObject<ConfigStructure>(json);

                if(data.token == "$TOKEN")
                {
                    Console.WriteLine("[JSONUTILITY] Please set token in config before relaunching.");
                    Environment.Exit(0);
                }
                
                Console.WriteLine($"[JSONUTILITY] Checking token format...");
                bool isTokenCorrect = Regex.IsMatch(data.token, @"^[A-Za-z0-9\-]{0,200}\.[A-Za-z0-9\-]{0,200}\.[A-Za-z0-9\-]{0,200}$"); 

                if(isTokenCorrect)
                {
                    await CheckToken(data.token);
                }
                else
                {
                    Console.WriteLine("[JSONUTILITY] Malformed token.");
                    Environment.Exit(0);
                }

                this.Token = data.token;
                this.Prefix = data.prefix;
            }
        }

        // Reads Tree from JSON 
        public async Task<Tree> ReadTree(ulong ID)
        {
            Console.WriteLine("[JSONUTILITY] Reading Tree...");

            try
            {
                using (StreamReader reader = new StreamReader(Path.Combine(Program.treeDir, $"{ID}.json"))) 
                {
                    string json = await reader.ReadToEndAsync();
                    return JsonConvert.DeserializeObject<Tree>(json);
                }
            }
            catch
            {
                Console.WriteLine("[JSONUTILITY] Failed to read tree... Writing...");

                // Return an empty Tree if reading fails 
                Tree newTree = new Tree();
                await WriteTree(ID, newTree);
                return newTree;
            }
        }

        // Writes Tree to a JSON file
        public async Task WriteTree(ulong ID, Tree tree)
        {
            await Task.Run(() => 
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented; 

                using (StreamWriter sw = new StreamWriter(Path.Combine(treeDirectory, $"{ID}.json"), false))
                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    writer.Indentation = 4; 
                    writer.IndentChar = ' '; 

                    serializer.Serialize(writer, tree);
                    writer.Flush();
                }
            });  

            Console.WriteLine($"[JSONUTILITY] Tree {ID}.json has been written!");
        }

        public async Task CheckToken(string token) 
        {
            Console.WriteLine("[JSONUTILITY] Checking token validity...");
            try
            {
                var discordClient = new DiscordClient(new DiscordConfiguration()
                {
                    Token = token,
                    TokenType = TokenType.Bot,
                    MinimumLogLevel = LogLevel.Critical
                    
                });

                await discordClient.ConnectAsync();
                await discordClient.DisconnectAsync();

                Console.WriteLine("[JSONUTILITY] Token valid!");

                return; 
            }
            catch (Exception)
            {
                Console.WriteLine("[JSONUTILITY] Invalid token or connection problem.");
                Environment.Exit(0);
            }
        }
    }

    // Config object to match json
    internal sealed class ConfigStructure
    {
        public string token = "$TOKEN";
        public string prefix = "$PREFIX";
    }
}