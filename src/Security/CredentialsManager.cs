using Sebagomez.TwitterLib.API.OAuth;
using Sebagomez.TwitterLib.Helpers;
using zombiefollower.Wrapper;

namespace zombiefollower.Security
{
	internal class CredentialsManager
	{
		#region Constants
		public const string TWITTER_API_KEY = "TWITTER_API_KEY";
		public const string TWITTER_API_SECRET = "TWITTER_API_SECRET";
		public const string STORAGE_ACCOUNT = "STORAGE_ACCOUNT";
		public const string STORAGE_KEY = "STORAGE_KEY";
		#endregion

		public string TwitterCredentialsPath { get; private set; }

		public string AzureCredentialsPath { get; private set; }

		public CredentialsManager(DirectoryInfo dir)
		{
			TwitterCredentialsPath = Path.Combine(dir.FullName, "twitter.user");
			AzureCredentialsPath = Path.Combine(dir.FullName, "azure.json");
		}

		public ZombieArguments? SignIn(string? twitterApiKey, string? twitterApiSecret, string? azureAccount, string? azureKey)
		{
			AuthenticatedUser? twiUser = TwitterSignIn(twitterApiKey, twitterApiSecret);
			if (twiUser is null)
				return null;

			TableStorageWrapper? storage = AzureStorageSignIn(azureAccount, azureKey);
			if (storage is null)
				return null;

			TwitterWrapper twitter = new TwitterWrapper(twiUser);

			return new ZombieArguments { Twitter = twitter, Azure = storage };
		}

		TableStorageWrapper? AzureStorageSignIn(string? azureAccount, string? azureKey)
		{
			string? storageAccount = azureAccount is null ? System.Environment.GetEnvironmentVariable(STORAGE_ACCOUNT) : azureAccount;
			string? storageKey = azureKey is null ? System.Environment.GetEnvironmentVariable(STORAGE_KEY) : azureKey;
			bool noParms = string.IsNullOrEmpty(azureAccount) || string.IsNullOrEmpty(azureKey);

			AzureCredentials? creds = AzureCredentials.Deserialize(AzureCredentialsPath);
			if (creds is null && noParms)
				throw new ApplicationException($"{STORAGE_ACCOUNT} and/or {STORAGE_KEY} not found");

			if (!noParms)
			{
				creds = new AzureCredentials() { Account = storageAccount!, Key = storageKey! };
				creds.Serialize(AzureCredentialsPath);
			}

			return new TableStorageWrapper(creds!.Account, creds!.Key);
		}

		AuthenticatedUser? TwitterSignIn(string? twitterApiKey, string? twitterApiSecret)
		{
			string? twitterKey = twitterApiKey is null ? System.Environment.GetEnvironmentVariable(TWITTER_API_KEY) : twitterApiKey;
			string? twitterSecret = twitterApiSecret is null ? System.Environment.GetEnvironmentVariable(TWITTER_API_SECRET) : twitterApiSecret;
			bool noParms = string.IsNullOrEmpty(twitterKey) || string.IsNullOrEmpty(twitterSecret);

			AuthenticatedUser twiUser = AuthenticatedUser.Deserialize(TwitterCredentialsPath);
			if (twiUser is null && noParms)
				throw new ApplicationException($"{TWITTER_API_KEY} and/or {TWITTER_API_SECRET} not found");

			if (twiUser != null && (noParms || (twiUser.AppSettings.AppKey == twitterKey || twiUser.AppSettings.AppSecret == twitterSecret)))
				return twiUser;

			twiUser = new AuthenticatedUser
			{
				AppSettings = new AppCredentials() { AppKey = twitterKey, AppSecret = twitterSecret }
			};

			Console.Write("Getting Twitter authentication token...");
			string token = OAuthAuthenticator.GetOAuthToken(twitterKey, twitterSecret).Result;
			Console.WriteLine("done!");
			Console.WriteLine("Please open your favorite browser and go to this URL to authenticate with Twitter:");
			Console.WriteLine($"https://api.twitter.com/oauth/authorize?oauth_token={token}");
			Console.Write("Insert the pin here:");
			string? pin = Console.ReadLine();

			Console.Write("Getting Twitter access token...");
			string accessToken = OAuthAuthenticator.GetPINToken(token, pin, twitterKey, twitterSecret).Result;
			twiUser.ParseTokens(accessToken);
			Console.WriteLine("done!");

			Console.WriteLine($"Welcome {twiUser.ScreenName}!");
			Console.WriteLine("");

			twiUser.Serialize(TwitterCredentialsPath);

			return twiUser;
		}
	}
}