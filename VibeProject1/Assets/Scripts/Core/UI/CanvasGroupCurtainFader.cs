using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// CanvasGroup 기반 "즉시 보이기 + EaseInCubic 페이드 아웃" 커튼 로직 공용화 - SceneTransitionCurtainView
    /// (Bootstrap 영속)와 FieldTransitionCurtainView(Field 콘텐츠 씬)가 스코프만 다를 뿐 동일한
    /// Show/FadeOut 로직을 각자 구현하고 있었다(DRY, Docs/Refactor/2026-09-08_Field.md §3 수정 L).
    /// SlideTransitionTimeline과 같은 자리에 둔다.
    /// </summary>
    internal static class CanvasGroupCurtainFader
    {
        public static void Show(GameObject curtainObject, CanvasGroup canvasGroup)
        {
            curtainObject.SetActive(true);
            canvasGroup.alpha = 1f;
        }

        public static void FadeOut(MonoBehaviour coroutineRunner, GameObject curtainObject, CanvasGroup canvasGroup, float duration, Action onComplete)
        {
            SlideTransitionTimeline.Run(coroutineRunner, duration,
                onStep: t => canvasGroup.alpha = 1f - t,
                onComplete: () =>
                {
                    canvasGroup.alpha = 0f;
                    curtainObject.SetActive(false);
                    onComplete?.Invoke();
                });
        }
    }
}
