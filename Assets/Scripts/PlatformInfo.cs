using UnityEngine;

public sealed class PlatformInfo : MonoBehaviour
{
    [SerializeField] private GameObject mobileControls;

    private void Start()
    {
        Debug.Log($"Platform={Application.platform}, Editor={Application.isEditor}");
        Debug.Log($"Version={Application.version}, Data={Application.dataPath}");
        Debug.Log($"Save={Application.persistentDataPath}");

        // 모바일과 연습용 Editor에서는 터치 UI를 표시하고, 일반 PC Player에서는 숨깁니다.
        mobileControls.SetActive(Application.isMobilePlatform || Application.isEditor);
    }
}