using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class Player
    {
        public PlayerInfo Info { get; set; } = new PlayerInfo(); // 정보
        public GameRoom Room { get; set; } // 들어가있는 게임룸
        public ClientSession Session { get; set; } // 뭔가 보내려면 클라이언트 세션을 알고 있는게 좋다

    }
}
