using System.Security.Cryptography;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamKeyBulkActivator;

public class Client
{
    private bool isRunning = true;
    private SteamUser? steamUser;
    private SteamClient steamClient;
    private CallbackManager callbackManager;
    private Thread thread;
    private string username;
    private string password;

    private string? authCode;
    private string? twoFactorCode;

    public event EventHandler LoggedInEvent;

    public void Login(string username, string password, string? twoFactorCode = null, string? authCode = null)
    {
        this.username = username;
        this.password = password;

        this.authCode = authCode;
        this.twoFactorCode = twoFactorCode;

        steamClient = new SteamClient();
        callbackManager = new CallbackManager(steamClient);

        steamUser = steamClient.GetHandler<SteamUser>();

        callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

        callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
        callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

        callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);


        Console.WriteLine("Connecting to Steam...");

        thread = new Thread(Run);
        thread.Start();
    }

    private void Run()
    {
        steamClient.Connect();

        while (isRunning)
        {
            // in order for the callbacks to get routed, they need to be handled by the manager
            callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
        }

        Console.WriteLine("Steam Thread exiting...");
    }

    private void OnConnected(SteamClient.ConnectedCallback callback)
    {
        Console.WriteLine("Connected to Steam! Logging in '{0}'...", username);

        /*
        byte[] sentryHash = null;
        if (File.Exists("sentry.bin"))
        {
            // if we have a saved sentry file, read and sha-1 hash it
            byte[] sentryFile = File.ReadAllBytes("sentry.bin");
            sentryHash = CryptoHelper.SHAHash(sentryFile);
        }
        */

        steamUser.LogOn(new SteamUser.LogOnDetails
        {
            Username = username,
            Password = password,

            // in this sample, we pass in an additional authcode
            // this value will be null (which is the default) for our first logon attempt
            AuthCode = authCode,

            // if the account is using 2-factor auth, we'll provide the two factor code instead
            // this will also be null on our first logon attempt
            TwoFactorCode = twoFactorCode,

            SentryFileHash = null,
        });
    }

    private void OnDisconnected(SteamClient.DisconnectedCallback callback)
    {
        Console.WriteLine("Disconnected from Steam");

        isRunning = false;
    }

    void OnLoggedOn(SteamUser.LoggedOnCallback callback)
    {
        // bool isSteamGuard = callback.Result == EResult.AccountLogonDenied;
        // bool is2FA = callback.Result == EResult.AccountLoginDeniedNeedTwoFactor;


        if (callback.Result != EResult.OK)
        {
            Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

            isRunning = false;
            return;
        }

        Console.WriteLine("Successfully logged on!");

        LoggedInEvent?.Invoke(this, null);
    }

    public async Task<EPurchaseResultDetail?> Redeem(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (steamClient == null)
        {
            throw new InvalidOperationException(nameof(Client));
        }

        if (!steamClient.IsConnected)
        {
            return null;
        }

        ClientMsgProtobuf<CMsgClientRegisterKey> request = new(EMsg.ClientRegisterKey)
        {
            SourceJobID = steamClient.GetNextJobID(),
            Body = {key = key}
        };

        steamClient.Send(request);

        try
        {
            var callback = await new AsyncJob<SteamApps.PurchaseResponseCallback>(steamClient, request.SourceJobID)
                .ToTask();
            return callback.PurchaseResultDetail;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
    {
        Console.WriteLine("Logged off of Steam: {0}", callback.Result);
    }

    private void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback)
    {
        /*
       Console.WriteLine("Updating sentryfile...");
      
       int fileSize;
       byte[] sentryHash;
       using (var fs = File.Open("sentry.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite))
       {
           fs.Seek(callback.Offset, SeekOrigin.Begin);
           fs.Write(callback.Data, 0, callback.BytesToWrite);
           fileSize = (int) fs.Length;

           fs.Seek(0, SeekOrigin.Begin);
           using var sha = SHA1.Create();
           sentryHash = sha.ComputeHash(fs);
       }
      

       // inform the steam servers that we're accepting this sentry file
       steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
       {
           JobID = callback.JobID,

           FileName = callback.FileName,

           BytesWritten = callback.BytesToWrite,
           FileSize = fileSize,
           Offset = callback.Offset,

           Result = EResult.OK,
           LastError = 0,

           OneTimePassword = callback.OneTimePassword,

           SentryFileHash = sentryHash,
       });
       Console.WriteLine("Done!");
        */
    }
}