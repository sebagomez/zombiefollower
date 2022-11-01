
using System.Text.Json;

namespace zombiefollower.Security
{
	public class AzureCredentials
	{
		public string Account { get; set; }
		public string Key { get; set; }

		public AzureCredentials()
		{
			Account = "";
			Key = "";
		}

		public void Serialize(string fileName)
		{
			File.WriteAllText(fileName, JsonSerializer.Serialize(this));
		}

		public static AzureCredentials? Deserialize(string fileName)
		{
			if (!File.Exists(fileName))
				return null;
			return JsonSerializer.Deserialize<AzureCredentials>(File.ReadAllText(fileName));
		}
	}

}