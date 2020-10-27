using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class MonsterController : CreatureController
{
    private Coroutine _coPatrol;
    private Coroutine _coSearch;
    [SerializeField]
    private Vector3Int _destCellPos;

    [SerializeField]
    private GameObject _target; // 추적대상

    [SerializeField]
    private float _searchRange = 5.0f; // 추격범위

    public override CreatureState State
    {
        get { return _state; }
        set
        {
            if (_state == value)
                return;

            base.State = value;

            // State 변경시 _coPatrol을 비운다.
            // 멤버변수도 virtual이 되네
            if (_coPatrol != null)
            {
                StopCoroutine(_coPatrol);
                _coPatrol = null;
            }

            if (_coSearch != null)
            {
                StopCoroutine(_coSearch);
                _coSearch = null;
            }
        }
    }

    protected override void Init()
    {
        base.Init();
        // State, Dir 값 정해줄때 Animator가 필요한데
        // Init()에서 Animator를 정해주니깐 State나 Dir 값 대입은
        // Init()아래에 있어야 한다.
        State = CreatureState.Idle;
        Dir = MoveDir.None;

        _speed = 3.0f;
    }

    protected override void UpdateIdle()
    {
        base.UpdateIdle();

        // 순찰하기
        if (_coPatrol == null)
        {
            _coPatrol = StartCoroutine("CoPatrol");
        }

        // 이동하는 도중에 유저를 찾으면?
        if (_coSearch == null)
        {
            _coSearch = StartCoroutine("CoSearch");
        }
    }

    // 목표까지 한칸씩 이동하는것을 구현
    // 유저를 만나면 패트롤을 멈추고 에이스타 같은걸로 추적
    // AStar를 이용해 경로를 찾아서 그 경로중에 딱 하나만 쓰는게 이상할수도 있지만
    // 내가 경로를 찾았을 때 그 경로에 대상이 가만히 있는 경우가 거의 없다.
    // 계속 상태가 변하기 때문에 일정시간마다 다시 계산하는게 나음
    protected override void MoveToNextPos()
    {
        // 순찰하는 경우도 있고, 유저를 찾아서 추적하는 경우도 생김.
        Vector3Int destPos = _destCellPos;
        if(_target != null)
        {
            destPos = _target.GetComponent<CreatureController>().CellPos;
        }

        // ignoreDestCollision: true의 의미는 destPos가 못가는 위치여도 일단 그 앞까지는 가야하기 때문
        List<Vector3Int> path = Managers.Map.FindPath(CellPos, destPos, ignoreDestCollision: true);
        if(path.Count < 2 || (_target && path.Count > 10))
        {
            // 타겟이 있는 경우에만 카운트 조건 체크, 10은 하드코딩한거.. 플레이어와 너무 멀어졌다는거임
            // 내 좌표 기준으로 카운트를 세는데 2보다 작으면? 1은 제자리에 있겠다는거고
            // 갈곳이 아예 없으면 여기 들어올수가 있음.
            _target = null;
            State = CreatureState.Idle;
            return;
        }

        Vector3Int nextPos = path[1]; // path[0]은 내 위치임
        Vector3Int moveCellDir = nextPos - CellPos;

        // 애니메이션을 위해서라도 이건 필요
        if (moveCellDir.x > 0)
            Dir = MoveDir.Right;
        else if (moveCellDir.x < 0)
            Dir = MoveDir.Left;
        else if (moveCellDir.y > 0)
            Dir = MoveDir.Up;
        else if (moveCellDir.y < 0)
            Dir = MoveDir.Down;
        else
            Dir = MoveDir.None;

        // 갈수있나체크
        if (Managers.Map.CanGo(destPos) && Managers.Object.Find(nextPos) == null)
        {
            // 갈수있다.
            CellPos = nextPos;
        }
        else
        {
            // 막혔다 -> 움직이던거 멈추고 대기
            State = CreatureState.Idle;
        }
    }

    // 얻어맞은쪽에서 처리
    public override void OnDamaged()
    {
        // 사망 이펙트 생성
        GameObject effect = Managers.Resource.Instantiate("Effect/DieEffect");
        effect.transform.position = transform.position; // 생성위치는 맞은놈 위치
        effect.GetComponent<Animator>().Play("START");
        // 이펙트를 0.5초후에 제거해달라고 예약
        GameObject.Destroy(effect, 0.5f);

        // 나를 제거
        Managers.Object.Remove(gameObject);
        Managers.Resource.Destroy(gameObject);
    }

    private IEnumerator CoPatrol()
    {
        int waitSeconds = Random.Range(1, 4);
        yield return new WaitForSeconds(waitSeconds);

        //
        for (int i = 0; i < 10; i++)
        {
            int xRange = Random.Range(-5, 6);
            int yRange = Random.Range(-5, 6);
            Vector3Int randPos = CellPos + new Vector3Int(xRange, yRange, 0);

            // 가려는 위치가 이동가능한 타일이고 거기에 아무도 없나?
            if (Managers.Map.CanGo(randPos) && Managers.Object.Find(randPos) == null)
            {
                _destCellPos = randPos;
                State = CreatureState.Moving;
                yield break; // 코투틴 종료
            }
        }

        State = CreatureState.Idle;
    }

    private IEnumerator CoSearch()
    {
        // 플레이어는 가만히 있지를 않으니깐 그 위치를 지속적으로 갱신해서 알고있는게 좋다
        while (true)
        {
            yield return new WaitForSeconds(1.0f);

            if (_target != null) // 타겟이 있으면 나감
                continue;

            // 타겟없다면 검색 (1초마다)
            _target = Managers.Object.Find(go =>
           {
               PlayerController pc = go.GetComponent<PlayerController>(); // 이놈이 플레이어인가?
               if (pc == null)
                   return false; //없으면 아님

               // 일정 거리에 있는 플레이어만 찾는다
               Vector3Int dir = (pc.CellPos - CellPos);
               if (dir.magnitude > _searchRange)
                   return false; // 내 범위안에 없다

               // 올바른 타겟 찾음
               return true;
           });
        }
    }
}