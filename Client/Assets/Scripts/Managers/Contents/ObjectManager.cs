using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager
{
    // 아직 ID도 없으니 이걸로 안함
    // Dictionary<int, GameObject> _objects = new Dictionary<int, GameObject>();
    // 리스트로 하자
    List<GameObject> _objects = new List<GameObject>();

    public void Add(GameObject go)
    {
        // 팩토리 패턴
        _objects.Add(go);
    }

    public void Remove(GameObject go)
    {
        _objects.Remove(go);
    }

    // 객체가 적다면 그냥저냥 쓸수있음
    public GameObject Find(Vector3Int cellPos)
    {
        foreach (GameObject obj in _objects)
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
        foreach (GameObject obj in _objects)
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
        _objects.Clear();
    }
}
