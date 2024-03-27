using System.Collections.Concurrent;
using System.Diagnostics;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        Program program = new Program();    
        program.Run();
        Console.ReadLine();

    }
    public void Run()
    {
        //FileSystemWatcher를 선언합니다.
        //어셈블리 : System.IO.FileSystem.Watcher.dll
        FileSystemWatcher watcher = new FileSystemWatcher();
        // 어느 경로의 폴더를 감시할 것인지 지정합니다.
        watcher.Path = @"D:\Test";

        // 확인 필터를 지정합니다. 설정한 내용중에 변경이 있을경우 이벤트 발생이 됩니다.
        watcher.NotifyFilter = NotifyFilters.Attributes             //속성 변경
                                    | NotifyFilters.CreationTime   //생성시간
                                    | NotifyFilters.DirectoryName  //디렉토리 이름
                                    | NotifyFilters.FileName       //파일 이름
                                    | NotifyFilters.LastAccess     //마지막 접근
                                    | NotifyFilters.LastWrite      //마지막 쓰여진
                                    | NotifyFilters.Security       //보안
                                    | NotifyFilters.Size;          //크기
                                                                   // 이벤트 선언
                                                                   // 변화가 있을경우 
        watcher.Changed += watcher_Changed;
        // 생성이 되었을 경우
        watcher.Created += watcher_Created;
        // 삭제가 되었을 경우
        watcher.Deleted += watcher_Deleted;
        // 이름이 변경 되었을 경우
        watcher.Renamed += watcher_Renamed;
        // 에러가 발생했을 경우
        watcher.Error += watcher_Error;

        // ****이거 엄청중요!!!
        // 예시로 text파일의 변경을 확인 하는 것이지
        // 만약 xml파일이 들어올때마다 Event를 선언 할 것이다면 "*.xml"
        // 모든 변경사항을 체크 할 경우 "*.*"
        watcher.Filter = "*.*";
        // 하위 디렉토리의 변화까지 확인할 것이다.
        watcher.IncludeSubdirectories = true;
        // 이벤트를 발생 할 것이다.
        watcher.EnableRaisingEvents = true;
        Console.WriteLine("종료하실거면 엔터를 눌러 주세요.");
        Console.ReadLine();
    }
    private void watcher_Changed(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Changed) return;
        Console.WriteLine($"변화된 파일 경로 : {e.FullPath}");
    }
    private void watcher_Created(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"생성된 파일 : {e.FullPath}");
    }
    private void watcher_Deleted(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"제거된 파일 : {e.FullPath}");
    }
    private void watcher_Renamed(object sender, RenamedEventArgs e)
    {
        Console.WriteLine($"이름이 변경되었습니다.");
        Console.WriteLine($"이전 이름 : {e.OldFullPath}");
        Console.WriteLine($"변경된 이름 : {e.FullPath}");
    }
    private void watcher_Error(object sender, ErrorEventArgs e)
    {
        Console.WriteLine($"Erroer Message : {e.GetException().Message}");
    }
}