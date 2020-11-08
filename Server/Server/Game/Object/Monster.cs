using Google.Protobuf.Protocol;
using Server.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Game
{
    // 몬스터 종류마다 몬스터 클래스를 상속받은 클래스를 파는게 아니라 몬스터 클래스 안에서 잘 쪼갠다..
    public class Monster : GameObject
    {
        public Monster()
        {
            ObjectType = GameObjectType.Monster;

            // 임시 : 시트에서 읽어올거임
            Stat.Level = 1;
            Stat.Hp = 100;
            Stat.MaxHp = 100;
            Stat.Speed = 5.0f;

            State = CreatureState.Idle;
        }

        // FSM (Finite State Machine)
        public override void Update()
        {
            switch (State)
            {
                case CreatureState.Idle:
                    UpdateIdle();
                    break;
                case CreatureState.Moving:
                    UpdateMoving();
                    break;
                case CreatureState.Skill:
                    UpdateSkill();
                    break;
                case CreatureState.Dead:
                    UpdateDead();
                    break;
            }
        }

        // 플레이어를 직접 참조해서 쓸 필요가 있는가?
        Player _target; // 이렇게 참조값으로 저장해도 되고, id만 받아놔도 됨
        int _searchCellDist = 10; // xy합쳐서 10칸이내면 사정거리라 가정함
        int _chaseCellDist = 20; // 거리가 20이상 벌어질때까지 쫒는다

        long _nextSearchTick = 0;
        protected virtual void UpdateIdle()
        {
            if (_nextSearchTick > Environment.TickCount64) // if문을 tick마다 체크하는게 좋은 방법은 아니다
                return;
            // 쿨타임이 왔다
            _nextSearchTick = Environment.TickCount64 + 1000; // 다시 1초간 딜레이를 준다

            // 범위내에 유저가 있는지 찾아보자
            // 범위? 가장 까까운곳에 있는애 하나? -> 오픈필드면?? 플레이어가 몇명이여
            Player target = Room.FindPlayer(p =>
            {
                Vector2Int dir = p.CellPos - CellPos; // 방향벡터
                return dir.cellDistFromZero <= _searchCellDist; // 차피 대각선으로 못가니깐 수직수평값 더해서 줌
            });

            if (target == null)
                return;

            _target = target;
            State = CreatureState.Moving; // Idle 상태 끝내고, 유저를 패러가자
        }

        int _skillRange = 1;
        long _nextMoveTick = 0;
        protected virtual void UpdateMoving()
        {
            if (_nextMoveTick > Environment.TickCount64)
                return;
            int moveTick = (int)(1000 / Speed); // Speed값 설정은 1초에 몇칸 이동시킬지임
            _nextMoveTick = Environment.TickCount64 + moveTick;

            // 타겟이 없다? or 다른맵으로 튐 or 나감, id로 관리하는 경우 조건값이 달라진다.
            if (_target == null || _target.Room != Room)
            {
                _target = null;
                State = CreatureState.Idle;
                BroadcastMove(); // 내가 idle 상태가 됐음을 알린다.
                return;
            }


            Vector2Int dir = _target.CellPos - CellPos; // 방향벡터
            int dist = dir.cellDistFromZero; // 적과의 거리가 가로몇칸세로몇칸 나는지 합쳐서 뱉어줌
            // 대충 계산해봐도 멀리있는 경우
            if(dist == 0 || dist > _chaseCellDist)
            {
                _target = null;
                State = CreatureState.Idle;
                BroadcastMove();
                return;
            }

            // 경로계산까지 해보니 멀리있는 경우
            // 플레이어나 몹을 고려하지 않은 경로를 찾는다. 어차피 얘들은 계속 움직이니깐
            List<Vector2Int> path = Room.Map.FindPath(CellPos, _target.CellPos, checkObjects: false);
            // 0번쨰 인덱스가 무조건 내 위치이기 때문에 무조건 2보다는 커야 길이 나온다.
            if (path.Count < 2 || path.Count > _chaseCellDist)
            {
                // 추적포기
                _target = null; 
                State = CreatureState.Idle;
                BroadcastMove();
                return;
            }

            // 스킬을 사용할지 체크
            // 스킬범위안에 있고 + x,y축중 하나가 동일하면 (일직선상)
            if(dist <= _skillRange && (dir.x == 0 || dir.y == 0))
            {
                _coolTick = 0;
                State = CreatureState.Skill;
                return;
            }

            // 이동 시작
            Dir = GetDirFromVec(path[1] - CellPos);
            Room.Map.ApplyMove(this, path[1]); // 맵에다가 위치갱신하라고 알림
            BroadcastMove();
        }

        void BroadcastMove()
        {
            S_Move movePacket = new S_Move
            {
                ObjectId = Id,
                PosInfo = PosInfo
            };
            Room.Broadcast(movePacket);
        }

        // update 방식이라서 딜레이 넣을거면 죄다 tick 체크해야함 ㅡㅡ;
        long _coolTick = 0;
        protected virtual void UpdateSkill()
        {
            // _coolTick이 0이라는것은 스킬 사용할 준비가 완료됐음을 뜻함
            if(_coolTick == 0)
            {
                // 유효한 타겟인지 체크
                if(_target == null || _target.Room != Room || _target.Hp == 0)
                {
                    _target = null;
                    State = CreatureState.Moving;
                    BroadcastMove(); // 좀 비효율적이지만..
                    return;
                }

                // 스킬이 아직 사용 가능한지
                Vector2Int dir = (_target.CellPos - CellPos);
                int dist = dir.cellDistFromZero;
                bool canUseSkill = (dist <= _skillRange && (dir.x == 0 || dir.y == 0));                
                if (canUseSkill == false)
                {
                    // 스킬을 사용할 수 없는 상태가 됨                 
                    // 플레이어가 도망가거나 게임 자체를 나가거나 할 때 그걸 다시 쫓을지는 Moving 상태에서 판단
                    State = CreatureState.Moving;
                    BroadcastMove();
                    return;
                }

                // 타게팅 방향 주시
                MoveDir lookDir = GetDirFromVec(dir); // 상대방 - 내위치 = 내가 상대방을 바라보는 방향
                if(Dir != lookDir)
                {
                    Dir = lookDir; // 모가지를 돌리고
                    BroadcastMove(); // 알림
                }

                Skill skillData = null;
                // 몬스터 시트를 따로 빼서 몬스터에 연결된 스킬과 아이디를 맵핑
                DataManager.SkillDict.TryGetValue(1, out skillData);

                // 데미지 판정
                _target.OnDamaged(this, skillData.damage + Stat.Attack);

                // 스킬 사용 Broadcast
                S_Skill skill = new S_Skill() { Info = new SkillInfo() };
                skill.ObjectId = Id;
                skill.Info.SkillId = skillData.id;
                Room.Broadcast(skill); // 몹이 스킬을 쓴다고 알림

                // 스킬 쿨타임 시작
                int coolTick = (int)(1000 * skillData.cooldown); // 단위 ms임
                _coolTick = Environment.TickCount64 + coolTick; // 다음 tick 시간 구함
            }

            // 다시 스킬을 사용할 준비가 됐는지 여부            
            if (_coolTick > Environment.TickCount64)
                return;

            // 햇갈릴수도 있겠네
            _coolTick = 0; //_coolTick이 0이면 스킬사용 준비가 됐다는 뜻이다.
        }

        protected virtual void UpdateDead()
        {

        }
    }
}
