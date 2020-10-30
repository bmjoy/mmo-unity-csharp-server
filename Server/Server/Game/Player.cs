using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class Player
    {
        // 플레이어 정보
        // PlayerInfo 안에 있는 PositionInfo도 new 해야함
        public PlayerInfo Info { get; set; } = new PlayerInfo() { PosInfo = new PositionInfo() };
        public GameRoom Room { get; set; } // 들어가있는 게임룸
        public ClientSession Session { get; set; } // 뭔가 보내려면 클라이언트 세션을 알고 있는게 좋다

    }
}
