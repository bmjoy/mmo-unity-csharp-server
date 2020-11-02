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

    // 생성할 플레이어의 정보, 지금 생성하는 플레이어가 내가 조작하는 플레이어 인가?
    public void Add(PlayerInfo info, bool myPlayer = false)
    {
        if (myPlayer)
        {
            GameObject go = Managers.Resource.Instantiate("Creature/MyPlayer");
            go.name = info.Name;
            _objects.Add(info.PlayerId, go);

            MyPlayer = go.GetComponent<MyPlayerController>();
            MyPlayer.Id = info.PlayerId;
            // 이렇게 position에 대한 클래스 하나가 있으면 코드 한줄에 모든 pos 관련 정보를 넣어줄수있다.
            MyPlayer.PosInfo = info.PosInfo;
        }
        else
        {
            // 내가 조작하는 플레이어가 아닌 경우
            GameObject go = Managers.Resource.Instantiate("Creature/Player");
            go.name = info.Name;
            _objects.Add(info.PlayerId, go);

            PlayerController pc = go.GetComponent<PlayerController>();
            pc.Id = info.PlayerId;
            pc.PosInfo = info.PosInfo;
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

    public void RemoveMyPlayer()
    {
        if (MyPlayer == null)
            return;

        Remove(MyPlayer.Id);
        MyPlayer = null;
    }

    public GameObject FindById(int id)
    {
        GameObject go = null;
        _objects.TryGetValue(id, out go);
        return go;
    }

    // 객체가 적다면 그냥저냥 쓸수있음
    public GameObject Find(Vector3Int cellPos)
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

    public void Clear()
    {
        foreach (GameObject obj in _objects.Values)
            Managers.Resource.Destroy(obj);

        _objects.Clear();
    }
}
