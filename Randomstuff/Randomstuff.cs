using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using System.Threading.Tasks;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.VisualBasic;

Console.Clear();

Console.CursorVisible = false;

Console.WriteLine("YouTube Random Video Picker");
Console.WriteLine("===========================");

Console.WriteLine("Loop? Exit by typing exit at anytime or ctrl+c obv\n(Y/n)");
bool loop = true;
string doLoop = Console.ReadLine();

if (doLoop.ToLower().Contains("n"))
    loop = false;

static void LoadEnv(string filePath)
{
    if (!File.Exists(filePath))
        throw new FileNotFoundException($"The file '{filePath}' does not exist.");

    foreach (var line in File.ReadAllLines(filePath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            continue;

        var parts = line.Split('=', 2);
        if (parts.Length != 2)
            continue;

        var key = parts[0].Trim();
        var value = parts[1].Trim();
        Environment.SetEnvironmentVariable(key, value);
    }
}

bool hasRun = false;

while (loop || !hasRun)
{
    hasRun = true;
    Console.WriteLine("Looking for something specific?\n(y/N)");
    string skipQuestions = Console.ReadLine();
    bool skip = true;

    if (skipQuestions.ToLower().Contains("exit"))
        return;

    if (skipQuestions.ToLower().Contains("y"))
        skip = false;

    LoadEnv("./secrets.env");
    string API_KEY = Environment.GetEnvironmentVariable("API_KEY");

    var youtubeService = new YouTubeService(new BaseClientService.Initializer()
    {
        ApiKey = API_KEY,
        ApplicationName = "RandomVideoPicker"
    });

    string revLanguage = "en";
    string searchQ = " ";
    string searchType = "video";
    string channelId = null;

    if (!skip)
    {
        Console.WriteLine("Language? defualt is en.");
        revLanguage = Console.ReadLine();
        if (revLanguage.ToLower().Contains("exit"))
            return;

        revLanguage ??= "en";
        if (revLanguage == "")
            revLanguage = "en";

        Console.WriteLine("What would you like to search? can be empty but preferably not,'|' = OR, '-' = NOT example: 'boating|sailing'");
        searchQ = Console.ReadLine();
        if (searchQ.ToLower().Contains("exit"))
            return;

        searchQ ??= " ";
        if (searchQ == "")
            searchQ = " ";

        Console.WriteLine("Video, Channel or Playlist? default is video.");
        searchType = Console.ReadLine().ToLower();
        if (searchType.ToLower().Contains("exit"))
            return;

        searchType ??= "video";
        if (searchType == "")
            searchType = "video";

        Console.WriteLine("Which channel? can be left empty.");
        channelId = Console.ReadLine();
        if (channelId.ToLower().Contains("exit"))
            return;

        if (channelId == "")
            channelId = null;
        else
        {
            var slrs = youtubeService.Search.List("snippet");
            slrs.Q = channelId;
            slrs.MaxResults = 1;
            slrs.Type = "channel";

            var slr = await slrs.ExecuteAsync();

            var vds = new List<string>();
            foreach (var item in slr.Items)
            {
                vds.Add(item.Id.ChannelId);
            }

            if (vds.Count == 0)
            {
                Console.WriteLine("No Channel found.");
                return;
            }
            else
            {
                channelId = vds[0];
            }
        }
    }

    var searchListRequest = youtubeService.Search.List("snippet");
    searchListRequest.RelevanceLanguage = revLanguage;
    searchListRequest.Q = searchQ;
    searchListRequest.MaxResults = 50;
    searchListRequest.Type = searchType;
    searchListRequest.ChannelId = channelId;

    var searchListResponse = await searchListRequest.ExecuteAsync();

    var videoIds = new List<string>();
    var items = new List<SearchResult>();

    foreach (var item in searchListResponse.Items)
    {
        items.Add(item);
    }

    if (items.Count == 0)
    {
        Console.WriteLine("No videos found.");
        return;
    }

    var random = new Random();
    SearchResult chosenItem = items[random.Next(items.Count)];
    string url = $"https://www.youtube.com/watch?v={chosenItem.Id.VideoId}";
    string title = chosenItem.Snippet.Title;
    string channelTitle = chosenItem.Snippet.ChannelTitle;

    Console.WriteLine($"Playing: {title}\n\nby: {channelTitle}\n");

    Process.Start(new ProcessStartInfo
    {
        FileName = url,
        UseShellExecute = true
    });

    Console.WriteLine("Proceed?\n(Y/n)");
    string proceed = Console.ReadLine();

    if (proceed.ToLower().Contains("n") || proceed.ToLower().Contains("exit"))
        return;

}