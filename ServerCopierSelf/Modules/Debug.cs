using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCopierSelf.Modules
{
    using System.Linq;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    using ServerCopierSelf.Handlers;

    public class Debug : ModuleBase<SocketCommandContext>
    {
        [Command("GetVoicePermissions")]
        public async Task GetVoiceAsync(IGuildChannel channel, SocketGuildUser user)
        {
            var p = user.GetPermissions(channel);

            var permstr = $"{string.Join("\n", p.ToList().Select(x => x.ToString()))}";

            await LogHandler.LogMessage(permstr);
        }
    }
}
