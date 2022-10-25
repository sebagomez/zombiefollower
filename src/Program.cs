using Sebagomez.TwitterLib.Helpers;
using Sebagomez.TwitterLib.API.OAuth;

namespace zombiefollower
{
	internal class Program
	{
		#region Constants
		const int MIN_MILLI_SECS = 500;
		const int MAX_MILLI_SECS = 3000;
		const string TWITTER_API_KEY = "TWITTER_API_KEY";
		const string TWITTER_API_SECRET = "TWITTER_API_SECRET";
		const string STORAGE_ACCOUNT = "STORAGE_ACCOUNT";
		const string STORAGE_KEY = "STORAGE_KEY";
		#endregion

		private static async Task<int> Main(string[] args)
		{
			AuthenticatedUser twiUser = TwitterSignIn();
			if (twiUser is null)
				return 1;

			TableStorageWrapper storage = AzureStorageSignIn();
			if (storage is null)
				return 1;

			TwitterWrapper twitter = new TwitterWrapper(twiUser);

			//Temporary, these should be parameters
			bool follow = true;
			bool dryrun = false;
			string searchTerm = "DevOpsDays";

			Random random = new Random();
			if (follow)
			{
				HashSet<long> following = await twitter.GetFollowing();
				HashSet<long> followed = new HashSet<long>();
				foreach (var status in (await twitter.GetTwits(searchTerm)))
				{
					if (following.Contains(status.user.id) || followed.Contains(status.user.id))
					{
						Console.WriteLine($"Already following {status.user}");
						continue;
					}

					if (status.user.id_str == twiUser.UserId)
					{
						Console.WriteLine($"Don't follow yourself {status.user}");
						continue;
					}

					Console.WriteLine($"Follow {status.user}");

					if (!dryrun)
					{
						Thread.Sleep(random.Next(MIN_MILLI_SECS, MAX_MILLI_SECS));

						await twitter.Follow(status.user.id);
						await storage.SaveFollowed(status.user.id, status.user.ToString());
						followed.Add(status.user.id);
					}
				}
			}
			else
			{
				foreach (KeyValuePair<long, string> followed in await storage.GetFollowedAfter(DateTime.Today.AddDays(-1)))
				{
					if (!dryrun)
					{
						Thread.Sleep(random.Next(MIN_MILLI_SECS, MAX_MILLI_SECS));

						await twitter.Unfollow(followed.Key);
						await storage.UpdateUnfollow(followed.Key);
					}
					Console.WriteLine($"Unfollowed {followed.Value}");
				}
			}

			return 0;
		}

		static TableStorageWrapper AzureStorageSignIn()
		{
			Console.WriteLine("Reading environment...");
			string? storageAccount = System.Environment.GetEnvironmentVariable(STORAGE_ACCOUNT);
			string? storageKey = System.Environment.GetEnvironmentVariable(STORAGE_KEY);

			if (storageAccount is null || storageKey is null)
			{
				Console.WriteLine($"{STORAGE_ACCOUNT} and/or {STORAGE_KEY} not found");
				return null;
			}

			return new TableStorageWrapper(storageAccount, storageKey);
		}

		static AuthenticatedUser TwitterSignIn()
		{
			AuthenticatedUser twiUser = AuthenticatedUser.Deserialize("./authenticated.user");
			if (twiUser is null)
			{
				Console.WriteLine("Reading environment...");
				string? twitterKey = System.Environment.GetEnvironmentVariable(TWITTER_API_KEY);
				string? twitterSecret = System.Environment.GetEnvironmentVariable(TWITTER_API_SECRET);

				if (twitterKey is null || twitterSecret is null)
				{
					Console.WriteLine($"{TWITTER_API_KEY} and/or {TWITTER_API_SECRET} not found");
					return null;
				}

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
				twiUser.Serialize("./authenticated.user");
			}

			return twiUser;
		}
	}
}