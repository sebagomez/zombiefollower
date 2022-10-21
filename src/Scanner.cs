using Sebagomez.TwitterLib;
using Sebagomez.TwitterLib.Helpers;
using Sebagomez.TwitterLib.API.Tweets;
using Sebagomez.TwitterLib.API.Options;
using Sebagomez.TwitterLib.Entities;

namespace zombiefollower
{
	public class Scanner
	{
		public static async Task<SearchResult> GetTwits(AuthenticatedUser user)
		{
			SearchOptions options = new SearchOptions { Query = "Kubernetes", User = user };
			return await Search.SearchTweets(options);
		}

	}
}