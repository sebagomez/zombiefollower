## 🧟 Zombie follower

I used this bot to follow accounts that twitted about a particular subject.

To make it work for your own go to https://developer.twitter.com/en/portal/projects-and-apps and create an APP. Grab the API Key and Secret. You'll also need an Azure Storage table to store followed accounts (in case you want to unfollow them at a later time).

Now execute the binary as follows:
```bash
dotnet zombiefollower.dll follow --search Kubernetes --twitter-api-key <twitter-api-key> --twitter-api-secret <twitter-api-secret> --azure-account <azure-account> --azure-key <azure-key> 
```

You will then be asked to validate your twitter account in your browser and paste the provided code back to the command line.

And voila! You'll be following (A LOT) of people that twitted about Kubernetes

```
❯ dotnet zombiefollower.dll --help
Description:
  Follows twitter users that had twiited a specific term

Usage:
  zombiefollower [command] [options]

Options:
  -dr, --dry-run                                  Shows the list of accounts that would be followed/unfollowed (no modifications done) [default: False]
  -tk, --twitter-api-key <twitter-api-key>        Twitter API Key (or TWITTER_API_KEY env var)
  -ts, --twitter-api-secret <twitter-api-secret>  Twitter API Secret (or TWITTER_API_SECRET env var)
  -aa, --azure-account <azure-account>            Azure Storage account name (or STORAGE_ACCOUNT env var)
  -ak, --azure-key <azure-key>                    Azure Storage key (or STORAGE_KEY env var)
  --version                                       Show version information
  -?, -h, --help                                  Show help and usage information

Commands:
  follow    Follows users that twitted about the <search> argument
  unfollow  Unfollows users followed via zombiefollower before the <from> date argument
```

![](./res/arch.png)

Use this bot at your own risks. There are sone twitter rules that prevent exacly this https://help.twitter.com/en/using-twitter/twitter-follow-limit

