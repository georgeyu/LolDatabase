using log4net;
using log4net.Config;
using LolApi;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using static LolApi.Api;

namespace LolDatabase
{
    internal class Program
    {
        private const int SafeExit = 0;
        private const string Path = "Config.txt";
        private static readonly MethodBase constructor = MethodBase.GetCurrentMethod();
        private static readonly ILog log = LogManager.GetLogger(constructor.DeclaringType);
        private static Api api;
        private static LolDbContext db;
        private static string[] args;

        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            XmlConfigurator.Configure();
            Program.args = args;
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
            var key = File.ReadAllText(Path);
            using (var client = new WebClient())
            using (db = new LolDbContext())
            {
                api = new Api(client, Region.NA, key);
                switch (command)
                {
                    case "account":
                        HandleAccountCommand();
                        break;
                    case "champions":
                        HandleChampionsCommand();
                        break;
                    case "matches":
                        HandleMatchesCommand();
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

        private static void HandleAccountCommand()
        {
            var name = args[1];
            var summoner = api.GetSummoner(name);
            db.Summoners.Add(summoner);
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

        private static void HandleMatchesCommand()
        {
            var name = args[1];
            var matchList = api.GetRankedRiftMatchList(name);
            log.InfoFormat("{0} matches.", matchList.matches.Count);
            var matchesToAdd = matchList.matches.Where(x => !db.Matches.Any(y => x.platformId == y.platformId && x.gameId == y.gameId));
            var matchesToAddCount = matchesToAdd.Count();
            log.InfoFormat("{0} matches to add.", matchesToAddCount);
            foreach (var matchToAdd in matchesToAdd)
            {
                var matchID = matchToAdd.gameId.ToString();
                var match = api.GetMatch(matchID);
                db.Matches.Add(match);
            }
        }
    }
}
