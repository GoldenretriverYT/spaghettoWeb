using spaghetto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace spaghettoWeb
{
    internal class SessionDB
    {
        public Dictionary<string, SessionData> SessionData { get; set; } = new();

        public bool TryGet(string session, string key, out string data)
        {
            if (!SessionData.TryGetValue(session, out SessionData sessionData))
            {
                data = null;
                return false;
            }

            if (!sessionData.Data.TryGetValue(key, out data)) return false;
            return true;
        }

        public bool TrySet(string session, string key, string data)
        {
            if (!SessionData.TryGetValue(session, out SessionData sessionData)) return false;
            sessionData.Data[key] = data;
            SaveSessionDB();
            return true;
        }

        public void SaveSessionDB()
        {
            File.WriteAllText("data/sessiondb.json", JsonConvert.SerializeObject(this));
        }
    }

    internal class SessionData
    {
        public Dictionary<string, string> Data { get; set; } = new();

        public SessionData()
        {

        }
    }
}
