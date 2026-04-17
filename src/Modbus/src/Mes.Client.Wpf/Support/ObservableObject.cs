using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mes.Client.Wpf.Support;

/// <summary>
/// 속성 변경 알림을 제공하는 뷰모델 기본 형식입니다.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <summary>
    /// 속성 값이 변경되었을 때 발생합니다.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 필드 값을 바꾸고 필요한 경우 변경 알림을 발생시킵니다.
    /// </summary>
    /// <typeparam name="T">비교할 값 형식입니다.</typeparam>
    /// <param name="storage">현재 필드 참조입니다.</param>
    /// <param name="value">새 값입니다.</param>
    /// <param name="propertyName">호출한 속성 이름입니다.</param>
    /// <returns>실제 값이 바뀌었으면 <see langword="true"/>를 반환합니다.</returns>
    protected bool SetProperty<T>(
        ref T storage,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    /// <summary>
    /// 지정한 속성의 변경 알림을 발생시킵니다.
    /// </summary>
    /// <param name="propertyName">변경된 속성 이름입니다.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
