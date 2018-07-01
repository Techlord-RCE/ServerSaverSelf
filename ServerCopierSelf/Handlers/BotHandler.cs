namespace ServerCopierSelf.Handlers
{
    using System.Threading.Tasks;

    using Discord;
    using Discord.WebSocket;

    using ServerCopierSelf.Models;

    /// <summary>
    /// The bot handler.
    /// </summary>
    public class BotHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BotHandler"/> class.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="events">
        /// The events.
        /// </param>
        /// <param name="config">
        /// The config.
        /// </param>
        public BotHandler(DiscordSocketClient client, EventHandler events, ConfigModel config)
        {
            Client = client;
            Event = events;
            Config = config;
        }

        /// <summary>
        /// Gets the config.
        /// </summary>
        private ConfigModel Config { get; }

        /// <summary>
        /// Gets the event.
        /// </summary>
        private EventHandler Event { get; }

        /// <summary>
        /// Gets the client.
        /// </summary>
        private DiscordSocketClient Client { get; }

        /// <summary>
        /// Initializes and logs the bot in.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task InitializeAsync()
        {
            // These are our events, each time one of these is triggered it runs the corresponding method. Ie, the bot receives a message we run Event.MessageReceivedAsync
            Client.Log += m => LogHandler.LogMessage(m.Message, m.Severity);
            Client.Connected += () => LogHandler.LogMessage("Connected");
            Client.Ready += () => LogHandler.LogMessage("Ready");
            Client.MessageReceived += Event.MessageReceivedAsync;

            // Here we log the bot in and start it. This MUST run for the bot to connect to discord.
            await Client.LoginAsync(TokenType.User, Config.Token);
            await LogHandler.LogMessage("Logged In");
            await Client.StartAsync();
            await LogHandler.LogMessage("Started");
        }
    }
}