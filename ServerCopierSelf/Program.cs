namespace ServerCopierSelf
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    using Microsoft.Extensions.DependencyInjection;

    using Newtonsoft.Json;

    using ServerCopierSelf.Handlers;
    using ServerCopierSelf.Models;

    using EventHandler = ServerCopierSelf.Handlers.EventHandler;

    /// <summary>
    /// The program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Gets or sets The client.
        /// </summary>
        public static DiscordSocketClient Client { get; set; }

        /// <summary>
        /// Entry point of the program
        /// </summary>
        /// <param name="args">Discarded Args</param>
        public static void Main(string[] args)
        {
            try
            {
                StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Initialization of our service provider and bot
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private static async Task StartAsync()
        {
            // This ensures that our bots setup directory is initialized and will be were the database config is stored.
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));
            }

            var config = new ConfigModel();
            if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "setup/config.json")))
            {

                Console.WriteLine("Please input your user token");
                config.Token = Console.ReadLine();
                Console.WriteLine("Please input a bot prefix");
                config.Prefix = Console.ReadLine();
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "setup/config.json"), JsonConvert.SerializeObject(config, Formatting.Indented));
            }
            else
            {
                config = JsonConvert.DeserializeObject<ConfigModel>(
                    File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "setup/config.json")));
            }

            var services = new ServiceCollection()
                    .AddSingleton(new CommandService(new CommandServiceConfig
                    {
                        ThrowOnError = false,
                        IgnoreExtraArgs = false,
                        DefaultRunMode = RunMode.Sync
                    }))
                .AddSingleton(config)
                    .AddSingleton<BotHandler>()
                    .AddSingleton<EventHandler>()
                    .AddSingleton(new Random(Guid.NewGuid().GetHashCode()))
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Warning
                })).BuildServiceProvider();



            await services.GetRequiredService<BotHandler>().InitializeAsync();
            await services.GetRequiredService<EventHandler>().InitializeAsync();

            // Indefinitely delay the method from finishing so that the program stays running until stopped.
            await Task.Delay(-1);
        }
    }
}