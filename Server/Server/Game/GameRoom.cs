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

        // 이제 id를 통해 플레이어를 빠르게 찾는다
        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        Map _map = new Map();

        public void Init(int mapId)
        {
            _map.LoadMap(mapId);
        }

        public void EnterGame(Player newPlayer)
        {
            if (newPlayer == null)
                return;

            // 리스트를 건드는 부분은 락을 걸어놓자
            lock (_lock)
            {
                _players.Add(newPlayer.Info.PlayerId, newPlayer);
                newPlayer.Room = this;

                // 본인에게 정보 전송
                {
                    S_EnterGame enterPacket = new S_EnterGame();

                    // 패킷 보낼때마다 new 해서 Player 만들어줘도 되지만
                    // 받는쪽에서는 Player 자체가 아니라 그 플레이어의 정보만 궁금함
                    enterPacket.Player = newPlayer.Info;
                    newPlayer.Session.Send(enterPacket); // 나에게 내 정보 전송

                    S_Spawn spawnPacket = new S_Spawn();
                    foreach (Player p in _players.Values)
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
                    foreach (Player p in _players.Values)
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
                Player player = null;
                if (_players.Remove(playerId, out player) == false)
                    return; // 지울 플레이어가 없다.

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
                    foreach (Player p in _players.Values)
                    {
                        if(player != p)
                            p.Session.Send(despawnPacket);
                    }
                }
            }
        }

        public void HandleMove(Player player, C_Move movePacket)
        {
            if (player == null)
                return;

            // 정보수정은 한번에 한쓰레드만
            lock (_lock)
            {
                // 검증
                PositionInfo movePosInfo = movePacket.PosInfo; // 이동하려는 목적지 좌표
                PlayerInfo info = player.Info; // 이동하기 전의 플레이어 좌표

                // 다른 좌표로 이동할 경우, 갈 수 있는지 체크
                if(movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
                {
                    if (_map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
                        return; // 플레이어가 가려고 요청한 곳에 갈 수 있는지를 실제 맵 데이터와 대조
                }

                // 좌표가 바뀌지 않고 상태만 바뀌었을 경우 아래만 실행
                info.PosInfo.State = movePosInfo.State;
                info.PosInfo.MoveDir = movePosInfo.MoveDir;
                // 이동하는 부분을 GameRoom에서 하지않고 map 클래스에서 처리
                // 왜냐면 map이 들고있는 플레이어 좌표 배열에 가서 기존 위치를 null 처리하고
                // 이동시켜야 하기 떄문에 map클래스 안에서 하는게 편하다
                _map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY));
                
                // 다른 플레이어에게도 알려준다
                S_Move resMovePacket = new S_Move();
                resMovePacket.PlayerId = player.Info.PlayerId; // 움직이는 사람의 id
                resMovePacket.PosInfo = movePacket.PosInfo;

                Broadcast(resMovePacket); // 들어와있는 모든 유저에게 알림
            }
        }

        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            if (player == null) // player가 지금 GameRoom에 소속되었는지도 체크
                return;

            lock (_lock)
            {
                PlayerInfo info = player.Info;
                if (info.PosInfo.State != CreatureState.Idle)
                    return;

                // 스킬 사용 가능 여부 체크

                // 통과
                info.PosInfo.State = CreatureState.Skill;

                S_Skill skill = new S_Skill() { Info = new SkillInfo() }; // info도 클래스임
                skill.PlayerId = info.PlayerId;
                skill.Info.SkillId = 1; // 나중에 시트로 뺄거야
                Broadcast(skill);

                // 데미지 판정 -> 항상 치팅 대비
                // 내 공격방향에 적이 있나 없나 체크
                // GetFrontCellPos에 MoveDir.None 처리가 없으니 
                // 항상 공격자의 위치를 반환해서 아무대나 떄려도 타격이 되는 문제가 있었음
                // MoveDir.None이 그냥 키입력 여부를 받는거라 서버에는 필요없으므로 전체적으로 없애기로함
                Vector2Int skillPos = player.GetFrontCellPos(info.PosInfo.MoveDir);
                Player target = _map.Find(skillPos);
                if(target != null)
                {
                    Console.WriteLine("Hit Player!");
                }
            }
        }

        public void Broadcast(IMessage packet)
        {
            lock (_lock)
            {
                // 나중에는 lock대신 job개념으로 할거임..
                foreach (Player player in _players.Values)
                {
                    // 접속한 유저들에게 패킷 뿌려주셈
                    player.Session.Send(packet);
                }
            }
        }
    }
}
