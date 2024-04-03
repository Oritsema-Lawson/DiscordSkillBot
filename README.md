# DiscordSkillBot
 Basic discord bot for managing skill-trees within a server, using DSharpPlus.
 Better bots definitely exist for this purpose, but this is more of a personal project.

## Setup
 In order for it to work, you need to place a config.json file inside the same folder as the build, with the following:
 ```
{
    "token": "BOT_TOKEN_HERE",
    "prefix": "BOT_PREFIX_HERE"
}
```

## Commands
Arguments with an * are optional

### Utility
Ping

> Check Latency

### Skill (Admin only)
addBranch [Branch Name\]

> Adds a new skill branch


addSkill [Branch Name\] [Skill Name\]

> Adds a new skill to an existing branch


removeSkill [Branch Name\] [Skill Name\]

> Removes a skill from an existing branch


removeBranch [Branch Name\]

> Removes a branch


addSP [Display Name or user @\] [Number\]

> Adds skill points to a user


setSP [User @\] [Number\]

> Sets a users SP


resetSP [User @\]

> Sets  a users SP to 0


resetSkillBranch [User @\] [Branch Name\]

> Resets a users progress on a specific skill branch


resetAllSkills [User @\]

> Resets a users progress on all skill branches


### Skill (All users)
checkSP [User @\]*

> Checks the number of skill points held by you or a specified user


unlockNext [Branch Name\]

> Unlocks the next skill on a branch if a user has the required amount of SP


listSkills

> Lists all skills and branches and marks those which have been unlocked


checkSkills [Branch Name\]

> Lists all unlocked skills on a specific branch 
