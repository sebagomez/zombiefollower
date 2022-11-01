using Sebagomez.TwitterLib.Helpers;
using Sebagomez.TwitterLib.API.OAuth;
using System.CommandLine;

namespace zombiefollower
{
	internal class Program
	{
		#region Constants
		const int OK = 0;
		const int NOT_OK = 1;
		const int MIN_MILLI_SECS = 500;
		const int MAX_MILLI_SECS = 3000;
		const string TWITTER_API_KEY = "TWITTER_API_KEY";
		const string TWITTER_API_SECRET = "TWITTER_API_SECRET";
		const string STORAGE_ACCOUNT = "STORAGE_ACCOUNT";
		const string STORAGE_KEY = "STORAGE_KEY";
		#endregion

		static int s_total = 0;
		static int s_changed = 0;

		private static async Task<int> Main(string[] args)
		{
			int exitCode = OK;

			var twitterApiKeyOption = new Option<string>(
				aliases: new string[] {"--twitter-api-key", "-tk"},
				description: "Twitter API Key"
			);

			var twitterApiSercetOption = new Option<string>(
				aliases: new string[] {"--twitter-api-secret", "-ts"},
				description: "Twitter API Secret"
			);

			var azureAccountOption = new Option<string>(
				aliases: new string[] {"--azure-account", "-aa"},
				description: "Azure Storage account name"
			);

			var azureKeyOption = new Option<string>(
				aliases: new string[] {"--azure-key", "-ak"},
				description: "Azure Storage key"
			);

			var searchOption = new Option<string>(
				aliases: new string[] {"--search", "-s"},
				description: "Search term you want to follow"
			);
			searchOption.IsRequired = true;

			var fromOption = new Option<DateOnly?>(
				aliases: new string[] {"--from", "-f"},
				description: "Date from when the user was followed"
			);

			var rootCommand = new RootCommand("Follows twitter users that had twiited a specific term");
			rootCommand.AddGlobalOption(twitterApiKeyOption);
			rootCommand.AddGlobalOption(twitterApiSercetOption);
			rootCommand.AddGlobalOption(azureKeyOption);
			rootCommand.AddGlobalOption(azureAccountOption);

			var followCommand = new Command("follow", "Follows users that twitted about the searchTerm")
			{
				searchOption
			};
			followCommand.SetHandler<string, string?, string?, string?, string?>(Follow, searchOption, twitterApiKeyOption, twitterApiSercetOption, azureAccountOption, azureKeyOption);

			var unfollowCommand = new Command("unfollow", "Unfollows users followed over a week ago")
			{
				fromOption
			};
			unfollowCommand.SetHandler<DateOnly?, string?, string?, string?, string?>(Unfollow, fromOption, twitterApiKeyOption, twitterApiSercetOption, azureAccountOption, azureKeyOption);

			rootCommand.AddCommand(followCommand);
			rootCommand.AddCommand(unfollowCommand);

			try
			{
				exitCode = await rootCommand.InvokeAsync(args);

				if (exitCode == OK && args.Length > 0 && (followCommand.Aliases.Contains(args[0]) || unfollowCommand.Aliases.Contains(args[0])))
				{
					string action = $"{args[0]}ed";
					Console.WriteLine($"{action} {s_changed} accounts out of {s_total}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"ERROR:{ex.Message}");
				exitCode = NOT_OK;
			}

			return exitCode;
		}

		static async Task Follow(string searchTerm, string? twitterApiKey, string? twitterApiSecret, string? azureAccount, string? azureKey)
		{
			ZombieArguments? args = SignIn(twitterApiKey, twitterApiSecret, azureAccount, azureKey);
			if (args is null)
				return;

			Random random = new Random();
			HashSet<long> following = await args.Twitter!.GetFollowing();
			HashSet<long> followed = new HashSet<long>();
			foreach (var status in (await args.Twitter!.GetTwits(searchTerm)))
			{
				s_total++;
				if (following.Contains(status.user.id) || followed.Contains(status.user.id))
				{
					Console.WriteLine($"Already following {status.user}");
					continue;
				}

				if (status.user.id_str == args.Twitter!.Me.UserId)
				{
					Console.WriteLine($"Don't follow yourself {status.user}");
					continue;
				}

				Console.WriteLine($"Follow {status.user}");

				Thread.Sleep(random.Next(MIN_MILLI_SECS, MAX_MILLI_SECS));

				await args.Twitter!.Follow(status.user.id);
				await args.Azure!.SaveFollowed(status.user.id, status.user.ToString(), searchTerm);
				followed.Add(status.user.id);
				s_changed++;
			}
			
		}

		static async Task Unfollow(DateOnly? fromDate, string? twitterApiKey, string? twitterApiSecret, string? azureAccount, string? azureKey)
		{
			ZombieArguments? args = SignIn(twitterApiKey, twitterApiSecret, azureAccount, azureKey);
			if (args is null)
				return;

			Random random = new Random();
			if (fromDate is null)
				fromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
			foreach (KeyValuePair<long, string> followed in await args.Azure!.GetFollowedBefore(fromDate.Value.ToDateTime(TimeOnly.MinValue)))
			{
				s_total++;

				Thread.Sleep(random.Next(MIN_MILLI_SECS, MAX_MILLI_SECS));

				await args.Twitter!.Unfollow(followed.Key);
				await args.Azure.UpdateUnfollow(followed.Key);
				s_changed++;

				Console.WriteLine($"Unfollowed {followed.Value}");
			}
		}

		static ZombieArguments? SignIn(string? twitterApiKey, string? twitterApiSecret, string? azureAccount, string? azureKey)
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
				Console.WriteLine("Reading environment...");
				storageAccount = azureAccount is null ? System.Environment.GetEnvironmentVariable(STORAGE_ACCOUNT) : azureAccount;
				storageKey = azureKey is null ? System.Environment.GetEnvironmentVariable(STORAGE_KEY) : azureKey;

				if (storageAccount is null || storageKey is null)
					throw new ApplicationException($"{STORAGE_ACCOUNT} and/or {STORAGE_KEY} not found");
				
				creds = new AzureCredentials() { Account = storageAccount, Key = storageKey};
				creds.Serialize("./azure.json");
			}

			return new TableStorageWrapper(creds.Account, creds.Key);
		}

		static AuthenticatedUser? TwitterSignIn(string? twitterApiKey, string? twitterApiSecret)
		{
			//Temporary (?) solution
			AuthenticatedUser twiUser = AuthenticatedUser.Deserialize("./twitter.user");
			if (twiUser is null)
			{
				Console.WriteLine("Reading environment...");
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


				//Temporary
				twiUser.Serialize("./twitter.user");
			}

			return twiUser;
		}
	}
}