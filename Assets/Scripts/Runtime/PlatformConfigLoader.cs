
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public sealed class PlatformConfig
{
    public int roundSeconds;
}

public sealed class PlatformConfigLoader : MonoBehaviour
{
    [SerializeField] private GameSession session;
    [SerializeField] private string remoteUrl;

    public void ApplySettings()
    {
        // GameSession 연결 여부 확인
        if (session == null)
        {
            Debug.LogError("GameSession is not assigned.");
            return;
        }

        // 진행 중인 라운드에서는 설정을 변경하지 않습니다.
        if (session.IsPlaying || session.IsLoading || !session.IsReady)
        {
            return;
        }

        session.SetBusy(true);
        StartCoroutine(LoadSettings());
    }

    private IEnumerator LoadSettings()
    {
        int seconds = 30;

        // 1. Remote URL 우선 시도
        if (!string.IsNullOrWhiteSpace(remoteUrl))
        {
            using (UnityWebRequest request = UnityWebRequest.Get(remoteUrl))
            {
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (TryParseConfig(request.downloadHandler.text, out seconds))
                    {
                        goto ApplyConfig;
                    }

                    Debug.LogWarning("Remote config is invalid. Trying StreamingAssets.");
                }
                else
                {
                    Debug.LogWarning($"Remote config failed: {request.error}. Trying StreamingAssets.");
                }
            }
        }

        // 2. Remote URL 실패 시 StreamingAssets 사용
        {
            string path = Application.streamingAssetsPath + "/platform-config.json";
            string url = path.Contains("://") ? path : new Uri(path).AbsoluteUri;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (!TryParseConfig(request.downloadHandler.text, out seconds))
                    {
                        Debug.LogWarning("StreamingAssets config is invalid. Use default 30.");
                    }
                }
                else
                {
                    Debug.LogWarning($"StreamingAssets failed: {request.error}. Use default 30.");
                }
            }
        }

        ApplyConfig:

        // 3. 설정 적용
        session.SetRoundSeconds(seconds);
        session.SetBusy(false);
        session.SetStatus($"Next round: {seconds} seconds");
    }

    // JSON 파싱 및 유효성 검사
    private bool TryParseConfig(string json, out int seconds)
    {
        seconds = 30;

        try
        {
            PlatformConfig config = JsonUtility.FromJson<PlatformConfig>(json);

            if (config != null &&
                config.roundSeconds >= 10 &&
                config.roundSeconds <= 120)
            {
                seconds = config.roundSeconds;
                return true;
            }
        }
        catch (Exception error)
        {
            Debug.LogWarning($"Invalid JSON: {error.Message}");
        }

        return false;
    }
}