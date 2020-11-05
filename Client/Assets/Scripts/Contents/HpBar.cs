using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HpBar : MonoBehaviour
{
    [SerializeField]
    Transform _hpBar = null;

    public void SetHpBar(float ratio)
    {
        ratio = Mathf.Clamp(ratio, 0, 1); // 0~1 사이의 값만 나오게
        _hpBar.localScale = new Vector3(ratio, 1, 1);
    }
}
