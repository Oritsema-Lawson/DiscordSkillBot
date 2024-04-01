#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.

using System.Reflection;
using System.IO;
using Newtonsoft.Json;


namespace SkillBot
{
    internal class JsonUtility
    {
        // Properties to store configuration values
        public string? Token { get; private set; } 
        public string? Prefix { get; private set; }
        public ulong ServerID { get; private set; }

        // Gets program directory
        public string? currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        // Reads bot token and prefix from the "config.json" file
        public async Task ReadConfig()
        {
            string _path = Path.Combine(currentDir, "config.json");

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

                        serializer.Serialize(writer, new ConfigStructure());

                        Environment.Exit(0);
                    }
                }
                ConfigStructure? data = JsonConvert.DeserializeObject<ConfigStructure>(json);

                this.Token = data.token;
                this.Prefix = data.prefix;
                this.ServerID = data.serverID;
            }
        }

        // Reads Tree from JSON 
        public async Task<Tree> ReadTree(Tree tree)
        {
            Console.WriteLine("[JSONUTILITY] Reading Tree...");

            try
            {
                using (StreamReader reader = new StreamReader(Path.Combine(currentDir, $"{tree.treeName}.json"))) 
                {
                    string json = await reader.ReadToEndAsync();
                    Console.WriteLine(json); 
                    return JsonConvert.DeserializeObject<Tree>(json);
                }
            }
            catch
            {
                Console.WriteLine("[JSONUTILITY] Failed to read tree.");

                // Return an empty Tree if reading fails 
                Tree newTree = new Tree(){ treeName = $"{tree.treeName}" };
                await WriteTree(newTree);
                return newTree;
            }
        }

        // Writes Tree to a JSON file
        public async Task WriteTree(Tree tree)
        {
            await Task.Run(() => 
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented; 

                using (StreamWriter sw = new StreamWriter(Path.Combine(currentDir, $"{tree.treeName}.json"), false))
                using (JsonTextWriter writer = new JsonTextWriter(sw))
                {
                    writer.Indentation = 4; 
                    writer.IndentChar = ' '; 

                    serializer.Serialize(writer, tree);
                }
            });  
        }
    }

    // Config object to match json
    internal sealed class ConfigStructure
    {
        public string token = "$TOKEN";
        public string prefix = "$PREFIX";
        public ulong serverID = 0;
    }
}