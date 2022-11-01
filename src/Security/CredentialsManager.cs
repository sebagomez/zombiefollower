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
		public static ZombieArguments? SignIn(string? twitterApiKey, string? twitterApiSecret, string? azureAccount, string? azureKey)
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

		static TableStorageWrapper? AzureStorageSignIn(string? azureAccount, string? azureKey)
		{
			string? storageAccount = "";
			string? storageKey = "";
			AzureCredentials? creds = AzureCredentials.Deserialize("./azure.json");
			if (creds is null)
			{
				if (string.IsNullOrEmpty(azureAccount) || string.IsNullOrEmpty(azureKey))
					Console.WriteLine("Reading environment for Azure Storage credentials");

				storageAccount = azureAccount is null ? System.Environment.GetEnvironmentVariable(STORAGE_ACCOUNT) : azureAccount;
				storageKey = azureKey is null ? System.Environment.GetEnvironmentVariable(STORAGE_KEY) : azureKey;

				if (storageAccount is null || storageKey is null)
					throw new ApplicationException($"{STORAGE_ACCOUNT} and/or {STORAGE_KEY} not found");
				
				creds = new AzureCredentials() { Account = storageAccount, Key = storageKey};
				creds.Serialize("./azure.json");
			}
			else
				Console.WriteLine("Azure credentials file found");

			return new TableStorageWrapper(creds.Account, creds.Key);
		}

		static AuthenticatedUser? TwitterSignIn(string? twitterApiKey, string? twitterApiSecret)
		{
			AuthenticatedUser twiUser = AuthenticatedUser.Deserialize("./twitter.user");
			if (twiUser is null)
			{
				if (string.IsNullOrEmpty(twitterApiKey) || string.IsNullOrEmpty(twitterApiSecret))
					Console.WriteLine("Reading environment for Twitter credentials");

				string? twitterKey = twitterApiKey is null ? System.Environment.GetEnvironmentVariable(TWITTER_API_KEY) : twitterApiKey;
				string? twitterSecret = twitterApiSecret is null ? System.Environment.GetEnvironmentVariable(TWITTER_API_SECRET) : twitterApiSecret;

				if (twitterKey is null || twitterSecret is null)
					throw new ApplicationException($"{TWITTER_API_KEY} and/or {TWITTER_API_SECRET} not found");

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

				twiUser.Serialize("./twitter.user");
			}
			else
				Console.WriteLine("Twitter credentials file found");

			return twiUser;
		}
	}
}