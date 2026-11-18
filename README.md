# PlatformCoinLab — 게임플랫폼 포트폴리오

> 표기 규칙 — **[수업]** 수업에서 따라 구현 · **[외부]** 외부 자료·도구 활용 · **[추가]** 직접 추가·개선
> 이미지는 `Submission/Evidence/` 폴더의 파일을 링크한다. 파일명은 [Submission/Evidence/README.md](Submission/Evidence/README.md)를 따른다.

---

## 1. 프로젝트 소개

| 항목 | 내용 |
| :--- | :--- |
| 게임 이름 | PlatformCoinLab |
| 장르 | 3D 탑뷰 시간 제한 코인 수집 캐주얼 |
| 대상 플랫폼 | PC (Windows x64) · Android (APK, ARM64) · Web (WebGL) — iOS 제외 |
| 핵심 플레이 | 제한 시간(기본 30초, 설정 JSON으로 10~120초) 안에 바닥 위의 코인을 최대한 모은다. 라운드가 끝나면 점수와 최고 점수를 표시하고 재시작할 수 있다. |

## 2. 개발 정보

| 항목 | 내용 |
| :--- | :--- |
| 제작자 | 최종화 (GitHub: lechoi1991) |
| 개발 기간 | 2026-10-26 ~ 2026-11-10 (수업 실습, Git 기록 기준) · 2026-11-18 제출 정리 |
| Unity | 6000.6.7f1 (URP, PC/Mobile 렌더러 분리) |
| 주요 패키지 | Addressables 2.11.2 · Input System 1.20.1 · URP 17.6.0 · uGUI 2.6.0 (TextMeshPro 포함) |
| 서버 | ASP.NET Core (.NET 10, SDK 10.0.402) |
| 앱 정보 | productName `PlatformCoinLab`, version `0.1.0`, Android minSdk 26 |

## 3. 실행 자료

| 자료 | 위치 (제출 패키지 기준) | 상태 |
| :--- | :--- | :--- |
| Unity 프로젝트 | `UnityProject/` (Assets·Packages·ProjectSettings) | 준비됨 |
| 시작 씬 | `Assets/Scenes/PlatformMain.unity` (Build Settings 유일 씬) | 준비됨 |
| PC 빌드 | `Builds/Windows/PlatformCoinLab.exe` + `PlatformCoinLab_Data/` 등 | 빌드 성공 (2026-11-18), 실행 시 오류 로그 없음 |
| Android 빌드 | `Builds/Android/PlatformCoinLab.apk` (32.5 MB, 디버그 키 서명) | 빌드 성공 (2026-11-18) |
| Web 빌드 | `Builds/Web/` (index.html·Build·TemplateData·StreamingAssets, 압축 없음) | 빌드 성공 (2026-11-18), ContentServer 제공 확인|
| 파일 서버 | `ContentServer/` — 실행 절차는 [ContentServer/README.md](ContentServer/README.md) | 준비됨 (2026-11-18 실행 확인) |
| Addressables 원격 출력 | `UnityProject/ServerData/StandaloneWindows64/` | 수업 실습 출력 |
| 확인 자료 | `Docs/Evidence/` |  |

> 이 Git 저장소에는 빌드 결과물(`Builds/`)이 포함되지 않습니다(`.gitignore`). 저장소를 받은 뒤 아래 메뉴로 다시 빌드하거나, 별도로 제출한 제출 패키지(zip)의 `Builds/`를 사용합니다.

빌드는 Editor 메뉴 `Tools > Platform Lab > Submission > Build Windows / Build Android APK / Build Web`으로 만든다(`Assets/Scripts/Editor/SubmissionBuilder.cs`, Addressables 콘텐츠 → Player 순서). 제출 패키지는 `Submission/Tools/Make-SubmissionPackage.ps1`로 만든다.

---

## 4. 주요 구현 기능

각 기능은 *무엇 → 왜 → 어떻게 → 실행 결과* 순서로 정리한다.

### 4.1 라운드 규칙과 자료 구조 [수업]

- **무엇**: 점수·남은 시간·코인 중복 수집 방지를 담당하는 순수 C# 규칙 클래스.
- **왜**: MonoBehaviour와 분리해 규칙만 독립적으로 검증하고, 같은 코인이 두 번 점수가 되는 문제를 막기 위해.
- **어떻게**: `HashSet<int> collectedIds`로 수집한 코인 ID를 기록한다. `TryCollect`는 종료 상태이거나 `Add`가 false(이미 수집)이면 거절한다. `Tick`은 음수·NaN·무한대 경과 시간을 예외로 거절하고 남은 시간을 0 아래로 내리지 않는다. `Reset`은 집합을 비워 재시작을 지원한다.
- **핵심 코드**: [CoinRound.cs](Assets/Scripts/Core/CoinRound.cs) — `TryCollect`, `Tick`, `Reset`
- **실행 결과**: 같은 코인 재접촉 시 점수 미증가 화면

### 4.2 게임 흐름: 시작·진행·종료·재시작 [수업]

- **무엇**: 메뉴 → HUD(진행) → 결과 → 재시작/메뉴의 상태 흐름.
- **왜**: 플랫폼 공통으로 같은 흐름을 유지하고, 로딩 중·진행 중에 잘못된 입력(시작, 설정 변경)을 막기 위해.
- **어떻게**: `GameSession`이 `playing / showingResult / busy / paused` 플래그로 상태를 관리한다.
  1. `Start` → `SetStage`로 코인 연결, `IsReady`(코인 있음 && 로딩 아님)일 때만 Start 버튼 활성
  2. `BeginRound` → `CoinRound.Reset`, 플레이어·코인 위치 복원
  3. `Update` → `round.Tick(Time.deltaTime)` → `IsFinished`면 `FinishRound` → `SaveBest`
  4. Restart(`BeginRound`) / Menu(`ReturnToMenu`) / Quit(`ExitGame.Exit`)
- **핵심 코드**: [GameSession.cs](Assets/Scripts/Runtime/GameSession.cs) — `BeginRound`, `Update`, `FinishRound`, `ReturnToMenu`, `RefreshUi`

### 4.3 입력: PC 키보드·게임패드, Android 터치 [수업]

- **무엇**: 하나의 `Move` 액션으로 WASD·방향키·게임패드 왼쪽 스틱·화면 터치 스틱/버튼을 모두 처리.
- **왜**: 플랫폼마다 이동 코드를 따로 두지 않기 위해.
- **어떻게**: `PlatformControls.inputactions`의 `Gameplay/Move`(2D Vector). 터치는 Input System의 `OnScreenStick`(`<Gamepad>/leftStick`)과 `OnScreenButton`(W/A/S/D)이 같은 액션에 값을 넣는다. `PlatformMover`는 입력 길이를 `ClampMagnitude(…, 1)`로 제한(대각선 가속 방지)하고 `Time.deltaTime`으로 프레임 독립 이동, X·Z를 -9~9로 제한한다. `PlatformInfo`는 모바일·Editor에서만 터치 UI를 켠다.
- **핵심 코드**: [PlatformMover.cs](Assets/Scripts/PlatformMover.cs), [PlatformInfo.cs](Assets/Scripts/PlatformInfo.cs), `Assets/Input/PlatformControls.inputactions`

### 4.4 화면 비율·Safe Area [수업]

- **무엇**: 노치·펀치홀·제스처 영역을 피해 UI를 배치.
- **어떻게**: `SafeAreaFitter`가 `Screen.safeArea`를 화면 크기로 나눠 `anchorMin/Max`(0~1)로 변환한다. 안전 영역과 화면 크기가 바뀐 경우에만 다시 계산하고, 크기 0일 때 나눗셈을 건너뛴다(회전·창 크기 변경 대응).
- **핵심 코드**: [SafeAreaFitter.cs](Assets/Scripts/SafeAreaFitter.cs) — `Update`

### 4.5 저장·재실행, 중단·복귀, 효과음 [수업]

- **저장**: 최고 점수를 `PlayerPrefs`(`PlatformCoinLab.Best`)에 저장. `Mathf.Max`로 기록이 낮아지지 않게 하고 `PlayerPrefs.Save()`로 즉시 반영 — 종료 시, 앱 중단 시, Save 버튼으로 저장. 재실행 시 `Awake`에서 읽고 없으면 0.
- **중단·복귀**: `OnApplicationPause(true)`에서 `paused`로 이동·타이머를 멈추고 저장, 복귀(false) 시 재개.
- **효과음**: 외부 AudioClip이 없으면 880Hz·0.1초 감쇠 사인파를 `AudioClip.Create`로 생성해 재생하고, `OnDestroy`에서 직접 만든 클립만 해제.
- **핵심 코드**: [GameSession.cs](Assets/Scripts/Runtime/GameSession.cs) — `Awake`, `SaveBest`, `OnApplicationPause`, `TryCollect`, `OnDestroy`

### 4.6 Editor 전용 코드 분리와 플랫폼별 처리 [수업]

- `Assets/Scripts/Editor/` 폴더의 코드(`PlatformDiagnostics`, `SubmissionBuilder`)는 Editor 어셈블리로만 컴파일되어 Player에 포함되지 않는다.
- `ExitGame.Exit`는 조건부 컴파일로 분기한다: `UNITY_EDITOR` → Play Mode 종료, `UNITY_WEBGL` → 탭을 닫을 수 없으므로 메뉴 복귀 + 안내, 그 외 → `Application.Quit()`.
- 핵심 코드: [ExitGame.cs](Assets/Scripts/ExitGame.cs), [PlatformDiagnostics.cs](Assets/Scripts/Editor/PlatformDiagnostics.cs)

### 4.7 Addressables 비동기 로드·실패 안내·재시도·해제 [수업]

- **무엇**: 스테이지 프리팹(`stage-main`, 그룹 `CourseStage`)을 비동기로 생성.
- **왜**: 콘텐츠를 Player와 분리해 교체·원격 배포할 수 있게 하고, 로딩 중 화면이 멈추지 않게 하기 위해.
- **어떻게 (처리 순서)**
  1. `LoadStage` → 진행·로딩 중이면 무시, `SetBusy(true)`로 Start 잠금, 이전 코인 참조 해제
  2. 이전 핸들 반납(`ReleaseOwnedOperation`) 후 `Addressables.InstantiateAsync`
  3. `OnCompleted` — 실패: `"Stage load failed. Press Reload Stage."` 안내 + 핸들 반납 / 코인 0개: 안내 + 반납 / 성공: `SetStage(coins)`
  4. **재시도**: RELOAD STAGE 버튼 → `LoadStage`
  5. **해제**: 성공 핸들은 `ReleaseInstance`, 실패 핸들은 `Release`. 로딩 중 오브젝트가 파괴되면 `ownerDestroyed` 플래그로 완료 시점에 해제
- **핵심 코드**: [StageLoader.cs](Assets/Scripts/Runtime/StageLoader.cs) — `LoadStage`, `OnCompleted`, `ReleaseOwnedOperation`, `OnDestroy`

### 4.8 JSON 설정 검증·적용·기본값 [수업]

- **무엇**: 라운드 시간(`roundSeconds`)을 원격 JSON → StreamingAssets JSON → 기본값 30 순서로 결정.
- **어떻게**: LOAD SETTINGS 버튼 → `ApplySettings`(진행·로딩 중이면 무시) → `LoadSettings` 코루틴. `UnityWebRequest`(timeout 10초)로 `https://localhost:5443/platform-config.json` 요청 → 실패/검증 실패 시 `StreamingAssets/platform-config.json`(Android·Web은 URL 그대로, PC는 `file://` URI로 변환) → 그것도 실패 시 30. `TryParseConfig`는 `JsonUtility` 예외를 잡고 10~120 범위만 허용하며, `GameSession.SetRoundSeconds`도 한 번 더 `Clamp`한다.
- **핵심 코드**: [PlatformConfigLoader.cs](Assets/Scripts/Runtime/PlatformConfigLoader.cs) — `ApplySettings`, `LoadSettings`, `TryParseConfig`
- **값**: 서버 45초 · StreamingAssets 40초 · 기본 30초 → 화면의 `Next round: N seconds`로 어느 경로가 적용됐는지 구분 가능

### 4.9 ASP.NET Core HTTPS 파일 서버 [수업]

- 지정 폴더(`Content`, `--FilesRoot`로 변경) 하나만 제공하고, 확장자 화이트리스트로 소스 파일 노출을 막는다. `Cache-Control: no-cache`로 수정한 설정이 바로 반영되며 `/health`로 상태를 확인한다.
- 실패·복구 확인 절차: [ContentServer/README.md](ContentServer/README.md#실패복구-확인-방법)
- 실행 결과(2026-11-18, Windows 로컬): `/health` → `{"status":"ok"}`, `/platform-config.json` → 200 `application/json` `no-cache` `{"roundSeconds":45}`, 없는 파일 → 404

---

## 5. 플랫폼 대응

| 항목 | PC (Windows) | Android | Web |
| :--- | :--- | :--- | :--- |
| 입력 | 키보드 WASD·방향키, 게임패드 | 화면 스틱·W/A/S/D 터치 버튼 | 키보드 |
| 화면 | 전체 화면(`fullscreenMode: 1`), PC_RPAsset | 전체 화면, Safe Area, Mobile_RPAsset | 브라우저 캔버스 |
| 저장 | PlayerPrefs → 레지스트리 | PlayerPrefs → SharedPreferences | PlayerPrefs → IndexedDB |
| 생명주기 | `Application.Quit`, 백그라운드 실행 안 함(`runInBackground: 0`) | 홈/복귀 시 `OnApplicationPause`로 일시정지·저장 | 탭 닫기 불가 → 메뉴 복귀 안내 |
| 콘텐츠 | Addressables Local + 원격 JSON | 원격 JSON 실패 시 StreamingAssets(APK 내부 jar URL) | 같은 서버에서 열 때 원격 JSON 성공 (CORS 미설정) |
| 빌드 | 성공 (106 MB, 1분 27초) | 성공 (APK 32.5 MB, 10분 39초, ASCII 경로 복사본에서 빌드 — 7장 사례 3) | 성공 (48.7 MB, 10분 3초) |
| 실행 결과 | Player 실행 로그 `Platform=WindowsPlayer, Version=0.1.0`, 예외 없음. |  | ContentServer에서 index.html·loader·framework·wasm·data 200 및 올바른 MIME 확인.|

> Android 실행 결과는 **실기기**와 **에뮬레이터**를 나눠 적는다. 교사 시연 결과는 본인 결과로 적지 않는다.

---

## 6. 추가·개선한 부분

수업 기능(4장)은 모두 **수업 기본 구현**이다. 제출 준비 과정에서 아래를 추가·수정했다. (작업 시 AI 코딩 도구 Claude Code의 도움을 받았으므로 **[외부]**도 함께 표시한다.)

| 항목 | 수업 구현과의 차이 | 필요한 이유 | 구현 방법 | 동작 결과 |
| :--- | :--- | :--- | :--- | :--- |
| 서버 프로젝트 파일 복원 [추가][외부] | 저장소에 `ContentServer.csproj`가 없었음 | `.gitignore`의 `*.csproj` 규칙 때문에 서버를 저장소만으로 빌드할 수 없음 | Web SDK·net10.0 `.csproj` 작성, `.gitignore`에 예외 추가, 커밋되어 있던 `bin/obj` 추적 해제 | `dotnet build` 경고 0·오류 0, 서버 실행·응답 확인 |
| 원격 카탈로그 `.bin` 제공 [추가][외부] | 수업 서버는 `.bin` 미제공 | Addressables 2.x 원격 카탈로그는 `catalog_*.bin`이라 404 발생 | `Program.cs`에 `.bin` → `application/octet-stream` 매핑 추가 | `--FilesRoot ../ServerData`로 실행 시 `catalog_0.1.0.bin` 404 → 200 확인(2026-11-18). |
| 제출 빌드 스크립트 [추가][외부] | 수동 빌드 | 세 플랫폼 빌드를 같은 순서·같은 버전으로 재현하기 위해 | `SubmissionBuilder`: 타깃 전환 → Addressables 콘텐츠 빌드 → Player 빌드 → `Builds/build-log.txt` 기록, 배치 모드 실패 시 종료 코드 1 | 배치 모드로 Windows·Web·Android 빌드 성공 (2026-11-18, `Builds/build-log.txt`) |
| 제출 패키지 스크립트 [추가][외부] | 없음 | Library·Temp·bin·obj·서명 키 제외 규칙을 매번 지키기 위해 | `Make-SubmissionPackage.ps1`: 필요한 폴더만 복사, 키·인증서 확장자 제외, `.meta` 누락 검사, zip 생성 | 빌드 후 실행 |

---

## 7. 문제 해결 사례

### 사례 1. StageLoader 주석 인코딩 깨짐 [추가]

| 단계 | 내용 |
| :--- | :--- |
| 문제 | `StageLoader.cs`의 한글 주석이 VS Code·GitHub·Git diff에서 `��û�� ����…`처럼 깨져 보임. 다른 스크립트는 정상 |
| 재현 조건 | 해당 파일만 CP949(EUC-KR)로 저장되어 있음. UTF-8로 읽는 도구(GitHub 웹, `git diff`, VS Code 기본 설정)에서 열기 |
| 원인 | Day06 커밋 시 파일이 시스템 기본 인코딩(CP949)으로 저장됨. 프로젝트의 다른 `.cs`는 UTF-8 |
| 수정 | `iconv -f CP949 -t UTF-8`로 변환. 코드 줄은 그대로이고 주석 11줄만 바뀜 (`git diff --ignore-cr-at-eol --stat`로 확인) |
| 재검증 | 변환 후 주석이 정상 표시됨(예: “요청을 시작하기 전에 Start와 중복 로드를 잠그고…”), Unity 배치 모드 컴파일 오류 0 |

### 사례 2. 저장소만으로 ContentServer를 빌드할 수 없음 [추가]

| 단계 | 내용 |
| :--- | :--- |
| 문제 | 저장소를 새로 받으면 `dotnet run --project ContentServer`가 프로젝트 파일을 찾지 못함 |
| 재현 조건 | 저장소 클론 → `ContentServer/` 안에 `.csproj` 없음. 대신 다른 PC의 `bin/obj` 출력만 커밋되어 있음 (`obj/project.assets.json`의 원래 경로가 다른 PC 경로) |
| 원인 | Unity용 `.gitignore`의 `*.csproj` 규칙이 서버 프로젝트 파일까지 제외 |
| 수정 | `ContentServer.csproj` 복원, `.gitignore`에 `!/ContentServer/ContentServer.csproj`와 `bin/obj` 제외 추가, `git rm --cached`로 `bin/obj` 추적 해제 |
| 재검증 | `dotnet build` 성공(경고 0·오류 0), 실행 후 `/health` 200, 설정 JSON 200, 없는 파일 404 |

### 사례 3. 한글 경로에서 Android 빌드 실패 [추가]

| 단계 | 내용 |
| :--- | :--- |
| 문제 | Windows·Web 빌드는 성공했지만 Android 빌드만 0.8초 만에 실패 (`errors=1`, APK 없음) |
| 재현 조건 | 프로젝트가 `C:\Users\…\OneDrive\문서\GitHub\PlatformCoinLab`처럼 한글이 들어간 경로에 있을 때 `SubmissionBuilder.BuildAndroid` 실행 |
| 원인 | 빌드 로그: `UnityException: Invalid project path … contains non-ASCII characters at position 24, Android Tools don't work properly with non-ASCII paths` — Gradle·NDK 도구가 비 ASCII 경로를 지원하지 않음 |
| 수정 | 프로젝트(Assets·Packages·ProjectSettings·Library)를 ASCII 경로 `C:\Build\PlatformCoinLab`에 복사해 같은 메서드로 빌드한 뒤 APK만 원래 `Builds/Android`로 가져옴 |
| 재검증 | `Android Succeeded size=738513917 bytes time=00:10:39 errors=0`, APK 32.5 MB 생성 (`Builds/build-log.txt`에 실패·성공 기록 모두 남음) |
| 근본 해결 | 프로젝트 자체를 한글이 없는 경로(예: `C:\Dev\PlatformCoinLab`)로 옮기면 복사 없이 빌드 가능 |

---

## 8. 성능 개선 — UI 갱신 조건부 처리 [수업에서 재현·개선]

| 항목 | 내용 |
| :--- | :--- |
| 대상 | `GameSession.RefreshUi` (매 프레임 호출) |
| 수정 전 | Day04 커밋 `b05b437`: 매 프레임 패널 `SetActive`, 버튼 상태, `$"Score: … Time: …"` 문자열 3개 생성·TMP 텍스트 대입 |
| 수정 후 | DAY14 커밋 `72005a9`: 표시 값(점수, **올림한 초**, 최고 점수, 상태 플래그)이 바뀐 경우만 갱신. `ProfilerMarker("CoinLab.PresentUi")`로 구간 측정 |
| 기대 효과 | 30초 라운드에서 텍스트 갱신이 프레임마다 → 약 1초에 1회(+점수 변경 시)로 줄어 GC Alloc과 TMP 메시 재생성 감소 |
| 기능 유지 | 점수·시간·최고 점수 표시와 패널 전환이 수정 전과 같음 |
| 측정 한계 | Editor가 아닌 실제 Player에서 측정해야 함. Development Build 자체 오버헤드 포함. 수정 전 코드는 마커가 없으므로 같은 위치에 마커만 추가한 빌드로 측정 |

측정 방법: 수정 전 버전은 `git stash` 후 `git checkout b05b437 -- Assets/Scripts/Runtime/GameSession.cs`로 되돌리고 `RefreshUi`에 같은 `ProfilerMarker`를 넣어 빌드 → Profiler(CPU Usage, Hierarchy에서 `CoinLab.PresentUi`, GC Alloc 열) 캡처 → 원래 파일로 복원 후 같은 조건으로 캡처.

---

## 9. 실행·복원 방법과 한계

### 조작법

| 동작 | PC | Android |
| :--- | :--- | :--- |
| 이동 | WASD / 방향키 / 게임패드 왼쪽 스틱 | 화면 스틱 또는 W·A·S·D 버튼 |
| 메뉴 | Start · LOAD SETTINGS · RELOAD STAGE · Save · Quit | 같음 (터치) |
| 결과 화면 | Restart · Menu | 같음 |

### 복원·빌드 순서

1. Unity Hub에서 **6000.6.7f1**(Android Build Support, Web Build Support, Windows Build Support 모듈 포함)로 `UnityProject/`를 연다. 패키지는 `Packages/manifest.json`대로 자동 설치된다.
2. 시작 씬 `Assets/Scenes/PlatformMain.unity`를 연다.
3. (서버 사용 시) [ContentServer/README.md](ContentServer/README.md) 순서로 서버 실행: `dotnet dev-certs https --trust` → `dotnet run --project ContentServer`.
4. `Tools > Platform Lab > Submission > Build …` 실행 — Addressables 콘텐츠 빌드 후 Player 빌드가 `Builds/<플랫폼>`에 생성된다.
   - `Build Web`은 빌드하는 동안만 압축을 Disabled로 바꾸고 끝나면 원래 설정으로 되돌린다(ContentServer가 `Content-Encoding`을 보내지 않기 때문).
   - Android는 프로젝트 경로에 한글 등 비 ASCII 문자가 있으면 실패한다. 한글이 없는 경로에서 연다(7장 사례 3).
5. PC: `Builds/Windows/PlatformCoinLab.exe` 실행 / Android: `adb install -r Builds/Android/PlatformCoinLab.apk` / Web: 빌드 폴더를 서버 제공 폴더에 두고 `https://localhost:5443/` 접속.

### 알려진 문제

- Android에서 `https://localhost:5443`은 기기 자신을 가리키고 개발 인증서도 신뢰하지 않으므로 원격 설정은 실패하고 StreamingAssets(40초)가 적용된다(의도된 대체 경로).
- 서버에 CORS·`Content-Encoding` 설정이 없어 Web 빌드는 같은 서버에서, 압축 없이 열어야 한다. Brotli Release 빌드는 다른 정적 서버가 필요하다.
- Android 서명은 Unity 디버그 키를 사용한다(서명 키는 제출하지 않음). 패키지 이름은 템플릿 기본값(`com.UnityTechnologies.com.unity.template.urpblank`)이다.
- 저장소 루트의 `Data/Plugins/lib_burst_generated.*`는 이전 Web 빌드에서 생긴 부산물로 보이며 제출 대상이 아니다.