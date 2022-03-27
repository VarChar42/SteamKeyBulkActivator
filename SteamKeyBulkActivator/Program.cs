using System.Text.RegularExpressions;
using System.Windows;
using SteamKit2;

namespace SteamKeyBulkActivator
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var cache = new KeyCache();

            var readClipboard = false;
            var cmd = true;
            while (cmd)
            {
                Console.WriteLine("Press ENTER to redeem codes from clipboard!");
                Console.WriteLine("Write \"done\" to start redeeming...");
                Console.WriteLine("      \"print\" to print all keys");
                Console.WriteLine("      \"remove <key>\" to remove a keys");
                var line = Console.ReadLine();
                var text = Clipboard.GetText();
                var lineArgs = line.Split(" ");
                
                
                
                switch (lineArgs[0])
                {
                    case "done":
                        cmd = false;
                        break;
                    case "print":
                        cache.Print();
                        break;
                    case "remove":
                        if (lineArgs.Length != 2)
                        {
                            break;
                        }

                        if (!cache.Remove(lineArgs[1]))
                        {
                            Console.WriteLine("Key not found!");
                        }
                        else
                        {
                            Console.WriteLine("Key removed!");
                            cache.Save();
                        }
                        
                        break;
                    
                    default:
                        readClipboard = true;
                        break;
                }

                if (!readClipboard)
                {
                    continue;
                }

                readClipboard = false;
                
                var codes = Regex.Matches(text, @"\w{5}-\w{5}-\w{5}").Select(match => match.Value);

                var newCodes = 0;
                var oldCodes = 0;
                foreach (var code in codes)
                {
                    if (cache.IsRedeemed(code, false))
                    {
                        oldCodes++;
                    }
                    else
                    {
                        Console.WriteLine(code);
                        newCodes++;
                    }
                }
                
                Console.WriteLine($"Found {newCodes} new codes and {oldCodes} old codes");
                cache.Save();
                
            }
            
            
            var client = new Client();

            Console.Write("Username: ");
            var username = Console.ReadLine();
            Console.Write("Pw: ");
            var pw = Console.ReadLine();

            Console.Write("Steam Guard: ");
            var steamGuard = Console.ReadLine();
            
            if (username.Length < 1 || pw.Length < 1)
            {
                Console.WriteLine("Invalid credentials!");
                return;
            }
            
            client.Login(username, pw, steamGuard);
            
            var resetEvent = new AutoResetEvent(false);
            client.LoggedInEvent += (_, _) => { resetEvent.Set(); };

            resetEvent.WaitOne();


            Console.WriteLine("Start redeeming...");
        
            foreach (var code in cache.Codes)
            {
                if (cache.IsRedeemed(code))
                {
                    continue;
                }

                Console.WriteLine($"Trying to redeem: {code}");
                
                var result = client.Redeem(code).Result;

                if (result != null)
                {
                    cache.SetResultDetails(code, result.Value);
                    cache.Save();
                }
                if (result == EResult.RateLimitExceeded)
                {
                    Console.WriteLine("RateLimited ... Aborting...");
                    break;
                }

                Console.WriteLine($"{code} : {result?.ToString() ?? "ERROR"}");
            }
            
            Console.WriteLine("DONE");

            client.Disconnect();
        }
        
        
    }
}