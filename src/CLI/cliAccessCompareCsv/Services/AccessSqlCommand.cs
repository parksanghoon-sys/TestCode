using cliAccessCompareCsv.Models.AcessModels;
using cliAccessCompareCsv.Models.Tables;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliAccessCompareCsv.Services
{
    public interface IAccessControl<T> where T : class
    {
        IList<T> GetAllAccessRead();
        T GetAccessRead();
    }
    internal class AccessSqlCommand : IAccessControl<StatusNimonic>
    {
        private readonly string _connectionString;
        public AccessSqlCommand(string accssconectionString)
        {
            _connectionString = accssconectionString;
        }    
        public IList<StatusNimonic> GetAllAccessRead()
        {
            IList<StatusNimonic> access = new List<StatusNimonic>();
            // 데이터 출력 또는 처리

            for(int i = 0; i < (int)EBAccessTables.TOTAL; i ++)
            {
                string headerTableName = ((EBAccessTables)i).ToString();
                string fieldTableName = headerTableName.Replace("header", "field");
                string bitTableName = headerTableName.Replace("header", "bit");
                string sql = @$"
                            SELECT DISTINCT {.[명칭(니모닉)], GCS_AVS_IMC_bit.[Bit Name], GCS_AVS_IMC_field.[Field Name]
                            FROM (GCS_AVS_IMC_header
                            INNER JOIN GCS_AVS_IMC_field ON GCS_AVS_IMC_header.[명칭(니모닉)] = GCS_AVS_IMC_field.[명칭(니모닉)])
                            INNER JOIN GCS_AVS_IMC_bit ON GCS_AVS_IMC_field.[Field Name] = GCS_AVS_IMC_bit.[Field Name]
                            WHERE(GCS_AVS_IMC_field.[Field Default] = '전시' AND  GCS_AVS_IMC_field.[Field Type] <> 'UD')OR GCS_AVS_IMC_bit.[Bit Default] = '전시'
                            ";

                using (OleDbConnection connection = new OleDbConnection(_connectionString))
                {
                    connection.Open();

                    using (OleDbCommand nimonicCommand = new OleDbCommand(sql, connection))
                    using (OleDbDataReader nimonicReader = nimonicCommand.ExecuteReader())
                    {
                        while (nimonicReader.Read())
                        {
                            StatusNimonic statusNimonic = new StatusNimonic()
                            {

                            };
                            Console.WriteLine(statusNimonic.FullName);
                        }
                    }
                }
            }
            
        }

        public StatusNimonic GetAccessRead(string sql)
        {
            throw new NotImplementedException();
        }
    }
}
