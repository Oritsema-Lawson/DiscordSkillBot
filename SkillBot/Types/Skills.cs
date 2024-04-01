class Skill
{
    public string? skillName;
    public string? skillDescription;
    public int skillCost;
}

class Branch
{
    public string? branchName;
    public string? branchDescription;
    public List<Skill> skills = new List<Skill>();
}

class Tree
{   
    public string? treeName;
    public List<Branch> branches = new List<Branch>();
    public List<ulong> existingUsers = new List<ulong>();
}