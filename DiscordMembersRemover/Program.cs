using DiscordMembersRemover;

class Program
{
    public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

    private async Task MainAsync()
    {
        Console.WriteLine("Hello, i'm Discord members remover!");
        var bot = new DiscordBot();
        bot.TryConnect();

        await Task.Delay(Timeout.Infinite);
    }
}
