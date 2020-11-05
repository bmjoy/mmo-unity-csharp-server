using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Data
{
    #region Stat
    //[Serializable]
    //public class Stat
    //{
    //    public int level;
    //    public int maxHp;
    //    public int attack;
    //    public int totalExp;
    //}

    [Serializable]
    public class StatData : ILoader<int, StatInfo>
    {
        public List<StatInfo> stats = new List<StatInfo>();

        public Dictionary<int, StatInfo> MakeDict()
        {
            Dictionary<int, StatInfo> dict = new Dictionary<int, StatInfo>();
            foreach (StatInfo stat in stats)
            {
                stat.Hp = stat.MaxHp; // 초기 피는 만피로
                dict.Add(stat.Level, stat);
            }
            return dict;
        }
    }
    #endregion

    // 스킬을 데이터만으로 표현하기는 매우 어려운 편이고 기획에 따라 많이 바뀐다
    // 데이터 모델링
    #region Skill
    [Serializable]
    public class Skill
    {
        public int id;
        public string name; // 개발단계에서 구별하기 위한 이름 or 실제 스킬이름.. 어쨌든 지어놓으면 편하다
        public float cooldown;
        public int damage;
        public SkillType skillType; // enum값을 파싱가능? -> enum의 이름 매칭이 되면 
        public ProjectileInfo projectile;
    }

    public class ProjectileInfo
    {
        public string name; 
        public float speed;
        public int range; // 최대 도달 거리
        public string prefab; // 유니띠용
    }

    [Serializable]
    public class SkillData : ILoader<int, Skill>
    {
        public List<Skill> skills = new List<Skill>();

        public Dictionary<int, Skill> MakeDict()
        {
            Dictionary<int, Skill> dict = new Dictionary<int, Skill>();
            foreach (Skill skill in skills)
                dict.Add(skill.id, skill);
            return dict;
        }
    }
    #endregion
}
