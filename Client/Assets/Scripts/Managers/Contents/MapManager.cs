using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// 실시간으로 맵을 로드 및 삭제
// 맵에 딸린 충돌 정보 로드

public class MapManager
{
    public Grid CurrentGrid { get; private set; }

    // 맵의 min,max
    public int MinX { get; set; }
    public int MaxX { get; set; }
    public int MinY { get; set; }
    public int MaxY { get; set; }

    bool[,] _collision; 

    public bool CanGo(Vector3Int cellPos)
    {
        // _collision 을 이용하여 MapManager에게 내가 가려는 위치가 이동 가능한 위치인지 질의
        if (cellPos.x < MinX || cellPos.x > MaxX)
            return false;
        if (cellPos.y < MinY || cellPos.y > MaxY)
            return false;

        // 맵 좌표가 Min 또는 Max 값부터 시작하기 때문에 
        // X의 경우 : _collision 배열에서의 대응하는 값을 찾으려면 MinX값을 빼면 된다.
        // Y의 경우 : 위로 갈수록 y값이 커지므로 MaxY값에서 빼자
        // _collision에 맵 정보를 입력할 때 위에서부터 넣었는데
        // 우리가 받은 cellPos.y는 아래서부터 얼마나 떨어져있는지 값이니깐
        // 가장 위인 MaxY 기준으로 얼마나 떨어진 좌표인지로 변환해야 한다.
        int x = cellPos.x - MinX;
        int y = MaxY - cellPos.y;
        return !_collision[y, x]; // 갈수있다 0, 갈수없다 1인데 질의가 CanGo라서 반전시켜서 뱉음
    }

    public void LoadMap(int mapId)
    {
        DestroyMap(); // 기존맵이 있을수도 있으니

        // 맵 프리펩 로드
        // Prefabs/Maps/...
        string mapName = "Map_" + mapId.ToString("000"); // 아이디가 1이면 001로 변환
        GameObject go = Managers.Resource.Instantiate($"Map/{mapName}");
        // 객체 생성됨
        go.name = "Map";

       GameObject collision = Util.FindChild(go, "Tilemap_Collision", true);
        if (collision != null)
            collision.SetActive(false);

        // 그리드 정보를 갖고있게 
        CurrentGrid = go.GetComponent<Grid>();

        // Collision 관련 파일 로드
        TextAsset txt = Managers.Resource.Load<TextAsset>($"Map/{mapName}");
        // 파싱
        StringReader reader = new StringReader(txt.text);

        // 맵 정보 파일 구조
        // 1~4줄 xMin,xMax,yMin,yMax // 나머지줄 맵 가장 윗줄부터
        MinX = int.Parse(reader.ReadLine());
        MaxX = int.Parse(reader.ReadLine());
        MinY = int.Parse(reader.ReadLine());
        MaxY = int.Parse(reader.ReadLine());

        // MinX와 MaxX 좌표도 모두 맵에 포함되기 때문에 카운트값에 +1 해줘야 한다. Y도
        int xCount = MaxX - MinX + 1;
        int yCount = MaxY - MinY + 1;
        _collision = new bool[yCount, xCount];

        // _collision에 충돌정보 입력
        for (int y = 0; y < yCount; y++)
        {
            string line = reader.ReadLine();
            for(int x = 0; x < xCount; x++)
            {
                _collision[y, x] = (line[x] == '1' ? true : false);
            }
        }
    }

    public void DestroyMap()
    {
        // 맵 객체들의 이름은 Map_***으로 통일
        GameObject map = GameObject.Find("Map");
        if(map != null)
        {
            GameObject.Destroy(map);
            CurrentGrid = null;
        }
    }

}
