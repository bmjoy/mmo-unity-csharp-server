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

    public void Clear()
    {
        _objects.Clear();
    }
}
