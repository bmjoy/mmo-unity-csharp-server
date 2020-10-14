using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

// 유니티 에디터일때만 컴파일함, 라이브에서는 포함하면 안됨.
#if UNITY_EDITOR
using UnityEditor;
#endif
public class MapEditor
{
#if UNITY_EDITOR

    // % (Ctrl), # (Shift), & (Alt)
    [MenuItem("Tools/GenerateMap %#g")]
    private static void GenerateMap()
    {
        GameObject[] gameObjects = Resources.LoadAll<GameObject>("Prefabs/Map");

        foreach (GameObject go in gameObjects)
        {
            Tilemap tm = Util.FindChild<Tilemap>(go, "Tilemap_Collision", true);

            // 맵의 크기 or minmax 좌표를 받는다
            // 바이너리(01011010...)상태로 보관할지, 텍스트 상태로 보관할지 (보기쉽다)
            // blocked 목록을 파일로 빼서 서버에 줘야함
            // 갈수있는곳 0 막힌곳 1
            using (var writer = File.CreateText($"Assets/Resources/Map/{go.name}.txt"))
            {
                writer.WriteLine(tm.cellBounds.xMin);
                writer.WriteLine(tm.cellBounds.xMax);
                writer.WriteLine(tm.cellBounds.yMin);
                writer.WriteLine(tm.cellBounds.yMax);

                // 타일맵 탐색
                // 위에서부터 아래로 훑는다
                for (int y = tm.cellBounds.yMax; y >= tm.cellBounds.yMin; y--)
                {
                    // 좌에서 우로 훑는다
                    for (int x = tm.cellBounds.xMin; x <= tm.cellBounds.xMax; x++)
                    {
                        TileBase tile = tm.GetTile(new Vector3Int(x, y, 0));
                        if (tile != null)
                            writer.Write("1"); // 타일이 깔려있다
                        else
                            writer.Write("0"); // 타일이 없다.
                    }
                    // 띄워주기
                    writer.WriteLine();
                }
            }
        }

        //// 모든 포지션을 돌면서 그 위치에 타일이 깔렸는가 체크
        //List<Vector3Int> blocked = new List<Vector3Int>();
        //foreach (Vector3Int pos in tm.cellBounds.allPositionsWithin) // 타일맵의 모든 좌표 체크
        //{
        //    TileBase tile = tm.GetTile(pos);
        //    if (tile != null)
        //    {
        //        blocked.Add(pos);
        //    }
        //}
    }

#endif
}
