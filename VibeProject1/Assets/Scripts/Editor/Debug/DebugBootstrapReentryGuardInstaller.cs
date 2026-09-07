using System.Collections.Generic;
using System.Linq;
using Game.Core.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core.Editor.DebugTools
{
    /// <summary>
    /// Build Settings에 등록된 콘텐츠 씬(Bootstrap 제외) 전부에 DebugBootstrapReentryGuard를
    /// 설치/제거한다. 콘텐츠 씬 목록을 Build Settings에서 직접 읽으므로, 씬이 늘어나도
    /// 이 파일을 건드리지 않고 재실행만으로 자동 반영된다.
    /// 걷어낼 때는 이 파일과 DebugBootstrapReentryGuard.cs만 지우고 Remove 메뉴를 한 번 실행하면 된다.
    /// </summary>
    public static class DebugBootstrapReentryGuardInstaller
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        private const string GuardObjectName = "DebugBootstrapReentryGuard";

        [MenuItem("Tools/Game/Debug/Install/Bootstrap Reentry Guards")]
        public static void InstallGuards()
        {
            foreach (var scenePath in GetContentScenePaths())
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                var guardObject = FindGuardObject(scene);
                if (guardObject == null)
                {
                    guardObject = new GameObject(GuardObjectName);
                    Undo.RegisterCreatedObjectUndo(guardObject, $"Create {GuardObjectName}");
                    SceneManager.MoveGameObjectToScene(guardObject, scene);
                }

                if (guardObject.GetComponent<DebugBootstrapReentryGuard>() == null)
                {
                    Undo.AddComponent<DebugBootstrapReentryGuard>(guardObject);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("DebugBootstrapReentryGuard 설치/동기화 완료. Ctrl+S로 각 씬을 저장했다.");
        }

        [MenuItem("Tools/Game/Debug/Remove/Bootstrap Reentry Guards")]
        public static void RemoveGuards()
        {
            foreach (var scenePath in GetContentScenePaths())
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

                var guardObject = FindGuardObject(scene);
                if (guardObject != null)
                {
                    Object.DestroyImmediate(guardObject);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }

                EditorSceneManager.CloseScene(scene, true);
            }

            Debug.Log("DebugBootstrapReentryGuard 제거 완료.");
        }

        private static GameObject FindGuardObject(Scene scene)
        {
            return scene.GetRootGameObjects().FirstOrDefault(go => go.name == GuardObjectName);
        }

        // Build Settings에 등록만 되고 실제 파일은 지워진 씬(예: 기본 템플릿의 SampleScene.unity)이
        // 남아있으면 EditorSceneManager.OpenScene이 ArgumentException을 던져 이후 씬 전부를 건너뛰게
        // 만든다 - 파일 존재 여부를 먼저 걸러 그런 항목은 경고만 남기고 건너뛴다.
        private static string[] GetContentScenePaths()
        {
            var paths = new List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled || scene.path == BootstrapScenePath) continue;

                if (!System.IO.File.Exists(scene.path))
                {
                    Debug.LogWarning($"Build Settings에 등록된 '{scene.path}' 씬 파일을 찾을 수 없어 건너뛴다 - Build Settings에서 정리하는 것을 권장한다.");
                    continue;
                }

                paths.Add(scene.path);
            }
            return paths.ToArray();
        }
    }
}
