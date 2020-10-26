using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class MonsterController : CreatureController
{
    protected override void Init()
    {
        base.Init();
        // State, Dir 값 정해줄때 Animator가 필요한데
        // Init()에서 Animator를 정해주니깐 State나 Dir 값 대입은
        // Init()아래에 있어야 한다.
        State = CreatureState.Idle;
        Dir = MoveDir.None;
    }

    protected override void UpdateController()
    {
        // GetDirInput();
        base.UpdateController();
    }

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
}
