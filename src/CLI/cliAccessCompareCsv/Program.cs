using cliAccessCompareCsv.AcessModels;
using System.Data.OleDb;

internal partial class Program
{
    private static void Main(string[] args)
    {
        string csvFileName = "IFF_StatusDisplay.csv";
        string dbTableName = "GCS_AVS_IMC_header";
        string dbFilePath = @"D:\Project\02.Document\2024\01.항전개조\2.ICD\B\2024.02.23_통합 ICD_V5.02.accdb";
        string csvFilePath = @$"C:\B.Settings\Csv\StatusCsv\{csvFileName}";
        string connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbFilePath};Persist Security Info=False;";
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
        var csvList = csvReader.ReadCsv(csvFilePath);
        var errorList = new List<string>();
        // 데이터를 저장할 리스트 생성
        List<Nimonic> dataList = new List<Nimonic>();

        // Access 데이터베이스에 연결
        //using (OleDbConnection connection = new OleDbConnection(connectionString))
        //{
        //    connection.Open();

        //    // Nimonic 테이블 조회
        //    string nimonicQuery = $"SELECT * FROM {dbTableName}";
        //    using (OleDbCommand nimonicCommand = new OleDbCommand(nimonicQuery, connection))
        //    using (OleDbDataReader nimonicReader = nimonicCommand.ExecuteReader())
        //    {
        //        while (nimonicReader.Read())
        //        {
        //            Nimonic nimonic = new Nimonic();

        //            // 필드별 데이터 읽어오기
        //            nimonic.Id = nimonicReader[0].ToString();
        //            nimonic.Title = nimonicReader[1].ToString();
        //            nimonic.Description = nimonicReader[2].ToString();
        //            nimonic.Type = nimonicReader[4].ToString();
        //            nimonic.Range = nimonicReader[11].ToString();
        //            var dbFildTableName = dbTableName.Replace("header", "field");

        //            // DetailNimonic 조회
        //            string detailQuery = $"SELECT * FROM {dbTableName.Replace("header", "field")} WHERE [명칭(니모닉)] = '{nimonic.Title}'";
        //            using (OleDbCommand detailCommand = new OleDbCommand(detailQuery, connection))
        //            using (OleDbDataReader detailReader = detailCommand.ExecuteReader())
        //            {
        //                List<DetailNimonic> details = new List<DetailNimonic>();

        //                while (detailReader.Read())
        //                {
        //                    DetailNimonic detailNimonic = new DetailNimonic()
        //                    {
        //                        FildIndex = Convert.ToInt32(detailReader[1].ToString()),
        //                        FieldName = detailReader[2]?.ToString(),
        //                        FieldDescription = detailReader[3]?.ToString(),
        //                        IsFieldDefault = detailReader[11].ToString() == "전시" ? true : false,
        //                        IsAVTEFieldDefault = detailReader[14].ToString() == "전시" ? true : false,
        //                    };

        //                    // BitNimonic 조회
        //                    if (detailNimonic.IsFieldDefault == true)
        //                    {
        //                        string bitQuery = $"SELECT * FROM {dbTableName.Replace("header", "bit")} WHERE [Field Name] = '{detailNimonic.FieldName}'";
        //                        using (OleDbCommand bitCommand = new OleDbCommand(bitQuery, connection))
        //                        using (OleDbDataReader bitReader = bitCommand.ExecuteReader())
        //                        {
        //                            List<BitNimonic> bits = new List<BitNimonic>();

        //                            while (bitReader.Read())
        //                            {
        //                                BitNimonic bit = new BitNimonic()
        //                                {
        //                                    BitNumber = bitReader[1].ToString(),
        //                                    BitName = bitReader[2].ToString(),
        //                                    BitDescription = bitReader[3].ToString(),
        //                                    IsBitDefault = bitReader[4].ToString() == "전시" ? true : false,
        //                                    IsAVTEFieldDefault = bitReader[8].ToString() == "전시" ? true : false,
        //                                };

        //                                bits.Add(bit);
        //                            }

        //                            detailNimonic.BitNimonics = bits;
        //                        }
        //                    }

        //                    details.Add(detailNimonic);
        //                }

        //                nimonic.DetailNimonics = details;
        //            }

        //            dataList.Add(nimonic);
        //        }
        //    }
        //}


        string sql = "SELECT Name FROM MSysObjects WHERE Type = 1 AND Flags = 0;";
        using (OleDbConnection connection = new OleDbConnection(connectionString))
        {
            connection.Open();
       
            using (OleDbCommand nimonicCommand = new OleDbCommand(sql, connection))
            using (OleDbDataReader nimonicReader = nimonicCommand.ExecuteReader())
            {
                while (nimonicReader.Read())
                {
                    Console.WriteLine(nimonicReader.GetString(0));
                }
            }
        }


    }
}
