using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

using System.IO;
using System.Net;
using System.Threading;
using System.Runtime.Serialization;          //Over in References tab in Solution Explorer inside Visual Studio, you need to include System.Runtime.Serialization
using System.Runtime.Serialization.Json;

namespace Example_ARCHCAS_Download_CSV
{
    [DataContract]
    public class User_Identity
    {
        [DataMember]
        public int id;

        [DataMember]
        public string type;

        [DataMember]
        public string association;

        [DataMember]
        public string institution;

        [DataMember]
        public string organization;

        [DataMember]
        public string cycle;
    }


    [DataContract]
    public class User_Identity_List
    {
        [DataMember]
        public string href;

        [DataMember]
        public User_Identity[] user_identities;

    }

    [DataContract]
    public class Export
    {
        [DataMember]
        public int id;

        [DataMember]
        public string name;

        [DataMember]
        public string list_type;

        [DataMember]
        public string format;

        [DataMember]
        public string mime_type;
    }


    [DataContract]
    public class Export_List
    {
        [DataMember]
        public string href;

        [DataMember]
        public Export[] exports;

    }

    [DataContract]
    public class Export_Files_Alone
    {
        [DataMember]
        public int id;

        [DataMember]
        public string href;

        [DataMember]
        public int export_id;

        [DataMember]
        public string status;
    }

    [DataContract]
    public class Export_Files
    {
        [DataMember]
        public Export_Files_Alone export_files;
    }

    [DataContract]
    public class Status_Check_Alone
    {
        [DataMember]
        public int id;

        [DataMember]
        public string href;

        [DataMember]
        public int export_id;

        [DataMember]
        public string status;

        [DataMember]
        public string download_url;
    }

    [DataContract]
    public class Status_Check
    {
        [DataMember]
        public Status_Check_Alone export_files;
    }


    class Program
    {
        static bool done = false;

        static void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs ee)
        {
            Console.WriteLine("Downloading {0} threadid={1}", ee.ProgressPercentage, Thread.CurrentThread.ManagedThreadId);
        }

        static void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs ee)
        {
            Console.WriteLine("Downloading DONE");
            done = true;
        }

        static void Main(string[] args)
        {
            string restful_api_base_url = "https://woodbury.webadmit.org/api/v1/";
            string my_api_key_from_archcas_users_website = "b03d389943290bed5cf3a667ef01e788";

            WebClient wc = new WebClient();
            string api_key_header = string.Format("x-api-key:{0}", my_api_key_from_archcas_users_website);
            wc.Headers[HttpRequestHeader.ContentType] = "application/json";
            wc.Headers.Add(api_key_header);
            wc.DownloadProgressChanged += wc_DownloadProgressChanged;
            wc.DownloadFileCompleted += wc_DownloadFileCompleted;

            /************************************************************************************************************************************************
              (1) To get my user_id from my api_key.

              curl -n https://woodbury.webadmit.org/api/v1/user_identities -H "x-api-key: b03d389943290bed5cf3a667ef01e788"
        
              {"href":"/api/v1/user_identities","user_identities":[{"id":261149,"type":"Admissions User","organization":"Woodbury University","association":"ArchCAS","cycle":"2017 - 2018","institution":"Woodbury University"}]}

              261149
            *************************************************************************************************************************************************/
            string userlist_url           = string.Format("{0}user_identities", restful_api_base_url);
            string userlist_json_response = wc.DownloadString(userlist_url);

            DataContractJsonSerializer userlist_ser = new DataContractJsonSerializer(typeof(User_Identity_List));
            MemoryStream userlist_mem = new MemoryStream(System.Text.ASCIIEncoding.ASCII.GetBytes(userlist_json_response));
            User_Identity_List userlist = (User_Identity_List)userlist_ser.ReadObject(userlist_mem);
            int user_id = userlist.user_identities[0].id;

            Console.WriteLine("user_id = {0}", user_id);
            Thread.Sleep(2000); //API Rate Limit per user is 5000 times in a 1-hour period, 3600 / 5000 = 0.72 seconds per api-call, lets wait 2 seconds
            
            
            
            
            
            /***************************************************************************************************************************************************
              (2) To see what .csv exports belong to my user_id. It will give you an export_id.

              curl -n https://woodbury.webadmit.org/api/v1/user_identities/261149/exports -H "x-api-key: b03d389943290bed5cf3a667ef01e788"

              {"href":"/api/v1/user_identities/261149/exports","exports":[{"id":335158,"name":"my_second_test_report","list_type":"all","format":"Comma-Separated Values","mime_type":"text/csv;charset=iso-8859-1"},{"id":335046,"name":"my_first_test_csv_export","list_type":"all","format":"Comma-Separated Values","mime_type":"text/csv;charset=iso-8859-1"}]}

              335158
            ****************************************************************************************************************************************************/
            string exports_url           = string.Format("{0}user_identities/{1}/exports", restful_api_base_url, user_id);
            string exports_json_response = wc.DownloadString(exports_url);

            DataContractJsonSerializer exports_ser = new DataContractJsonSerializer(typeof(Export_List));
            MemoryStream exports_mem = new MemoryStream(System.Text.ASCIIEncoding.ASCII.GetBytes(exports_json_response));
            Export_List exports = (Export_List)exports_ser.ReadObject(exports_mem);
            int export_id = exports.exports[0].id;

            Console.WriteLine("export_id = {0}", export_id);
            Thread.Sleep(2000); //API Rate Limit per user is 5000 times in a 1-hour period, 3600 / 5000 = 0.72 seconds per api-call, lets wait 2 seconds

            /******************************************************************************************************************************************************
              (3) To initiate a run (sql query on which the .csv is created from) based on export_id and user_id. It will give you export_file_id.

              curl -n -X POST https://woodbury.webadmit.org/api/v1/user_identities/261149/exports/335158/export_files -H "Content-Type: application/json" -H "x-api-key: b03d389943290bed5cf3a667ef01e788"

              {"export_files":{"id":267520,"href":"/api/v1/exports/335158/export_files/267520","export_id":335158,"status":"Queued"}}

              267520
            *******************************************************************************************************************************************************/
            string run_url           = string.Format("{0}user_identities/{1}/exports/{2}/export_files", restful_api_base_url, user_id, export_id);
            string run_json_response = wc.UploadString(run_url, "");

            DataContractJsonSerializer run_ser = new DataContractJsonSerializer(typeof(Export_Files));
            MemoryStream run_mem = new MemoryStream(System.Text.ASCIIEncoding.ASCII.GetBytes(run_json_response));
            Export_Files run_exportfiles = (Export_Files)run_ser.ReadObject(run_mem);
            int export_file_id = run_exportfiles.export_files.id;

            Console.WriteLine("export_file_id = {0}", export_file_id);
            Thread.Sleep(2000); //API Rate Limit per user is 5000 times in a 1-hour period, 3600 / 5000 = 0.72 seconds per api-call, lets wait 2 seconds

            /************************************************************************************************************************************************************
               (4) Check status of that run to see if file available for download

               curl -n https://woodbury.webadmit.org/api/v1/exports/335158/export_files/267520 -H "x-api-key: b03d389943290bed5cf3a667ef01e788"

               {"export_files":{"id":267520,"href":"/api/v1/exports/335158/export_files/267520","export_id":335158,"status":"Available","download_url":"https://webadmit-production.s3.amazonaws.com/export_files/reports/000/267/520/6ba17bcedbd5c204c5ba28118beb432b_original.txt?AWSAccessKeyId=AKIAIT7746UHRBGHSHEA&Expires=1527193390&Signature=3%2FkTo%2B8J2iV8x1rShKviSRTXzlg%3D&response-content-disposition=attachment%3B%20filename%3D%22my_second_test_report.csv%22&response-content-type=text%2Fcsv%3Bcharset%3Diso-8859-1"}}

            **************************************************************************************************************************************************************/
            string status_url = string.Format("{0}exports/{1}/export_files/{2}", restful_api_base_url, export_id, export_file_id);
            string status_json_response = wc.DownloadString(status_url);

            DataContractJsonSerializer status_ser = new DataContractJsonSerializer(typeof(Status_Check));
            MemoryStream status_mem = new MemoryStream(System.Text.ASCIIEncoding.ASCII.GetBytes(status_json_response));
            Status_Check status = (Status_Check)status_ser.ReadObject(status_mem);
            string status_str = status.export_files.status;


            Console.WriteLine("status = {0}", status_str);
            while (status_str.ToLower().Equals("available") == false)
            {
                Console.WriteLine("Sleeping 3 seconds till download file is available");
                Thread.Sleep(3000);

                Console.WriteLine("Calling status check restful-api call again");
                status_json_response = wc.DownloadString(status_url);
                status_mem = new MemoryStream(System.Text.ASCIIEncoding.ASCII.GetBytes(status_json_response));
                status = (Status_Check)status_ser.ReadObject(status_mem);
                status_str = status.export_files.status;
            }

            
            

            Uri download_url = new Uri(status.export_files.download_url);
            string output_csv  = @".\output.csv";

            done = false;
            wc.DownloadFileAsync(download_url, output_csv);

            while (done == false)
            {
                Console.WriteLine("main threadid={0} not done, sleep 100 milliseconds.....", Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(100);
            }
            Console.WriteLine("Main DONE");
            
        }

    }
}
