using log4net;
using log4net.Config;
using LolApi;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using static LolApi.Api;
using System.Collections.Generic;

namespace LolDatabase
{
    internal class Program
    {
        private const int SafeExit = 0;
        private const int ConfigKeyLine = 0;
        private const int ConfigNamesLine = 1;
        private const string Path = "Config.txt";
        private const string DefaultFlag = "-d";
        private static readonly MethodBase constructor = MethodBase.GetCurrentMethod();
        private static readonly ILog log = LogManager.GetLogger(constructor.DeclaringType);
        private static Api api;
        private static LolDbContext db;

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            XmlConfigurator.Configure();
            if (args.Length == 0)
            {
                Console.WriteLine("usage: loldb <command> [<args>]");
                Console.WriteLine();
                Console.WriteLine("    account     Archive account ID by summoner name");
                Console.WriteLine("    champions   Archive champion IDs");
                Console.WriteLine("    matches     Archive ranked matches by summoner name");
                return;
            }
            var command = args[0];
            var config = File.ReadAllLines(Path);
            var key = config[ConfigKeyLine];
            using (var client = new WebClient())
            using (db = new LolDbContext())
            {
                api = new Api(client, Region.NA, key);
                var commandArgs = args.Skip(1);
                switch (command)
                {
                    case "account":
                        HandleAccountCommand(commandArgs);
                        break;
                    case "champions":
                        HandleChampionsCommand();
                        break;
                    case "matches":
                        HandleMatchesCommand(commandArgs);
                        break;
                    default:
                        Console.WriteLine("{0} command doesn't exist.", command);
                        return;
                }
                db.SaveChanges();
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var eString = e.ExceptionObject.ToString();
            log.Info(eString);
            Environment.Exit(SafeExit);
        }

        private static void HandleAccountCommand(IEnumerable<string> args)
        {
            var name = args.First();
            var summoner = api.GetSummoner(name);
            var count = db.Summoners.Where(x => x.accountId == summoner.accountId && x.id == summoner.id).Count();
            if (count == 0)
            {
                db.Summoners.Add(summoner);
            }
        }

        private static void HandleChampionsCommand()
        {
            var champions = api.GetChampions();
            var championsToAdd = champions.Where(x => !db.Champions.Any(y => x.Id == y.Id));
            var championsToAddCount = championsToAdd.Count();
            log.InfoFormat("{0} champions to add.", championsToAddCount);
            foreach (var championToAdd in championsToAdd)
            {
                db.Champions.Add(championToAdd);
            }
        }

        private static void HandleMatchesCommand(IEnumerable<string> args)
        {
            var firstArg = args.First();
            var splitArgs = firstArg.Split(',');
            var config = File.ReadAllLines(Path);
            var defaultNames = config[ConfigNamesLine];
            var defaultNamesSplit = defaultNames.Split(',');
            var names = firstArg == DefaultFlag ? defaultNamesSplit : splitArgs;
            var matches = new List<MatchSummary>();
            foreach (var name in names)
            {
                var matchLists = api.GetRankedRiftMatchLists(name);
                foreach (var matchList in matchLists)
                {
                    log.InfoFormat("{0} matches for {1}.", matchList.matches.Count, name);
                    var nameMatches = matchList.matches.Where(x => !db.Matches.Any(y => x.platformId == y.platformId && x.gameId == y.gameId));
                    var nameMatchesCount = nameMatches.Count();
                    log.InfoFormat("{0} matches to add for {1}.", nameMatchesCount, name);
                    matches.AddRange(nameMatches);
                }
            }
            foreach (var matchSummary in matches)
            {
                var matchID = matchSummary.gameId.ToString();
                try
                {
                    var match = api.GetMatch(matchID);
                    db.Matches.Add(match);
                }
                catch (WebException)
                {
                    log.WarnFormat("Failed to get match {0}.", matchID);
                }
            }
        }
    }
}
