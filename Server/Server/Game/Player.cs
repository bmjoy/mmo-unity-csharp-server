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

        // Room이나 Info의 get, set에 lock을 걸면 괜찮지 않나 싶은데
        // 위의 것들을 get 하는순간 참조를 가져오는거기 때문에 잠궈봤자 그거 무시하고 접근가능
        // 걍 null 체크하고 조심하자
        public PlayerInfo Info { get; set; } = new PlayerInfo() { PosInfo = new PositionInfo() };
        public GameRoom Room { get; set; } // 들어가있는 게임룸
        public ClientSession Session { get; set; } // 뭔가 보내려면 클라이언트 세션을 알고 있는게 좋다

    }
}
