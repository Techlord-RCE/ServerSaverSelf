namespace ServerCopierSelf.Modules
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using Discord.Commands;

    using Newtonsoft.Json;

    using ServerCopierSelf.Models;

    public class Messaging : ModuleBase<SocketCommandContext>
    {
        private bool isMessaging = false;
        
        [Command("MessageUsers", RunMode = RunMode.Async)]
        public async Task MessageAsync([Remainder] string message)
        {
            if (isMessaging)
            {
                Console.WriteLine("Please wait until the current message task is complete before running it again");
                return;
            }

            if (Context.Message.Attachments.Count != 1)
            {
                Console.WriteLine("Please attach a config file, saved from the SelfBot, this should be in the form `.json`");
                return;
            }

            using (var webClient = new WebClient())
            {
                var data = webClient.DownloadData(Context.Message.Attachments.First().Url);

                using (Stream mem = new MemoryStream(data))
                {
                    mem.Seek(0, SeekOrigin.Begin);
                    StreamReader reader = new StreamReader(mem);
                    string text = await reader.ReadToEndAsync().ConfigureAwait(false);

                    GuildConfig config;
                    try
                    {
                        config = JsonConvert.DeserializeObject<GuildConfig>(text);
                    }
                    catch
                    {
                        Console.WriteLine("Invalid File");
                        return;
                    }


                    await MessageTaskAsync(config, message);
                }
            }
        }

        public async Task MessageTaskAsync(GuildConfig config, string message)
        {
            await Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync(message);
            isMessaging = true;
            Console.WriteLine($"Running message task, this will take up to {config.Users.Count * 3} seconds");
            var usersMessaged = 0;
            foreach (var user in config.Users)
            {
                var dUser = Context.Client.GetUser(user.UserId);
                if (dUser == null)
                {
                    Console.Write($"User with ID {user.UserId} not found, continuing");
                    continue;
                }

                if (dUser.IsBot || dUser.IsWebhook || dUser.Id == Context.User.Id)
                {
                    continue;
                }

                try
                {
                    var dm = await dUser.GetOrCreateDMChannelAsync();
                    await dm.SendMessageAsync(message);
                    Console.WriteLine($"{dUser} was sent a message");
                    usersMessaged++;
                    await Task.Delay(3000);
                }
                catch
                {
                    Console.WriteLine($"Unable to send {dUser} a message, continuing");
                }
            }

            Console.WriteLine($"Success, message task completed. {usersMessaged} users messaged.");
            isMessaging = false;
        }
    }
}
