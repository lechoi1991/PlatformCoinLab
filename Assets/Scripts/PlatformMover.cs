using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlatformMover : MonoBehaviour
{
    [SerializeField] private InputActionReference move;
    [SerializeField] private GameSession session;
    [SerializeField] private float speed = 5f;

    private void OnEnable()
    {
        move.action.Enable();
    }

    private void OnDisable()
    {
        move.action.Disable();
    }

    private void Update()
    {
        if (!session.CanMove)
        {
            return;
        }

        // 대각선 입력 (1,1)의 길이는 약 1.41입니다. 최대 길이를 1로 제한해 더 빠르게 움직이지 않게 합니다.
        // 길이가 1보다 작은 스틱 입력은 그대로 유지합니다.
        Vector2 input = Vector2.ClampMagnitude(move.action.ReadValue<Vector2>(), 1f);
        // 입력의 x·y를 바닥의 X·Z 방향으로 옮깁니다.
        // 초당 속도에 이번 프레임의 시간(초)을 곱하면 이번 프레임에 이동할 거리가 됩니다.
        Vector3 next = transform.position + new Vector3(input.x, 0f, input.y) * speed * Time.deltaTime;
        // X·Z 좌표를 각각 -9~9로 제한해 바닥 밖으로 나가지 않게 합니다.
        next.x = Mathf.Clamp(next.x, -9f, 9f);
        next.z = Mathf.Clamp(next.z, -9f, 9f);
        transform.position = next;
    }
}