using System.Windows;
using Mes.Client.Wpf.Shell;

namespace Mes.Client.Wpf;

/// <summary>
/// 스테이션 셸 메인 창을 나타냅니다.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 바인딩할 셸 뷰모델을 받아 메인 창을 초기화합니다.
    /// </summary>
    /// <param name="viewModel">메인 창 데이터 컨텍스트입니다.</param>
    public MainWindow(ShellViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;
    }
}
