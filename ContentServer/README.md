# ContentServer — HTTPS 파일 서버

PlatformCoinLab의 JSON 설정(`platform-config.json`)과 Addressables 원격 콘텐츠, Web 빌드를 `https://localhost:5443`으로 제공하는 ASP.NET Core 서버입니다. (수업 구현)

## 구성

| 파일 | 역할 |
| :--- | :--- |
| `Program.cs` | 정적 파일 제공, 확장자 화이트리스트, `Cache-Control: no-cache`, `/health` |
| `ContentServer.csproj` | .NET 10 Web SDK 프로젝트 파일 |
| `Content/platform-config.json` | 원격 설정 (`roundSeconds`: 10~120) |
| `appsettings*.json`, `Properties/launchSettings.json` | 로깅·실행 프로필 (서버 주소는 `Program.cs`에서 5443 고정) |

제공 확장자: `.bundle .bin .hash .json .html .js .css .wasm .data .png .ico .svg` — 그 외(.cs, .csproj 등)는 404입니다.

## 실행 절차

필요: .NET SDK 10.x

```bash
dotnet dev-certs https --trust
```

```bash
dotnet run --project ContentServer
```

다른 폴더를 제공하려면 `--FilesRoot` 인수를 사용합니다(상대 경로는 ContentServer 폴더 기준).

```bash
dotnet run --project ContentServer -- --FilesRoot ../ServerData
```

확인:

```bash
curl -k https://localhost:5443/health
```

```bash
curl -k https://localhost:5443/platform-config.json
```

예상 결과: `{"status":"ok"}`, `{"roundSeconds":45}` (2026-11-18 확인, 미존재 파일은 404)

## 콘텐츠 생성·배포 절차

1. **설정 JSON**: `Content/platform-config.json`의 `roundSeconds`를 수정합니다. 서버 재시작 없이 다음 요청부터 반영됩니다(`no-cache`).
2. **Addressables 원격 번들**
   1. Unity에서 `Window > Asset Management > Addressables > Profiles`의 활성 프로필을 `RemotePractice`(Remote.LoadPath = `https://localhost:5443/[BuildTarget]`)로 바꿉니다.
   2. `CourseStage` 그룹의 Build/Load Path를 Remote로 지정하고 `Build > New Build > Default Build Script`를 실행합니다.
   3. 출력된 `ServerData/<BuildTarget>/` 폴더(catalog `.bin`·`.hash`, `.bundle`)를 서버 제공 폴더에 `<BuildTarget>/` 이름 그대로 복사하거나, `--FilesRoot ../ServerData`로 서버를 실행합니다.
   - 참고: Addressables 2.x 원격 카탈로그는 `catalog_*.bin`입니다. 수업 버전 서버는 `.bin`을 제공하지 않아 404가 발생했으므로 제출본에서 `.bin` 매핑을 추가했습니다. 현재 제출본의 활성 프로필은 `Default`이며 스테이지 번들은 Player 내부(Local)에서 로드합니다.
3. **Web 빌드**: Web 빌드 출력(index.html, Build, TemplateData)을 제공 폴더에 넣으면 `https://localhost:5443/`로 열 수 있습니다. 서버에 `Content-Encoding` 설정이 없으므로 **압축 Disabled**로 빌드한 Web 빌드(`Web - Desktop - Development` 프로필)를 사용합니다.

## 실패·복구 확인 방법

| 상황 | 재현 | 기대 동작 |
| :--- | :--- | :--- |
| 서버 중지 | 서버를 끄고 게임에서 설정 적용 | 원격 요청 실패 경고 후 `StreamingAssets/platform-config.json`(40초) 적용 |
| 잘못된 JSON | `Content/platform-config.json`을 `{"roundSeconds":5}` 또는 깨진 JSON으로 변경 | 검증 실패 경고 후 StreamingAssets 값 적용, 그것도 실패하면 기본값 30초 |
| 복구 | 서버 재시작·JSON 원복 후 다시 적용 | 원격 값(45초) 적용 |

## 알려진 한계

- 개발 인증서는 `localhost`에만 유효합니다. Android 실기기에서 `localhost`는 기기 자신을 뜻하고, 에뮬레이터는 호스트를 `10.0.2.2`로 접근해야 하며 개발 인증서를 신뢰하지 않습니다. 따라서 Android에서 원격 설정은 실패 → StreamingAssets 대체 경로로 동작합니다.
- CORS 헤더를 보내지 않으므로 Web 빌드는 같은 서버(같은 출처)에서 열어야 원격 설정 요청이 성공합니다.
