using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager
{
    // 나 자신(플레이중인 캐릭터)는 접근하기 편하게 한느게 낫다
    public MyPlayerController MyPlayer { get; set; } 
    // 딕셔너리 하나에 넣어서 관리해도 되고 종류별로 딕셔너리 늘려도 됨
    Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();
    
    public static GameObjectType GetObjectTypeById(int id) // 서버에서 긁어와도 된다.
    {
        int type = (id >> 24) & 0x7F;
        return (GameObjectType)type;
    }

    public void Add(ObjectInfo info, bool myPlayer = false)
    {
        // 이제 add 전에 타입 구하고 그 타입별로 다르게 add 해야함
        GameObjectType objectType = GetObjectTypeById(info.ObjectId);
        if(objectType == GameObjectType.Player)
        {
            if (myPlayer)
            {
                GameObject go = Managers.Resource.Instantiate("Creature/MyPlayer");
                go.name = info.Name;
                _objects.Add(info.ObjectId, go);

                MyPlayer = go.GetComponent<MyPlayerController>();
                MyPlayer.Id = info.ObjectId;
                // 이렇게 position에 대한 클래스 하나가 있으면 코드 한줄에 모든 pos 관련 정보를 넣어줄수있다.
                MyPlayer.PosInfo = info.PosInfo;
                MyPlayer.Stat = info.StatInfo;
                MyPlayer.SyncPos(); // 스르륵 이동 없이 즉시 좌표이동을 반영
            }
            else
            {
                // 내가 조작하는 플레이어가 아닌 경우
                GameObject go = Managers.Resource.Instantiate("Creature/Player");
                go.name = info.Name;
                _objects.Add(info.ObjectId, go);

                PlayerController pc = go.GetComponent<PlayerController>();
                pc.Id = info.ObjectId;
                pc.PosInfo = info.PosInfo;
                pc.Stat = info.StatInfo;
                pc.SyncPos();
            }
        }
        else if (objectType == GameObjectType.Monster)
        {
            // 몬스터 생성
            GameObject go = Managers.Resource.Instantiate("Creature/Monster");
            go.name = info.Name;
            _objects.Add(info.ObjectId, go);

            MonsterController mc = go.GetComponent<MonsterController>();
            mc.Id = info.ObjectId;
            mc.PosInfo = info.PosInfo;
            mc.Stat = info.StatInfo;
            mc.SyncPos();
        }
        else if (objectType == GameObjectType.Projectile)
        {
            // 이건 여러가지 종류가 있을텐데
            // 일단 화살이라 치자
            GameObject go = Managers.Resource.Instantiate("Creature/Arrow");
            go.name = "Arrow";
            _objects.Add(info.ObjectId, go);

            ArrowController ac = go.GetComponent<ArrowController>();
            ac.PosInfo = info.PosInfo;
            ac.Stat = info.StatInfo;
            ac.SyncPos(); // 이건 좀 햇갈리네
        }
    }

    public void Remove(int id)
    {
        GameObject go = FindById(id);
        if (go == null)
            return;

        _objects.Remove(id);
        Managers.Resource.Destroy(go);
    }

    public GameObject FindById(int id)
    {
        GameObject go = null;
        _objects.TryGetValue(id, out go);
        return go;
    }

    // 객체가 적다면 그냥저냥 쓸수있음
    // 이제 화살은 CreatureController 상속받지 않아서 따로 만들어야함
    public GameObject FindCreature(Vector3Int cellPos)
    {
        foreach (GameObject obj in _objects.Values)
        {
            CreatureController cc = obj.GetComponent<CreatureController>();
            if (cc == null)
                continue;

            // cellPos에 obj가 있다.
            if (cc.CellPos == cellPos)
                return obj;
        }
        // 암것도 없더라
        return null;
    }

    public GameObject Find(Func<GameObject, bool> condition)
    {
        // 오브젝트를 던지고 이게 있나 없나 찾아줌
        foreach (GameObject obj in _objects.Values)
        {
            // 내가 던져준 조건에 부합하는애면 그대로 반환
            if (condition.Invoke(obj))
                return obj;
        }

        // 조건에 안맞더라
        return null;
    }

    // 퇴장할때 기존 오브젝트를 밀어놓지 않으면
    // 재입장하면서 _objects에 다시 모든 오브젝트들을 밀어넣기 때문에 id들이 겹쳐버림
    public void Clear()
    {
        foreach (GameObject obj in _objects.Values)
            Managers.Resource.Destroy(obj);

        _objects.Clear();
        MyPlayer = null;
    }
}
