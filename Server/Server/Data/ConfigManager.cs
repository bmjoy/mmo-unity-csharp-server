using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// 서버에서 쓰는 파일들의 경로 저장

namespace Server.Data
{
    [Serializable]
    public class ServerConfig
    {
        public string dataPath;
        public string connectionString;
    }

    public class ConfigManager
    {
        public static ServerConfig Config { get; private set; }
        public static void LoadConfig()
        {
            // config 파일은 보통 실행파일과 동일한 위치에 넣어놔서 인자로 안받는 경우가 많다고 함
            string text = File.ReadAllText("config.json"); 
            Config = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerConfig>(text);
        }
    }
}
