using System.Windows.Input;

namespace Mes.Client.Wpf.Support;

/// <summary>
/// 비동기 실행용 WPF 명령 구현입니다.
/// </summary>
public sealed class AsyncDelegateCommand : ICommand
{
    private readonly Func<CancellationToken, Task> _executeAsync;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception>? _onException;
    private bool _isExecuting;

    /// <summary>
    /// 실행 동작과 선택적인 실행 조건 및 예외 처리기로 비동기 명령을 초기화합니다.
    /// </summary>
    /// <param name="executeAsync">실행할 비동기 동작입니다.</param>
    /// <param name="canExecute">실행 가능 여부를 계산하는 함수입니다.</param>
    /// <param name="onException">예상하지 못한 예외가 발생했을 때 호출할 처리기입니다.</param>
    public AsyncDelegateCommand(
        Func<CancellationToken, Task> executeAsync,
        Func<bool>? canExecute = null,
        Action<Exception>? onException = null)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
        _onException = onException;
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
        return !_isExecuting && (_canExecute?.Invoke() ?? true);
    }

    /// <summary>
    /// 명령을 비동기로 실행합니다.
    /// </summary>
    /// <param name="parameter">WPF가 전달하는 명령 매개변수입니다.</param>
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _isExecuting = true;
        RaiseCanExecuteChanged();

        try
        {
            await _executeAsync(CancellationToken.None);
        }
        catch (Exception exception) when (_onException is not null)
        {
            _onException(exception);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// 내부 상태 변화에 따라 실행 가능 여부를 다시 평가하도록 알립니다.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
