using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class Player : GameObject
    {
        // 플레이어 정보
        public ClientSession Session { get; set; } // 뭔가 보내려면 클라이언트 세션을 알고 있는게 좋다

        public Player()
        {
            ObjectType = GameObjectType.Player; // 오브젝트 타입을 Player로 변경
            Speed = 20.0f; // 시트로 빠지겠지만
        }

        public override void OnDamaged(GameObject attacker, int damage)
        {
            Console.WriteLine($"Damage! : {damage}");
        }
    }
}
