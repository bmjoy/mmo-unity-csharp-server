using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

// 플레이어나 몬스터 컨트롤러의 공통부분을 여기에
public class CreatureController : MonoBehaviour
{
    //public Grid _grid; // 플레이어가 위치한 맵의 그리드를 받는다.
    public float _speed = 5.0f;

    // 내가 cell 기준으로 어떤 cell에 위치해 있는지?
    // 추후 접근해서 쓸 가능성이 있는 변수들은 protected
    protected Vector3Int _cellPos = Vector3Int.zero;
    // protected bool _isMoving = false;
    protected Animator _animator;
    protected SpriteRenderer _sprite;

    // 만약 스킬을 쓸 때 이동하면 안된다고 치면.. 또 bool을 늘릴까? isSkill isJump isCinematic..?
    // 상태관리용도의 bool은 최소한으로 유지한다 State로 관리하는게 나음.. Define.cs
    CreatureState _state = CreatureState.Idle;
    // State가 변화하면 애니메이션도 같이 변할 확률이 매우 크다.
    public CreatureState State
    {
        get { return _state; }
        set
        {
            if (_state == value)
                return;

            _state = value;
            // 이제 애니메이션을 어케하지
            UpdateAnimation(); // state 바꿨으니 알아서 틀어주셈
        }
    }

    MoveDir _lastDir = MoveDir.Down; // Idle 상태일때 틀어줄 애니메이션을 결정, UpdateAnimation 추가하면서 이게 필요해짐
    MoveDir _dir = MoveDir.Down; // 어떤 애니메이션을 틀어줄지와 밀접한 관계가 있다.
    public MoveDir Dir
    {
        get { return _dir; }
        set
        {
            if (_dir == value)
                return;

            _dir = value;
            if (value != MoveDir.None)
                _lastDir = value;

            UpdateAnimation();
        }
    }

    // 상태에 따른 애니메이션을 자동으로 호출한다. 지금은 state와 dir의 영향을 받음
    protected virtual void UpdateAnimation()
    {
        // switch 문으로 해도 되지만 내부에서 또 switch 쓸거기 때문에 가독성을 위해 if~else로
        if(_state == CreatureState.Idle)
        {
            switch (_lastDir)
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
        else if(_state == CreatureState.Moving)
        {
            // dir값에 맞는 방향을 바라보면서 걸어가는 애니메이션 재생
            switch (_dir)
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
        else if (_state == CreatureState.Skill)
        {
            // TODO
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
        Vector3 pos = Managers.Map.CurrentGrid.CellToWorld(_cellPos) + new Vector3(0.5f, 0.5f);
        transform.position = pos;
    }

    // Update에 똑같은거 때려넣지 말고 필요하면 UpdateController()를 재정의
    protected virtual void UpdateController()
    {
        UpdatePosition(); // 이미 이동한 상태지만 부드럽게 이동하는것처럼 보여주기 위한
        UpdateIsMoving(); // 입력기준으로 이동
    }

    // 스르륵 이동 처리
    private void UpdatePosition()
    {
        if (State != CreatureState.Moving)
            return;

        // 서버기준으로는 이미 _cellPos에 이동해있는 상황임
        Vector3 destPos = Managers.Map.CurrentGrid.CellToWorld(_cellPos) + new Vector3(0.5f, 0.5f);
        Vector3 moveDir = destPos - transform.position; // 방향벡터 구하기

        // 도착 여부 체크
        float dist = moveDir.magnitude; // 목적지까지의 남은 거리
        if (dist < _speed * Time.deltaTime) // 1틱에 이동 가능한 거리보다 더 적게 남았으면 도착처리
        {
            // 도착
            transform.position = destPos;
            // 예외적으로 애니메이션을 직접 컨트롤
            // 키를 계속 누르고 있는 경우에도 계속 Idle 애니메이션이 재생되는 문제때문에 이렇게 함
            _state = CreatureState.Idle; // _state에 직접 넣으면 UpdateAnimation 호출을 안함
            if (_dir == MoveDir.None) // 이동키에서 정말로 손을 땠다 (멈춤)
                UpdateAnimation(); // Idle 애니메이션을 틀어줘
        }
        else
        {
            // 아직 가는 중
            transform.position += moveDir.normalized * _speed * Time.deltaTime;
            State = CreatureState.Moving;
        }
    }

    // 이동 가능한 상태일 때, 실제로 이동
    private void UpdateIsMoving()
    {
        // 움직이지 않을때만
        if (State == CreatureState.Idle && _dir != MoveDir.None)
        {
            // 움직이는 중이 아니라면 -> 움직일수있다
            Vector3Int destPos = _cellPos;
            switch (Dir)
            {
                case MoveDir.Up:
                    destPos += Vector3Int.up;
                    break;
                case MoveDir.Down:
                    destPos += Vector3Int.down;
                    break;
                case MoveDir.Left:
                    destPos += Vector3Int.left;
                    break;
                case MoveDir.Right:
                    destPos += Vector3Int.right;
                    break;
            }

            // 이제 맵 매니저에게 허락받고 움직여야함
            if (Managers.Map.CanGo(destPos))
            {
                _cellPos = destPos;
                State = CreatureState.Moving;
            }
        }
    }
}
