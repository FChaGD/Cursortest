using Game.Core;
using Game.Core.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Core.Editor.DebugTools
{
    /// <summary>
    /// 전투 뷰 강제 승리/패배 디버그 버튼(BattleDebugResultView)을 BattleManager와 같은 GameObject에
    /// 설치/제거한다. BattleGizmoInstaller와 같은 이유로 ManagerHierarchyInstaller와 분리했다 -
    /// "게임 빌드"와 "디버그 도구 켜고 끄기"는 다른 관심사라, 이 버튼만 껐다 켜고 싶을 때 전체 매니저
    /// 하이어라키를 다시 빌드할 필요가 없게 한다. Bootstrap.unity를 열고 실행해야 한다(BattleManager가
    /// 그 씬 하나에만 있음).
    /// 걷어낼 때는 이 파일과 BattleDebugResultView.cs(+.meta 전부)만 지우고 Remove 메뉴를 한 번
    /// 실행하면 된다.
    /// </summary>
    public static class BattleDebugResultInstaller
    {
        [MenuItem("Tools/Game/Debug/Install/Battle Result Buttons")]
        public static void InstallResultButtons()
        {
            var battleManager = Object.FindFirstObjectByType<BattleManager>(FindObjectsInactive.Include);
            if (battleManager == null)
            {
                Debug.LogWarning("씬에서 BattleManager를 찾을 수 없다 - Bootstrap.unity를 열고 다시 실행하라.");
                return;
            }

            if (battleManager.gameObject.GetComponent<BattleDebugResultView>() == null)
            {
                Undo.AddComponent<BattleDebugResultView>(battleManager.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(battleManager.gameObject.scene);
            Debug.Log("전투 강제 승리/패배 디버그 버튼 설치 완료. Ctrl+S로 씬을 저장했다.");
        }

        [MenuItem("Tools/Game/Debug/Remove/Battle Result Buttons")]
        public static void RemoveResultButtons()
        {
            var battleManager = Object.FindFirstObjectByType<BattleManager>(FindObjectsInactive.Include);
            if (battleManager == null)
            {
                return;
            }

            var component = battleManager.gameObject.GetComponent<BattleDebugResultView>();
            if (component != null)
            {
                Undo.DestroyObjectImmediate(component);
            }

            EditorSceneManager.MarkSceneDirty(battleManager.gameObject.scene);
            Debug.Log("전투 강제 승리/패배 디버그 버튼 제거 완료. Ctrl+S로 씬을 저장했다.");
        }
    }
}
