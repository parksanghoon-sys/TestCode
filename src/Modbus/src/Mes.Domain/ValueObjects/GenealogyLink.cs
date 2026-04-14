namespace Mes.Domain.ValueObjects;

/// <summary>
/// 자재 Lot과 WIP 단위 사이의 추적성 연결을 표현합니다.
/// </summary>
/// <param name="ParentMaterialLotId">상위 자재 Lot 식별자입니다.</param>
/// <param name="ChildWipUnitId">연결된 하위 WIP 단위 식별자입니다.</param>
/// <param name="LinkedAt">연결이 생성된 시각입니다.</param>
public sealed record GenealogyLink(
    MaterialLotId ParentMaterialLotId,
    WipUnitId ChildWipUnitId,
    DateTimeOffset LinkedAt);
