using cliAccessCompareCsv.AcessModels;
using cliAccessCompareCsv.Models.AcessModels;
using cliAccessCompareCsv.Services;
using System.Data.OleDb;

internal partial class Program
{
    private static void Main(string[] args)
    {
        string csvFileFolderPath = @"C:\B.Settings\Csv\StatusCsv";
        string[] csvFiless = Directory.GetFiles(csvFileFolderPath, "*.csv");
        string csvFileName = "UVHF_StatusDisplay.csv";
        string csvWriteFileName = "Not전시.csv";
        string dbFilePath = @"D:\Project\02.Document\2024\01.항전개조\2.ICD\B\2024.02.23_통합 ICD_V5.02.accdb";
        string csvFilePath = @$"C:\B.Settings\Csv\StatusCsv\{csvWriteFileName}";
        string connectionString = $"Provider=Microsoft.ACE.OLEDB.16.0;Data Source={dbFilePath};Persist Security Info=False;";

        // Table : GCS_AVS_IMC_header, GCS_AVS_IMC_field, GCS_AVS_IMC_bit
        // GCS_AVS_IMC_header 과 GCS_AVS_IMC_field 는 GCS_AVS_IMC_header.[명칭(니모닉)] 로 외래키
        // GCS_AVS_IMC_field 와 GCS_AVS_IMC_bit 는 GCS_AVS_IMC_field.[Field Name] 로 외래키
        // 조건 : GCS_AVS_IMC_field.[Field Default] = '전시'
        // 출력 : GCS_AVS_IMC_header.[명칭(니모닉)] , GCS_AVS_IMC_bit[Bit Name]

        //SELECT DISTINCT GCS_AVS_IMC_header.[명칭(니모닉)], GCS_AVS_IMC_bit.[Bit Name], GCS_AVS_IMC_field.[Field Name]
        //FROM(GCS_AVS_IMC_header
        //INNER JOIN GCS_AVS_IMC_field ON GCS_AVS_IMC_header.[명칭(니모닉)] = GCS_AVS_IMC_field.[명칭(니모닉)])
        //INNER JOIN GCS_AVS_IMC_bit ON GCS_AVS_IMC_field.[Field Name] = GCS_AVS_IMC_bit.[Field Name]
        //WHERE(GCS_AVS_IMC_field.[Field Default] = '전시' AND  GCS_AVS_IMC_field.[Field Type] <> 'UD')OR GCS_AVS_IMC_bit.[Bit Default] = '전시'


        // 무시 Reserved

        // CSV 파일 읽기
        CsvReader csvReader = new CsvReader();
        List<string> csvNames = new List<string>();
        foreach (var csvFile in csvFiless)
        {
            var csvList = csvReader.ReadCsv(csvFile);
            csvNames.AddRange(csvList);
        }


        IAccessControl<string> accessControl = new AccessStatusNimonicCommand(connectionString);

        var nimonics = accessControl.GetAllAccessRead();

        foreach (var nimonic in nimonics)
        {
            if (csvNames.Contains(nimonic) == false)
            {
                if(nimonic.Contains("PPC") == true)
                {
                    Console.WriteLine(nimonic);
                    AppendToCsv(nimonic, csvWriteFileName);
                }
                
            }
        }
        //foreach(var csv in csvList)
        //{
        //    if(nimonics.Contains(csv.Key) == false)
        //    {
        //        Console.WriteLine(csv.Key);
        //    }
        //}

        // 데이터 출력 또는 처리
        //string sql = @" 
        //SELECT DISTINCT GCS_AVS_IMC_header.[명칭(니모닉)], GCS_AVS_IMC_bit.[Bit Name], GCS_AVS_IMC_field.[Field Name]
        //                FROM(GCS_AVS_IMC_header
        //                INNER JOIN GCS_AVS_IMC_field ON GCS_AVS_IMC_header.[명칭(니모닉)] = GCS_AVS_IMC_field.[명칭(니모닉)])
        //                LEFT OUTER JOIN GCS_AVS_IMC_bit ON GCS_AVS_IMC_field.[Field Name] = GCS_AVS_IMC_bit.[Field Name]
        //                WHERE(GCS_AVS_IMC_field.[Field Default] = '전시' AND  GCS_AVS_IMC_field.[Field Type] <> 'UD')OR GCS_AVS_IMC_bit.[Bit Default] = '전시'
        //                ";

        //string sql = @" 
        //SELECT DISTINCT GCS_AVS_FLCC_header.[명칭(니모닉)], GCS_AVS_FLCC_bit.[Bit Name], GCS_AVS_FLCC_field.[Field Name]
        //                FROM(GCS_AVS_FLCC_header
        //                INNER JOIN GCS_AVS_FLCC_field ON GCS_AVS_FLCC_header.[명칭(니모닉)] = GCS_AVS_FLCC_field.[명칭(니모닉)])
        //                INNER JOIN GCS_AVS_FLCC_bit ON GCS_AVS_FLCC_field.[Field Name] = GCS_AVS_FLCC_bit.[Field Name]
        //                WHERE(GCS_AVS_FLCC_field.[Field Default] = '전시' AND  GCS_AVS_FLCC_field.[Field Type] <> 'UD')OR GCS_AVS_FLCC_bit.[Bit Default] = '전시'
        //                ";
    }
    static void WriteToCsv(List<string> stringList, string filePath)
    {
        try
        {
            // StreamWriter를 사용하여 CSV 파일에 문자열을 작성
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (string item in stringList)
                {
                    // CSV 파일에 각 항목을 쉼표로 구분하여 씀
                    writer.WriteLine(item);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CSV 파일 작성 중 오류 발생: {ex.Message}");
        }
    }
    static void AppendToCsv(string input, string filePath)
    {
        try
        {
            // StreamWriter를 사용하여 CSV 파일에 문자열 추가 모드로 열기
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                // CSV 파일에 입력된 문자열을 쓰기
                writer.WriteLine(input);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CSV 파일에 문자열을 추가하는 중 오류 발생: {ex.Message}");
        }
    }
}
