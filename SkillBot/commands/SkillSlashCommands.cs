#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.


using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace SkillBot
{
    class SkillSlashCommands : ApplicationCommandModule
    {
        private ServerList _slist = Program.serverList;
        private JsonUtility _json = Program.jsonUtility;
        private DatabaseUtility _dbUtil = Program.databaseUtility;


        [SlashCommand("AddBranch", "Adds a branch to the skill tree.")]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        public async Task AddBranch(InteractionContext ctx, 
        [Option("BranchName", "Name of the branch to be added")] string _branchName, 
        [Option("BranchDescription", "Description of the branch to be added")] string _branchDesc = " ")
        {
            if (_slist[ctx.Guild.Id].tree.branches.Any(b => b.branchName.Equals(_branchName, StringComparison.OrdinalIgnoreCase)))
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"A branch named **{_branchName}** already exists."));
                return; 
            }

            _slist[ctx.Guild.Id].tree.branches.Add( new Branch { branchName = _branchName, branchDescription = _branchDesc });
            
            await _json.WriteTree(ctx.Guild.Id, _slist[ctx.Guild.Id].tree);
            await _dbUtil.AddBranchColumn(_branchName, ctx.Guild.Id);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Added branch **{_branchName}** to main tree."));
        }


        [SlashCommand("AddSkill", "Adds a skill to a specified branch")]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        public async Task AddSkill(InteractionContext ctx, 
        [Option("BranchName", "Name of the branch in which to add the skill")] string _branchName, 
        [Option("SkillName", "Name of the skill to add")] string _skillName, 
        [Option("SkillDesc", "Description of the skill to add")] string _skillDescription, 
        [Option("SkillCost", "Cost to unlock the skill")] long _skillCost)
        {
            int branchIndex = _slist[ctx.Guild.Id].tree.branches.FindIndex(b => b.branchName == _branchName);
           
            if(branchIndex > -1){

                if (_slist[ctx.Guild.Id].tree.branches[branchIndex].skills.Any(s => s.skillName.Equals(_skillName, StringComparison.OrdinalIgnoreCase)))
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"A skill named {_skillName} already exists in the {_branchName} branch."));
                    return;
                }

                _slist[ctx.Guild.Id].tree.branches[branchIndex].skills.Add(
                        new Skill{
                            skillName = _skillName,
                            skillDescription = _skillDescription,
                            skillCost = (int)_skillCost
                        }
                );
                
                await _json.WriteTree(ctx.Guild.Id, _slist[ctx.Guild.Id].tree);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Added skill **{_slist[ctx.Guild.Id].tree.branches[branchIndex].skills.Count}** to **{_slist[ctx.Guild.Id].tree.branches[branchIndex].branchName}** costing **{_skillCost}** points."));
            }
        }
        
        [SlashCommand("RemoveSkill", "Removes a skill from a specified branch")]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        public async Task RemoveSkill(InteractionContext ctx, 
        [Option("BranchName","The branch containing the skill to be removed")] string _branchName, 
        [Option("SkillName","The skill to be removed")] string _skillName)
        {
            int branchIndex = _slist[ctx.Guild.Id].tree.branches.FindIndex(b => b.branchName == _branchName);
           
            if(branchIndex > -1){

                int skillIndex = _slist[ctx.Guild.Id].tree.branches[branchIndex].skills.FindIndex(s => s.skillName.Equals(_skillName, StringComparison.OrdinalIgnoreCase));

                if (skillIndex > -1)
                {
                    _slist[ctx.Guild.Id].tree.branches[branchIndex].skills.RemoveAt(skillIndex);

                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"**{_skillName}** has been deleted."));

                    await _json.WriteTree(ctx.Guild.Id, _slist[ctx.Guild.Id].tree);

                    return;
                }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Skill not found."));
            }
        }

        [SlashCommand("RemoveBranch", "Removes a branch from the skill tree")]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        public async Task RemoveBranch(InteractionContext ctx, 
        [Option("BranchName", "The name of the branch to be removed")] string _branchName)
        {
            int branchIndex = _slist[ctx.Guild.Id].tree.branches.FindIndex(b => b.branchName == _branchName);
           
            if(branchIndex > -1){
                    _slist[ctx.Guild.Id].tree.branches.RemoveAt(branchIndex);
                    await _json.WriteTree(ctx.Guild.Id, _slist[ctx.Guild.Id].tree);
                    await _dbUtil.DeleteBranchColumn(_branchName, ctx.Guild.Id);
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Branch **{_branchName}** deleted!"));

                    return;
            }
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Branch not found."));
        }

        [SlashCommand("AddSP", "Give a user a specified number of skill points.")]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        public async Task AddSP(InteractionContext ctx, 
        [Option("User", "User")] DiscordUser user, 
        [Option("Points", "Number of points to add")] long points)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Adding SP..."));
            int currentSP = await _dbUtil.GetSkillPoints((ulong)user.Id, ctx.Guild.Id);
            
            currentSP += (int)points;
            await _dbUtil.SetSkillPoints((ulong)user.Id, currentSP, ctx.Guild.Id);

            await Task.Delay(1000);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Added {points} SP to user {user.Username}, for a total of {currentSP}"));
        }

        [SlashCommand("ResetSP", "Resets a user's SP to 0")]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        public async Task ResetSP(InteractionContext ctx, 
        [Option("User", "User")] DiscordUser user)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Resetting SP..."));

            await _dbUtil.SetSkillPoints(user.Id, 0, ctx.Guild.Id);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"**{user.Mention}**'s SP has been reset to 0."));
        }

        [SlashCommand("SetSP", "Sets a user's SP to a specified value.")]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        public async Task SetSP(InteractionContext ctx, 
        [Option("User","User")] DiscordUser user, 
        [Option("Points", "Points")] long points)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Setting SP..."));

            await _dbUtil.SetSkillPoints(user.Id, (int)points, ctx.Guild.Id);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"**{user.Mention}**'s SP has been set to **{points}** points."));
        }

        [SlashCommand("ResetSkillBranchProgress", "Resets a users progress on a specified skill branch.")]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        public async Task ResetSkillBranchProgress(InteractionContext ctx, 
        [Option("User", "User")] DiscordUser user, 
        [Option("BranchName", "Name of the branch to be reset")] string branchName)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Resetting..."));

            try
            {
                await _dbUtil.SetBranchProgress(user.Id, branchName + "Progress", 0, ctx.Guild.Id);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"**{user.Mention}**'s progress in the {branchName} branch has been reset."));
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Branch or user not found."));
            }
        }

        [SlashCommand("ResetAllSkillProgress", "Resets a user's progress on the skill tree.")]
        [SlashRequireUserPermissions(Permissions.ManageChannels)]
        public async Task ResetAllSkillProgress(InteractionContext ctx, 
        [Option("User", "User")] DiscordUser user)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Resetting all skills..."));

            foreach (var branch in _slist[ctx.Guild.Id].tree.branches)
            {
                await _dbUtil.SetBranchProgress(user.Id, branch.branchName + "Progress", 0, ctx.Guild.Id);
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"**{user.Mention}**'s progress in all branches has been reset."));
        }

        [SlashCommand("checkSP", "Checks your SP balance or another user's")]
        public async Task CheckSP(InteractionContext ctx, 
        [Option("User", "The user to check")] DiscordUser? user = null)
        {
            var targetUser = user ?? ctx.User;

            int currentSP = await _dbUtil.GetSkillPoints(targetUser.Id, ctx.Guild.Id);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"**{targetUser.Mention}** currently has **{currentSP}** SP"));
        }

        [SlashCommand("UnlockNext", "Unlock the next skill on a branch.")]
        public async Task UnlockNext(InteractionContext ctx, 
        [Option("BranchName","Branch Name")]string _branchName)
        {
            int branchIndex = _slist[ctx.Guild.Id].tree.branches.FindIndex(b => b.branchName.Equals(_branchName, StringComparison.OrdinalIgnoreCase));

            if (branchIndex > -1)
            {
                int currentSP = await _dbUtil.GetSkillPoints(ctx.User.Id, ctx.Guild.Id);
                int branchProgress = await _dbUtil.GetBranchProgress(ctx.User.Id, _branchName, ctx.Guild.Id);

                try
                {
                    if(_slist[ctx.Guild.Id].tree.branches[branchIndex].skills[branchProgress].skillCost < currentSP)
                    {
                        currentSP -= _slist[ctx.Guild.Id].tree.branches[branchIndex].skills[branchProgress].skillCost;
                        branchProgress++;
                        await _dbUtil.SetBranchProgress(ctx.User.Id, _branchName, branchProgress, ctx.Guild.Id);
                        await _dbUtil.SetSkillPoints(ctx.User.Id, currentSP, ctx.Guild.Id);

                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Skill **{_slist[ctx.Guild.Id].tree.branches[branchIndex].skills[branchProgress - 1].skillName}** unlocked!"));

                        return;
                    }
                    else
                    {
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Not enough SP!"));
                        return;
                    }
                }
                catch
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("No skills to unlock on this branch."));
                    return;
                }
                
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Branch not found."));
        }

        [SlashCommand("listSkills", "Lists all available skills.")] 
        public async Task ListSkills(InteractionContext ctx)
        {
                if (_slist[ctx.Guild.Id].tree.branches.Count == 0)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("There are currently no branches in the skill tree."));
                return;
            }

            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle("Skill Tree Branches");

            foreach (var branch in _slist[ctx.Guild.Id].tree.branches) 
            {
                var skillStringBuilder = new StringBuilder();
                int branchProgress = await _dbUtil.GetBranchProgress(ctx.User.Id, branch.branchName, ctx.Guild.Id);

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

            var responseBuilder = new DiscordInteractionResponseBuilder().AddEmbed(embedBuilder.Build());
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, responseBuilder);
        }

        [SlashCommand("checkSkills", "Check unlocked skills on a specific branch.")]
        public async Task CheckSkills(InteractionContext ctx, 
        [Option("BranchName", "Name of the branch to check.")] string _branchName)
        {
            int branchIndex = _slist[ctx.Guild.Id].tree.branches.FindIndex(b => b.branchName.Equals(_branchName, StringComparison.OrdinalIgnoreCase));

            if (branchIndex == -1)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, 
                    new DiscordInteractionResponseBuilder().WithContent("Branch not found."));
                return;
            }

            int branchProgress = await _dbUtil.GetBranchProgress(ctx.User.Id, _branchName, ctx.Guild.Id);

            var unlockedSkills = _slist[ctx.Guild.Id].tree.branches[branchIndex].skills.Take(branchProgress);

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

            var responseBuilder = new DiscordInteractionResponseBuilder().AddEmbed(embedBuilder.Build());
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, responseBuilder);
        } 
    }
}