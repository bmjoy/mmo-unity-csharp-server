using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

// 플레이어나 몬스터 컨트롤러의 공통부분을 여기에
public class CreatureController : MonoBehaviour
{
    public int Id { get; set; } // 아이디는 모든 생성된 플레이어들(몹)이 하나는 갖고 있어야됨

    //public Grid _grid; // 플레이어가 위치한 맵의 그리드를 받는다.
    [SerializeField]
    public float _speed = 5.0f;

    // 일종의 dirty flag로서 유저의 cellpos state dir 셋 중 하나라도 변경사항이 있나 체크
    // 이게 MyPlayerController에서만 쓰는건데 .. 일단 여따넣음
    protected bool _updated = false;

    PositionInfo _positionInfo = new PositionInfo();
    public PositionInfo PosInfo
    {
        get { return _positionInfo; }
        set
        {
            if (_positionInfo.Equals(value))
                return;

            // 위치 갱신 - 여기서 Dir을 덮어버려서 자꾸 멈췄을때 아래만 쳐다봄
            CellPos = new Vector3Int(value.PosX, value.PosY, 0);
            State = value.State;
            Dir = value.MoveDir;
            // 각 상태가 변경될 때 애니메이션은 알아서 갱신됨
        }
    }

    public void SyncPos()
    {
        // 지금 이동 구현이 일단 유니티상 좌표는 즉시 바뀌고 그 다음에
        // CellPos를 이용해 스르륵 이동하는 것을 구현하고 있다.
        // 그런데 스르륵 이동하는 표현 없이 좌표이동을 즉시 반영하고 싶으면?
        // 내 CellPos와 실제 Transform을 맞춰주겠다는 소리.

        // CellPos에 따른 내가 실제로 있어야 할 위치를(transform 좌표) 얻어옴
        Vector3 destPos = Managers.Map.CurrentGrid.CellToWorld(CellPos) + new Vector3(0.5f, 0.5f); // 0.5는 캐릭터를 셀 안에 넣기위한 보정
        transform.position = destPos;
    }

    // 내가 cell 기준으로 어떤 cell에 위치해 있는지?
    // 추후 접근해서 쓸 가능성이 있는 변수들은 protected
    // protected Vector3Int _cellPos = Vector3Int.zero;
    // ObjectManager에서 끌어다 써야 하므로 public으로 변경
    public Vector3Int CellPos
    {
        get
        {
            return new Vector3Int(PosInfo.PosX, PosInfo.PosY, 0);
        }

        set
        {
            if (PosInfo.PosX == value.x && PosInfo.PosY == value.y)
                return;

            PosInfo.PosX = value.x;
            PosInfo.PosY = value.y;
            _updated = true;
        }
    }
    
    // protected bool _isMoving = false;
    protected Animator _animator;
    protected SpriteRenderer _sprite;

    // 만약 스킬을 쓸 때 이동하면 안된다고 치면.. 또 bool을 늘릴까? isSkill isJump isCinematic..?
    // 상태관리용도의 bool은 최소한으로 유지한다 State로 관리하는게 나음.. Define.cs
    // State가 변화하면 애니메이션도 같이 변할 확률이 매우 크다.
    public virtual CreatureState State
    {
        get { return PosInfo.State; }
        set
        {
            if (PosInfo.State == value)
                return;

            PosInfo.State = value;
            // 이제 애니메이션을 어케하지
            UpdateAnimation(); // state 바꿨으니 알아서 틀어주셈
            _updated = true;
        }
    }

    // protected MoveDir _lastDir = MoveDir.Down; // Idle 상태일때 틀어줄 애니메이션을 결정, UpdateAnimation 추가하면서 이게 필요해짐
    public MoveDir Dir
    {
        get { return PosInfo.MoveDir; }
        set
        {
            if (PosInfo.MoveDir == value)
                return;

            PosInfo.MoveDir = value;

            UpdateAnimation();
            _updated = true;
        }
    }

    public MoveDir GetDirFromVec(Vector3Int dir)
    {
        if (dir.x > 0)
            return Dir = MoveDir.Right;
        else if (dir.x < 0)
            return Dir = MoveDir.Left;
        else if (dir.y > 0)
            return Dir = MoveDir.Up;
        else
            return Dir = MoveDir.Down;
    }

    // 내가 바라보는 방향의 바로 앞 Cell 포지션 (타격처리용)
    // 범위공격인 경우?
    public Vector3Int GetFrontCellPos(int range = 1)
    {
        Vector3Int cellPos = CellPos;

        // 내가 타격직전 마지막으로 바라보는곳 기준으로
        switch (Dir)
        {
            case MoveDir.Up:
                cellPos += Vector3Int.up * range;
                break;
            case MoveDir.Down:
                cellPos += Vector3Int.down * range;
                break;
            case MoveDir.Left:
                cellPos += Vector3Int.left * range;
                break;
            case MoveDir.Right:
                cellPos += Vector3Int.right * range;
                break;
        }

        return cellPos;
    }

    // 상태에 따른 애니메이션을 자동으로 호출한다. 지금은 state와 dir의 영향을 받음
    protected virtual void UpdateAnimation()
    {
        // switch 문으로 해도 되지만 내부에서 또 switch 쓸거기 때문에 가독성을 위해 if~else로
        if(State == CreatureState.Idle)
        {
            switch (Dir)
            {
                // 서버입장에서는 멈췄냐가 문제지 어느방향으로 가다가 멈췄는지는 관심이 없다
                // 바뀌기 직전의 _dir을 받아다가 씀.
                case MoveDir.Up:
                    _animator.Play("IDLE_BACK");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Down:
                    _animator.Play("IDLE_FRONT");
                    _sprite.flipX = false; 
                    break;
                case MoveDir.Left:
                    _animator.Play("IDLE_RIGHT");
                    _sprite.flipX = true; // 스프라이트 x축 반전 (오->왼)
                    break;
                case MoveDir.Right:
                    _animator.Play("IDLE_RIGHT");
                    _sprite.flipX = false;
                    break;
            }
        }
        else if(State == CreatureState.Moving)
        {
            // dir값에 맞는 방향을 바라보면서 걸어가는 애니메이션 재생
            switch (Dir)
            {
                case MoveDir.Up:
                    _animator.Play("WALK_BACK");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Down:
                    _animator.Play("WALK_FRONT");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Left:
                    _animator.Play("WALK_RIGHT");
                    _sprite.flipX = true; // 스프라이트 x축 반전 (오->왼)
                    break;
                case MoveDir.Right:
                    _animator.Play("WALK_RIGHT");
                    _sprite.flipX = false;
                    break;
            }
        }
        else if (State == CreatureState.Skill)
        {
            // 이제 스킬에 따라 여러가지 애니메이션이 나온다.
            // 내가 스킬쓰기 직전까지 바라보고 있던 방향으로 스킬이 시전되어야 한다
            switch (Dir) 
            {
                case MoveDir.Up:
                    _animator.Play("ATTACK_BACK");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Down:
                    _animator.Play("ATTACK_FRONT");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Left:
                    _animator.Play("ATTACK_RIGHT");
                    _sprite.flipX = true; // 스프라이트 x축 반전 (오->왼)
                    break;
                case MoveDir.Right:
                    _animator.Play("ATTACK_RIGHT");
                    _sprite.flipX = false;
                    break;
            }
        }
        else
        {
            // 죽었을 때
        }
    }

    void Start()
    {
        Init();
    }

    void Update()
    {
        UpdateController();
    }

    // 플레이어나 몬스터 컨트롤러 클래스는 Start를 직접 쓰지 않고
    // 아래 Init을 재정의해서 쓴다.
    protected virtual void Init()
    {
        _animator = GetComponent<Animator>();
        _sprite = GetComponent<SpriteRenderer>();
        // _cellPos에 따른 내 위치를 잡는다 

        // 당장은 내 위치가 cell과 world가 1대1 대응이 되지만
        // 캐릭터 사이즈가 cell 사이즈보다 커지면 이렇게 하는게 나음
        // Vector3(0.5f, 0.5f) 값은 캐릭터가 셀 안에 들어가게 하기 위한 보정치
        Vector3 pos = Managers.Map.CurrentGrid.CellToWorld(CellPos) + new Vector3(0.5f, 0.5f);
        transform.position = pos;

        // 서버에서 아무값도 안왔다면 하단의 설정대로 하고
        // 값이 왔으면 그걸로 덮어씌워질거임
        State = CreatureState.Idle;
        Dir = MoveDir.Down;
        // CellPos = new Vector3Int(0, 0, 0); // 위치를 강제로 건드는 코드는 조심
        UpdateAnimation();
    }

    // Update에 똑같은거 때려넣지 말고 필요하면 UpdateController()를 재정의
    protected virtual void UpdateController()
    {
        // State 에 따라 어떤 컨트롤러들을 Update 해줄건지 나눔
        // UpdatePosition()의 경우 Moving 상태일떄만 들어오길 원하니
        // UpdatePosition 내부에  if (State != CreatureState.Moving) 같은 코드가 있는데 이것을 대체
        // State별로 전담하는 Update 함수가 달라지므로 책임이 명확해진다
        switch (State)
        {
            case CreatureState.Idle:
                UpdateIdle(); // 입력기준으로 이동
                break;
            case CreatureState.Moving:
                UpdateMoving(); // 이미 이동한 상태지만 부드럽게 이동하는것처럼 보여주기 위한
                break;
            case CreatureState.Skill:
                UpdateSkill();
                break;
            case CreatureState.Dead:
                UpdateDead();
                break;
            default:
                break;
        }
    }

    protected virtual void UpdateIdle()
    {
    }

    // 스르륵 이동하는 것처럼 보이게 하는 코드
    protected virtual void UpdateMoving()
    {
        // 내가 원하는 상태(직전)만 받음
        //if (State != CreatureState.Moving)
        //    return;

        // 서버기준으로는 이미 _cellPos에 이동해있는 상황임
        Vector3 destPos = Managers.Map.CurrentGrid.CellToWorld(CellPos) + new Vector3(0.5f, 0.5f);
        Vector3 moveDir = destPos - transform.position; // 방향벡터 구하기

        // 도착 여부 체크
        float dist = moveDir.magnitude; // 목적지까지의 남은 거리
        if (dist < _speed * Time.deltaTime) // 1틱에 이동 가능한 거리보다 더 적게 남았으면 도착처리
        {
            // 도착
            transform.position = destPos;

            // 이 부분은 MoveToNexPos()가 대체
            // 예외적으로 애니메이션을 직접 컨트롤
            // 키를 계속 누르고 있는 경우에도 계속 Idle 애니메이션이 재생되는 문제때문에 이렇게 함
            //_state = CreatureState.Idle; // _state에 직접 넣으면 UpdateAnimation 호출을 안함
            //if (_dir == MoveDir.None) // 이동키에서 정말로 손을 땠다 (멈춤)
            //    UpdateAnimation(); // Idle 애니메이션을 틀어줘
            
            // 다음 좌표는 무엇인지? 멈춰야 할지 선택
            // 오브젝트 타입별로 어떤식의 움직임을 가져갈지도 정한다.
            MoveToNextPos(); // 다음 목적지로 이동
        }
        else
        {
            // 아직 가는 중
            // 스르륵 이동 구현부
            transform.position += moveDir.normalized * _speed * Time.deltaTime;
            State = CreatureState.Moving;
        }
    }

    protected virtual void MoveToNextPos()
    {
        // 플레이어의 좌표를 직접 건드는 부분은 
        // 내가 조작하는 플레이어에 한해서만 한정한다
        // 또는 동기화가 크게 중요하지 않은 오브젝트들
        // ex) 화살
    }

    protected virtual void UpdateSkill()
    {

    }

    protected virtual void UpdateDead()
    {

    }

    public virtual void OnDamaged() // 누가 어떤식으로 때렸는지 인자 추가 가능
    {
        // 피격처리의 책임을 누가 질것인가?
        // 얻어맞는 쪽이 처리하는게 가장 흔하다.

        // 모두가 얻어맞는건 아니다.

        // OnDamaged 구현을 여기저기 복붙해서 쓸 필요가 없이
        // CreatureController 상속받은 해당 컨트롤러의 OnDamaged만 수정하면 됨. 

    }
}
