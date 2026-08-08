using LateToTheParty.Configuration;
using LateToTheParty.Helpers;
using LateToTheParty.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using System.Reflection;

namespace LateToTheParty.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class ConfigUtil
    {
        private const string FILENAME_CONFIG = "config.json";
        private const string FILENAME_LOOTRANKINGDATA = "loot_ranking.json";

        protected virtual string ConfigFileDirectory => ServerModDirectory;

        private string _serverModDirectory = null!;
        public string ServerModDirectory
        {
            get
            {
                if (_serverModDirectory == null)
                {
                    _serverModDirectory = GetServerModDirectory();
                }

                return _serverModDirectory;
            }
        }

        private ModConfig _currentConfig = null!;
        public ModConfig CurrentConfig
        {
            get
            {
                if (_currentConfig == null)
                {
                    _currentConfig = GetObject<ModConfig>(FILENAME_CONFIG);
                }

                return _currentConfig;
            }
        }

        private Dictionary<string, LootRankingDataConfig> _lootRankingData = null!;
        public Dictionary<string, LootRankingDataConfig> LootRankingData
        {
            get
            {
                if (_lootRankingData == null)
                {
                    if (LootRankingDataExists)
                    {
                        _lootRankingData = GetObject<Dictionary<string, LootRankingDataConfig>>(FILENAME_LOOTRANKINGDATA);
                    }
                    else
                    {
                        _lootRankingData = new Dictionary<string, LootRankingDataConfig>();
                    }
                }

                return _lootRankingData;
            }
            set
            {
                _lootRankingData = value;
                WriteObjectToFile(_lootRankingData, FILENAME_LOOTRANKINGDATA);
            }
        }

        public bool LootRankingDataExists => File.Exists(Path.Combine(ConfigFileDirectory, FILENAME_LOOTRANKINGDATA));

        private ModHelper _modHelper;

        public ConfigUtil(ModHelper modHelper)
        {
            _modHelper = modHelper;
        }

        private string GetServerModDirectory()
        {
            return _modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        }

        private T GetObject<T>(string filename)
        {
            string fileText = File.ReadAllText(Path.Combine(ConfigFileDirectory, filename));
            T? obj = ConfigHelpers.DeserializeAndInitializeMissingFields<T>(fileText);
            if (obj == null)
            {
                throw new InvalidOperationException($"Could not deserialize {filename}");
            }

            return obj;
        }

        private void WriteObjectToFile<T>(T obj, string filename)
        {
            if (obj == null)
            {
                throw new InvalidOperationException($"Could not serialize null object to {filename}");
            }

            string fileText = ConfigHelpers.SerializePretty(obj);
            File.WriteAllText(Path.Combine(ConfigFileDirectory, filename), fileText);
        }
    }
}
