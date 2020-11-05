using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

// 플레이어나 몬스터 컨트롤러의 공통부분을 여기에
public class CreatureController : BaseController
{
    HpBar _hpBar; // 화살은??

    public override StatInfo Stat
    {
        get { return base.Stat; }
        set { base.Stat = value; UpdateHpBar(); }
    }

    public override int Hp
    {
        get { return Stat.Hp; }
        set { base.Hp = value; UpdateHpBar(); }
    }

    // AddHpBar와 UpdateHpBar는 체력개념이 있는 Creature에만 사용한다.
    protected void AddHpBar()
    {
        GameObject go = Managers.Resource.Instantiate("UI/HpBar", transform);
        go.transform.localPosition = new Vector3(0, 0.5f, 0);
        go.name = "HpBar";
        _hpBar = go.GetComponent<HpBar>(); // sethp 편하게 쓰려고
        UpdateHpBar();
    }

    void UpdateHpBar()
    {
        if (_hpBar == null)
            return;

        float ratio = 0.0f;
        // 3 / 2 = 1 (int끼리 나누는 경우) 한놈을 float로 캐스팅 해야 결과값이 정확하게 나옴
        if (Stat.MaxHp > 0)
            ratio = ((float)Hp / Stat.MaxHp);

        _hpBar.SetHpBar(ratio);
    }

    // 플레이어나 몬스터 컨트롤러 클래스는 Start를 직접 쓰지 않고
    // 아래 Init을 재정의해서 쓴다.
    protected override void Init()
    {
        base.Init();
        AddHpBar();
    }

    public virtual void OnDamaged() // 누가 어떤식으로 때렸는지 인자 추가 가능
    {
        // 피격처리의 책임을 누가 질것인가?
        // 얻어맞는 쪽이 처리하는게 가장 흔하다.

        // 모두가 얻어맞는건 아니다.

        // OnDamaged 구현을 여기저기 복붙해서 쓸 필요가 없이
        // CreatureController 상속받은 해당 컨트롤러의 OnDamaged만 수정하면 됨. 

    }

    public virtual void OnDead()
    {
        // 죽음처리
        State = CreatureState.Dead;

        // 이펙트 생성
        GameObject effect = Managers.Resource.Instantiate("Effect/DieEffect");
        effect.transform.position = transform.position; // 생성위치는 맞은놈 위치
        effect.GetComponent<Animator>().Play("START");
        // 이펙트를 0.5초후에 제거해달라고 예약
        GameObject.Destroy(effect, 0.5f);
    }
}
