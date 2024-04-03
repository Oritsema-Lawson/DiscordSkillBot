public class Skill
{
    public string? skillName;
    public string? skillDescription;
    public int skillCost;
}

public class Branch
{
    public string? branchName;
    public string? branchDescription;
    public List<Skill> skills = new List<Skill>();
}

public class Tree
{   
    public List<Branch> branches = new List<Branch>();
    public List<ulong> existingUsers = new List<ulong>();
}