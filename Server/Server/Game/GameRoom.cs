using Google.Protobuf;
using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    public class GameRoom
    {
        object _lock = new object();
        public int RoomId { get; set; }

        // 그리드 단위로 갖고있어도 된다.
        List<Player> _players = new List<Player>();

        public void EnterGame(Player newPlayer)
        {
            if (newPlayer == null)
                return;

            // 리스트를 건드는 부분은 락을 걸어놓자
            lock (_lock)
            {
                _players.Add(newPlayer);
                newPlayer.Room = this;

                // 본인에게 정보 전송
                {
                    S_EnterGame enterPacket = new S_EnterGame();

                    // 패킷 보낼때마다 new 해서 Player 만들어줘도 되지만
                    // 받는쪽에서는 Player 자체가 아니라 그 플레이어의 정보만 궁금함
                    enterPacket.Player = newPlayer.Info;
                    newPlayer.Session.Send(enterPacket); // 나에게 내 정보 전송

                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (Player p in _players)
                    {
                        // 내 정보는 위에서 보냈으니, 내가 아닌 플레이어들의 정보만 담는다
                        if (newPlayer != p)
                            spawnPacket.Players.Add(p.Info);
                    }
                    newPlayer.Session.Send(spawnPacket); // 나에게 현재 게임에 있는 타인의 정보를 알린다.
                }
                // 타인에게 정보 전송
                {
                    S_Spawn spawnPacket = new S_Spawn();
                    spawnPacket.Players.Add(newPlayer.Info);
                    // 신규유저의 정보를 기존에 접속중인 모두에게 알림
                    foreach (Player p in _players)
                    {
                        if (newPlayer != p)
                            p.Session.Send(spawnPacket);
                    }
                }
            }
        }

        public void LeaveGame(int playerId)
        {
            lock (_lock)
            {
                Player player = _players.Find(p => p.Info.PlayerId == playerId);
                if (player == null)
                    return;

                _players.Remove(player);
                player.Room = null;

                // 본인에게 정보 전송
                {
                    S_LeaveGame leavePacket = new S_LeaveGame();
                    player.Session.Send(leavePacket);
                }
                // 타인에게 정보 전송
                {
                    // 나갔다고 알려줄 패킷 생성
                    S_Despawn despawnPacket = new S_Despawn();
                    despawnPacket.PlayerIds.Add(player.Info.PlayerId);
                    foreach (Player p in _players)
                    {
                        if(player != p)
                            p.Session.Send(despawnPacket);
                    }
                }
            }
        }
        public void Broadcast(IMessage packet)
        {
            lock (_lock)
            {
                // 나중에는 lock대신 job개념으로 할거임..
                foreach (Player player in _players)
                {
                    // 접속한 유저들에게 패킷 뿌려주셈
                    player.Session.Send(packet);
                }
            }
        }
    }
}
