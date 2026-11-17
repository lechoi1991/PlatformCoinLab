<#
  제출 패키지를 SubmissionPackage/ 아래에 모으고 zip으로 묶습니다.
  - Unity 프로젝트: Assets(+.meta), Packages, ProjectSettings 만 복사 (Library·Temp·Logs·UserSettings·IDE 캐시 제외)
  - 서버: ContentServer 소스 (bin·obj 제외), ServerData
  - 빌드: Builds/Windows, Builds/Android/*.apk, Builds/Web
  - 문서·확인 자료: Submission/ (Tools 제외)
  서명 키(*.keystore, *.jks), 인증서(*.pfx, *.pem, *.key), *.user 파일은 어디서든 제외합니다.

  사용: powershell -ExecutionPolicy Bypass -File Submission/Tools/Make-SubmissionPackage.ps1
#>
param(
    [string]$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path,
    [string]$OutName = "PlatformCoinLab_Submission"
)

$ErrorActionPreference = "Stop"
$out = Join-Path $ProjectRoot "SubmissionPackage\$OutName"
$secretPatterns = @("*.keystore", "*.jks", "*.pfx", "*.p12", "*.pem", "*.key", "*.user", "*.suo")

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force $out | Out-Null

function Copy-Tree([string]$From, [string]$To, [string[]]$ExcludeDirs = @()) {
    $src = Join-Path $ProjectRoot $From
    if (-not (Test-Path $src)) { Write-Warning "없음(건너뜀): $From"; return $false }
    $dst = Join-Path $out $To
    $rcArgs = @($src, $dst, "/E", "/NFL", "/NDL", "/NJH", "/NJS", "/NP", "/XF") + $secretPatterns
    if ($ExcludeDirs.Count -gt 0) { $rcArgs += @("/XD") + $ExcludeDirs }
    robocopy @rcArgs | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "복사 실패: $From (robocopy $LASTEXITCODE)" }
    return $true
}

# 1. Unity 프로젝트
Copy-Tree "Assets" "UnityProject\Assets" | Out-Null
Copy-Tree "Packages" "UnityProject\Packages" | Out-Null
Copy-Tree "ProjectSettings" "UnityProject\ProjectSettings" | Out-Null
foreach ($f in ".gitignore", ".gitattributes", "PlatformCoinLab.slnx") {
    if (Test-Path (Join-Path $ProjectRoot $f)) { Copy-Item (Join-Path $ProjectRoot $f) (Join-Path $out "UnityProject\$f") }
}
# Addressables 원격 콘텐츠 출력 (서버 배포용)
Copy-Tree "ServerData" "UnityProject\ServerData" | Out-Null

# 2. 파일 서버 (bin·obj·.vs 제외)
Copy-Tree "ContentServer" "ContentServer" @("bin", "obj", ".vs") | Out-Null

# 3. 빌드
$builds = @{
    "Builds\Windows" = "Builds\Windows"
    "Builds\Web"     = "Builds\Web"
}
foreach ($k in $builds.Keys) { Copy-Tree $k $builds[$k] @("PlatformCoinLab_BurstDebugInformation_DoNotShip", "*_BackUpThisFolder_ButDontShipItWithYourGame") | Out-Null }
$apk = Get-ChildItem (Join-Path $ProjectRoot "Builds\Android") -Filter *.apk -ErrorAction SilentlyContinue
if ($apk) {
    New-Item -ItemType Directory -Force (Join-Path $out "Builds\Android") | Out-Null
    $apk | Copy-Item -Destination (Join-Path $out "Builds\Android")
} else { Write-Warning "APK 없음: Builds/Android/*.apk" }

# 4. 문서·확인 자료
Copy-Tree "Submission" "Docs" @("Tools") | Out-Null
Copy-Item (Join-Path $ProjectRoot "README.md") (Join-Path $out "README.md")

# 5. 검사: 제외 대상이 섞였는지 확인
$bad = foreach ($item in Get-ChildItem $out -Recurse -Force) {
    if ($item.PSIsContainer -and $item.Name -in @("Library", "Temp", "obj", "bin", ".vs", ".idea", "UserSettings")) { $item; continue }
    if (-not $item.PSIsContainer) {
        foreach ($p in $secretPatterns) { if ($item.Name -like $p) { $item; break } }
    }
}
if ($bad) { $bad | ForEach-Object { Write-Warning "제외 대상 발견: $($_.FullName)" } }

$missingMeta = Get-ChildItem (Join-Path $out "UnityProject\Assets") -Recurse -Force |
    Where-Object { $_.Extension -ne ".meta" -and -not (Test-Path ($_.FullName + ".meta")) }
if ($missingMeta) { $missingMeta | ForEach-Object { Write-Warning ".meta 없음: $($_.FullName)" } }

# 6. zip
$zip = "$out.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$out\*" -DestinationPath $zip
$size = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "완료: $zip ($size MB)"
