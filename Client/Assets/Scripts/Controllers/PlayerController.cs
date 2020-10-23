using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class PlayerController : CreatureController
{
    Coroutine _coSkill; // 스킬 시전중에 State 변화 막을 용도
    // CreatureController의 Start와 Update가 자동으로 호출됨
    protected override void Init()
    {
        base.Init();
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
            _coSkill = StartCoroutine("CoStartPunch"); 
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
        yield return new WaitForSeconds(0.5f);
        State = CreatureState.Idle;
        _coSkill = null;
    }
}
