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
            Console.Write("Username: ");
            var username = Console.ReadLine();
            Console.Write("Pw: ");
            var pw = Console.ReadLine();

            Console.Write("Steam Guard: ");
            var steamGuard = Console.ReadLine();

            if (username.Length < 1 || pw.Length < 1 || steamGuard.Length < 1)
            {
                Console.WriteLine("Invalid credentials!");
                return;
            }

            Client client = new Client();

            client.Login(username, pw, steamGuard);

            var resetEvent = new AutoResetEvent(false);
            client.LoggedInEvent += (_, _) => { resetEvent.Set(); };

            resetEvent.WaitOne();

            while (true)
            {
                Console.WriteLine("Press ENTER to redeem codes from clipboard!");
                Console.ReadLine();
                var text = Clipboard.GetText();
                var codes = Regex.Matches(text, @"\w{5}-\w{5}-\w{5}").Select(match => match.Value).ToArray();
                Console.WriteLine($"Trying to redeem {codes.Length} Codes");

                foreach (var code in codes)
                {
                    var result = client.Redeem(code).Result;
                    if (result == EPurchaseResultDetail.RateLimited)
                    {
                        Console.WriteLine("RateLimited ... Aborting...");
                        break;
                    }

                    Console.WriteLine($"{code} : {result?.ToString() ?? "ERROR"}");
                }
            }
        }
    }
}