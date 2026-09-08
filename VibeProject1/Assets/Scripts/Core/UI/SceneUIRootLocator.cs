using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    /// <summary>
    /// "씬 이름 → 그 씬의 SceneUIRoot"를 조회하는 로직 단일화. HubUIWiring/FieldUIWiring이 씬당 한 번만
    /// 호출해 하위 RegisterXxxUI 전체에 결과를 넘긴다 - 예전엔 6개 소비자가 각자 이 조회를 반복했다
    /// (DRY, Docs/Refactor/2026-09-08_Hub.md §2-2/§3 수정 J).
    /// </summary>
    public static class SceneUIRootLocator
    {
        public static bool TryFind(string sceneName, out SceneUIRoot sceneUIRoot)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                Debug.LogWarning($"'{sceneName}' 씬을 찾을 수 없다.");
                sceneUIRoot = null;
                return false;
            }

            foreach (var rootObject in scene.GetRootGameObjects())
            {
                sceneUIRoot = rootObject.GetComponentInChildren<SceneUIRoot>(true);
                if (sceneUIRoot != null)
                {
                    return true;
                }
            }

            Debug.LogWarning($"'{sceneName}' 씬에서 {nameof(SceneUIRoot)}를 찾을 수 없다.");
            sceneUIRoot = null;
            return false;
        }
    }
}
