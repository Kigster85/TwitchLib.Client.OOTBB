using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NLog;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using TwitchBot.ChatCommands;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;
namespace TwitchBot.Server
{
  public class AuthServer
    {
    private readonly string _clientId;
    private readonly string _clientSecret;
    // Other fields

    public AuthServer(IConfiguration configuration)
    {
      _clientId = configuration["TWITCH_CLIENTID"];
      _clientSecret = configuration["TWITCH_CLIENTSECRET"];
      // Initialize other fields or services as needed
    }


    public static string RedirectUrl = "https://localhost:13377";
        public static List<string> Scopes = new List<string>
        {   "channel:bot","channel:moderate","chat:edit","chat:read","user:bot","user:read:chat","user:write:chat"};


    private static readonly Logger logger = Logging.MyLogging.GetLogger();
    private static CommandHandler _commandHandler;
    private static TwitchClient ChannelOwner;
    private static TwitchAPI UserTwitchAPI;
    private static string CachedClientToken;
    private static string TwitchChannelName;
    private static string TwitchChannelId;
    public string ClientId => _clientId;
    public string ClientSecret => _clientSecret;
    public event Action AuthenticationCompleted;

    public Task RunServer()
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();

            app.MapGet("/", Handler);

            return app.RunAsync("https://localhost:13377");
        }

    public async void InitializeWebServer()
    {
      Task.Run(() => RunServer());

      var authUrl = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={_clientId}&redirect_uri={RedirectUrl}&scope={string.Join("+", Scopes)}";
      Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
      await Handler(new CancellationToken(), null);
    }


    private async Task<string> Handler(CancellationToken cancellationToken, [FromQuery(Name = "code")] string? code)
        {
            if (code is not null)
            {
                var (accessToken, refreshToken) = await GetAccessAndRefreshTokens(code);
                CachedClientToken = accessToken;
                SetNameAndID(CachedClientToken).Wait();
        InitializeChannelOwner(TwitchChannelName, CachedClientToken, new ConnectionCredentials(TwitchChannelName, CachedClientToken), accessToken);
        InitializeTwitchAPI(CachedClientToken);

      }
      return $"Hello your code was {code}! Please remember to blame Pumpkin... I blame Pumpkin";
        }

        private void InitializeTwitchAPI(string accessToken)
        {
            UserTwitchAPI = new TwitchAPI();
            UserTwitchAPI.Settings.ClientId = ClientId;
            UserTwitchAPI.Settings.AccessToken = accessToken;
        }

        private async Task SetNameAndID(string accessToken)
        {
            //logger.Info("TwitchAPI started");

            var api = new TwitchAPI();
      Console.WriteLine($"ClientID: {ClientId}, AccessToken: {accessToken}");
            api.Settings.ClientId = ClientId;
            api.Settings.AccessToken = accessToken;

            var oauthedUser = await api.Helix.Users.GetUsersAsync();
            TwitchChannelId = oauthedUser.Users[0].Id;
            TwitchChannelName = oauthedUser.Users[0].Login;
        }

    private void InitializeChannelOwner(string twitchChannelName, string cachedClientToken, ConnectionCredentials credentials, string accessToken)
    {
      _commandHandler = new CommandHandler();
      Console.WriteLine($"Username: {twitchChannelName}, OAuth: {accessToken} and {twitchChannelName}");
      ConnectionCredentials channelCredentials = new ConnectionCredentials(twitchChannelName, accessToken);

      WebSocketClient customClient = new WebSocketClient();
      ChannelOwner = new TwitchClient(customClient);


        // Initialize the TwitchClient for each channel
        // Correctly passing the ConnectionCredentials instance to Initialize
        ChannelOwner.Initialize(channelCredentials, twitchChannelName);

        ChannelOwner.OnLog += Client_OnLog;
      ChannelOwner.OnJoinedChannel += Client_OnJoinedChannel;
      ChannelOwner.OnMessageReceived += Client_OnMessageReceived;
      ChannelOwner.OnWhisperReceived += Client_OnWhisperReceived;
      ChannelOwner.OnNewSubscriber += Client_OnNewSubscriber;
      ChannelOwner.OnConnected += Client_OnConnected;

      ChannelOwner.Connect();

      Console.WriteLine($"{username} Connected");
      Console.WriteLine($"Joined channel: {TwitchChannelName}");
    }



    private async Task<Tuple<string, string>> GetAccessAndRefreshTokens(string code)
    {
      HttpClient client = new HttpClient();
            var values = new Dictionary<string, string>
            {
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "code", code },
                { "grant_type", "authorization_code" },
                { "redirect_uri", RedirectUrl }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", content);

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseString);
            return new Tuple<string, string>(json["access_token"].ToString(), json["refresh_token"].ToString());
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
      Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
    }

    private void Client_OnConnected(object sender, OnConnectedArgs e)
    {
      Console.WriteLine($"Connected to {e.AutoJoinChannel}");
    }

    private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            ChannelOwner.SendMessage(TwitchChannelName, "I have arrived to take over your chat!");
      Console.WriteLine($"User { e.BotUsername} Has connected (bot access)!");
            Console.WriteLine("Joined channel {channel}", e.Channel);


        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            Console.WriteLine(e.ChatMessage.Username, e.ChatMessage.Message,e.ChatMessage.Channel);
      if (e.ChatMessage.Message.Contains("badword"))
        ChannelOwner.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromMinutes(30), "Bad word! 30 minute timeout!");
    }
        private void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {

            _commandHandler.HandleMessage(e.Command.ChatMessage.Username, e.Command.CommandText, ChannelOwner, e.Command.ChatMessage.Channel);
            
            string commandText = e.Command.CommandText.ToLower();

            if (commandText.Equals("test", StringComparison.OrdinalIgnoreCase))
            {
                ChannelOwner.SendMessage(TwitchChannelName, "is what a test?");
            }
            if (CommandsStaticResponses.ContainsKey(commandText))
            {
                ChannelOwner.SendMessage(TwitchChannelName, CommandsStaticResponses[commandText]);
            }
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            ChannelOwner.SendWhisper(e.WhisperMessage.Username, "I got your message.");
        }

        //public static void Client_OnChatCommandReceived(CommandHandler.CommandHandler.HandleMessage,e.ChatMessage.Username, e.ChatMessage.Message, client, e.ChatMessage.Channel)
        //{
        //    CommandHandler _commandhandler = new CommandHandler();

        //    if ()) ;

        //}
        //private static void ChatCommands.Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        //{
        //    _commandHandler.HandleMessage(e.ChatMessage.Username, e.ChatMessage.Message, client, e.ChatMessage.Channel);
        //}

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Console.WriteLine($"OnDisconnected Event");

            // if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
            //     client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
            // else
            //     client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
        }

        public static Dictionary<string, string> CommandsStaticResponses = new Dictionary<string, string>
        {
            {"coffee", "Please sir, i could really use some more coffee." },
            {"whoopsie", "I don't beleive he just did that again!!" },
            {"test", "this is a test for the sake of doing a test!!" }


        };

        public static string username { get; private set; }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
      if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
        ChannelOwner.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers!  So kind of you to use your Twitch Prime on this channel!");
      else
        ChannelOwner.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! ");
    }


    }
}
