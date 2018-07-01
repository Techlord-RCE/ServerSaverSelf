namespace ServerCopierSelf.Models
{
    using System;
    using System.Collections.Generic;

    using Discord;

    public class GuildConfig
        {
            public List<Ban> Bans { get; set; } = new List<Ban>();

            public List<Category> CategoryChannels { get; set; } = new List<Category>();

            public DefaultMessageNotifications DefaultMessageNotifications { get; set; }

            public List<GuildEmote> GuildEmotes { get; set; } = new List<GuildEmote>();

            public ulong GuildId { get; set; }

            public string IconUrl { get; set; }

            public string Name { get; set; }

            public List<Role> Roles { get; set; } = new List<Role>();

            public string SplashUrl { get; set; }

            public List<TextChannel> TextChannels { get; set; } = new List<TextChannel>();

            public List<User> Users { get; set; } = new List<User>();

            public VerificationLevel VerificationLevel { get; set; }

            public List<VoiceChannel> VoiceChannels { get; set; } = new List<VoiceChannel>();

            public class Ban
            {
                public string AuditLogReason { get; set; }

                public ulong UserId { get; set; }

                public string Username { get; set; }
            }

            public class Category
            {
                public List<ulong> Channels = new List<ulong>();

                public string Name { get; set; }

                public ulong OriginalId { get; set; }

                public List<Overwrite> PermissionOverwrites { get; set; } = new List<Overwrite>();

                public int Position { get; set; }
            }

            public class Channel
            {
                public ulong? CategoryId { get; set; }

                public string Name { get; set; }

                public ulong OriginalId { get; set; }

                public List<Overwrite> PermissionOverwrites { get; set; } = new List<Overwrite>();

                public int Position { get; set; }
            }

            public class GuildEmote
            {
                public bool Animated { get; set; }

                public bool IsManaged { get; set; }

                public string Name { get; set; }

                public ulong OriginalId { get; set; }

                public bool RequireColons { get; set; }

                public string Url { get; set; }
            }

            public class GuildPermissions
            {
                public ulong RawValue { get; set; }
            }

            public class Overwrite
            {
                public OverwritePermissions Permissions { get; set; }

                public ulong TargetId { get; set; }

                public PermissionTarget TargetType { get; set; }

                public class OverwritePermissions
                {
                    public ulong AllowValue { get; set; }

                    public ulong DenyValue { get; set; }
                }
            }

            public class Role
            {
                public RoleColor Color { get; set; }

                public GuildPermissions GuildPermissions { get; set; }

                public bool IsEveryone { get; set; }

                public bool IsHoisted { get; set; }

                public bool IsMentionable { get; set; }

                public List<ulong> Members { get; set; } = new List<ulong>();

                public string Name { get; set; }

                public ulong OriginalId { get; set; }

                public int Position { get; set; }

                public class RoleColor
                {
                    public uint RawValue { get; set; }
                }
            }

            public class TextChannel
            {
                public ulong? CategoryId { get; set; }

                public bool IsNsfw { get; set; }

                public List<Message> Messages { get; set; } = new List<Message>(100);

                public string Name { get; set; }

                public ulong OriginalId { get; set; }

                public List<Overwrite> PermissionOverwrites { get; set; } = new List<Overwrite>();

                public int Position { get; set; }

                public string Topic { get; set; }

                public class Message
                {
                    public List<string> AttachmentUrls { get; set; } = new List<string>();

                    public string AuthorName { get; set; }

                    public string Content { get; set; }

                    public EmbedBuilder Embed { get; set; }

                    public bool IsPinned { get; set; }

                    public DateTimeOffset Timestamp { get; set; }
                }
            }

            public class User
            {
                public string Nickname { get; set; }

                public List<ulong> Roles { get; set; } = new List<ulong>();

                public ulong UserId { get; set; }
            }

            public class VoiceChannel
            {
                public int BitRate { get; set; }

                public ulong? CategoryId { get; set; }

                public string Name { get; set; }

                public ulong OriginalId { get; set; }

                public List<Overwrite> PermissionOverwrites { get; set; } = new List<Overwrite>();

                public int Position { get; set; }

                public int? UserLimit { get; set; }
            }
        }
}
