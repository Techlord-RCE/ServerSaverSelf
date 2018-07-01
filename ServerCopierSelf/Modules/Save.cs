namespace ServerCopierSelf.Modules
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Discord.WebSocket;

    using global::Discord;

    using global::Discord.Addons.Interactive;

    using global::Discord.Commands;

    using Microsoft.Extensions.DependencyInjection;

    using Newtonsoft.Json;

    using ServerCopierSelf.Handlers;
    using ServerCopierSelf.Models;

    /// <summary>
    /// Base is what we inherit our context from, ie ReplyAsync, Context.Guild etc.
    /// Example is our module name
    /// </summary>
    [RequireContext(ContextType.Guild)] // You can also use precondition attributes on a module to ensure commands are only run if they pass the precondition
    public class Save : ModuleBase<SocketCommandContext>
    {

        public async Task<List<GuildConfig.Ban>> GetBansAsync()
        {
            await LogHandler.LogMessage("Saving Bans");

            if ((Context.User as IGuildUser).GuildPermissions.BanMembers)
            {
                return (from ban in await Context.Guild.GetBansAsync()
                        select new GuildConfig.Ban
                                   {
                                       AuditLogReason = ban.Reason,
                                       UserId = ban.User.Id,
                                       Username = ban.User.Username
                                   }).ToList();
            }

            await LogHandler.LogMessage("Skipped Bans, Insufficient Permissions", LogSeverity.Warning);
            return new List<GuildConfig.Ban>();
        }

        public async Task<List<GuildConfig.Category>> GetCategoriesAsync()
        {
            await LogHandler.LogMessage("Saving Category Channels");
            return Context.Guild.CategoryChannels.Where(
                x =>
                    {
                        var channelPermissions = (Context.User as IGuildUser).GetPermissions(x);
                        if (!channelPermissions.ViewChannel)
                        {
                            LogHandler.LogMessage($"Skipped {x.Name}, Insufficient Permissions", LogSeverity.Warning);
                            return false;
                        }

                        return true;
                    }).Select(
                categoryChannel => new GuildConfig.Category
                {
                    Name = categoryChannel.Name,
                    Position = categoryChannel.Position,
                    OriginalId = categoryChannel.Id,
                    PermissionOverwrites =
                                               GetOverwrites(
                                                   categoryChannel
                                                       .PermissionOverwrites),
                    Channels = categoryChannel.Channels
                                               .Select(x => x.Id).ToList()
                }).ToList();
        }

        public async Task<List<GuildConfig.GuildEmote>> GetEmotesAsync()
        {
            await LogHandler.LogMessage("Saving Emotes");
            return Context.Guild.Emotes.Select(
                guildEmote => new GuildConfig.GuildEmote
                {
                    Animated = guildEmote.Animated,
                    IsManaged = guildEmote.IsManaged,
                    Name = guildEmote.Name,
                    OriginalId = guildEmote.Id,
                    RequireColons = guildEmote.RequireColons,
                    Url = guildEmote.Url
                }).ToList();
        }

        public List<GuildConfig.Overwrite> GetOverwrites(IReadOnlyCollection<Overwrite> overwrites)
        {
            var returnList = new List<GuildConfig.Overwrite>();
            foreach (var permissionOverwrite in overwrites)
            {
                returnList.Add(
                    new GuildConfig.Overwrite
                    {
                        TargetType = permissionOverwrite.TargetType,
                        TargetId = permissionOverwrite.TargetId,
                        Permissions =
                                new GuildConfig.Overwrite.
                                OverwritePermissions
                                {
                                    AllowValue =
                                            permissionOverwrite
                                                .Permissions
                                                .AllowValue,
                                    DenyValue =
                                            permissionOverwrite
                                                .Permissions.DenyValue
                                }
                    });
            }

            return returnList;
        }

        public async Task<List<GuildConfig.Role>> GetRolesAsync()
        {
            await LogHandler.LogMessage("Saving Roles");
            var roles = new List<GuildConfig.Role>();
            foreach (var socketRole in Context.Guild.Roles)
            {
                if (socketRole.IsManaged)
                {
                    await LogHandler.LogMessage($"Skipped Role {socketRole.Name} => Role is discord managed", LogSeverity.Warning);
                    continue;
                }

                var gRole = new GuildConfig.Role
                {
                    Color =
                                        new GuildConfig.Role.RoleColor
                                        {
                                            RawValue
                                                    = socketRole
                                                        .Color
                                                        .RawValue
                                        },
                    IsEveryone = socketRole.IsEveryone,
                    IsHoisted = socketRole.IsHoisted,
                    IsMentionable = socketRole.IsMentionable,
                    Name = socketRole.Name,
                    OriginalId = socketRole.Id,
                    Position = socketRole.Position,
                    GuildPermissions =
                                        new GuildConfig.GuildPermissions
                                        {
                                            RawValue
                                                    = socketRole
                                                        .Permissions
                                                        .RawValue
                                        },
                    Members = socketRole.Members.Select(x => x.Id).ToList()
                };
                roles.Add(gRole);
            }

            return roles;
        }

        public async Task<List<GuildConfig.TextChannel>> GetTextChannelsAsync()
        {
            await LogHandler.LogMessage("Saving Text Channels");

            var channels = new List<GuildConfig.TextChannel>();

            foreach (var textChannel in Context.Guild.TextChannels)
            {
                List<GuildConfig.TextChannel.Message> messageList = new List<GuildConfig.TextChannel.Message>();

                var channelPermissions = (Context.User as IGuildUser).GetPermissions(textChannel);
                if (!channelPermissions.ViewChannel)
                {
                    await LogHandler.LogMessage($"Skipped {textChannel.Name}, Insufficient Permissions", LogSeverity.Warning);
                    continue;
                }

                try
                {
                    var messages = textChannel.GetMessagesAsync();
                    var flattened = await messages.FlattenAsync().ConfigureAwait(false);
                    messageList = flattened.Select(
                        x =>
                            {
                                var msg = new GuildConfig.TextChannel.Message
                                {
                                    AttachmentUrls =
                                                      x.Attachments.Select(
                                                          a => a.Url).ToList(),
                                    AuthorName =
                                                      x.Author.ToString(),
                                    Content = x.Content,
                                    IsPinned = x.IsPinned,
                                    Timestamp = x.Timestamp
                                };
                                try
                                {
                                    msg.Embed = x.Embeds?.FirstOrDefault(e => e.Type == EmbedType.Rich)?.ToEmbedBuilder();
                                }
                                catch
                                {
                                    // Ignored
                                }

                                return msg;
                            }).ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Message Load Permissions Error\n" + e);
                }

                channels.Add(
                    new GuildConfig.TextChannel
                    {
                        Name = textChannel.Name,
                        OriginalId = textChannel.Id,
                        IsNsfw = textChannel.IsNsfw,
                        CategoryId = textChannel.CategoryId,
                        Position = textChannel.Position,
                        Topic = textChannel.Topic,
                        PermissionOverwrites =
                                GetOverwrites(textChannel.PermissionOverwrites),
                        Messages = messageList
                    });
            }

            return channels;
        }

        public async Task<List<GuildConfig.User>> GetUsersAsync()
        {
            await LogHandler.LogMessage("Saving Users");
            return Context.Guild.Users.Select(
                user => new GuildConfig.User
                {
                    Nickname = user.Nickname,
                    Roles = user.Roles.Select(x => x.Id).ToList(),
                    UserId = user.Id
                }).ToList();
        }

        public async Task<List<GuildConfig.VoiceChannel>> GetVoiceChannelsAsync()
        {
            await LogHandler.LogMessage("Saving Voice Channels");
            return Context.Guild.VoiceChannels.Where(
                x =>
                    {
                        var channelPermissions = (Context.User as IGuildUser).GetPermissions(x);
                        if (!channelPermissions.ViewChannel)
                        {
                            LogHandler.LogMessage($"Skipped {x.Name}, Insufficient Permissions", LogSeverity.Warning);
                            return false;
                        }

                        return true;
                    }).Select(
                voiceChannel => new GuildConfig.VoiceChannel
                {
                    BitRate = voiceChannel.Bitrate,
                    Position = voiceChannel.Position,
                    CategoryId = voiceChannel.CategoryId,
                    Name = voiceChannel.Name,
                    OriginalId = voiceChannel.Id,
                    UserLimit = voiceChannel.UserLimit,
                    PermissionOverwrites =
                                            GetOverwrites(
                                                voiceChannel.PermissionOverwrites)
                }).ToList();
        }

        [Command("Save", RunMode = RunMode.Async)]
        public async Task StatsAsync()
        {
            await LogHandler.LogMessage("Saving Guild Setup");
            var newObject = new GuildConfig
            {
                GuildId = Context.Guild.Id,
                Name = Context.Guild.Name,
                IconUrl = Context.Guild.IconUrl,
                SplashUrl = Context.Guild.SplashUrl,
                DefaultMessageNotifications =
                                        Context.Guild.DefaultMessageNotifications,
                VerificationLevel = Context.Guild.VerificationLevel,
                CategoryChannels =
                                        new List<GuildConfig.Category>(),
                TextChannels =
                                        new List<GuildConfig.TextChannel>(),
                VoiceChannels =
                                        new List<GuildConfig.VoiceChannel>(),
                Roles = new List<GuildConfig.Role>(),
                Users = new List<GuildConfig.User>(),
                GuildEmotes = new List<GuildConfig.GuildEmote>()
            };
            try
            {
                newObject.CategoryChannels = await GetCategoriesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                newObject.TextChannels = await GetTextChannelsAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            try
            {
                newObject.VoiceChannels = await GetVoiceChannelsAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            try
            {
                newObject.Roles = await GetRolesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            try
            {
                newObject.Bans = await GetBansAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            try
            {
                newObject.GuildEmotes = await GetEmotesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            try
            {
                newObject.Users = await GetUsersAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, $"setup/{DateTime.UtcNow.Ticks} {Context.Guild.Id}.json"), JsonConvert.SerializeObject(newObject, Formatting.Indented));
        }
    }
}