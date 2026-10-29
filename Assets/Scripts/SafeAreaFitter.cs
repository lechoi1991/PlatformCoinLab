using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class SafeAreaFitter : MonoBehaviour
{
    private RectTransform target;
    private Rect previous;
    private Vector2Int previousSize;

    private void Awake()
    {
        target = GetComponent<RectTransform>();
    }

    private void Update()
    {
        Rect safe = Screen.safeArea;
        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        // 화면 크기가 0이면 나눗셈을 하지 않습니다.
        // 안전 영역과 화면 크기가 모두 그대로이면 같은 배치를 다시 계산하지 않습니다.
        if (size.x <= 0 || size.y <= 0 || (safe == previous && size == previousSize))
        {
            return;
        }

        previous = safe;
        previousSize = size;

        // 픽셀 좌표를 화면 크기로 나누어 Anchor가 사용하는 0~1 비율로 바꿉니다.
        // 예: 화면 너비 1000에서 안전 영역의 왼쪽이 50이면 X 최소 Anchor는 0.05입니다.
        // anchorMin은 왼쪽 아래, anchorMax는 오른쪽 위 모서리입니다.
        target.anchorMin = new Vector2(safe.xMin / size.x, safe.yMin / size.y);
        target.anchorMax = new Vector2(safe.xMax / size.x, safe.yMax / size.y);
        // 추가 픽셀 여백을 없애 RectTransform이 위에서 정한 Anchor 영역을 그대로 채우게 합니다.
        target.offsetMin = Vector2.zero;
        target.offsetMax = Vector2.zero;
    }
}