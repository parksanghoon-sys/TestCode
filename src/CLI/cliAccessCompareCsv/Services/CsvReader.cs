using cliAccessCompareCsv.CsvModel;

internal partial class Program
{
    public interface IReadCsv
    {
        List<string> ReadCsv(string filePath);
    }
    public class CsvReader : IReadCsv
    {
        List<string> _csvList = new();

        public List<string> ReadCsv(string filePath)
        {            
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
                        Name= values[8].Trim(),
                    };
                    _csvList.Add(model.Name);
                }
            }            
            return _csvList;
        }
    }

}