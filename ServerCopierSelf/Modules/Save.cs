namespace ServerCopierSelf.Modules
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Commands;

    using Newtonsoft.Json;

    using ServerCopierSelf.Handlers;
    using ServerCopierSelf.Models;

    /// <summary>
    /// The save module
    /// </summary>
    [RequireContext(ContextType.Guild)] 
    public class Save : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Gets all bans in the server and converts them to a serializable list (or returns an empty list if insufficient permissions)
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
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

        /// <summary>
        /// Gets all categories in the server and converts them to a serializable list
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<List<GuildConfig.Category>> GetCategoriesAsync()
        {
            await LogHandler.LogMessage("Saving Category Channels");

            return Context.Guild.CategoryChannels/*.Where(
                x =>
                    {
                        var channelPermissions = (Context.User as IGuildUser).GetPermissions(x);

                        if (!channelPermissions.ViewChannel)
                        {
                            LogHandler.LogMessage($"Skipped {x.Name}, Insufficient Permissions", LogSeverity.Warning);
                            return false;
                        }

                        return true;
                    })*/.Select(
                categoryChannel => new GuildConfig.Category
                {
                    Name = categoryChannel.Name,
                    Position = categoryChannel.Position,
                    OriginalId = categoryChannel.Id,
                    PermissionOverwrites = GetOverwrites(categoryChannel.PermissionOverwrites),
                    Channels = categoryChannel.Channels.Select(x => x.Id).ToList()
                }).ToList();
        }

        /// <summary>
        /// Returns a list of all emotes in the server
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
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

        /// <summary>
        /// Converts a list of Discord Overwrites and converts them to a serializable overwrite list.
        /// </summary>
        /// <param name="overwrites">
        /// The overwrites.
        /// </param>
        /// <returns>
        /// The a serializable list of overwrites
        /// </returns>
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
                                    AllowValue = permissionOverwrite.Permissions.AllowValue,
                                    DenyValue = permissionOverwrite.Permissions.DenyValue
                                }
                    });
            }

            return returnList;
        }

        /// <summary>
        /// Converts all roles in the server to a serializable one
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<List<GuildConfig.Role>> GetRolesAsync()
        {
            await LogHandler.LogMessage("Saving Roles");
            var roles = new List<GuildConfig.Role>();
            foreach (var socketRole in Context.Guild.Roles)
            {

                // Ensures discord managed roles (ones used by bots) are skipped
                // This is because they are specific to the bot and will not be useable unless the bot get's re-invited to the new server
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
                        RawValue = socketRole.Color.RawValue
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
                        RawValue = socketRole.Permissions.RawValue
                    },
                    Members = socketRole.Members.Select(x => x.Id).ToList()
                };
                roles.Add(gRole);
            }

            return roles;
        }

        /// <summary>
        /// Converts all text channels into a serializable list for the bot to use.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task<List<GuildConfig.TextChannel>> GetTextChannelsAsync()
        {
            await LogHandler.LogMessage("Saving Text Channels");
            var channels = new List<GuildConfig.TextChannel>();
            foreach (var textChannel in Context.Guild.TextChannels)
            {
                List<GuildConfig.TextChannel.Message> messageList = new List<GuildConfig.TextChannel.Message>();

                var channelPermissions = (Context.User as IGuildUser).GetPermissions(textChannel);

                // If a channel is unable to be viewed skip it. 
                // The returned object is incomplete and cannot be serialized with useable information
                if (!channelPermissions.ViewChannel)
                {
                    await LogHandler.LogMessage($"Skipped {textChannel.Name}, Insufficient Permissions", LogSeverity.Warning);
                    continue;
                }
                

                try
                {
                    // Try to get messages from the channel and convert them into a serializable list.
                    // This will get the most recent 100 messages from the channel
                    var messages = textChannel.GetMessagesAsync();

                    // Flatten the list so it can be used
                    var flattened = await messages.FlattenAsync().ConfigureAwait(false);
                    messageList = flattened.Select(
                        x =>
                            {
                                var msg = new GuildConfig.TextChannel.Message
                                {
                                    AttachmentUrls = x.Attachments.Select(a => a.Url).ToList(),
                                    AuthorName = x.Author.ToString(),
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
                                    // Ignored, in-case the embed cannot be converted to an embed builder
                                    // This can happen if an embed contains an invalid url or image
                                    msg.Embed = null;
                                }

                                return msg;
                            }).ToList();
                }
                catch (Exception e)
                {
                    await LogHandler.LogMessage($"Message Load Permissions Error\n{e}", LogSeverity.Warning);
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
                        PermissionOverwrites = GetOverwrites(textChannel.PermissionOverwrites),
                        Messages = messageList
                    });
            }

            return channels;
        }

        /// <summary>
        /// Gets all users in the guild, saving their nickname (is applicable), roles and ID
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
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

        /// <summary>
        /// Converts all voice channels in the guild into a serializable format
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
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
                    PermissionOverwrites = GetOverwrites(voiceChannel.PermissionOverwrites)
                }).ToList();
        }

        /// <summary>
        /// TSerializes all available guild info into a useable format.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [Command("Save", RunMode = RunMode.Async)]
        public async Task SaveAsync()
        {
            await LogHandler.LogMessage("Saving Guild Setup");
            var newObject = new GuildConfig
            {
                GuildId = Context.Guild.Id,
                Name = Context.Guild.Name,
                IconUrl = Context.Guild.IconUrl,
                SplashUrl = Context.Guild.SplashUrl,
                DefaultMessageNotifications = Context.Guild.DefaultMessageNotifications,
                VerificationLevel = Context.Guild.VerificationLevel,

                // Initialize these all as empty lists in the case that one of the below methods fail.
                CategoryChannels = new List<GuildConfig.Category>(),
                TextChannels = new List<GuildConfig.TextChannel>(),
                VoiceChannels = new List<GuildConfig.VoiceChannel>(),
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
                await LogHandler.LogMessage($"Category load error\n{e}", LogSeverity.Error);
            }

            try
            {
                newObject.TextChannels = await GetTextChannelsAsync();
            }
            catch (Exception e)
            {
                await LogHandler.LogMessage($"Text Channel load error\n{e}", LogSeverity.Error);
            }

            try
            {
                newObject.VoiceChannels = await GetVoiceChannelsAsync();
            }
            catch (Exception e)
            {
                await LogHandler.LogMessage($"Voice Channel load error\n{e}", LogSeverity.Error);
            }

            try
            {
                newObject.Roles = await GetRolesAsync();
            }
            catch (Exception e)
            {
                await LogHandler.LogMessage($"Roles load error\n{e}", LogSeverity.Error);
            }

            try
            {
                newObject.Bans = await GetBansAsync();
            }
            catch (Exception e)
            {
                await LogHandler.LogMessage($"Bans load error\n{e}", LogSeverity.Error);
            }

            try
            {
                newObject.GuildEmotes = await GetEmotesAsync();
            }
            catch (Exception e)
            {
                await LogHandler.LogMessage($"Bans load error\n{e}", LogSeverity.Error);
            }

            try
            {
                newObject.Users = await GetUsersAsync();
            }
            catch (Exception e)
            {
                await LogHandler.LogMessage($"Users load error\n{e}", LogSeverity.Error);
            }

            File.WriteAllText(Path.Combine(AppContext.BaseDirectory, $"setup/{DateTime.UtcNow.Ticks} {Context.Guild.Id}.json"), JsonConvert.SerializeObject(newObject, Formatting.Indented));
            await LogHandler.LogMessage($"Saving {Context.Guild.Name} Complete");
        }
    }
}