using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;

namespace VKApiApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var connString = "Host=127.0.0.1;Username=postgres;Password=password1234;Database=postgres";
            //NpgsqlConnection.GlobalTypeMapper.UseJsonNet();
            var conn = new NpgsqlConnection(connString);    
            conn.Open();
            conn.TypeMapper.UseJsonNet();
            using (var delCmd = new NpgsqlCommand("DROP TABLE IF EXISTS Friends", conn))
            {
                delCmd.ExecuteNonQuery();
            }
            Console.WriteLine("Making API Call...");
            using (var myCmd = new NpgsqlCommand("CREATE TABLE Friends(id int, first_name text, last_name text, jinfo jsonb)", conn))
            {
                myCmd.ExecuteNonQuery();
                using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
                {
                    client.BaseAddress = new Uri("https://api.vk.com/method/");

                    HttpResponseMessage response = client.GetAsync("friends.get?v=5.52&access_token=9289c3a7e13041911cae47b9912caa2c72546704d2c7680d2a6f58f724f988a3c9c663d1a079f7c782fe0").Result;
                    response.EnsureSuccessStatusCode();

                    string idsResult = response.Content.ReadAsStringAsync().Result;
                    JObject jIdsResult = JObject.Parse(idsResult);
                    JToken jIdsArray = ((JObject)jIdsResult.GetValue("response")).GetValue("items");
                    string ids = "";
                    foreach (JValue el in jIdsArray)
                    {
                        ids += ',' + el.ToString();
                    }

                    ids = ids.Substring(1);

                    response = client.GetAsync("users.get?v=5.52&user_ids=" + ids + "&fields=sex,city&access_token=9289c3a7e13041911cae47b9912caa2c72546704d2c7680d2a6f58f724f988a3c9c663d1a079f7c782fe0").Result;
                    response.EnsureSuccessStatusCode();

                    string result = response.Content.ReadAsStringAsync().Result;
                    JObject jResult = JObject.Parse(result);
                    var jArray = jResult.GetValue("response");
                    myCmd.CommandText = "INSERT INTO Friends VALUES(@id, @first_name, @last_name, @jinfo)";
                    foreach (JObject el in jArray)
                    {
                        int id = Convert.ToInt32(el.GetValue("id"));
                        string name = el.GetValue("first_name").ToString();
                        string surname = el.GetValue("last_name").ToString();
                        //Console.WriteLine(id.ToString() + " " + name + " " + surname);
                        myCmd.Parameters.AddWithValue("id", id);
                        myCmd.Parameters.AddWithValue("first_name", name);
                        myCmd.Parameters.AddWithValue("last_name", surname);
                        //string maybejsonbstr = "{ \"first_name\" : \"" + name + "\", \"last_name\" : \"" + surname + "\" }";
                        //JObject maybejsonb = JObject.Parse(maybejsonbstr);
                        //myCmd.Parameters.AddWithValue("jinfo", maybejsonbstr);
                        myCmd.Parameters.Add(new NpgsqlParameter("jinfo", NpgsqlDbType.Jsonb) { Value = el });
                        myCmd.ExecuteNonQuery();
                        myCmd.Parameters.Remove("id");
                        myCmd.Parameters.Remove("first_name");
                        myCmd.Parameters.Remove("last_name");
                        myCmd.Parameters.Remove("jinfo");
                    }
                    Console.WriteLine("DONE!");
                }
            }          
            Console.ReadLine();
            conn.Close();
        }
    }
}
