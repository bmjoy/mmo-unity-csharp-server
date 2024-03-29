﻿using Google.Protobuf;
using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;

namespace Server.Game
{
    // 기존 lock 대신 JobSerializer를 왜 썼는지 생각해보자 -> 모든 함수마다 lock걸어버리면 경합이 심해져서 싱글쓰레드와 별 차이가 없어짐.
    // JobSerializer를 컴포넌트로 갖고있는게 낫지 않나
    // GameRoom에서 하는 모든 작업은 즉시 실행됨을 보장하지 않는다.
    public class GameRoom : JobSerializer
    {
        private object _lock = new object();
        public int RoomId { get; set; }

        // 이제 id를 통해 플레이어를 빠르게 찾는다
        // 각 오브젝트 타입별로 따로따로 딕셔너리를 만들던지
        // 딕셔너리 하나에 다 때려넣던지
        // 분리해서 관리하는게 나중에 브로드캐스팅 할때 편함. -> 빠름
        private Dictionary<int, Player> _players = new Dictionary<int, Player>(); // 플레이어 목록

        private Dictionary<int, Monster> _monsters = new Dictionary<int, Monster>();
        private Dictionary<int, Projectile> _projectiles = new Dictionary<int, Projectile>();
        public Map Map { get; private set; } = new Map();

        public void Init(int mapId)
        {
            Map.LoadMap(mapId);

            // 임시 : 몬스터 생성
            Monster monster = ObjectManager.Instance.Add<Monster>();
            monster.CellPos = new Vector2Int(5, 5);
            EnterGame(monster);

            // TestTimer();
        }
        
        // Test
        //void TestTimer()
        //{
        //    Console.WriteLine("TestTimer");
        //    PushAfter(100, TestTimer); // JobTimer에 자신을 밀어넣음.
        //}


        // 클라는 1초당 120프렘정도 업댓함
        // 서버는 1초당 10번정도면 충분하다
        // 이거 누가 호출? -> 메인에서 돌리고 있다
        // 누군가가 주기적으로 호출해줘야 하는 함수 -> Flush하기 좋음
        // Update 호출 방식이 바뀌어서 기본적으로 50ms의 딜레이가 생겼다. 딜레이는 처음에 설정 가능
        public void Update()
        {
            foreach (Monster monster in _monsters.Values)
            {
                monster.Update();
            }

            foreach (Projectile projectile in _projectiles.Values)
            {
                projectile.Update();
            }

            Flush();
        }

        public void EnterGame(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            GameObjectType type = ObjectManager.GetObjectTypeById(gameObject.Id);

            // 각 오브젝트 타입별 처리
            if (type == GameObjectType.Player)
            {
                Player player = gameObject as Player;
                _players.Add(gameObject.Id, player); // 해당하는 아이디와 플레이어를 딕셔너리에 삽입
                player.Room = this;

                // 이거 안하고 스폰 후 가만히 있으면, 화살같은거 안박히는 버그남
                Map.ApplyMove(player, new Vector2Int(player.CellPos.x, player.CellPos.y)); // 맵에다가 나를 이동시켜달라 요청

                // 본인에게 스폰됐다는 정보 전송
                {
                    S_EnterGame enterPacket = new S_EnterGame();

                    // 패킷 보낼때마다 new 해서 Player 만들어줘도 되지만
                    // 받는쪽에서는 Player 자체가 아니라 그 플레이어의 정보만 궁금함
                    enterPacket.Player = player.Info;
                    player.Session.Send(enterPacket); // 나에게 내 정보 전송

                    // 입장한 플레이어에게
                    // 접속중인 다른 플레이어뿐만 아니라 몬스터나 투사체들의 정보도 던져줘야 한다.
                    S_Spawn spawnPacket = new S_Spawn();

                    foreach (Player p in _players.Values)
                    {
                        // 내 정보는 위에서 보냈으니, 내가 아닌 플레이어들의 정보만 담는다
                        if (player != p)
                            spawnPacket.Objects.Add(p.Info);
                    }

                    foreach (Monster m in _monsters.Values)
                        spawnPacket.Objects.Add(m.Info);

                    foreach (Projectile p in _projectiles.Values)
                        spawnPacket.Objects.Add(p.Info);

                    player.Session.Send(spawnPacket); // 나에게 현재 게임에 있는 타인의 정보를 알린다.
                }
            }
            else if (type == GameObjectType.Monster)
            {
                Monster monster = gameObject as Monster;
                _monsters.Add(gameObject.Id, monster);
                monster.Room = this;

                Map.ApplyMove(monster, new Vector2Int(monster.CellPos.x, monster.CellPos.y)); // 맵에다가 나를 이동시켜달라 요청
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

        public void LeaveGame(int objectId)
        {
            GameObjectType type = ObjectManager.GetObjectTypeById(objectId); // objectId를 통해 타입 추출

            // 각 오브젝트 타입별 처리
            if (type == GameObjectType.Player)
            {
                Player player = null;
                if (_players.Remove(objectId, out player) == false) // 딕셔너리에서 지우고
                    return; // 지울 플레이어가 없다.

                // room이 null이면 ApplyLeave 처리가 안됨
                Map.ApplyLeave(player); // 맵에다가도 내가 나갔음을 처리
                player.Room = null; // player에 지정된 룸을 밀고

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

                // Room이 null인지 체크하는것 때문에 순서 바꿈 -> 다른 작업자가 안햇갈릴수 있음?
                Map.ApplyLeave(monster);
                monster.Room = null;
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
                    if (p.Id != objectId) // 자기자신 빼고 모두에게 알림
                        p.Session.Send(despawnPacket);
                }
            }
        }

        public void HandleMove(Player player, C_Move movePacket)
        {
            if (player == null)
                return;

            // 검증
            PositionInfo movePosInfo = movePacket.PosInfo; // 이동하려는 목적지 좌표
            ObjectInfo info = player.Info; // 이동하기 전의 플레이어 좌표

            // 다른 좌표로 이동할 경우, 갈 수 있는지 체크
            if (movePosInfo.PosX != info.PosInfo.PosX || movePosInfo.PosY != info.PosInfo.PosY)
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

        // GameRoom의 JobSerilizer에 의해 실행되기 때문에 EnterGame같은거 직접 불러도 됨
        public void HandleSkill(Player player, C_Skill skillPacket)
        {
            if (player == null) // player가 지금 GameRoom에 소속되었는지도 체크
                return;

            // 스킬분기처리.. 몇 개 없다는 가정하에
            // 스킬이 많아지면 Skill 클래스를 따로 파서 플레이어에 주입하는게 낫다

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
                        // 직접 불러서 해도 전혀 문제없긴함 EnterGame(arrow)
                        Push(EnterGame, arrow); // 치팅방지 + 코드재사용(화살이 생성됐음을 모두에게 알림)
                    }
                    break;
            }
        }

        // Job이나 Task 방식은 보면 응답을 받을 필요가 없는 함수에만 쓰고 있다.
        // FindPlayer경우 호출한곳이 Monster 클래스인데, 이 게임에서 몬스터들은 GameRoom에서 관리를 하고 있고
        // 운좋게도 몬스터들의 Update까지 GameRoom에서 관리를 하고 있어서 응답이 느릴지언정 터질일은 없다
        // 그런데 GameRoom과 전혀 관계없는 ClientSession 같은대서 GameRoom을 받아다가 FindPlayer 호출을 하면 충돌이 날 위험이 있다.
        // 해결방법으로는 FindPlayer같은 즉시응답이 필요한 애들을 GameRoom과 관계없는대서 안쓰도록 하든지
        // 아니면 FindPlayer 자체를 GameRoom에서 실시간으로 꺼내오지 않도록 해야하는데-,-??
        public Player FindPlayer(Func<GameObject, bool> condition)
        {
            foreach (Player player in _players.Values)
            {
                if (condition.Invoke(player))
                    return player;
            }

            return null;
        }

        // Broadcast 사용하는 클래스들이 모두 GameRoom에 관계된 것들이라 지금까지는 문제가 없다..
        // GameRoom에 JobSerializer에 의해 실행되는 애들이 직접 부르는건 딱히 상관이 없다.
        // 그런데 위에처럼 전혀 관계없는대서 쓴다면 또 문제가 생길수가 있음
        public void Broadcast(IMessage packet)
        {
            // 나중에는 lock대신 job개념으로 할거임..
            foreach (Player p in _players.Values)
            {
                // 접속한 유저들에게 패킷 뿌려주셈
                p.Session.Send(packet);
            }
        }
    }
}