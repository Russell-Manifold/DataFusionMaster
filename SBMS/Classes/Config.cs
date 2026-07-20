using System;
namespace SBMS.Classes
{
    public class Config
    {
        public static string GetConnectionString()
        {
            return string.Format(
                "metadata=res://*/Models.SBMS.csdl|res://*/Models.SBMS.ssdl|res://*/Models.SBMS.msl;provider=System.Data.SqlClient;provider connection string=\"{0}\"",
                GetRawConnectionString());
        }

        public static string GetRawConnectionString()
        {
            string sqlInstance = $"RHYOLITEHEXAGON\\MANIFOLDSQL";
            //string sqlInstance = $"MANIFOLDSERVER\\SQL2022";
            string dbName = ApiUrlCall.dbName;
            #if DEBUG
                        //sqlInstance = $"SYNCFLO-DESKTOP\\SYNCFLOSQL";
                        sqlInstance = $"RUSSELL-DELL\\DELLSQL";
            #endif

            string dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? $"sa";
            string dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? $"M@nif0LD";

            return string.Format(
                "data source={0};initial catalog={1};persist security info=True;user id={2};password={3};MultipleActiveResultSets=True;App=EntityFramework",
                sqlInstance, dbName, dbUser, dbPassword);
        }

     }
}