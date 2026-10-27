using System;
using System.Collections.Generic;

public sealed class CoinRound
{
    private readonly HashSet<int> collectedIds = new HashSet<int>();

    public int Score { get; private set; }
    public float Remaining { get; private set; }

    public bool IsFinished => Remaining <= 0f;

    public CoinRound(float seconds)
    {
        Reset(seconds);
    }

    public bool TryCollect(int coinId)
    {
        // 안->밖: Add는 새 ID를 넣으면 true, 이미 있는 ID이면 false를 돌려줍니다.
        // !로 뒤집어 중복을 거절하며, 종료 상태이면 ||의 오른쪽 Add는 실행하지 않습니다.
        if (IsFinished || !collectedIds.Add(coinId))
        {
            return false;
        }

        Score += 1;

        return true;
    }

    public void Tick(float deltaTime)
    {
        // NaN은 숫자가 아닌 값입니다. 음수·NaN·무한대는 정상적인 경과 시간으로 계산하지 않습니다.
        if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        // 오->왼: 남은 시간에서 경과 시간을 빼고, 음수가 되면 0을 저장합니다.
        Remaining = Math.Max(0f, Remaining - deltaTime);
    }

    public void Reset(float seconds)
    {
        if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        // 다음 라운드에서는 같은 코인을 다시 모을 수 있도록 수집 ID를 비웁니다.
        collectedIds.Clear();
        Score = 0;
        Remaining = seconds;
    }
}