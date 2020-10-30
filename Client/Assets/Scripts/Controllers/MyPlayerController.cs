using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

// 내가 조작하고 있는 플레이어에 대한 컨트롤러
public class MyPlayerController : PlayerController
{
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
                break;
            case CreatureState.Moving:
                GetDirInput();
                break;
        }
        base.UpdateController();
    }

    // 스킬 전담 클래스가 따로 있는게 낫다.
    // 스킬 사용중에는 상태변화를 막고 싶은데 시간 기준으로 한다 치면 어떻게 시간을 카운트?
    // 1. Update()
    // 2. Coroutine
    protected override void UpdateIdle()
    {
        // 이동 상태로 바뀔건지 확인
        if (Dir != MoveDir.None)
        {
            State = CreatureState.Moving; // UpdateMoving()으로 넘어감
            return;
        }

        // 이동상태로 바뀔게 아니면 스킬 사용 가능한 상태가 됨.
        // 스킬은 Idle 상태일때만 사용가능
        // GetDirInput()에 있던 것을 분리
        if (Input.GetKey(KeyCode.Space))
        {
            State = CreatureState.Skill; // UpdateAnimation은 알아서 불러줄거임
            // 0.5초 후에 스킬시전상태(CreatureState.Skill)를 푼다
            // _coSkill = StartCoroutine("CoStartPunch"); 
            _coSkill = StartCoroutine("CoStartShootArrow");
        }
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

    // 움직이고 있다는 정보는 내가 조작하고 있는 플레이어만 보내면 된다.
    protected override void MoveToNextPos()
    {
        // 실질적으로 내 좌표가 변할때 서버에 뭔가 요청한다.
        // 상태변화
        CreatureState prevState = State;
        Vector3Int prevCellPos = CellPos;

        base.MoveToNextPos(); // 이동부분 처리는 여기서

        // 이전과 state가 다르거나 cell좌표가 다르면 패킷전송
        if(prevState != State || CellPos != prevCellPos)
        {
            C_Move movePacket = new C_Move();
            movePacket.PosInfo = PosInfo; // xyz좌표 및 state정보를 한번에
            Managers.Network.Send(movePacket); // 클라에서 서버로(C_Move) 보내기
        }
    }
}
