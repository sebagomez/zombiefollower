using System.CommandLine;
using zombiefollower.Security;
using zombiefollower.Wrapper;

namespace zombiefollower
{
	internal class Program
	{
		#region Constants
		const int OK = 0;
		const int NOT_OK = 1;
		const int MIN_MILLI_SECS = 500;
		const int MAX_MILLI_SECS = 3000;
		#endregion

		static int s_total = 0;
		static int s_changed = 0;

		private static async Task<int> Main(string[] args)
		{
			int exitCode = OK;

			#region Options
			var twitterApiKeyOption = new Option<string>(
				aliases: new string[] {"--twitter-api-key", "-tk"},
				description: $"Twitter API Key (or {CredentialsManager.TWITTER_API_KEY} env var)"
			);

			var twitterApiSercetOption = new Option<string>(
				aliases: new string[] {"--twitter-api-secret", "-ts"},
				description: $"Twitter API Secret (or {CredentialsManager.TWITTER_API_SECRET} env var)"
			);

			var azureAccountOption = new Option<string>(
				aliases: new string[] {"--azure-account", "-aa"},
				description: $"Azure Storage account name (or {CredentialsManager.STORAGE_ACCOUNT} env var)"
			);

			var azureKeyOption = new Option<string>(
				aliases: new string[] {"--azure-key", "-ak"},
				description: $"Azure Storage key (or {CredentialsManager.STORAGE_KEY} env var)"
			);

			var searchOption = new Option<string>(
				aliases: new string[] {"--search", "-s"},
				description: "Search term you want to follow"
			);
			searchOption.IsRequired = true;

			var fromOption = new Option<DateOnly?>(
				aliases: new string[] {"--from", "-f"},
				description: "Date from when the user was followed."
			);
			DateOnly defaultDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
			fromOption.SetDefaultValue(defaultDate);

			var rootCommand = new RootCommand("Follows twitter users that had twiited a specific term");
			rootCommand.AddGlobalOption(twitterApiKeyOption);
			rootCommand.AddGlobalOption(twitterApiSercetOption);
			rootCommand.AddGlobalOption(azureAccountOption);
			rootCommand.AddGlobalOption(azureKeyOption);

			var followCommand = new Command("follow", "Follows users that twitted about the <search> argument")
			{
				searchOption
			};
			followCommand.SetHandler<string, string?, string?, string?, string?>(Follow, searchOption, twitterApiKeyOption, twitterApiSercetOption, azureAccountOption, azureKeyOption);

			var unfollowCommand = new Command("unfollow", "Unfollows users followed via zombiefollower before the <from> date argument")
			{
				fromOption
			};
			unfollowCommand.SetHandler<DateOnly?, string?, string?, string?, string?>(Unfollow, fromOption, twitterApiKeyOption, twitterApiSercetOption, azureAccountOption, azureKeyOption);

			rootCommand.AddCommand(followCommand);
			rootCommand.AddCommand(unfollowCommand);
			#endregion

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
			ZombieArguments? args = CredentialsManager.SignIn(twitterApiKey, twitterApiSecret, azureAccount, azureKey);
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
			ZombieArguments? args = CredentialsManager.SignIn(twitterApiKey, twitterApiSecret, azureAccount, azureKey);
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

		
	}
}