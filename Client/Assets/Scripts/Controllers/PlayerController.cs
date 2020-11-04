using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class PlayerController : CreatureController
{
    // MyPlayerController에서 쓸수도 있으니 protected처리
    protected Coroutine _coSkill; // 스킬 시전중에 State 변화 막을 용도
    protected bool _rangedSkill = false; // 스킬이 몇개 없으면 이렇게 해도 되는데 나중에는 스킬데이터시트로 뺀다.

    // CreatureController의 Start와 Update가 자동으로 호출됨
    protected override void Init()
    {
        base.Init();
    }

    // 화살쏘기 이런건 유저만 쓰니깐 UpdateAnimation을 오버라이드 해서 사용
    protected override void UpdateAnimation()
    {
        if (_animator == null || _sprite == null)
            return;

        // switch 문으로 해도 되지만 내부에서 또 switch 쓸거기 때문에 가독성을 위해 if~else로
        if (State == CreatureState.Idle)
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
        else if (State == CreatureState.Moving)
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
            // 스킬이 하나밖에 없어서 삼항식으로 했는데 보통 시트에서 스킬데이터 읽어와서 재생함
            switch (Dir)
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
        // 얘는 더이상 키보드 입력을 받지 않는다.
        base.UpdateController();
    }

    // 스킬 전담 클래스가 따로 있는게 낫다.
    // 스킬 사용중에는 상태변화를 막고 싶은데 시간 기준으로 한다 치면 어떻게 시간을 카운트?
    // 1. Update()
    // 2. Coroutine

    // 더 이상 내가 조작하는 플레이어 이외의 다른 플레이어들은 직접 건들지 않는다.
    //protected override void UpdateIdle()
    //{
    //    // 이동 상태로 바뀔건지 확인
    //    if(Dir != MoveDir.None)
    //    {
    //        State = CreatureState.Moving; // UpdateMoving()으로 넘어감
    //        return;
    //    }
    //}

    public void UseSkill(int skillId)
    {
        if(skillId == 1)
        {
            // 주먹질
            _coSkill = StartCoroutine("CoStartPunch");
        }
        else if (skillId == 2)
        {
            _coSkill = StartCoroutine("CoStartShootArrow");
        }
    }

    protected virtual void CheckUpdatedFlag()
    {

    }

    // 플레이어 평타를 맞혔을때
    IEnumerator CoStartPunch()
    {
        // 피격 판정 -> 이걸 클라에서 처리하나?
        // 내가 바라보고 있는 방향 기준 바로 앞의 좌표
        //GameObject go = Managers.Object.Find(GetFrontCellPos());
        //if (go != null)
        //{
        //    CreatureController cc = go.GetComponent<CreatureController>();
        //    if (cc != null)
        //        cc.OnDamaged();
        //}

        // 대기 시간
        _rangedSkill = false; // 원거리 공격인지 체크
        State = CreatureState.Skill;
        // 처리는 서버가 하지만, 클라측에서도 패킷을 마구잡이로 생성해다가 쏘는것을 방지하기 위해 
        // 아래처럼 쿨타임을 준다.
        yield return new WaitForSeconds(0.5f);
        State = CreatureState.Idle;
        _coSkill = null;
        CheckUpdatedFlag(); // (임시) MyPlayer일때만 CheckUpdatedFlag에서 서버와 통신할거임
    }

    IEnumerator CoStartShootArrow()
    {
        // 화살 생성 -> 서버의 spawn 패킷으로 하는중
        //GameObject go = Managers.Resource.Instantiate("Creature/Arrow");
        //ArrowController ac = go.GetComponent<ArrowController>(); // null이면 즉시 수정해야하니 체크안함
        //// 플레이어가 보고있는 방향으로 쏴야함. 
        //// 키보드를 누르고 있지 않더라도 직전 방향으로라도 쏴야함
        //ac.Dir = Dir;
        //ac.CellPos = CellPos; // 화살은 내 위치 기준으로 발사

        // 대기 시간
        _rangedSkill = true; // 원거리 공격인지 체크
        State = CreatureState.Skill;
        yield return new WaitForSeconds(0.3f); // 화살 발사 딜레이
        State = CreatureState.Idle;
        _coSkill = null;
        CheckUpdatedFlag(); // (임시) MyPlayer일때만 CheckUpdatedFlag에서 서버와 통신할거임
    }

    public override void OnDamaged()
    {
        Debug.Log("Player HIT !!!");
    }
}
