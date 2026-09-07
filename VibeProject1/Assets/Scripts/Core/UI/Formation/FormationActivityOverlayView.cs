using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// 정비창 슬롯 위에 표시하는 배치/이동 진행 표시(Docs/기획/20번 §3.2/§3.3, 설계 25번 §5.1) -
    /// 반투명 유닛 아이콘 + 잔여 초 텍스트. Field 배치 UI에서만 쓰인다(Hub는 진행 활동이 없어 항상
    /// 비활성). FormationSlotView의 자식이 아니라 FormationGridView가 slotContent 아래 별도
    /// 풀(get-or-create)로 관리한다 - 렌더 순서(슬롯 배경 < 경로선 < 이 마크 < 이동 아이콘, 실전
    /// 확인) 때문에 슬롯 자식으로 두면 형제 인덱스를 독립적으로 강제할 수 없었다.
    /// </summary>
    public class FormationActivityOverlayView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text remainingSecondsText;

        public void Bind(Sprite icon, float remainingSeconds)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (remainingSecondsText != null)
            {
                remainingSecondsText.text = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds)) + "초";
            }
        }
    }
}
