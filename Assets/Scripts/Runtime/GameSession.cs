using UnityEngine;
using TMPro;

public sealed class GameSession : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private CoinPickup[] coins;
    [SerializeField] private AudioSource collectAudio;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text hud;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private UnityEngine.UI.Button startButton;

    [SerializeField] private TMP_Text bestText;

    private const string BestKey = "PlatformCoinLab.Best";
    private int best;

    private AudioClip generatedCollectClip;
    private CoinRound round;
    private Vector3 startPosition;
    private float roundSeconds = 30f;
    private bool playing;
    private bool showingResult;
    private bool busy;
    private bool paused;

    public int Score => round.Score;
    public bool IsPlaying => playing;
    public bool CanMove => playing && !paused;
    public bool IsLoading => busy;
    // &&는 왼쪽부터 확인하므로 coins가 null이면 Length에 접근하지 않습니다.
    public bool IsReady => coins != null && coins.Length > 0 && !busy;

    private void Awake()
    {
        // 저장한 최고 점수가 없으면 기본값 0으로 시작합니다.
        best = PlayerPrefs.GetInt(BestKey, 0);
        round = new CoinRound(roundSeconds);
        startPosition = player.position;

        // 외부 AudioClip이 연결되어 있지 않을 때만 짧은 수집음을 직접 만듭니다.
        if (collectAudio != null && collectAudio.clip == null)
        {
            // 초당 22,050개 표본 중 2,205개를 사용하므로 약 0.1초 길이입니다.
            float[] samples = new float[2205];
            for (int i = 0; i < samples.Length; i++)
            {
                // i / 22050f는 현재 표본의 시간(초), 880f는 1초에 진동하는 횟수입니다.
                // Mathf.PI는 원주율(π)입니다. Sin(2π × 진동수 × 시간)에 남은 길이 비율을 곱해 소리를 서서히 줄입니다.
                // (float)는 정수 나눗셈을 피하기 위한 변환이며, 0.25f는 파형의 크기를 줄이는 계수입니다.
                samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * i / 22050f) * (1f - (float)i / samples.Length) * 0.25f;
            }

            // 표본 수·모노 1채널·초당 표본 수를 지정한 뒤, SetData로 처음 위치 0부터 파형을 채웁니다.
            generatedCollectClip = AudioClip.Create("CollectTone", samples.Length, 1, 22050, false);
            generatedCollectClip.SetData(samples, 0);

            collectAudio.clip = generatedCollectClip;
        }
    }

    private void OnDestroy()
    {
        if (generatedCollectClip != null)
        {
            // 직접 생성한 클립만 정리합니다. Inspector에 연결한 외부 음원은 제거하지 않습니다.
            Destroy(generatedCollectClip);
        }
    }

    private void Start()
    {
        SetStage(coins);
    }

    private void Update()
    {
        if (playing && !paused)
        {
            // 위->아래: 이번 프레임의 시간을 반영한 뒤 종료 여부를 판단합니다.
            round.Tick(Time.deltaTime);

            if (round.IsFinished)
            {
                FinishRound();
            }
        }

        RefreshUi();
    }

    public void BeginRound()
    {
        if (!IsReady || playing)
        {
            return;
        }

        round.Reset(roundSeconds);
        player.position = startPosition;

        foreach (CoinPickup coin in coins)
        {
            coin.gameObject.SetActive(true);
        }

        playing = true;
        showingResult = false;
        paused = false;

        RefreshUi();
    }

    public bool TryCollect(int id)
    {
        // 진행 중이 아니면 수집을 요청하지 않고, 진행 중이어도 최종 승인 여부는 CoinRound가 판단합니다.
        if (!CanMove || !round.TryCollect(id))
        {
            return false;
        }

        if (collectAudio != null)
        {
            collectAudio.Play();
        }

        return true;
    }

    public void ReturnToMenu()
    {
        playing = false;
        showingResult = false;
        paused = false;

        RefreshUi();
    }

    public void SetStage(CoinPickup[] stageCoins)
    {
        // null은 아직 스테이지가 없다는 뜻입니다. 빈 배열로 바꾸어 아래 foreach를 안전하게 건너뜁니다.
        coins = stageCoins ?? new CoinPickup[0];
        foreach (CoinPickup coin in coins)
        {
            coin.Initialize(this, player);
        }

        RefreshUi();
    }

    public void SetBusy(bool value)
    {
        busy = value;

        RefreshUi();
    }

    public void SetStatus(string message)
    {
        statusText.text = message;
    }

    public void SetRoundSeconds(int seconds)
    {
        if (playing)
        {
            return;
        }

        // 다음 라운드의 제한 시간을 허용 범위인 10~120초 안으로 맞춥니다.
        roundSeconds = Mathf.Clamp(seconds, 10, 120);
    }

    private void FinishRound()
    {
        playing = false;
        showingResult = true;
        SaveBest();
    }

    public void SaveBest()
    {
        // 기존 최고 점수와 이번 점수 중 큰 값만 남겨 기록이 낮아지지 않게 합니다.
        best = Mathf.Max(best, Score);
        PlayerPrefs.SetInt(BestKey, best);
        // 메모리에서 바꾼 저장 값을 실제 저장소에 반영합니다.
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool value)
    {
        // 중단이면 true, 복귀이면 false입니다. 같은 값을 이동·시간 처리의 잠금에도 사용합니다.
        paused = value;
        if (value)
        {
            SaveBest();
        }
    }

    private void RefreshUi()
    {
        menuPanel.SetActive(!playing && !showingResult);
        hudPanel.SetActive(playing);
        resultPanel.SetActive(showingResult);
        startButton.interactable = IsReady;
        // 0.2초가 남았을 때 0으로 보이지 않도록 올림해서 1초로 표시합니다.
        hud.text = $"Score: {Score}  Time: {Mathf.CeilToInt(round.Remaining)}";
        resultText.text = $"Score: {Score}";
        bestText.text = $"Best: {best}";
    }
}