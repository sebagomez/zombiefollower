using Azure;
using Azure.Data.Tables;

namespace zombiefollower
{
	internal class TableStorageWrapper
	{
		#region Constants
		const string CONNSTRING_TEMPLATE = "DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};EndpointSuffix=core.windows.net";
		const string PARTITION_KEY = "PartitionKey";
		const string ROW_KEY = "RowKey";
		const string ID = "ID";
		const string HANDLE = "Handle";
		const string FOLLOWED = "Followed";
		const string UNFOLLOWED = "Unfollowed";
		const string SEARCH_TERM = "SearchTerm";
		#endregion

		TableClient m_tableClient;

		public TableStorageWrapper(string account, string key, string table = "ZombieFollower")
		{
			m_tableClient = new TableClient(string.Format(CONNSTRING_TEMPLATE, account, key, "core.windows.net"), table);
			m_tableClient.CreateIfNotExists();
		}

		public async Task<bool> SaveFollowed(long id, string handle, string searchTerm)
		{
			Dictionary<string, object> dic = new Dictionary<string, object>
			{
				{ PARTITION_KEY, DateTime.UtcNow.ToString("yyyyMMdd") },
				{ ROW_KEY, (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond).ToString() },
				{ ID, id },
				{ HANDLE, handle },
				{ FOLLOWED, DateTime.UtcNow },
				{ UNFOLLOWED, false },
				{ SEARCH_TERM, searchTerm }
			};
			TableEntity entity = new TableEntity(dic);
			Azure.Response resp = await m_tableClient.AddEntityAsync(entity);

			return !resp.IsError;
		}

		public async Task<Dictionary<long, string>> GetFollowedBefore(DateTime after)
		{
			string queryTemplate = "Followed lt datetime'{0}' and Unfollowed eq false";

			AsyncPageable<TableEntity> queryResultsFilter = m_tableClient.QueryAsync<TableEntity>(string.Format(queryTemplate, after.ToString("yyyy-MM-dd")));

			Dictionary<long, string> results = new Dictionary<long, string>();
			await foreach (TableEntity entity in queryResultsFilter)
				results.Add((long)entity[ID], (string)entity[HANDLE]);

			return results;
		}

		public async Task UpdateUnfollow(long id)
		{
			string queryTemplate = "ID eq {0}L";

			AsyncPageable<TableEntity> queryResultsFilter = m_tableClient.QueryAsync<TableEntity>(string.Format(queryTemplate, id));

			HashSet<long> results = new HashSet<long>();
			await foreach (TableEntity entity in queryResultsFilter)
			{
				entity[UNFOLLOWED] = true;
				await m_tableClient.UpdateEntityAsync<TableEntity>(entity, Azure.ETag.All);
			}

		}
	}
}