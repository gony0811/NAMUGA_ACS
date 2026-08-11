# ACS UI 배포 절차 (Velopack)

> 작성일: 2026-06-12
> 대상: ACS.UI (Avalonia 데스크탑 클라이언트)
> 배포 방식: Velopack 패키징 + ACS.App 정적 릴리스 피드 + 클라이언트 자동 업데이트

---

## Context

ACS.UI는 고객 현장 PC에 설치되는 Avalonia 데스크탑 앱이며, **Velopack 기반 자동 업데이트** 체계를
갖추고 있다. 빌드 PC에서 릴리스를 패키징해 CS(컨트롤 서버)의 정적 피드 경로에 올리면, 설치된
클라이언트들이 주기적으로 피드를 확인해 스스로 업데이트한다.

배포 관련 구성은 패키징 스크립트 주석과 코드에 흩어져 있으므로, 본 문서는 **회차 배포 시 참조할
단계별 절차**를 정리한다. 특히 빌드 PC와 CS 서버가 분리된 환경에서 **델타 업데이트 체인이 깨지지
않도록** 하는 주의사항을 명시한다.

---

## 구성 요소 한눈에

| 역할 | 위치 / 값 |
|------|-----------|
| 패키징 스크립트 | `src/ACS/publish-ui.ps1` |
| 빌드 출력 폴더 | `src/ACS/releases/ui` (회차 간 **보존** — 델타 생성에 필요) |
| 피드 서빙(서버) | `src/ACS/ACS.App/Program.cs` — `app.UseStaticFiles(... RequestPath="/releases/ui")` |
| 피드 물리 경로 | `Acs:Api:ClientReleasePath` (기본 `C:\acs\releases\ui`) — `ACS.App/appsettings.json` |
| 피드 HTTP URL | `http://<CS호스트>:5100/releases/ui` |
| 클라이언트 부트스트랩 | `src/ACS/ACS.UI/Program.cs` — `VelopackApp.Build().Run()` (Avalonia 초기화보다 먼저) |
| 업데이트 서비스 | `src/ACS/ACS.UI/Services/UpdateService.cs` — `SimpleWebSource("<baseUrl>/releases/ui")` |
| 업데이트 주기/권한 | 4시간 주기 백그라운드 체크, **Admin / Operator만** (Viewer 제외) |
| 버전 | `src/ACS/ACS.UI/ACS.UI.csproj` `<Version>` — 스크립트가 `-p:Version`으로 덮어씀 |
| 사이트별 백엔드 주소 | `C:\ProgramData\ACS.UI\appsettings.json` (`Backend.Host`/`Port`, 업데이트에도 **보존**) |

---

## 사전 준비 (최초 1회)

- **.NET 8 SDK** 설치
- **vpk CLI** 설치: `dotnet tool install -g vpk`
- **PowerShell 5.1+** (pwsh 권장)

> 클라이언트 PC에는 .NET 런타임이 필요 없다. 릴리스는 self-contained(win-x64)로 빌드된다.

---

## 델타 업데이트 핵심 원리

`vpk pack`은 **출력 폴더(`src/ACS/releases/ui`)에 직전 회차의 `.nupkg`가 존재할 때만** 델타
패키지를 생성한다. 델타가 있으면 클라이언트는 변경분만 내려받아 빠르게 업데이트된다.

- 출력 폴더가 비어 있으면 → full 패키지만 생성됨(동작은 하나, 매 업데이트마다 전체 다운로드 = 비효율).
- 따라서 **이전 릴리스 파일을 절대 지우지 말고 누적 유지**해야 한다.
- 빌드 PC가 CS와 분리되어 출력 폴더에 이력이 없다면, **빌드 전에 CS 피드를 먼저 내려받아 동기화**한다(절차 B 1단계).

---

## 버전 규칙

- **SemVer** (`MAJOR.MINOR.PATCH`), 회차마다 **반드시 증가**해야 한다. vpk가 중복 버전을 거부한다.
- `ACS.UI.csproj`의 `<Version>`을 직접 고칠 필요 없음 — 스크립트 `-Version` 인자가 `-p:Version`으로 덮어쓴다.

---

## 피드 자동 부트스트랩 (CS 기동 시)

CS(control)는 기동 시 피드 폴더가 **완전히 비어 있으면** 자동으로 릴리스를 구성한다
(`ACS.App/Web/ReleaseFeedBootstrapper.cs`). 파일이 하나라도 있으면 델타 체인 보호를 위해 건드리지 않는다.

우선순위:

1. **시드 복사 (배포본)** — CS 실행 폴더의 `releases-seed\ui\`에 `releases.win.json`이 있으면
   해당 폴더 전체를 피드로 복사. CS 배포본을 만들 때 `src/ACS/releases/ui` 산출물을
   `releases-seed\ui\`로 함께 담아두면 된다. 소스/SDK 없는 프로덕션 서버에서도 동작.
2. **소스 트리 기존 산출물 복사 (개발)** — 실행 폴더 상위에서 `publish-ui.ps1`이 발견되고(소스
   실행) 그 옆 `releases\ui\`에 이미 패키징된 산출물이 있으면 그대로 피드로 복사.
   재패키징하지 않는 이유: vpk가 기존 버전 이하의 재패키징을 거부한다(중복 버전).
3. **런타임 자동 빌드 (개발, 산출물 없음)** — 위 둘 다 없고 `vpk` CLI가 설치되어 있으면
   **백그라운드로** `publish-ui.ps1 -Version <csproj Version> -ReleaseDir <피드경로>`를 실행한다.
   버전은 `ACS.UI.csproj`의 `<Version>`을 사용. CS 기동은 지연되지 않으며, 진행/실패 내역은
   CS 로그의 `[ReleaseFeed]` 항목으로 확인한다.
4. 모두 불가하면 경고 로그만 남기고 기존처럼 빈 피드로 기동한다.

> 자동 구성은 **최초 1회(빈 피드) 전용**이다. 이후 회차 배포는 아래 절차 A/B를 그대로 따른다.
> 폐쇄망 빌드 PC에서는 restore 지연으로 백그라운드 빌드가 오래 걸릴 수 있다(기동에는 영향 없음).

---

## 절차 A — CS 서버에서 직접 빌드

CS 서버에서 빌드/배포를 모두 수행하는 경우, 한 줄로 끝난다.

```powershell
pwsh src/ACS/publish-ui.ps1 -Version 1.0.1 -ReleaseDir C:\acs\releases\ui
```

스크립트가 자동 수행:
1. `dotnet publish` — self-contained win-x64 → `.publish-ui-staging`
2. `vpk pack` — `src/ACS/releases/ui`에 Setup.exe + full/delta `.nupkg` + `releases.win.json` 생성
3. `robocopy`로 `C:\acs\releases\ui` 피드 경로에 **누적 복사**(`/MIR` 미사용)

ACS.App이 실행 중이면 복사 즉시 피드에 반영된다(재시작 불필요).

---

## 절차 B — 빌드 PC ≠ CS 서버 (분리 환경)

빌드는 개발 PC에서, 배포 대상은 별도 CS 서버인 경우. **델타 체인 보존이 관건**이다.

### 1. (빌드 PC) CS 피드를 로컬로 동기화 — 매 회차 빌드 전

```powershell
vpk download http --url http://<CS호스트>:5100/releases/ui --outputDir src/ACS/releases/ui
```

이 단계로 빌드 PC가 직전 릴리스 이력을 갖게 되어 델타가 정상 생성된다.

### 2. (빌드 PC) 패키징 — `-ReleaseDir` 생략

```powershell
pwsh src/ACS/publish-ui.ps1 -Version 1.0.1
```

- 버전은 직전보다 증가.
- 결과물이 `src/ACS/releases/ui`에 생성: `AcsUi-1.0.1-full.nupkg`, (이력 있으면) `AcsUi-1.0.1-delta.nupkg`, `RELEASES`, `releases.win.json`, `AcsUi-win-Setup.exe`.
- `-ReleaseDir`를 생략하는 이유: 빌드 PC에는 CS의 `C:\acs\releases\ui` 경로가 없으므로 복사는 3단계에서 수동.

### 3. (빌드 PC → CS 서버) 피드 경로로 누적 복사 — `/MIR` 절대 금지

`src/ACS/releases/ui`의 **전체 내용**을 CS의 `C:\acs\releases\ui`로 복사한다. 기존 파일을 지우지 말고 덮어쓰기/추가만.

네트워크 공유가 접근 가능하면:
```powershell
robocopy src\ACS\releases\ui \\<CS호스트>\acs\releases\ui /E /R:1 /W:1
```

> ⚠️ `/MIR`(미러)는 절대 금지 — 이전 회차/델타 파일이 삭제되면 향후 델타 생성이 깨진다.
> 공유가 없으면 USB 등으로 옮긴 뒤 CS의 기존 폴더에 **합치기**(삭제 없이 덮어쓰기).

### 4. (CS 서버) 반영 확인

```
http://<CS호스트>:5100/releases/ui/releases.win.json   → 새 버전(1.0.1) 표기 확인
```

ACS.App 실행 중이면 복사 즉시 반영된다(재시작 불필요).

### 회차별 요약 (빌드 PC에서)

```powershell
# 1. CS 피드 동기화 (델타 보존)
vpk download http --url http://<CS호스트>:5100/releases/ui --outputDir src/ACS/releases/ui
# 2. 패키징
pwsh src/ACS/publish-ui.ps1 -Version <새버전>
# 3. src/ACS/releases/ui  →  CS의 C:\acs\releases\ui 로 누적 복사 (/MIR 금지)
```

---

## 클라이언트 반영

### 기존 설치 PC (자동 업데이트)
- 4시간 주기로 피드 확인 → 다운로드 → **종료 시 적용**(`ApplyOnExit`).
- Admin/Operator만 동작(Viewer는 체크 스킵).
- 업데이트 준비되면 UI 상단에 재시작 배너 표시 → 즉시 재시작 적용도 가능.

### 신규 PC (최초 설치)
1. 브라우저로 Setup 다운로드 후 실행:
   ```
   http://<CS호스트>:5100/releases/ui/AcsUi-win-Setup.exe
   ```
2. 사이트별 백엔드 주소 설정 — `C:\ProgramData\ACS.UI\appsettings.json` (업데이트에도 보존됨):
   ```json
   { "Backend": { "Host": "10.0.26.2", "Port": 5100 } }
   ```

---

## 흔한 실수

| 증상 | 원인 | 대응 |
|------|------|------|
| 클라이언트가 매번 전체 패키지를 받음 | 빌드 전 피드 동기화 누락 → 델타 미생성 | 절차 B 1단계(`vpk download`) 수행 |
| 일정 회차 뒤 업데이트가 깨짐 | 복사 시 `/MIR`로 이전 파일 삭제 → 델타 체인 붕괴 | `/MIR` 금지, 누적 복사 유지 |
| `vpk pack` 실패 | 버전 미증가(중복) | `-Version`을 직전보다 증가 |
| 피드 파일이 404 | ACS.App 미실행 또는 `ClientReleasePath` 경로 불일치 | ACS.App 기동 / `appsettings.json` 경로 확인 |
| 피드 자동 구성이 안 됨 | 피드에 파일이 이미 있음 / vpk 미설치 / 소스 트리 아님 / 시드 폴더 없음 | CS 로그의 `[ReleaseFeed]` 경고 확인 후 조건 충족 또는 수동 배포 |

---

## 검증 체크리스트

- [ ] `http://<CS호스트>:5100/releases/ui/releases.win.json`에 새 버전이 보이는가
- [ ] `src/ACS/releases/ui`에 이전 회차 `.nupkg`가 함께 남아 있는가(델타 보존)
- [ ] 클라이언트(Admin/Operator)에서 일정 시간 내 재시작 배너가 뜨고, 재시작 후 버전이 올라갔는가
