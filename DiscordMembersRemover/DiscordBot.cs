using Discord;
using Discord.WebSocket;

namespace DiscordMembersRemover;

public class DiscordBot
{
    private DiscordSocketClient? _client;
    private SocketGuild? _mainGuild;
    private SocketRole? _roleToRemove;
    private List<SocketGuildUser>? _membersToRemove;
    private string? _byeMessage;

    public DiscordBot()
    {
        CreateInstance();
    }

    private void CreateInstance()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents =
                GatewayIntents.Guilds |
                GatewayIntents.GuildMembers,
            AlwaysDownloadUsers = true
        });
        _client.Ready += Ready;
        _client.Disconnected += Disconnected;
    }

    private Task Disconnected(Exception arg)
    {
        Console.WriteLine("Bot has been disconnected, try to pass correct token!");

        // TODO: null checks
        _client.Dispose();

        CreateInstance();
        TryConnect();

        return Task.CompletedTask;
    }

    private Task Ready()
    {
        Console.WriteLine("Bot successfully connected!");
        AskForGuildId();
        return Task.CompletedTask;
    }

    public async void TryConnect()
    {
        while (true)
        {
            Console.Write("Send me your bot token: ");
            var token = Console.ReadLine();
            if (token == null || token.Length < 10)
            {
                Console.WriteLine("You passed wrong token, try it again!");
                TryConnect();
                continue;
            }

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            break;
        }
    }

    private void AskForGuildId()
    {
        while (true)
        {
            Console.Write("Send me your guild id: ");
            var guildId = Console.ReadLine();

            if (!ulong.TryParse(guildId, out var parsed))
            {
                Console.WriteLine("You passed wrong guild id, try again!");
                continue;
            }

            _mainGuild = _client.GetGuild(parsed);
            if (_mainGuild == null)
            {
                Console.WriteLine("You passed wrong guild id, try again!");
                continue;
            }

            Console.WriteLine(
                $"Successfully found your guild {_mainGuild.Name} ({_mainGuild.Id}) [{_mainGuild.MemberCount} members]");
            AskForRoleId();
            break;
        }
    }

    private void AskForRoleId()
    {
        while (true)
        {
            Console.Write("Send me your role id: ");
            var roleId = Console.ReadLine();

            if (!ulong.TryParse(roleId, out var parsed))
            {
                Console.WriteLine("You passed wrong role id, try again!");
                continue;
            }

            _roleToRemove = _mainGuild.GetRole(parsed);
            if (_roleToRemove == null)
            {
                Console.WriteLine("You passed wrong role id, try again!");
                continue;
            }

            Console.WriteLine(
                $"Successfully found role {_roleToRemove.Name} ({_roleToRemove.Id}) [{_roleToRemove.Members.ToList().Count} members]");
            AskForDays();
            break;
        }
    }

    private void AskForDays()
    {
        while (true)
        {
            Console.Write("For how much days you wanna save users from prune: ");
            var days = Console.ReadLine();

            if (!int.TryParse(days, out var parsed))
            {
                Console.WriteLine("You passed wrong number, try again!");
                continue;
            }

            _membersToRemove = _roleToRemove.Members.ToList()
                .FindAll(user => user.JoinedAt < DateTime.Now.Subtract(TimeSpan.FromDays(parsed)));

            Console.WriteLine(
                $"Found {_membersToRemove.Count} members joined more than {parsed} days ago with \"{_roleToRemove.Name}\" role.");
            AskForMessage();
            break;
        }
    }
    
    private void AskForMessage()
    {
        while (true)
        {
            Console.Write("Do you wanna send them last message? (y/n): ");
            var answer = Console.ReadLine();

            if (answer is {Length: > 0})
            {
                switch (answer)
                {
                    case "y":
                    case "yes":
                        while (true)
                        {
                            Console.Write("Send your last message: ");
                            var message = Console.ReadLine();
                            if (message is {Length: < 1})
                            {
                                Console.WriteLine("You sent incorrect message, try again!");
                                continue;
                            }
                            _byeMessage = message;
                            break;
                        }
                        goto loopEnd;
                    case "n":
                    case "no":
                        goto loopEnd;
                    default:
                        Console.WriteLine("You passed wrong answer, try again!");
                        break;
                }
            }
            else
            {
                Console.Write("You passed wrong answer, try again!");
            }
        }

        loopEnd:
        AskForRemove();
    }

    private void AskForRemove()
    {
        while (true)
        {
            Console.Write($"Do you wanna remove {_membersToRemove.Count} members(y/n): ");
            var answer = Console.ReadLine();

            if (answer is {Length: > 0})
            {
                switch (answer)
                {
                    case "y":
                    case "yes":
                        goto loopEnd;
                    case "n":
                    case "no":
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("You passed wrong answer, try again!");
                        break;
                }
            }
            else
            {
                Console.Write("You passed wrong answer, try again!");
            }
        }

        loopEnd:
        StartRemoving();
    }

    private async void StartRemoving()
    {
        if (_membersToRemove is {Count: > 0})
        {
            foreach (var member in _membersToRemove)
            {
                if (_byeMessage != null)
                {
                    try
                    {
                        await member.SendMessageAsync(_byeMessage);
                    }
                    catch (Exception _)
                    {
                        Console.WriteLine($"Cannot send message to {member.Username}#{member.Discriminator} :(");
                    }
                    
                }
                await member.KickAsync("DiscordMembersRemover: automated removed inactive member!");
                Console.WriteLine($"Member {member.Username}#{member.Discriminator} has been kicked :(");
            }
        }
        Console.WriteLine("All listed members was removed!");
        Console.WriteLine("Press any button to turn off the application!");
        Console.ReadLine();
        Environment.Exit(0);
    }
}
