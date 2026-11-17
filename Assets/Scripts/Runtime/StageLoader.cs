using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class StageLoader : MonoBehaviour
{
    [SerializeField] private GameSession session;
    [SerializeField] private string address = "stage-main";

    private AsyncOperationHandle<GameObject> handle;
    private bool ownerDestroyed;

    private void Start()
    {
        LoadStage();
    }

    public void LoadStage()
    {
        if (session.IsPlaying || session.IsLoading)
        {
            return;
        }

        // 요청을 시작하기 전에 Start와 중복 로드를 잠그고 이전 코인 참조를 비웁니다.
        session.SetBusy(true);
        session.SetStatus("Loading stage...");
        session.SetStage(null);

        // 이전 스테이지와 요청 참조를 반납한 뒤 새 핸들을 저장합니다.
        ReleaseOwnedOperation();

        // 프리팹을 원점·기본 회전으로 생성 요청하고, 완료 처리는 아래 콜백에 맡깁니다.
        handle = Addressables.InstantiateAsync(address, Vector3.zero, Quaternion.identity);
        // 성공과 실패 모두 완료 콜백에서 확인합니다.
        handle.Completed += OnCompleted;
    }

    private void OnCompleted(AsyncOperationHandle<GameObject> operation)
    {
        // 요청한 오브젝트가 먼저 파괴되었다면, 도착한 결과는 씬에 연결하지 않고 해제합니다.
        if (ownerDestroyed)
        {
            ReleaseOwnedOperation();
            return;
        }

        if (operation.Status != AsyncOperationStatus.Succeeded)
        {
            session.SetBusy(false);
            session.SetStatus("Stage load failed. Press Reload Stage.");
            Debug.LogError(operation.OperationException);
            ReleaseOwnedOperation();
            return;
        }

        // true를 주어 비활성 코인까지 포함합니다. 로드 성공 후에만 Result에 접근합니다.
        CoinPickup[] coins = operation.Result.GetComponentsInChildren<CoinPickup>(true);
        if (coins.Length == 0)
        {
            ReleaseOwnedOperation();
            session.SetBusy(false);
            session.SetStatus("Stage has no coins. Check the prefab.");
            return;
        }

        session.SetStage(coins);
        session.SetBusy(false);
        session.SetStatus("Stage ready. Press Start.");
    }

    private void OnDestroy()
    {
        ownerDestroyed = true;

        // 진행 중인 요청은 여기서 취소되지 않습니다. 완료된 요청만 지금 정리하고,
        // 아직 진행 중이면 위 OnCompleted의 ownerDestroyed 분기에서 나중에 정리합니다.
        if (handle.IsValid() && handle.IsDone)
        {
            ReleaseOwnedOperation();
        }
    }

    private void ReleaseOwnedOperation()
    {
        if (!handle.IsValid())
        {
            return;
        }

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // 성공한 InstantiateAsync 요청은 생성된 인스턴스와 참조를 함께 해제합니다.
            Addressables.ReleaseInstance(handle);
        }
        else
        {
            // 실패한 요청은 인스턴스가 없지만, 요청 핸들이 가진 참조는 반납해야 합니다.
            Addressables.Release(handle);
        }

        // 정리한 핸들을 비워 이후 호출에서 다시 해제하지 않게 합니다.
        handle = default;
    }
}