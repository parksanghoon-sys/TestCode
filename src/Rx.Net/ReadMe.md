# Rx.Net

### 장점

* 메시지 버스, 브로드캐스트 같은 통합이벤트, WebSocket API이벤트, MQTT등의 실시간 메세지와 잘어울린다
* 동시성을 도입하고 관리하느데 적합
* IEnumerable'<T> 라이브 이벤트를 모델링 하려는경우 Rx 를 고려해야한다
* 비동기 작업을 사용할때 사용한다 


### Cli 사용법

```
mkdir TryRx
cd TryRx
dotnet new console
dotnet add package System.Reactive
```