namespace MessageBusLib.Serialization;

/// <summary>
/// 타입 정보를 포함한 데이터 래퍼
/// </summary>
/// <param name="TypeName"> 타입 이름 (AssemblyQualifiedName) </param>
/// <param name="Data"> 직렬화된 데이터 </param>
[Serializable]
public record TypedData(string TypeName, byte[] Data);
