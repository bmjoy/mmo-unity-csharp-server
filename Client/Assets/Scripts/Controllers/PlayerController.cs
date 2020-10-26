using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class PlayerController : CreatureController
{
    Coroutine _coSkill; // 스킬 시전중에 State 변화 막을 용도
    bool _rangedSkill = false; // 스킬이 몇개 없으면 이렇게 해도 되는데 나중에는 스킬데이터시트로 뺀다.

    // CreatureController의 Start와 Update가 자동으로 호출됨
    protected override void Init()
    {
        base.Init();
    }

    // 화살쏘기 이런건 유저만 쓰니깐 UpdateAnimation을 오버라이드 해서 사용
    protected override void UpdateAnimation()
    {
        // switch 문으로 해도 되지만 내부에서 또 switch 쓸거기 때문에 가독성을 위해 if~else로
        if (_state == CreatureState.Idle)
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
        else if (_state == CreatureState.Moving)
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
            // 이제 스킬에 따라 여러가지 애니메이션이 나온다.
            // 내가 스킬쓰기 직전까지 바라보고 있던 방향으로 스킬이 시전되어야 한다
            // 스킬이 하나밖에 없어서 삼항식으로 했는데 보통 시트에서 스킬데이터 읽어와서 재생함
            switch (_lastDir)
            {
                case MoveDir.Up:
                    _animator.Play(_rangedSkill ? "ATTACK_WEAPON_BACK" : "ATTACK_BACK");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Down:
                    _animator.Play(_rangedSkill ? "ATTACK_WEAPON_FRONT" : "ATTACK_FRONT");
                    _sprite.flipX = false;
                    break;
                case MoveDir.Left:
                    _animator.Play(_rangedSkill ? "ATTACK_WEAPON_RIGHT" : "ATTACK_RIGHT");
                    _sprite.flipX = true; // 스프라이트 x축 반전 (오->왼)
                    break;
                case MoveDir.Right:
                    _animator.Play(_rangedSkill ? "ATTACK_WEAPON_RIGHT" : "ATTACK_RIGHT");
                    _sprite.flipX = false;
                    break;
            }
        }
        else
        {
            // 죽었을 때
        }
    }

    protected override void UpdateController()
    {
        // 스킬을 사용중이면 방향전환을 막는다던지..
        switch (State)
        {
            case CreatureState.Idle:
                GetDirInput();
                GetIdleInput();
                break;
            case CreatureState.Moving:
                GetDirInput();
                break;
        }
        base.UpdateController();
    }



    void LateUpdate()
    {
        // 카메라 (2D 게임의 경우 카메라의 기본 z좌표는 -10으로 고정임)
        Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
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

    // 스킬 전담 클래스가 따로 있는게 낫다.
    // 스킬 사용중에는 상태변화를 막고 싶은데 시간 기준으로 한다 치면 어떻게 시간을 카운트?
    // 1. Update()
    // 2. Coroutine
    private void GetIdleInput()
    {
        // 스킬(임시)
        // GetDirInput()에 있던 것을 분리
        if (Input.GetKey(KeyCode.Space))
        {
            State = CreatureState.Skill; // UpdateAnimation은 알아서 불러줄거임
            // 0.5초 후에 스킬시전상태(CreatureState.Skill)를 푼다
            // _coSkill = StartCoroutine("CoStartPunch"); 
            _coSkill = StartCoroutine("CoStartShootArrow");
        }
    }

    IEnumerator CoStartPunch()
    {
        // 피격 판정
        // 내가 바라보고 있는 방향 기준 바로 앞의 좌표
        GameObject go = Managers.Object.Find(GetFrontCellPos());
        if (go != null)
        {
            Debug.Log(go.name);
        }

        // 대기 시간
        _rangedSkill = false; // 범위스킬 체크
        yield return new WaitForSeconds(0.5f);
        State = CreatureState.Idle;
        _coSkill = null;
    }

    IEnumerator CoStartShootArrow()
    {
        // 화살 생성
        GameObject go = Managers.Resource.Instantiate("Creature/Arrow");
        ArrowController ac = go.GetComponent<ArrowController>(); // null이면 즉시 수정해야하니 체크안함
        // 플레이어가 보고있는 방향으로 쏴야함. 
        // 키보드를 누르고 있지 않더라도 직전 방향으로라도 쏴야함
        ac.Dir = _lastDir;
        ac.CellPos = CellPos; // 화살은 내 위치 기준으로 발사

        // 대기 시간
        _rangedSkill = true; // 범위스킬 체크
        yield return new WaitForSeconds(0.3f); // 화살 발사 딜레이
        State = CreatureState.Idle;
        _coSkill = null;
    }
}
