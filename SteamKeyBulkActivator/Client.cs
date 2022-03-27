using System.Security.Cryptography;
using SteamKit2;
using SteamKit2.Internal;

namespace SteamKeyBulkActivator;

public class Client
{
    private const string SentryFileName = "sentry.bin";

    private bool isRunning = false;
    private SteamUser? steamUser;
    private SteamClient steamClient;
    private CallbackManager callbackManager;
    private Thread thread;
    private string? username;
    private string? password;

    private string? authCode;
    private string? twoFactorCode;
    private string? loginKey;

    public bool SentryLoginAvailable => File.Exists(SentryFileName);

    public event EventHandler<SteamUser.LoggedOnCallback> LoggedInEvent;
    
    public void Login(string? username, string? password, string? twoFactorCode = null, string? authCode = null)
    {
        if (isRunning)
        {
            return;
        }

        isRunning = true;

        this.username = username;
        this.password = password;

        if (twoFactorCode?.Length > 5)
        {
            this.loginKey = twoFactorCode;
            Console.WriteLine("Using loginKey");
        }
        else
        {
            this.twoFactorCode = twoFactorCode;
        }
        
        this.authCode = authCode;
        

        steamClient = new SteamClient();
        callbackManager = new CallbackManager(steamClient);

        steamUser = steamClient.GetHandler<SteamUser>();

        callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
        callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

        callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
        callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
        
        callbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnLoginKey);

        Console.WriteLine("Connecting to Steam...");

        thread = new Thread(Run);
        thread.Start();
    }

    private void OnLoginKey(SteamUser.LoginKeyCallback obj)
    {
        Console.WriteLine($"Use this key for your steam guard login: {obj.LoginKey}");
    }

    private void Run()
    {
        steamClient.Connect();

        while (isRunning)
        {
            callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
        }

        Console.WriteLine("Steam Thread exiting...");
    }

    private void OnConnected(SteamClient.ConnectedCallback callback)
    {
        Console.WriteLine("Connected to Steam! Logging in '{0}'...", username);
        
        steamUser.LogOn(new SteamUser.LogOnDetails
        {
            Username = username,
            Password = password,
            
            AuthCode = authCode,
            TwoFactorCode = twoFactorCode,
            LoginKey = loginKey,
            
            ShouldRememberPassword = true,
        });
    }


    private void OnDisconnected(SteamClient.DisconnectedCallback callback)
    {
        Console.WriteLine("Disconnected from Steam");
        isRunning = false;
    }

    void OnLoggedOn(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result != EResult.OK)
        {
            Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

            return;
        }

        Console.WriteLine("Successfully logged on!");
        LoggedInEvent?.Invoke(this, callback);
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
    
    public void Disconnect()
    {
        steamClient.Disconnect();
    }
}