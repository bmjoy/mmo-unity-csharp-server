using Google.Protobuf.Protocol;
using UnityEngine;
using static Define;

// 하는 일이 좀 단순함.. 앞으로 나가는거랑 충돌처리
// CreatureController 상속받는게 과할수도 있지만 공통으로 쓰는게 좀 있어서

public class ArrowController : CreatureController
{
    protected override void Init()
    {
        //화살 방향은 생성될때 결정
        switch (Dir)
        {
            case MoveDir.Up:
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;

            case MoveDir.Down:
                transform.rotation = Quaternion.Euler(0, 0, -180);
                break;

            case MoveDir.Left:
                transform.rotation = Quaternion.Euler(0, 0, 90);
                break;

            case MoveDir.Right:
                transform.rotation = Quaternion.Euler(0, 0, -90);
                break;
        }

        State = CreatureState.Moving;
        _speed = 15.0f;

        base.Init();
    }

    protected override void UpdateAnimation()
    {
        // 화살에는 필요 없음
        // 이렇게 하면 CreatureController의 UpdateAnimation()이 실행 안됨.
    }

    // 화살은 UpdateIdle 구현이 다르므로 오버라이딩
    protected override void MoveToNextPos()
    {
        // 움직이는 중이 아니라면 -> 움직일수있다
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

        // 화살을 맞혔을 때
        if (Managers.Map.CanGo(destPos))
        {
            // destPos에 갈수는 있는데 거기 충돌날 오브젝트가 있냐?
            GameObject go = Managers.Object.Find(destPos); // 피격받은 물체
            if (go == null)
            {
                // 화살을 이동
                CellPos = destPos;
            }
            else
            {
                // 화살이 박힘
                Debug.Log(go.name);

                // MonsterController인데 왜 CreatureController?
                // CreatureController는 MonsterController를 상속받았기 때문에
                // OnDamaged 호출시 CreatureController가 오버라이드한거(자기꺼) 호출
                // 밖에서 누구든 몹을 때리면 아래처럼 호출하면 됨
                CreatureController cc = go.GetComponent<CreatureController>();
                if (cc != null)
                    cc.OnDamaged();

                // 화살 제거
                Managers.Resource.Destroy(gameObject);
            }
        }
        else
        {
            // 화살의 경우 더이상 갈 곳이 없다면 없어져야함 (명중)
            Managers.Resource.Destroy(gameObject);
        }
    }
}