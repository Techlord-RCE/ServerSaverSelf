namespace ServerCopierSelf.Handlers
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    using ServerCopierSelf.Models;

    /// <summary>
    /// The event handler.
    /// </summary>
    public class EventHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandler"/> class.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="config">
        /// The config.
        /// </param>
        /// <param name="service">
        /// The service.
        /// </param>
        /// <param name="commandService">
        /// The command service.
        /// </param>
        public EventHandler(DiscordSocketClient client, ConfigModel config, IServiceProvider service, CommandService commandService)
        {
            Client = client;
            Config = config;
            Provider = service;
            CommandService = commandService;
            CancellationToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets the config.
        /// </summary>
        private ConfigModel Config { get; }

        /// <summary>
        /// Gets the provider.
        /// </summary>
        private IServiceProvider Provider { get; }

        /// <summary>
        /// Gets the client.
        /// </summary>
        private DiscordSocketClient Client { get; }

        /// <summary>
        /// Gets the command service.
        /// </summary>
        private CommandService CommandService { get; }

        /// <summary>
        /// Gets or sets the cancellation token.
        /// </summary>
        private CancellationTokenSource CancellationToken { get; set; }

        /// <summary>
        /// The initialize async.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task InitializeAsync()
        {
            // This will add all our modules to the command service, allowing them to be accessed as necessary
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        /// <summary>
        /// This logs discord messages to our LogHandler
        /// </summary>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal Task Log(LogMessage message)
        {
            return Task.Run(() => LogHandler.LogMessage(message.Message, message.Severity));
        }

        /// <summary>
        /// This event is triggered every time the a user sends a message in a channel, dm etc. that the bot has access to view.
        /// </summary>
        /// <param name="socketMessage">
        /// The socket message.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task MessageReceivedAsync(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage Message) || Message.Channel is IDMChannel || Message.Author.Id != Client.CurrentUser.Id)
            {
                return;
            }

            var context = new SocketCommandContext(Client, Message);

            var argPos = 0;

            // Filter out all messages that don't start with our Bot Prefix, bot mention or server specific prefix.
            if (!Message.HasStringPrefix(Config.Prefix, ref argPos))
            {
                return;
            }

            // Here we attempt to execute a command based on the user message
            var result = await CommandService.ExecuteAsync(context, argPos, Provider, MultiMatchHandling.Best);

            // Generate an error message for users if a command is unsuccessful
            if (!result.IsSuccess)
            {
                LogHandler.LogMessage(context, result.ErrorReason, LogSeverity.Error);
            }
            else
            {
                LogHandler.LogMessage(context);
            }
        }
    }
}