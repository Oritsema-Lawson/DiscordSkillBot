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

                        serializer.Serialize(writer, new ConfigStructure());

                        Environment.Exit(0);
                    }
                }
                ConfigStructure? data = JsonConvert.DeserializeObject<ConfigStructure>(json);

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
                    //Console.WriteLine(json); 
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
                }
            });  

            Console.WriteLine($"[JSONUTILITY] Tree {ID}.json has been written!");
        }
    }

    // Config object to match json
    internal sealed class ConfigStructure
    {
        public string token = "$TOKEN";
        public string prefix = "$PREFIX";
    }
}