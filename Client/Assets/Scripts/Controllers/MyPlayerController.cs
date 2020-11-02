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
        // 내가 키보드 방향키에서 손을 떼면 -> 대기
        if (Dir == MoveDir.None)
        {
            State = CreatureState.Idle;
            CheckUpdatedFlag(); // 얘 호출은 일단 해줘야함
            return;
        }

        // 이동
        Vector3Int destPos = CellPos;
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

        // 움직이는 애니메이션 자체는 충돌여부와 상관이 없게 분리
        // 충돌이 안났을때만 애니메이션을 틀어주면
        // 가다가 충돌했을때 다시 그 방향(충돌체가 있는방향)으로 제자리 걸음하는 애니메이션이 안나옴

        // _dir이 None이 아니면 어차피 Moving 상태이고 싶은거니깐 이제 필요없다.
        // State = CreatureState.Moving;

        // 이제 맵 매니저에게 허락받고 움직여야함
        if (Managers.Map.CanGo(destPos))
        {
            // ObjectManager의 Find()를 CanGo() 안에서 호출하느냐
            // 아니면 CanGo()를 호출한대서 같이 호출하느냐 문제가 있는데
            // Find()가 변경될 여지가 있으므로 CanGo()와 분리시켜 놓는게 좋다고 생각
            if (Managers.Object.Find(destPos) == null)
            {
                // 충돌 날 물체가 없다.
                CellPos = destPos;
            }
        }

        CheckUpdatedFlag();
    }

    void CheckUpdatedFlag()
    {
        if (_updated)
        {
            // 정말 갱신사항이 있으면 서버에다가 변경사항을 보냄
            C_Move movePacket = new C_Move();
            movePacket.PosInfo = PosInfo; // xyz좌표 및 state정보를 한번에
            Managers.Network.Send(movePacket); // 클라에서 서버로(C_Move) 보내기
            _updated = false;
        }
    }
}
