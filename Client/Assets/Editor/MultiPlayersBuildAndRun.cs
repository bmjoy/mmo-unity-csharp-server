using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MultiPlayersBuildAndRun
{
    [MenuItem("Tools/Run MultiPlayer/2 Players")]
    static void PerformWin64Build2()
    {
        PerformWin64Build(2);
    }

    [MenuItem("Tools/Run MultiPlayer/3 Players")]
    static void PerformWin64Build3()
    {
        PerformWin64Build(3);
    }

    [MenuItem("Tools/Run MultiPlayer/4 Players")]
    static void PerformWin64Build4()
    {
        PerformWin64Build(4);
    }

    static void PerformWin64Build(int playerCount)
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,
            BuildTarget.StandaloneWindows);

        for(int i = 1; i <= playerCount; i++)
        {
            // 빌드할 씬 위치들 + 출력경로 + 빌드할종류 + 빌드후실행
            BuildPipeline.BuildPlayer(GetScenePaths()
                , "Builds/Win64/" + GetProjectName() + i.ToString() + "/" + GetProjectName() + i.ToString() + ".exe"
                , BuildTarget.StandaloneWindows64, BuildOptions.AutoRunPlayer);
        }
    }

    // 프로젝트 이름 반환
    static string GetProjectName()
    {
        // 여기서는 Client라는 이름이 추출
        string[] s = Application.dataPath.Split('/');
        return s[s.Length - 2];
    }

    static string[] GetScenePaths()
    {
        // 지금같은경우 Game.unity가 잡힘
        string[] scenes = new string[EditorBuildSettings.scenes.Length];

        for(int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }

        return scenes;
    }
}
