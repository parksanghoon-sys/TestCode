using System;
using System.IO;
using System.Linq;

namespace FilePatternMover
{
    class Program
    {
        //static void Main(string[] args)
        //{
        //    if (args.Length < 3)
        //    {
        //        Console.WriteLine("사용법: FilePatternMover <소스디렉토리> <대상디렉토리> <파일패턴>");
        //        Console.WriteLine("예시: FilePatternMover C:\\Source C:\\Destination *aa.xml");
        //        Console.WriteLine("예시: FilePatternMover C:\\Source C:\\Destination *test*aa.xml");
        //        return;
        //    }

        //    string sourceDirectory = args[0];
        //    string destinationDirectory = args[1];
        //    string filePattern = args[2];

        //    try
        //    {
        //        // 소스 디렉토리가 존재하는지 확인
        //        if (!Directory.Exists(sourceDirectory))
        //        {
        //            Console.WriteLine($"소스 디렉토리가 존재하지 않습니다: {sourceDirectory}");
        //            return;
        //        }

        //        // 대상 디렉토리가 존재하지 않으면 생성
        //        if (!Directory.Exists(destinationDirectory))
        //        {
        //            Directory.CreateDirectory(destinationDirectory);
        //            Console.WriteLine($"대상 디렉토리가 생성되었습니다: {destinationDirectory}");
        //        }

        //        // 패턴에 맞는 파일 찾기
        //        string[] matchingFiles = Directory.GetFiles(sourceDirectory, filePattern, SearchOption.AllDirectories);

        //        if (matchingFiles.Length == 0)
        //        {
        //            Console.WriteLine($"패턴 '{filePattern}'과 일치하는 파일을 찾을 수 없습니다.");
        //            return;
        //        }

        //        Console.WriteLine($"찾은 파일 수: {matchingFiles.Length}");

        //        // 파일 이동
        //        int movedCount = 0;
        //        foreach (string sourceFilePath in matchingFiles)
        //        {
        //            string fileName = Path.GetFileName(sourceFilePath);
        //            string destinationFilePath = Path.Combine(destinationDirectory, fileName);

        //            // 대상 경로에 같은 이름의 파일이 있는 경우 처리
        //            if (File.Exists(destinationFilePath))
        //            {
        //                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        //                string fileExt = Path.GetExtension(fileName);
        //                string newFileName = $"{fileNameWithoutExt}_{DateTime.Now:yyyyMMddHHmmss}{fileExt}";
        //                destinationFilePath = Path.Combine(destinationDirectory, newFileName);
        //            }

        //            File.Move(sourceFilePath, destinationFilePath);
        //            Console.WriteLine($"이동됨: {sourceFilePath} -> {destinationFilePath}");
        //            movedCount++;
        //        }

        //        Console.WriteLine($"총 {movedCount}개 파일이 이동되었습니다.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"오류 발생: {ex.Message}");
        //    }
        //}

        static void Main()
        {
            Console.Write("검색할 확장자(예: .txt): ");
            string extension = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(extension) || !extension.StartsWith("."))
            {
                Console.WriteLine("올바른 확장자를 입력하세요. 예: .txt");
                return;
            }

            Console.Write("검색할 폴더 경로: ");
            string sourcePath = Console.ReadLine()?.Trim();

            Console.Write("복사할 대상 경로: ");
            string destPath = Console.ReadLine()?.Trim();

            if (!Directory.Exists(sourcePath))
            {
                Console.WriteLine("원본 폴더가 존재하지 않습니다.");
                return;
            }

            Directory.CreateDirectory(destPath); // 대상 폴더 없으면 생성

            string[] files = Directory.GetFiles(sourcePath, "*" + extension, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                try
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destPath, fileName);

                    File.Copy(file, destFile, overwrite: true);
                    Console.WriteLine($"복사됨: {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"복사 실패: {file} -> {ex.Message}");
                }
            }

            Console.WriteLine("작업 완료!");
        }

    }
}