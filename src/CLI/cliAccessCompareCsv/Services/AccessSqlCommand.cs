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
    public class AccessStatusNimonicCommand : IAccessControl<string>
    {
        private readonly string _connectionString;
        public AccessStatusNimonicCommand(string accssconectionString)
        {
            _connectionString = accssconectionString;
        }    
        public IList<string> GetAllAccessRead()
        {
            IList<string> nimonics = new List<string>();
            // 데이터 출력 또는 처리

            for(int i = 0; i < (int)EBAccessTables.TOTAL; i ++)
            {
                string headerTableName = ((EBAccessTables)i).ToString();
                string fieldTableName = headerTableName.Replace("header", "field");
                string bitTableName = headerTableName.Replace("header", "bit");
                string sql = @$"
                            SELECT DISTINCT {headerTableName}.[명칭(니모닉)], {bitTableName}.[Bit Name], {fieldTableName}.[Field Name]
                            FROM ({headerTableName}
                            INNER JOIN {fieldTableName} ON {headerTableName}.[명칭(니모닉)] = {fieldTableName}.[명칭(니모닉)])
                            LEFT OUTER JOIN {bitTableName} ON {fieldTableName}.[Field Name] = {bitTableName}.[Field Name]
                            WHERE({fieldTableName}.[Field Default] = '전시' AND  {fieldTableName}.[Field Type] <> 'UD')OR {bitTableName}.[Bit Default] = '전시'
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
                                Id = nimonicReader[0].ToString(),
                                BitName = nimonicReader[1]?.ToString(),
                                FieldName = nimonicReader[2].ToString(),
                            };
                            //if(statusNimonic.FieldName == "IFF_STATUS_02")
                            //{
                            //    Console.WriteLine("T");
                            //}
                            //Console.WriteLine(statusNimonic.FullName);
                            nimonics.Add(statusNimonic.FullName.Trim());
                        }
                    }
                  
                }
            }
            return nimonics ?? default(IList<string>);
        }

        public string GetAccessRead()
        {
            throw new NotImplementedException();
        }
    }
}
