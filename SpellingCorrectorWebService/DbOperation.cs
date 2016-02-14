using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace SpellingCorrectorWebService
{
    public class DbOperation
    {
        public static string ConnectionString()
        {
            return System.Configuration.ConfigurationManager.ConnectionStrings["DBCS"].ConnectionString;
        }

        public static bool CheckDatabaseExists(string databaseName)
        {
            bool result = false;

            using (var connection = new SqlConnection(ConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = string.Format("SELECT NAME FROM MASTER.SYS.DATABASES;");
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (databaseName.Equals(reader.GetString(0)))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public static void CreateDatabase(string dbName)
        {
            using (var connection = new SqlConnection(ConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = string.Format("CREATE DATABASE IF NOT EXISTS {0}", dbName);
                command.ExecuteNonQuery();
            }
        }

        public static void CreateTable()
        {
            using (var connection = new SqlConnection(ConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = string.Format("CREATE TABLE IF NOT EXISTS WORDSLIST (Name VARCHAR(500));");
                command.ExecuteNonQuery();
            }
        }

        public static void InsertWord(string word)
        {
            using (var connection = new SqlConnection(ConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = string.Format("INSERT INTO WORDSLIST VALUES ({0});", word);
                command.ExecuteNonQuery();
            }
        }

        public static List<string> GetAllWords()
        {
            var result = new List<string>();

            using (var connection = new SqlConnection(ConnectionString()))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = string.Format("SELECT * FROM WORDSLIST");
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(reader.GetString(0));
                    }
                }
            }

            return result;
        }
    }
}