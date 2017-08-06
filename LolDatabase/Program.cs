using log4net;
using System.Reflection;
using System.Data.Entity;
using LolApi;
using log4net.Config;
using System.Net;
using System.IO;
using static LolApi.Api;
using System.Linq;

namespace LolDatabase
{
    internal class Program
    {
        private const string Path = "Config.txt";
        private static readonly MethodBase constructor = MethodBase.GetCurrentMethod();
        private static readonly ILog log = LogManager.GetLogger(constructor.DeclaringType);

        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            using (var client = new WebClient())
            using (var db = new LolDbContext())
            {
                var key = File.ReadAllText(Path);
                var api = new Api(client, Region.NA, key);
            }
        }
    }
}
