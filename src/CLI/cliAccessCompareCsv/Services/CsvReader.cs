using cliAccessCompareCsv.CsvModel;

internal partial class Program
{
    public interface IReadCsv
    {
        List<CsvStatus> ReadCsv(string filePath);
    }
    public class CsvReader : IReadCsv
    {
        public List<CsvStatus> ReadCsv(string filePath)
        {
            List<CsvStatus> data = new List<CsvStatus>();

            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                 
                    // CSV 파일의 각 열을 데이터 모델에 매핑
                    CsvStatus model = new CsvStatus
                    {
                        Property = values[3],
                        StatusClass= values[4],
                        Name= values[8],
                    };

                    data.Add(model);
                }
            }

            data = data.OrderBy(x => x.StatusClass).ToList();
            return data;
        }
    }

}