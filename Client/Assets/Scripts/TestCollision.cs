using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TestCollision : MonoBehaviour
{
    // 타일이 깔린 영역과 깔리지 않은 영역을 구분해서 파일로 추출한다.
    public Tilemap _tilemap;
    public TileBase _tile;

    // Start is called before the first frame update
    void Start()
    {
        _tilemap.SetTile(new Vector3Int(0, 0, 0), _tile); // 0,0,0 위치에 지정한 타일이 깔림
    }

    // Update is called once per frame
    void Update()
    {
        // 모든 포지션을 돌면서 그 위치에 타일이 깔렸는가 체크
        List<Vector3Int> blocked = new List<Vector3Int>();
        foreach (Vector3Int pos in _tilemap.cellBounds.allPositionsWithin) // 타일맵의 모든 좌표 체크
        {
            TileBase tile = _tilemap.GetTile(pos);
            if(tile != null)
            {
                blocked.Add(pos);
            }
        }
    }
}
