# 확인 자료 폴더

소개 문서([../../README.md](../../README.md))가 링크하는 파일명입니다. 같은 제출 버전의 빌드에서 캡처합니다.

| 폴더 | 파일명 | 내용 |
| :--- | :--- | :--- |
| `Images/` | `01_main_pc.png` | 대표 이미지 (PC Player 플레이 화면) |
| | `02_collect.png` | 코인 수집·점수 증가 |
| | `03_flow_menu.png`, `03_flow_hud.png`, `03_flow_result.png` | 시작·진행·종료 화면 |
| | `04_safearea_portrait.png`, `04_safearea_landscape.png` | Safe Area (기기/시뮬레이터 이름 기재) |
| | `05_android_device.png` 또는 `05_android_emulator.png` | Android 실행 (실기기/에뮬레이터 구분) |
| | `06_web.png` | Web 실행 (브라우저 이름·버전) |
| | `07_config_server.png`, `07_config_fallback.png` | 원격 설정 45초 / 서버 OFF 시 40초 |
| `Troubleshooting/` | `encoding_before.png`, `encoding_after.png` | 사례 1 |
| | `server_build_before.txt`, `server_build_after.txt` | 사례 2 (`dotnet build` 출력) |
| | `addressables_fail.png`, `addressables_reload.png` | 로드 실패 안내 → 재시도 |
| `Profiler/` | `before_cpu.png`, `before_gc.png`, `after_cpu.png`, `after_gc.png` | 같은 기기·같은 구간의 `CoinLab.PresentUi` |
| `Logs/` | `player_pc.log`, `logcat_android.txt` | Player 로그 (개인 경로·기기 ID는 지우고 제출) |

PC Player 로그 위치: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\PlatformCoinLab\Player.log`
Android 로그: `adb logcat -s Unity > logcat_android.txt`
