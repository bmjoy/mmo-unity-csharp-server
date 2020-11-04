using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    // 온갖 투사체 종류중에 하나일뿐
    public class Arrow : Projectile
    {
        public GameObject Owner { get; set; }

        long _nextMoveTick = 0;

        // 무조건 게임 전체 틱을 따라갈 이유가 없다
        public override void Update()
        {
            if (Owner == null || Room == null)
                return;

            if (_nextMoveTick >= Environment.TickCount64)
                return;

            // 틱돌아옴
            _nextMoveTick = Environment.TickCount64 + 50;

            Vector2Int destPos = GetFrontCellPos(); // 내가 진행하는 방향의 바로 앞 좌표
            if (Room.Map.CanGo(destPos))
            {
                CellPos = destPos;

                S_Move movePacket = new S_Move();
                movePacket.ObjectId = Id;
                movePacket.PosInfo = PosInfo;
                Room.Broadcast(movePacket);

                Console.WriteLine("Move Arrow");
            }
            else
            {
                // 화살이 가다 막힘
                GameObject target = Room.Map.Find(destPos);
                // 벽이 아니고 게임 캐릭터였으면?
                if(target != null)
                {
                    // 아야
                }

                // 벽쾅 -> 소멸
                Room.LeaveGame(Id); // 화살이 떠납니다
            }
        }
    }
}
