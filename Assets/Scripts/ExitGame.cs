using UnityEngine;

public sealed class ExitGame : MonoBehaviour
{
    [SerializeField] private GameSession session;

    public void Exit()
    {
#if UNITY_EDITOR
        // 이 줄은 Editor에서만 컴파일됩니다. 실행 중인 게임 대신 Play Mode를 종료합니다.
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        // Web에서는 탭을 강제로 닫는 대신 메뉴로 돌아가 종료 방법을 안내합니다.
        session.ReturnToMenu();
        session.SetStatus("Use the page navigation or close the tab.");
#else
        Application.Quit();
#endif
    }
}