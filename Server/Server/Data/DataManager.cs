using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Data
{
    public interface ILoader<Key, Value>
    {
        Dictionary<Key, Value> MakeDict();
    }

    public class DataManager
    {
        // 외부에서 불러오려고 static
        public static Dictionary<int, Data.Stat> StatDict { get; private set; } = new Dictionary<int, Data.Stat>();
        public static Dictionary<int, Data.Skill> SkillDict { get; private set; } = new Dictionary<int, Data.Skill>();

        public static void LoadData()
        {
            StatDict = LoadJson<Data.StatData, int, Data.Stat>("StatData").MakeDict();
            SkillDict = LoadJson<Data.SkillData, int, Data.Skill>("SkillData").MakeDict();
        }

        // 클라와 동일한 데이터 읽어오기
        static Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
        {
            // JSON 라이브러리는 따로 갖다 써야함 -> Newtonsoft Json
            // 유니티랑 데이터 경로가 다름 + 서버는 실행되는 위치가 일정하지 않음
            // 그래서 경로 자체도 설정 파일로 빼놓는다 ConfigManager
            // 유니티와 다르게 경로 끝에 .json까지 붙여놔야함
            string text = File.ReadAllText($"{ConfigManager.Config.dataPath}/{path}.json"); 
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Loader>(text);
        }
    }
}
