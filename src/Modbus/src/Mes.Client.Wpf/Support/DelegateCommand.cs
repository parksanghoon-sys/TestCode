using System.Windows.Input;

namespace Mes.Client.Wpf.Support;

/// <summary>
/// 동기 실행용 WPF 명령 구현입니다.
/// </summary>
public sealed class DelegateCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// 실행 동작과 선택적 활성화 조건으로 명령을 초기화합니다.
    /// </summary>
    /// <param name="execute">실행할 동작입니다.</param>
    /// <param name="canExecute">실행 가능 여부를 계산하는 함수입니다.</param>
    public DelegateCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// 실행 가능 상태가 바뀌었을 때 발생합니다.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// 현재 명령을 실행할 수 있는지 반환합니다.
    /// </summary>
    /// <param name="parameter">WPF가 전달하는 명령 매개변수입니다.</param>
    /// <returns>실행 가능하면 <see langword="true"/>를 반환합니다.</returns>
    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    /// <summary>
    /// 명령을 실행합니다.
    /// </summary>
    /// <param name="parameter">WPF가 전달하는 명령 매개변수입니다.</param>
    public void Execute(object? parameter)
    {
        _execute();
    }

    /// <summary>
    /// 외부 상태 변화에 따라 실행 가능 여부를 다시 평가하도록 알립니다.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
