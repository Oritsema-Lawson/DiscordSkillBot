#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8601 // Possible null reference assignment.

using System.ComponentModel.Design;
using System.Data.Entity.Infrastructure;
using System.Text;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace SkillBot
{
    class SkillCommands : BaseCommandModule
    {
        private Tree _mainTree = Program.mainTree;
        private JsonUtility _json = Program.jsonUtility;
        private DatabaseUtility _dbUtil = Program.databaseUtility;


        [Command("addBranch")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task AddBranch(CommandContext ctx, string _branchName, string _branchDesc = " ")
        {
            if (_mainTree.branches.Any(b => b.branchName.Equals(_branchName, StringComparison.OrdinalIgnoreCase)))
            {
                await ctx.RespondAsync($"A branch named {_branchName} already exists.");
                return; 
            }

            _mainTree.branches.Add( new Branch { branchName = _branchName, branchDescription = _branchDesc });
            
            await _json.WriteTree(_mainTree);
            await ctx.RespondAsync($"Added branch {_branchName} to main tree.");

            await _dbUtil.AddBranchColumn(_branchName);
            await ctx.Channel.SendMessageAsync($"Added branch {_branchName} to db.");

            await _json.WriteTree(_mainTree);
        }

        [Command("addSkill")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task AddSkill(CommandContext ctx, string _branchName, string _skillName, string _skillDescription, int _skillCost = 0)
        {
            int branchIndex = _mainTree.branches.FindIndex(b => b.branchName == _branchName);
           
            if(branchIndex > -1){

                if (_mainTree.branches[branchIndex].skills.Any(s => s.skillName.Equals(_skillName, StringComparison.OrdinalIgnoreCase)))
                {
                    await ctx.RespondAsync($"A skill named {_skillName} already exists in the {_branchName} branch.");
                    return;
                }

                _mainTree.branches[branchIndex].skills.Add(
                        new Skill{
                            skillName = _skillName,
                            skillDescription = _skillDescription,
                            skillCost = _skillCost
                        }
                );
                
                await _json.WriteTree(_mainTree);
                await ctx.RespondAsync($"Added skill {_mainTree.branches[branchIndex].skills.Count} to {_mainTree.branches[branchIndex].branchName} costing {_skillCost} points.");
                await _json.WriteTree(_mainTree);
            }
        }

        [Command("removeSkill")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task RemoveSkill(CommandContext ctx, string _branchName, string _skillName)
        {
            int branchIndex = _mainTree.branches.FindIndex(b => b.branchName == _branchName);
           
            if(branchIndex > -1){

                int skillIndex = _mainTree.branches[branchIndex].skills.FindIndex(s => s.skillName.Equals(_skillName, StringComparison.OrdinalIgnoreCase));

                if (skillIndex > -1)
                {
                    var message = await ctx.RespondAsync($"Skill {_skillName} exists in the {_branchName} branch, deleting...");

                    _mainTree.branches[branchIndex].skills.RemoveAt(skillIndex);

                    await message.ModifyAsync($"**{_skillName}** has been deleted.");

                    await _json.WriteTree(_mainTree);

                    return;
                }

                await ctx.RespondAsync($"Skill not found.");
            }
        }

        [Command("removeBranch")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task RemoveBranch(CommandContext ctx, string _branchName)
        {
            int branchIndex = _mainTree.branches.FindIndex(b => b.branchName == _branchName);
           
            if(branchIndex > -1){
                    var message = await ctx.RespondAsync($"**{_branchName}** found, deleting...");

                    _mainTree.branches.RemoveAt(branchIndex);

                    await _json.WriteTree(_mainTree);
                    
                    await _dbUtil.DeleteBranchColumn(_branchName);

                    await message.ModifyAsync($"**{_branchName}** has been deleted.");

                    return;
            }

            await ctx.RespondAsync($"Branch not found.");
        }

        [Command("addSP")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task AddSP(CommandContext ctx, DiscordMember user, int points)
        {
            var message = await ctx.RespondAsync("Adding SP...");
            int currentSP = await _dbUtil.GetSkillPoints((ulong)user.Id);
            
            currentSP += points;
            await _dbUtil.SetSkillPoints((ulong)user.Id, currentSP);

            await Task.Delay(1000);
            await message.ModifyAsync($"Added {points} SP to user {user.Username}, for a total of {currentSP}");
        }

        [Command("addSP")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task AddSP(CommandContext ctx, string username, int points)
        {
            var message = await ctx.RespondAsync("Searching for User...");
            
            var userSearchResult = ctx.Guild.Members.Values.Where(_user => _user.DisplayName.Contains(username, StringComparison.OrdinalIgnoreCase));
            List<DiscordMember> userResultList = userSearchResult.ToList<DiscordMember>();
            if(userResultList.Count == 1)
            {
                DiscordMember user = userResultList.First<DiscordMember>();

                await message.ModifyAsync("Adding SP...");
                int currentSP = await _dbUtil.GetSkillPoints((ulong)user.Id);
                
                currentSP += points;
                await _dbUtil.SetSkillPoints((ulong)user.Id, currentSP);

                await Task.Delay(1000);
                await message.ModifyAsync($"Added {points} SP to user {user.DisplayName}, for a total of {currentSP}");
            }
            else if (userResultList.Count == 0)
            {
                await message.ModifyAsync("No users found!");
            }
            else if (userResultList.Count > 1)
            {
                await message.ModifyAsync("Multiple users found!");
            }
            
        }

        [Command("resetSP")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task ResetSP(CommandContext ctx, DiscordMember user)
        {
            var message = await ctx.RespondAsync("Resetting SP...");

            await _dbUtil.SetSkillPoints(user.Id, 0);
            await message.ModifyAsync($"{user.DisplayName}'s SP has been reset to 0.");
        }

        [Command("setSP")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task SetSP(CommandContext ctx, DiscordMember user, int points)
        {
            var message = await ctx.RespondAsync("Setting SP...");

            await _dbUtil.SetSkillPoints(user.Id, points);
            await message.ModifyAsync($"{user.DisplayName}'s SP has been set to {points}.");
        }

        [Command("resetSkillBranch")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task ResetSkillBranch(CommandContext ctx, DiscordMember user, string branchName)
        {
            var message = await ctx.RespondAsync("Resetting branch...");

            await _dbUtil.SetBranchProgress(user.Id, branchName + "Progress", 0);
            await message.ModifyAsync($"{user.DisplayName}'s progress in the {branchName} branch has been reset.");
        }

        [Command("resetAllSkills")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task ResetAllSkills(CommandContext ctx, DiscordMember user)
        {
            var message = await ctx.RespondAsync("Resetting all skills...");

            foreach (var branch in _mainTree.branches)
            {
                await _dbUtil.SetBranchProgress(user.Id, branch.branchName + "Progress", 0);
            }

            await message.ModifyAsync($"{user.DisplayName}'s progress in all branches has been reset.");
        }

        [Command("checkSP")]
        public async Task CheckSP(CommandContext ctx)
        {
            int currentSP = await _dbUtil.GetSkillPoints(ctx.User.Id);
            await ctx.RespondAsync($"You currently have {currentSP} SP");
        }

        [Command("checkSP")]
        public async Task CheckSP(CommandContext ctx, DiscordMember user)
        {
            int currentSP = await _dbUtil.GetSkillPoints(user.Id);
            await ctx.RespondAsync($"{user.DisplayName} has {currentSP} SP");
        }

        [Command("unlockNext")]
        public async Task UnlockNext(CommandContext ctx, string _branchName)
        {
            int branchIndex = _mainTree.branches.FindIndex(b => b.branchName.Equals(_branchName, StringComparison.OrdinalIgnoreCase));

            if (branchIndex > -1)
            {
                int currentSP = await _dbUtil.GetSkillPoints(ctx.User.Id);
                int branchProgress = await _dbUtil.GetBranchProgress(ctx.User.Id, _branchName);

                try
                {
                    if(_mainTree.branches[branchIndex].skills[branchProgress].skillCost < currentSP)
                    {
                        currentSP -= _mainTree.branches[branchIndex].skills[branchProgress].skillCost;
                        branchProgress++;
                        await _dbUtil.SetBranchProgress(ctx.User.Id, _branchName, branchProgress);
                        await _dbUtil.SetSkillPoints(ctx.User.Id, currentSP);

                        await ctx.RespondAsync($"Skill {_mainTree.branches[branchIndex].skills[branchProgress - 1].skillName} unlocked!");

                        return;
                    }
                    else
                    {
                        await ctx.RespondAsync("Not enough SP!");
                        return;
                    }
                }
                catch
                {
                    await ctx.RespondAsync("No skills to unlock on this branch.");
                    return;
                }
                
            }

            await ctx.RespondAsync("Branch not found.");
        }

        [Command("listSkills")] 
        public async Task ListSkills(CommandContext ctx)
        {
            if (_mainTree.branches.Count == 0)
            {
                await ctx.RespondAsync("There are currently no branches in the skill tree.");
                return;
            }

            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Skill Tree Branches");

            foreach (var branch in _mainTree.branches) 
            {
                var skillStringBuilder = new StringBuilder();
                int branchProgress = await _dbUtil.GetBranchProgress(ctx.User.Id, branch.branchName);

                for (int i = 0; i < branch.skills.Count; i++)
                {
                    var skill = branch.skills[i];
                    skillStringBuilder.AppendLine($"  - {skill.skillName} : {skill.skillDescription} ({skill.skillCost} SP) {(i < branchProgress ? "**(Unlocked)**" : "")}"); 
                }

                if (skillStringBuilder.Length == 0)
                {
                    skillStringBuilder.AppendLine("*No skills yet*");
                }

                embedBuilder.AddField(branch.branchName, skillStringBuilder.ToString());
            }

            await ctx.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }

        [Command("checkSkills")]
        public async Task CheckSkills(CommandContext ctx, string _branchName)
        {
            int branchIndex = _mainTree.branches.FindIndex(b => b.branchName.Equals(_branchName, StringComparison.OrdinalIgnoreCase));

            if (branchIndex == -1)
            {
                await ctx.RespondAsync("Branch not found.");
                return;
            }

            int branchProgress = await _dbUtil.GetBranchProgress(ctx.User.Id, _branchName);

            var unlockedSkills = _mainTree.branches[branchIndex].skills.Take(branchProgress);

            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle($"Unlocked Skills in {_branchName}");

            if (unlockedSkills.Any())
            {
                foreach (var skill in unlockedSkills)
                {
                    embedBuilder.AddField(skill.skillName, skill.skillDescription);
                }
            }
            else
            {
                embedBuilder.WithDescription("You haven't unlocked any skills in this branch yet.");
            }

            await ctx.Channel.SendMessageAsync(embed: embedBuilder.Build());
        }
    }
}