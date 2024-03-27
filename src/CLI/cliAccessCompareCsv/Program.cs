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
        
        string dbFilePath = @"D:\Project\02.Document\2024\01.항전개조\2.ICD\B\2024.02.23_통합 ICD_V5.02.accdb";
        string csvFilePath = @$"C:\B.Settings\Csv\StatusCsv\{csvFileName}";
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
        foreach(var csvFile in csvFiless)
        {
            var csvList = csvReader.ReadCsv(csvFile);
            csvNames.AddRange(csvList);
        }
        

        IAccessControl<string> accessControl = new AccessStatusNimonicCommand(connectionString);

        var nimonics = accessControl.GetAllAccessRead();

        foreach(var nimonic in nimonics)
        {
            if(csvNames.Contains(nimonic) == false)
            {
                Console.WriteLine(nimonic);
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
}
