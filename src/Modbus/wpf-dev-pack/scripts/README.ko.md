# Codex 친화형 로컬 훅 스크립트

## 파일 구성

- `Install-GitHooks.ps1`: 현재 클론에 `core.hooksPath=.githooks` 설정
- `Invoke-CodexPreCommit.ps1`: 스테이징된 C# 포맷, XAML 기본 검증
- `Invoke-CodexPrePush.ps1`: 저장소 빌드 및 테스트 프로젝트 실행
- `codex-hook.config.json`: 기본 동작 토글

## 설치

```powershell
pwsh -File wpf-dev-pack/scripts/Install-GitHooks.ps1
```

## 수동 실행

```powershell
pwsh -File wpf-dev-pack/scripts/Invoke-CodexPreCommit.ps1
pwsh -File wpf-dev-pack/scripts/Invoke-CodexPrePush.ps1
```


## HookRunner

pre-commit / pre-push 스크립트는 `wpf-dev-pack/hooks/WpfDevPack.HookRunner/WpfDevPack.HookRunner.csproj`도 호출하며, 이 실행기가 `wpf-dev-pack/hooks/*.cs`의 helper 목적을 Git hook 워크플로에 맞게 연결합니다.
