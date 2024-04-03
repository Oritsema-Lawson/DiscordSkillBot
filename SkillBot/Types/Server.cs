namespace SkillBot
{
    public struct ServerStruct
    {
        public string skillTreePath;
        public Tree tree;
    }

    public class ServerList : Dictionary<ulong, ServerStruct>
    {
        public void Add(ulong ID)
        {
            ServerStruct server;
            server.skillTreePath = Path.Combine(Program.treeDir, $"{ID}.json");
            server.tree = new Tree();
            this.Add(ID, server);
        }
    }
}