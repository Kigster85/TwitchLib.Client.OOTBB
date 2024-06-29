using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TwitchBot.Server; // Ensure this using directive is correct based on your project's namespace structure

namespace TwitchBot
{
  class Program
  {
    public void ConfigureServices(IServiceCollection services)
    {
      // Other service registrations
      services.AddSingleton<AuthServer>();

      var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

      var twitchBot = new AuthServer(configuration);


      // Or services.AddTransient<TwitchBot>(); depending on your use case
    }

    static async Task Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);
      var configuration = builder.Configuration;

      try
      {
        // Your existing setup and main functionality here
        Console.WriteLine("TwitchBot Console Application is starting...");

        // Initialize the AuthServer
        Console.WriteLine("Initializing the Authentication Server...");
        var authServer = new AuthServer(configuration);
        authServer.InitializeWebServer();
        Console.WriteLine("Authentication Server is running.");

        // Wait for authentication to complete

        // Now proceed with the rest of your application logic


        Console.WriteLine("Authentication completed. Proceeding with application logic...");

        Console.WriteLine("Application is running. Press Ctrl+C to exit.");

        // Example of waiting indefinitely until the application is stopped
        await Task.Delay(Timeout.Infinite);

      }
      catch (Exception ex)
      {
        // Log the exception and display a failure message
        Console.WriteLine($"An error occurred: {ex.Message}");
      }
      finally
      {
        // This block will execute when the program is stopped for any reason
        Console.WriteLine("The program has stopped running. Press any key to exit...");
        Console.ReadKey();
      }
    }


  }
}
