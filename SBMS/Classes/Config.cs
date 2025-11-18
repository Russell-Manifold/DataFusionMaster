using System;
namespace SBMS.Classes
{
    public class Config
    {
        public static string GetConnectionString()
        {
            string sqlInstance = $"RHYOLITEHEXAGON\\MANIFOLDSQL";
            //string sqlInstance = $"MANIFOLDSERVER\\SQL2022";
            string dbName = ApiUrlCall.dbName;
        #if DEBUG
            sqlInstance = $"MANIFOLDSERVER\\SQL2022";
            #endif

            string dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? $"sa";
            string dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? $"M@nif0LD";

            // Build the provider connection string (SQL Server connection string)
            string providerConnectionString = string.Format(
                "data source={0};initial catalog={1};persist security info=True;user id={2};password={3};MultipleActiveResultSets=True;App=EntityFramework",
                sqlInstance, dbName, dbUser, dbPassword);

            // Combine with Entity Framework metadata
            return string.Format(
                "metadata=res://*/Models.SBMS.csdl|res://*/Models.SBMS.ssdl|res://*/Models.SBMS.msl;provider=System.Data.SqlClient;provider connection string=\"{0}\"",
                providerConnectionString);
        }
     }
}