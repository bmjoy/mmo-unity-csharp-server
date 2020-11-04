using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class Projectile  : GameObject
    {
        public Data.Skill Data { get; set; } // 나를 생성한 스킬에 대한 데이타

        public Projectile()
        {
            ObjectType = GameObjectType.Projectile;
        }

        public virtual void Update()
        {

        }
    }
}
