using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Tamabot
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();
        
        private static DiscordSocketClient _client;
        private CommandHandler commandHandler;
        private CommandService _commands;
        private InteractiveService _interactive;
        private IServiceProvider services;

        public async Task MainAsync(){
            _client = new DiscordSocketClient();

            _client.Log += Log;

            // // this is for the interactive methods
            // TimeSpan timeout = new TimeSpan(0, 0, 15);
            // _interactive = new InteractiveService(_client, timeout);

            // _client.MessageReceived += MessageReceived; // this kinda isn't used so yeah
            // _commands = new CommandService();
            // commandHandler = new CommandHandler(_client, _commands);
            
            // await commandHandler.InstallCommandsAsync();

            services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();

            var token = File.ReadAllText("token.ignore");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            InfoModule.readDictionary(); // loads in the dictionary when the program starts
            InfoModule.constructEmbed(); // Initializes the common embeds

            // sets the custom status 
            await _client.SetGameAsync("!help | Keep giving suggestions!");

            _commands = new CommandService();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            _client.MessageReceived += HandleCommandAsync;

            // Block this task until the program is closed
            await Task.Delay(-1);
        }

        public async Task HandleCommandAsync(SocketMessage message){
            // // this is a first command yee
            // if(message.Content == "!ping"){
            //     await message.Channel.SendMessageAsync("Pong!");
            // }
            
            if (!(message is SocketUserMessage msg)) return;
            if (msg.Author.IsBot) return;

            int argPos = 0;
            if (!(msg.HasStringPrefix("!", ref argPos))) return;

            var context = new SocketCommandContext(_client, msg);
            await _commands.ExecuteAsync(context, argPos, services);
        }

        private Task Log(LogMessage message){
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        public CommandService GetCommandService(){
            return _commands;
        }

        public async static Task updateStatus(string status){
            await _client.SetGameAsync("!help | " + status);
        }
        
    }
}