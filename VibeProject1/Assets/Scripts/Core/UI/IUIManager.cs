using System;

namespace Game.Core
{
    public interface IUIManager
    {
        void Open(string panelId);
        void Close(string panelId);

        // 패널이 하나라도 열려있는지가 바뀔 때마다 통지한다(true=하나 이상 열림, false=전부 닫힘) -
        // 여러 패널이 중첩 전환되는 동안(예: 상행 준비→배치→다시 상행 준비)에는 계속 true만 유지되고,
        // 최상위 패널까지 완전히 닫힐 때만 false가 온다. Hub의 상시 노출 버튼처럼 "패널이 열려있는
        // 동안 숨겨야 하는 화면 요소"가 구독한다(HubUIController 참고).
        event Action<bool> OnAnyPanelOpenChanged;
    }
}
