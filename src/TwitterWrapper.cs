using Sebagomez.TwitterLib.Helpers;
using Sebagomez.TwitterLib.API.Tweets;
using Sebagomez.TwitterLib.API.Options;
using Sebagomez.TwitterLib.Entities;

namespace zombiefollower
{
	internal class TwitterWrapper
	{
		AuthenticatedUser m_user;

		public TwitterWrapper(AuthenticatedUser user)
		{
			if (user is null)
				throw new Exception("User not authenticated");

			m_user = user;
		}
		public async Task<IEnumerable<Status>> GetTwits(string query)
		{
			SearchOptions options = new SearchOptions { Query = query, IncludeEntities = false, User = m_user };
			return (await Search.SearchTweets(options)).statuses;
		}

		public async Task<HashSet<long>> GetFollowing()
		{
			FollowerListOptions followerOptions = new FollowerListOptions { User = m_user};
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
			FriendshipOptions friendshipOptions = new FriendshipOptions { UserId = userId, User = m_user};
			await Frienship.Follow(friendshipOptions);
		}

		public async Task Unfollow(long userId)
		{
			FriendshipOptions friendshipOptions = new FriendshipOptions { UserId = userId, User = m_user};
			await Frienship.Unfollow(friendshipOptions);
		}
	}
}