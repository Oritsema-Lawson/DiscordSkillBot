#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8601 // Possible null reference assignment.

using System.Security.Authentication;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace SkillBot
{
    class UtilityCommands : BaseCommandModule
    {
        DatabaseUtility _dbUtil = Program.databaseUtility;

        [Command("checkPerms")]
        public async Task getPerms(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"User {ctx.User.Username}, has permissions {(uint)ctx.Member.Permissions} {ctx.Member.Permissions.ToString()}");
        }

        [Command("ping")]
        [Description("Checks the bot's latency.")]
        public async Task PingCommand(CommandContext ctx)
        {
            // Time before sending the initial response
            var before = DateTimeOffset.Now;

            // Send a preliminary message to let the user know we're working on calculating the ping.
            var initialMessage = await ctx.RespondAsync("Bot is online! Calculating..."); 

            // Time after sending the initial response
            var after = DateTimeOffset.Now;

            // Calculate the difference for a basic latency measurement.
            var latency = (after - before).TotalMilliseconds;

            // Display results
            await initialMessage.ModifyAsync($"Bot is online! Latency: {latency:0}ms.");
        }

        /*[Command("addMeToDB")]
        public async Task AddUserToDB(CommandContext ctx)
        {
            await _dbUtil.AddUserIfNotExists(ctx.User.Id);
            await ctx.RespondAsync($"Added user {ctx.User.Username} to db.");
        }*/
    }
}