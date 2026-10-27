using UnityEngine;

public sealed class CoinPickup : MonoBehaviour
{
    [SerializeField] private int coinId;

    private GameSession session;
    private Transform player;

    public void Initialize(GameSession owner, Transform target)
    {
        session = owner;
        player = target;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 아직 초기화되지 않았거나, 접촉한 Collider가 지정한 Player의 것이 아니면 무시합니다.
        if (session == null || other.transform != player)
        {
            return;
        }

        // 게임 규칙에서 수집을 승인한 경우에만 코인을 숨깁니다.
        if (session.TryCollect(coinId))
        {
            gameObject.SetActive(false);
        }
    }
}