using Google.Protobuf.Protocol;
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
                return;
            }

            // 대충 계산해봐도 멀리있는 경우
            int dist = (_target.CellPos - CellPos).cellDistFromZero; // 적과의 거리가 가로몇칸세로몇칸 나는지 합쳐서 뱉어줌
            if(dist == 0 || dist > _chaseCellDist)
            {
                _target = null;
                State = CreatureState.Idle;
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
                return;
            }

            // 이동준비완료
            Dir = GetDirFromVec(path[1] - CellPos);
            Room.Map.ApplyMove(this, path[1]); // 맵에다가 위치갱신하라고 알림

            // 다른 플레이어에게도 알려준다
            S_Move movePacket = new S_Move
            {
                ObjectId = Id,
                PosInfo = PosInfo
            };
            Room.Broadcast(movePacket);
        }

        protected virtual void UpdateSkill()
        {

        }

        protected virtual void UpdateDead()
        {

        }
    }
}
