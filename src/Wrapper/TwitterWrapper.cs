using Sebagomez.TwitterLib.Helpers;
using Sebagomez.TwitterLib.API.Tweets;
using Sebagomez.TwitterLib.API.Options;
using Sebagomez.TwitterLib.Entities;
using System.Text.RegularExpressions;

namespace zombiefollower.Wrapper
{
	internal class TwitterWrapper
	{
		public AuthenticatedUser Me { get; private set; }

		public TwitterWrapper(AuthenticatedUser user)
		{
			if (user is null)
				throw new Exception("User not authenticated");

			Me = user;
		}
		public async Task<IEnumerable<Status>> GetTwits(string query)
		{
			const int MAX_RESULTS = 500;
			SearchOptions options = new SearchOptions { Query = query, IncludeEntities = false, User = Me };
			SearchResult result =  await Search.SearchTweets(options);

			List<Status> list = new List<Status>(result.statuses);

			while(!string.IsNullOrEmpty(result.search_metadata.next_results) && list.Count < MAX_RESULTS)
			{
				Match match = Regex.Match(result.search_metadata.next_results, ".*max_id=([0-9]+).*");
				if (match != null && match.Groups.Count > 1)
				{
					string max_id_str = match.Groups[1].Value;
					long new_max_id;
					if (long.TryParse(max_id_str, out new_max_id))
					{
						options.MaxId = new_max_id;
						result =  await Search.SearchTweets(options);
						list.AddRange(result.statuses);
					}
					else
						break;
				}
				else
					break;
				
			}

			return list;
		}

		public async Task<HashSet<long>> GetFollowing()
		{
			FollowerListOptions followerOptions = new FollowerListOptions { User = Me };
			FriendIDsList following = await Frienship.ListFollowersIDs(followerOptions);

			HashSet<long> result = new HashSet<long>(following.ids);
			while (following.next_cursor != 0)
			{
				followerOptions.Cursor = following.next_cursor_str;
				following = await Frienship.ListFollowersIDs(followerOptions);
				result.UnionWith(following.ids);
			}

			return result;
		}

		public async Task Follow(long userId)
		{
			FriendshipOptions friendshipOptions = new FriendshipOptions { UserId = userId, User = Me };
			await Frienship.Follow(friendshipOptions);
		}

		public async Task Unfollow(long userId)
		{
			FriendshipOptions friendshipOptions = new FriendshipOptions { UserId = userId, User = Me };
			await Frienship.Unfollow(friendshipOptions);
		}
	}
}