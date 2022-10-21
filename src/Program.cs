//Console.WriteLine("Hello, World!");

//Authenticate with Twitter
using Sebagomez.TwitterLib.Helpers;
using Sebagomez.TwitterLib.API.OAuth;
using Sebagomez.TwitterLib.Entities;

namespace zombiefollower
{
	internal class Program
	{
		private static async Task<int> Main(string[] args)
		{
			const string TWITTER_API_KEY = "TWITTER_API_KEY";
			const string TWITTER_API_SECRET = "TWITTER_API_SECRET";

			AuthenticatedUser twiUser = AuthenticatedUser.Deserialize("./authenticated.user");
			if (twiUser is null)
			{
				Console.WriteLine("Reading environment...");
				string twitterKey = System.Environment.GetEnvironmentVariable(TWITTER_API_KEY);
				string twitterSecret = System.Environment.GetEnvironmentVariable(TWITTER_API_SECRET);

				if (twitterKey is null || twitterSecret is null)
				{
					Console.WriteLine($"{TWITTER_API_KEY} and/or {TWITTER_API_SECRET} not found");
					return 1;
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
				string pin = Console.ReadLine();

				Console.Write("Getting Twitter access token...");
				string accessToken = OAuthAuthenticator.GetPINToken(token, pin, twitterKey, twitterSecret).Result;
				twiUser.ParseTokens(accessToken);
				Console.WriteLine("done!");

				Console.WriteLine($"Welcome {twiUser.ScreenName}!");
				Console.WriteLine("");


				//Temporary
				twiUser.Serialize("./authenticated.user");
			}

			//return twiUser;

			foreach (var status in (await Scanner.GetTwits(twiUser)).statuses)
			{
				Console.WriteLine($"I need to follow {status.user}");
			}

			return 0;
		}
	}
}