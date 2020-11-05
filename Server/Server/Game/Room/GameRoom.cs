using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
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
        // 각 오브젝트 타입별로 따로따로 딕셔너리를 만들던지
        // 딕셔너리 하나에 다 때려넣던지
        // 분리해서 관리하는게 나중에 브로드캐스팅 할때 편함. -> 빠름
        Dictionary<int, Player> _players = new Dictionary<int, Player>();
        Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
        Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();
        public Map Map { get; private set; } = new Map();

        public void Init(int mapId)
        {
            Map.LoadMap(mapId);
        }

        // 클라는 1초당 120프렘정도 업댓함
        // 서버는 1초당 10번정도면 충분하다
        // 이거 누가 호출? -> 메인에서 돌리고 있다
        public void Update()
        {
            lock (_lock)
            {
                foreach (Projectile projectile in _projectiles.Values)
                {
                    projectile.Update();
                }
            }
        }

        public void EnterGame(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

            // 리스트를 건드는 부분은 락을 걸어놓자
            lock (_lock)
            {
                // 각 오브젝트 타입별 처리
                if(type == GameObjectType.Player)
                {
                    Player player = gameObject as Player;
                    _players.Add(gameObject.Id, player); // 해당하는 아이디와 플레이어를 딕셔너리에 삽입
                    player.Room = this;

                    // 본인에게 스폰됐다는 정보 전송
                    {
                        S_EnterGame enterPacket = new S_EnterGame();

                        // 패킷 보낼때마다 new 해서 Player 만들어줘도 되지만
                        // 받는쪽에서는 Player 자체가 아니라 그 플레이어의 정보만 궁금함
                        enterPacket.Player = player.Info;
                        player.Session.Send(enterPacket); // 나에게 내 정보 전송

                        S_Spawn spawnPacket = new S_Spawn();
                        foreach (Player p in _players.Values)
                        {
                            // 내 정보는 위에서 보냈으니, 내가 아닌 플레이어들의 정보만 담는다
                            if (player != p)
                                spawnPacket.Objects.Add(p.Info);
                        }
                        player.Session.Send(spawnPacket); // 나에게 현재 게임에 있는 타인의 정보를 알린다.
                    }
                }
                else if (type == GameObjectType.Monster)
                {
                    Monster monster = gameObject as Monster;
                    _monsters.Add(gameObject.Id, monster);
                    monster.Room = this;
                }
                else if (type == GameObjectType.Projectile) // 투사체
                {
                    Projectile projectile = gameObject as Projectile;
                    _projectiles.Add(gameObject.Id, projectile);
                    projectile.Room = this;
                }


                // 타인에게 새로운 오브젝트가 스폰됐음을 알림
                {
                    S_Spawn spawnPacket = new S_Spawn();
                    spawnPacket.Objects.Add(gameObject.Info);
                    // 신규유저의 정보를 기존에 접속중인 모두에게 알림
                    foreach (Player p in _players.Values)
                    {
                        // 본인의 정보 (MyPlayer)는 S_EnterGame 패킷으로 받기로 했기 때문이기도 하다.
                        if (p.Id != gameObject.Id) // 위에서 생성한게 나 자신이 아닐때만 알림 -> 나 자신일때는 이미 위에서 알렸음
                            p.Session.Send(spawnPacket);
                    }
                }
            }
        }

        public void LeaveGame(int objectId)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(objectId); // objectId를 통해 타입 추출

            lock (_lock)
            {
                // 각 오브젝트 타입별 처리
                if (type == GameObjectType.Player)
                {
                    Player player = null;
                    if (_players.Remove(objectId, out player) == false) // 딕셔너리에서 지우고
                        return; // 지울 플레이어가 없다.

                    player.Room = null; // player에 지정된 룸을 밀고
                    // 맵에다가도 내가 나갔음을 처리
                    Map.ApplyLeave(player);

                    // 본인에게 정보 전송
                    {
                        S_LeaveGame leavePacket = new S_LeaveGame();
                        player.Session.Send(leavePacket);
                    }
                }
                else if (type == GameObjectType.Monster)
                {
                    Monster monster = null;
                    if (_monsters.Remove(objectId, out monster) == false)
                        return;

                    monster.Room = null;
                    Map.ApplyLeave(monster);
                }
                else if (type == GameObjectType.Projectile)
                {
                    Projectile projectile = null;
                    if (_projectiles.Remove(objectId, out projectile) == false)
                        return; // 못 찾음

                    // 찾음
                    projectile.Room = null;
                    // 화살은 충돌대상이 아니라 맵에다가 알리지는 않았음
                }

                // 타인에게 정보 전송
                {
                    // 누군가가 나갔다고 알려줄 패킷 생성
                    S_Despawn despawnPacket = new S_Despawn();
                    despawnPacket.ObjectIds.Add(objectId);
                    foreach (Player p in _players.Values)
                    {
                        if(p.Id != objectId) // 자기자신 빼고 모두에게 알림
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
                ObjectInfo info = player.Info; // 이동하기 전의 플레이어 좌표

                // 다른 좌표로 이동할 경우, 갈 수 있는지 체크
                if(movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
                {
                    if (Map.CanGo(new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)) == false)
                        return; // 플레이어가 가려고 요청한 곳에 갈 수 있는지를 실제 맵 데이터와 대조
                }

                // 좌표가 바뀌지 않고 상태만 바뀌었을 경우 아래만 실행
                info.PosInfo.State = movePosInfo.State;
                info.PosInfo.MoveDir = movePosInfo.MoveDir;
                // 이동하는 부분을 GameRoom에서 하지않고 map 클래스에서 처리
                // 왜냐면 map이 들고있는 플레이어 좌표 배열에 가서 기존 위치를 null 처리하고
                // 이동시켜야 하기 떄문에 map클래스 안에서 하는게 편하다
                Map.ApplyMove(player, new Vector2Int(movePosInfo.PosX, movePosInfo.PosY)); // 맵에다가 나를 이동시켜달라 요청
                
                // 다른 플레이어에게도 알려준다
                S_Move resMovePacket = new S_Move();
                resMovePacket.ObjectId = player.Info.ObjectId; // 움직이는 사람의 id
                resMovePacket.PosInfo = movePacket.PosInfo;

                Broadcast(resMovePacket); // 들어와있는 모든 유저에게 알림
            }
        }

        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            if (player == null) // player가 지금 GameRoom에 소속되었는지도 체크
                return;
            
            // 스킬분기처리.. 몇 개 없다는 가정하에
            // 스킬이 많아지면 Skill 클래스를 따로 파서 플레이어에 주입하는게 낫다
            lock (_lock)
            {
                ObjectInfo info = player.Info;
                if (info.PosInfo.State != CreatureState.Idle)
                    return;

                // 스킬 사용 가능 여부 체크
                
                // 스킬 애니메이션 맞춰주는 부분
                info.PosInfo.State = CreatureState.Skill;
                S_Skill skill = new S_Skill() { Info = new SkillInfo() }; // info도 클래스임
                skill.ObjectId = info.ObjectId;
                skill.Info.SkillId = skillPacket.Info.SkillId; // 나중에 시트로 뺄거야
                Broadcast(skill); // 에코서버마냥 전파한다

                // 이제 DB에서 Id에 해당하는 스킬데이터를 뽑아온다
                Data.Skill skillData = null;
                if (DataManager.SkillDict.TryGetValue(skillPacket.Info.SkillId, out skillData) == false)
                    return;

                // id에 해당하는 스킬이 있다
                switch (skillData.skillType)
                {
                    case SkillType.SkillAuto:
                        {
                            // 주먹질
                            // 데미지 판정 -> 항상 치팅 대비
                            // 내 공격방향에 적이 있나 없나 체크
                            // GetFrontCellPos에 MoveDir.None 처리가 없으니 
                            // 항상 공격자의 위치를 반환해서 아무대나 떄려도 타격이 되는 문제가 있었음
                            // MoveDir.None이 그냥 키입력 여부를 받는거라 서버에는 필요없으므로 전체적으로 없애기로함
                            Vector2Int skillPos = player.GetFrontCellPos(info.PosInfo.MoveDir);
                            GameObject target = Map.Find(skillPos);
                            if (target != null)
                            {
                                Console.WriteLine("Hit GameObject!");
                            }
                        }
                        break;
                    case SkillType.SkillProjectile:
                        {
                            // 화살 생성
                            // 클라에서도 화살이 날아가는것을 계산하고 있어야 치팅을 방지할수 있다.
                            // 어떤 투사체인지 구분할수 있는게 필요 (지금은 화살만..)
                            Arrow arrow = ObjectManager.Instance.Add<Arrow>();
                            if (arrow == null)
                                return;

                            // 데이터 대입
                            arrow.Owner = player;
                            arrow.Data = skillData; // 투사체를 생성한 주체(스킬)의 정보를 저장
                            arrow.PosInfo.State = CreatureState.Moving;
                            arrow.PosInfo.MoveDir = player.PosInfo.MoveDir;
                            arrow.PosInfo.PosX = player.PosInfo.PosX;
                            arrow.PosInfo.PosY = player.PosInfo.PosY;
                            arrow.Speed = skillData.projectile.speed;
                            EnterGame(arrow); // 치팅방지 + 코드재사용(화살이 생성됐음을 모두에게 알림)
                        }
                        break;
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
