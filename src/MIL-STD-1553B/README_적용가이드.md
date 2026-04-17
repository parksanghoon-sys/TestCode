# MIL-STD-1553B Codex 하네스 엔지니어링 템플릿 적용 가이드

실제 저장소 사용법, DLL 출력 방법, 테스트 실행 방법은 루트 `README.md`를 우선 참고한다.

이 템플릿은 다음 목적을 위해 작성되었다.

- Codex가 저장소에 들어오면 항상 **설계 검토 → 구현 → 테스트** 순서를 따르게 하기
- **TDD 3색 원칙(Red → Green → Refactor)** 을 강제 수준으로 습관화하기
- **클린 아키텍처 + OOP + 한글 XML 주석** 규칙을 저장소 정책으로 고정하기
- MIL-STD-1553B 하네스 엔지니어링에 필요한 최소 스킬 구조를 제공하기
- 작업 종료 시 설계 문서와 테스트 누락을 자동 점검하는 훅을 제공하기

---

## 포함 파일

```text
AGENTS.md
README_적용가이드.md
.codex/
  config.toml
  hooks.json
  hooks/
    stop_validate_flow.py
.agents/
  skills/
    harness-design-review/
      SKILL.md
      assets/
        설계검토서_템플릿.md
    harness-implementation/
      SKILL.md
      assets/
        구현_체크리스트.md
    harness-test-verification/
      SKILL.md
      assets/
        테스트계획서_템플릿.md
docs/
  design/
    README.md
  test/
    README.md
src/
  native/
    README.md
  dotnet/
    README.md
tests/
  native/
    README.md
  dotnet/
    README.md
```

---

## 적용 방법

1. 이 템플릿의 파일을 저장소 루트에 복사한다.
2. Codex를 저장소 루트 또는 하위 디렉터리에서 실행한다.
3. 작업 시작 시 `AGENTS.md` 와 필요한 스킬이 자동으로 로드되도록 유지한다.
4. 설계 작업은 `harness-design-review`
5. 구현 작업은 `harness-implementation`
6. 테스트/검증은 `harness-test-verification`
7. 작업 종료 전 훅이 설계 문서와 테스트 변경 여부를 점검한다.

---

## 권장 작업 예시

### 1. 설계 검토부터 시작
```text
$harness-design-review
새로운 RT 상태 워드 해석 기능을 추가한다.
C++ 네이티브 파서와 C# 상위 오케스트레이터의 책임을 분리하고 설계 검토서를 먼저 작성해라.
```

### 2. 구현 진행
```text
$harness-implementation
방금 작성한 설계 검토서를 기준으로 TDD 3색 원칙에 따라 최소 구현부터 진행해라.
주석은 반드시 한글 XML 형식으로 작성해라.
```

### 3. 테스트 및 검증
```text
$harness-test-verification
Bus A/B 전환, 타임아웃, BC→RT, RT→BC, Mode Code 시나리오를 포함해서 테스트를 작성하고 결과 문서를 갱신해라.
```

---

## 훅 동작 방식

현재 포함된 훅은 **Stop 훅 1개**만 사용한다.  
이 훅은 작업 종료 시점에 아래를 검사한다.

- 코드 파일이 변경되었는가?
- `docs/design/` 아래 설계 검토 문서가 함께 변경되었는가?
- `tests/` 아래 테스트 코드 또는 `docs/test/` 문서가 함께 변경되었는가?

조건을 만족하지 않으면, Codex에게 한 번 더 계속 진행하도록 피드백을 준다.

---

## 주의 사항

- 훅은 공식 문서 기준으로 **실험 기능**이다.
- 훅은 현재 **Windows에서 비활성**이므로, 훅 강제 적용은 WSL 또는 Linux 환경에서 사용하는 것을 권장한다.
- Windows 네이티브 Codex 환경에서는 훅 없이도 `AGENTS.md` 와 스킬만으로 동일한 절차를 따르게 할 수 있다.
- 설계 문서가 이전에 이미 존재하더라도, 이번 변경에 맞게 문서를 **반드시 갱신**해야 훅과 규칙이 일관되게 유지된다.

---

## 추천 추가 확장

현재는 최소 구성만 넣었다. 다음은 필요 시 추가하면 된다.

- 벤더 SDK 어댑터 전용 스킬
- 버스 시뮬레이터 작성 스킬
- 로그/리플레이 분석 스킬
- PR 리뷰 체크리스트 스킬
- 성능 측정 자동화 스킬
