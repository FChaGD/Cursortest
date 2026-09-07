using TMPro;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 전투 중 Field 배치 타이머가 완료되면 합류할 신규 아군을 미리 보여주는 반투명 유령(Docs/기획/20번
    /// §3.2 확장, 설계 25번 §6.3) - 정비창 UI를 열 수 없는 전투 중에도 "곧 합류한다"는 정보를 전달한다
    /// (사용자 확정, 2026-09-05 - 원래는 정비창 UI 안에서만 표시하기로 했으나 전투 중 신규 소환 건에
    /// 한해 전장에도 표시하도록 범위를 넓혔다). 실제로 합류하면(OnAllySpawnedMidBattle) BattleViewPresenter가
    /// 이 유령을 치우고 진짜 BattleCharacterUnitView로 교체한다.
    /// </summary>
    public class BattlePendingReinforcementView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private TextMeshPro remainingSecondsText;

        // 아군 진영 색(BattleCharacterUnitView.AllyColor)과 같은 색조에 반투명 알파만 낮춘 값 -
        // 실제 진영 색과 헷갈리지 않으면서도 "아군 쪽"이라는 인상은 유지한다.
        private static readonly Color GhostColor = new(0.3f, 0.5f, 1f, 0.4f);

        public void Bind(Vector2 position, float remainingSeconds)
        {
            transform.position = position;

            if (bodyRenderer != null)
            {
                bodyRenderer.sprite = BattlePlaceholderSprite.WhiteSquare;
                bodyRenderer.color = GhostColor;
            }

            if (remainingSecondsText != null)
            {
                remainingSecondsText.text = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds)) + "초";
            }
        }
    }
}
