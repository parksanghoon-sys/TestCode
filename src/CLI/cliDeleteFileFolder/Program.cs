using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileCleaner
{
    /// <summary>
    /// 파일 시스템 항목(파일 또는 폴더)을 나타내는 추상 클래스
    /// </summary>
    public abstract class FileSystemItem
    {
        public string Path { get; protected set; }

        protected FileSystemItem(string path)
        {
            Path = path;
        }

        public abstract Task CleanAsync();
        public abstract bool ShouldClean();
    }

    /// <summary>
    /// 파일을 나타내는 클래스
    /// </summary>
    public class FileItem : FileSystemItem
    {
        private readonly List<string> _extensionsToDelete;

        public FileItem(string path, List<string> extensionsToDelete) : base(path)
        {
            _extensionsToDelete = extensionsToDelete;
        }

        public override bool ShouldClean()
        {
            string extension = System.IO.Path.GetExtension(Path).ToLowerInvariant();
            return _extensionsToDelete.Contains(extension);
        }

        public override async Task CleanAsync()
        {
            if (ShouldClean())
            {
                try
                {
                    File.Delete(Path);
                    await Console.Out.WriteLineAsync($"파일 삭제됨: {Path}");
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"파일 삭제 오류: {Path}, 오류: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 폴더를 나타내는 클래스
    /// </summary>
    public class DirectoryItem : FileSystemItem
    {
        private readonly List<string> _foldersToDelete;

        public DirectoryItem(string path, List<string> foldersToDelete) : base(path)
        {
            _foldersToDelete = foldersToDelete;
        }

        public override bool ShouldClean()
        {
            string folderName = new DirectoryInfo(Path).Name;
            return _foldersToDelete.Any(f => folderName.Equals(f, StringComparison.OrdinalIgnoreCase));
        }

        public override async Task CleanAsync()
        {
            if (ShouldClean())
            {
                try
                {
                    Directory.Delete(Path, true);
                    await Console.Out.WriteLineAsync($"폴더 삭제됨: {Path}");
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync($"폴더 삭제 오류: {Path}, 오류: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 파일 정리 작업을 관리하는 클래스
    /// </summary>
    public class CleanupManager
    {
        private readonly string _rootPath;
        private readonly List<string> _extensionsToDelete;
        private readonly List<string> _foldersToDelete;
        private readonly List<FileSystemItem> _itemsToProcess = new();

        public CleanupManager(string rootPath, List<string> extensionsToDelete, List<string> foldersToDelete)
        {
            _rootPath = rootPath;
            _extensionsToDelete = extensionsToDelete;
            _foldersToDelete = foldersToDelete;
        }

        public async Task RunCleanupAsync()
        {
            await Console.Out.WriteLineAsync($"정리 시작: {_rootPath}");
            await Console.Out.WriteLineAsync($"삭제할 확장자: {string.Join(", ", _extensionsToDelete)}");
            await Console.Out.WriteLineAsync($"삭제할 폴더: {string.Join(", ", _foldersToDelete)}");

            try
            {
                // 폴더 검색 및 처리
                await ScanDirectoryAsync(_rootPath);

                // 검색된 항목 처리
                await ProcessItemsAsync();

                await Console.Out.WriteLineAsync("정리 완료!");
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"정리 중 오류 발생: {ex.Message}");
            }
        }

        private async Task ScanDirectoryAsync(string directory)
        {
            try
            {
                // 먼저 모든 폴더 확인 (상위 폴더부터 처리)
                foreach (var dir in Directory.GetDirectories(directory))
                {
                    var dirItem = new DirectoryItem(dir, _foldersToDelete);

                    if (dirItem.ShouldClean())
                    {
                        _itemsToProcess.Add(dirItem);
                    }
                    else
                    {
                        // 삭제 대상이 아니면 하위 폴더도 스캔
                        await ScanDirectoryAsync(dir);
                    }
                }

                // 현재 폴더의 파일 확인
                foreach (var file in Directory.GetFiles(directory))
                {
                    var fileItem = new FileItem(file, _extensionsToDelete);
                    if (fileItem.ShouldClean())
                    {
                        _itemsToProcess.Add(fileItem);
                    }
                }
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"스캔 오류: {directory}, 오류: {ex.Message}");
            }
        }

        private async Task ProcessItemsAsync()
        {
            int totalItems = _itemsToProcess.Count;
            int processedItems = 0;

            await Console.Out.WriteLineAsync($"{totalItems}개 항목 처리 중...");

            foreach (var item in _itemsToProcess)
            {
                await item.CleanAsync();
                processedItems++;

                // 진행 상황 표시 (10개 항목마다)
                if (processedItems % 10 == 0 || processedItems == totalItems)
                {
                    await Console.Out.WriteLineAsync($"진행 상황: {processedItems}/{totalItems} ({(processedItems * 100 / totalItems)}%)");
                }
            }
        }
    }

    /// <summary>
    /// 사용자 입력을 관리하는 클래스
    /// </summary>
    public class UserInterface
    {
        public static async Task<string> GetRootPathAsync()
        {
            await Console.Out.WriteAsync("정리할 폴더 경로를 입력하세요: ");
            string? path = await Console.In.ReadLineAsync();

            while (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                await Console.Out.WriteLineAsync("유효하지 않은 경로입니다. 다시 입력해주세요.");
                await Console.Out.WriteAsync("정리할 폴더 경로를 입력하세요: ");
                path = await Console.In.ReadLineAsync();
            }

            return path!;
        }

        public static async Task<List<string>> GetExtensionsToDeleteAsync()
        {
            await Console.Out.WriteAsync("삭제할 파일 확장자를 입력하세요 (쉼표로 구분, 예: .pdb,.config): ");
            string? input = await Console.In.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(input))
            {
                return new List<string>();
            }

            return input.Split(',')
                .Select(ext => ext.Trim())
                .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
                .ToList();
        }

        public static async Task<List<string>> GetFoldersToDeleteAsync()
        {
            await Console.Out.WriteAsync("삭제할 폴더 이름을 입력하세요 (쉼표로 구분, 예: obj,.vs): ");
            string? input = await Console.In.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(input))
            {
                return new List<string>();
            }

            return input.Split(',')
                .Select(folder => folder.Trim())
                .ToList();
        }

        public static async Task ShowCompletionMessageAsync(TimeSpan duration)
        {
            await Console.Out.WriteLineAsync($"모든 작업이 완료되었습니다. 소요 시간: {duration.TotalSeconds:F2}초");
            await Console.Out.WriteLineAsync("아무 키나 누르면 종료합니다...");
            Console.ReadKey();
        }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            await Console.Out.WriteLineAsync("=== 파일 및 폴더 정리 도구 ===");

            try
            {
                // 사용자 입력 받기
                string rootPath = await UserInterface.GetRootPathAsync();
                List<string> extensionsToDelete = await UserInterface.GetExtensionsToDeleteAsync();
                List<string> foldersToDelete = await UserInterface.GetFoldersToDeleteAsync();

                // 작업 시작 시간 기록
                var startTime = DateTime.Now;

                // 정리 관리자 생성 및 실행
                var cleanupManager = new CleanupManager(rootPath, extensionsToDelete, foldersToDelete);
                await cleanupManager.RunCleanupAsync();

                // 완료 메시지 표시
                var duration = DateTime.Now - startTime;
                await UserInterface.ShowCompletionMessageAsync(duration);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"오류 발생: {ex.Message}");
                await Console.Out.WriteLineAsync("아무 키나 누르면 종료합니다...");
                Console.ReadKey();
            }
        }
    }
}