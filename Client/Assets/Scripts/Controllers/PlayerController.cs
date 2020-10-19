using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class PlayerController : MonoBehaviour
{
    public Grid _grid; // 플레이어가 위치한 맵의 그리드를 받는다.
    public float _speed = 5.0f;

    // 내가 cell 기준으로 어떤 cell에 위치해 있는지?
    Vector3Int _cellPos = Vector3Int.zero;
    bool _isMoving = false;
    Animator _animator;

    MoveDir _dir = MoveDir.Down; // 어떤 애니메이션을 틀어줄지와 밀접한 관계가 있다.
    public MoveDir Dir
    {
        get { return _dir; }
        set 
        {
            if (_dir == value)
                return;

            switch (value)
            {
                case MoveDir.Up:
                    _animator.Play("WALK_BACK");
                    transform.localScale = new Vector3(1.0f, 1.0f, 1.0f); // 왼쪽때문에
                    break;
                case MoveDir.Down:
                    _animator.Play("WALK_FRONT");
                    transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    break;
                case MoveDir.Left:
                    _animator.Play("WALK_RIGHT"); // 플레이어의 scale을 건들면 반전됨.
                    transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f);
                    break;
                case MoveDir.Right:
                    _animator.Play("WALK_RIGHT");
                    transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    break;
                case MoveDir.None:
                    // 서버입장에서는 멈췄냐가 문제지 어느방향으로 가다가 멈췄는지는 관심이 없다
                    // 바뀌기 직전의 _dir을 받아다가 씀.
                    if (_dir == MoveDir.Up)
                    {
                        _animator.Play("IDLE_BACK");
                        transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    }
                    else if(_dir == MoveDir.Down)
                    {
                        _animator.Play("IDLE_FRONT");
                        transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    }
                    else if (_dir == MoveDir.Left)
                    {
                        _animator.Play("IDLE_RIGHT");
                        transform.localScale = new Vector3(-1.0f, 1.0f, 1.0f); // 아까랑 동일
                    }
                    else
                    {
                        _animator.Play("IDLE_RIGHT");
                        transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    }
                    break;
            }

            _dir = value;
        }
    }

    void Start()
    {
        _animator = GetComponent<Animator>();
        // _cellPos에 따른 내 위치를 잡는다 

        // 당장은 내 위치가 cell과 world가 1대1 대응이 되지만
        // 캐릭터 사이즈가 cell 사이즈보다 커지면 이렇게 하는게 나음
        // Vector3(0.5f, 0.5f) 값은 캐릭터가 셀 안에 들어가게 하기 위한 보정치
        Vector3 pos = _grid.CellToWorld(_cellPos) + new Vector3(0.5f, 0.5f);
        transform.position = pos;
    }

    void Update()
    {
        GetDirInput(); // 입력
        UpdatePosition(); // 이미 이동한 상태지만 부드럽게 이동하는것처럼 보여주기 위한
        UpdateIsMoving(); // 입력기준으로 이동
    }

    // 스르륵 이동 처리
    private void UpdatePosition()
    {
        if (_isMoving == false)
            return;

        // 서버기준으로는 이미 _cellPos에 이동해있는 상황임
        Vector3 destPos = _grid.CellToWorld(_cellPos) + new Vector3(0.5f, 0.5f);
        Vector3 moveDir = destPos - transform.position; // 방향벡터 구하기

        // 도착 여부 체크
        float dist = moveDir.magnitude; // 목적지까지의 남은 거리
        if(dist < _speed * Time.deltaTime) // 한번에 이동한 거리보다 더 적게 남았으면 도착처리
        {
            // 도착
            transform.position = destPos;
            _isMoving = false;
        }
        else
        {
            // 아직 가는 중
            transform.position += moveDir.normalized * _speed * Time.deltaTime;
            _isMoving = true;
        }
    }

    // 이동 가능한 상태일 때, 실제로 이동
    private void UpdateIsMoving()
    {
        if (_isMoving == false)
        {
            // 움직이는 중이 아니라면 -> 움직일수있다
            switch (Dir)
            {
                case MoveDir.None:
                    break;
                case MoveDir.Up:
                    _cellPos += Vector3Int.up;
                    _isMoving = true; // 이동 애니메이션이 끝날때까지는 이동불가
                    break;
                case MoveDir.Down:
                    _cellPos += Vector3Int.down;
                    _isMoving = true;
                    break;
                case MoveDir.Left:
                    _cellPos += Vector3Int.left;
                    _isMoving = true;
                    break;
                case MoveDir.Right:
                    _cellPos += Vector3Int.right;
                    _isMoving = true;
                    break;
                default:
                    break;
            }
        }
    }

    // Time.deltaTime 곱하는 이유는 기기 frame에 따라 이동량이 달라지는 경우를 막기 위해
    // 캐릭터는 맵의 한칸 단위로 움직인다
    // 키보드 입력 : 직접 이동시키는게 아니라 방향만 정해줌
    void GetDirInput()
    {
        if (Input.GetKey(KeyCode.W))
        {
            Dir = MoveDir.Up;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            Dir = MoveDir.Down;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            Dir = MoveDir.Left;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            Dir = MoveDir.Right;
        } 
        else
        {
            // 정지
            Dir = MoveDir.None;
        }
    }
}
