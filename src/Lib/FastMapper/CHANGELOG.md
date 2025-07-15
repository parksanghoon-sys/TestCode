# Change Log

## [1.0.0] - 2024-12-01

### Added
- Source Generator 기반 매핑 코드 생성
- Attribute 기반 매핑 설정
- 제네릭 매퍼 인터페이스 (`IMapper<TSource, TDestination>`)
- 양방향 매핑 지원 (`IBidirectionalMapper<TFirst, TSecond>`)
- 동적 매핑 지원 (`IDynamicMapper`)
- 의존성 주입 확장 (`AddFastMapper()`)
- 커스텀 변환 함수 지원
- 조건부 매핑 지원
- 컬렉션 매핑 지원
- 성능 모니터링 기능
- 포괄적인 테스트 스위트
- 벤치마크 도구

### Technical Details
- .NET 8.0 지원
- C# 12.0 최신 문법 활용
- Reflection 없는 고성능 매핑
- 컴파일 타임 코드 생성
- 중앙 집중식 패키지 관리

---

모든 주목할 만한 변경사항은 이 파일에 문서화됩니다.

[1.0.0]: https://github.com/fastmapper/fastmapper/releases/tag/v1.0.0
