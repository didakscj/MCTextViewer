using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using DropNet.Helpers;
using DropNet;
using RestSharp;
using RestSharp.Deserializers;
using System.Text.RegularExpressions;

using System.Linq;

using DropNet.Extensions;



namespace MCTextViewer
{
    public class DropBoxFileDownload
    {
        //private static string API_ROOT = "https://api.dropbox.com/0/";
        private static string API_CONTENT_ROOT = "https://api-content.dropbox.com/1/";
        //private static string API_TOKEN = "token";
        //private static string API_ACCOUNT = "account";
        //private static string API_ACCOUNT_INFO = "account/info";
        private static string API_FILES = "files/dropbox";
        //private static string API_METADATA = "metadata/dropbox";
        //private static string API_THUMBNAILS = "thumbnails/dropbox";
        //private static string API_FILEOPS_COPY = "fileops/copy";
        //private static string API_FILEOPS_CREATE_FOLDER = "fileops/create_folder";
        //private static string API_FILEOPS_DELETE = "fileops/delete";
        //private static string API_FILEOPS_MOVE = "fileops/move";

        //public void filesget(string path, DownloadProgressChangedEventHandler DownloadProgressChanged,
        //   OpenReadCompletedEventHandler OpenReadCompleted)
        //{
        //    string requesturl = helper.OAuthRequestUrl(API_ROOT + API_FILES + path.Replace(" ", "%20") + "/", new List<string>());

        //    WebClient webClient = new WebClient();
        //    webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressChanged);
        //    webClient.OpenReadCompleted += new OpenReadCompletedEventHandler(OpenReadCompleted);
        //    webClient.OpenReadAsync(new Uri(requesturl));
        //}

       
        private const string _apiBaseUrl = "https://api.dropbox.com";
        private const string _apiContentBaseUrl = "https://api-content.dropbox.com";
        private const string _version = "1";
        private string _dropboxRoot = "dropbox";



        public static string nonce()
        {
            return new Random().Next(123400, 9999999).ToString();
        }

        public static string timestamp()
        {
            TimeSpan span = (TimeSpan)(DateTime.UtcNow - new DateTime(0x7b2, 1, 1, 0, 0, 0, 0));
            return Convert.ToInt64(span.TotalSeconds).ToString();
        }

        private static string NormalizeRequestParameters(IList<Parameter> parameters)
        {
            StringBuilder builder = new StringBuilder();
            List<Parameter> list = parameters.Where(p =>
            {
                //Hackity hack, don't come back...
                return (p.Type == ParameterType.GetOrPost || p.Name == "file" || p.Name.StartsWith("oauth_"));
            }).ToList();

            Parameter parameter = null;

            for (int i = 0; i < list.Count; i++)
            {
                parameter = list[i];
                builder.AppendFormat("{0}={1}", parameter.Name, parameter.Value.ToString().UrlEncode());
                if (i < (list.Count - 1))
                {
                    builder.Append("&");
                }
            }
            return builder.ToString();
        }

        private string GenerateSignature(IRestRequest request, String _appSecret, String _userSecret)
        {
            Uri uri = this.BuildUri(request);
            string str = string.Format("{0}://{1}", uri.Scheme, uri.Host);
            if (((uri.Scheme != "http") || (uri.Port != 80)) && ((uri.Scheme != "https") || (uri.Port != 0x1bb)))
            {
                str = str + ":" + uri.Port;
            }
            str = str + uri.AbsolutePath;
            string str2 = NormalizeRequestParameters(request.Parameters);
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("{0}&", request.Method.ToString().ToUpper());
            builder.AppendFormat("{0}&", str.UrlEncode());
            builder.AppendFormat("{0}", str2.UrlEncode());
            string data = builder.ToString();
            HMACSHA1 hashAlgorithm = new HMACSHA1();
            hashAlgorithm.Key = Encoding.UTF8.GetBytes(string.Format("{0}&{1}", _appSecret.UrlEncode(), _userSecret.UrlEncode()));
            return ComputeHash(hashAlgorithm, data);
        }

        private Uri BuildUri(IRestRequest request)
        {
            string resource = request.Resource;
            resource = request.Parameters.Where<Parameter>(delegate(Parameter p)
            {
                return (p.Type == ParameterType.UrlSegment);
            }).Aggregate<Parameter, string>(resource, delegate(string current, Parameter p)
            {
                return current.Replace("{" + p.Name + "}", p.Value.ToString().UrlEncode());
            });
            return new Uri(string.Format("{0}/{1}", _apiContentBaseUrl, resource));
        }

        private static string ComputeHash(HashAlgorithm hashAlgorithm, string data)
        {
            if (hashAlgorithm == null)
            {
                throw new ArgumentNullException("hashAlgorithm");
            }
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentNullException("data");
            }
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(hashAlgorithm.ComputeHash(bytes));
        }

        // Nested Types
        private class QueryParameterComparer : IComparer<Parameter>
        {
            // Methods
            public int Compare(Parameter x, Parameter y)
            {
                return ((x.Name == y.Name) ? string.Compare(x.Value.ToString(), y.Value.ToString()) : string.Compare(x.Name, y.Name));
            }
        }


        public String getfiledownloadrequest(String filePath, String appToken, String appScret, String UserToken, String UserScret)
        {
            var request = new RestRequest(Method.GET);
            request.Resource = "{version}/files/{root}{path}/?oauth_consumer_key={oauth_consumer_key}&oauth_nonce={oauth_nonce}&oauth_token={oauth_token}&oauth_timestamp={oauth_timestamp}&oauth_signature_method={oauth_signature_method}&oauth_version={oauth_version}";
            request.AddParameter("version", _version, ParameterType.UrlSegment);
            request.AddParameter("path", filePath, ParameterType.UrlSegment);
            request.AddParameter("root", _dropboxRoot, ParameterType.UrlSegment);
            request.AddParameter("oauth_consumer_key", appToken, ParameterType.UrlSegment);
            request.AddParameter("oauth_nonce", nonce(), ParameterType.UrlSegment);
            request.AddParameter("oauth_token", UserToken, ParameterType.UrlSegment);
            request.AddParameter("oauth_timestamp", timestamp(), ParameterType.UrlSegment);
            request.AddParameter("oauth_signature_method", "HMAC-SHA1", ParameterType.UrlSegment);
            request.AddParameter("oauth_version", "1.0", ParameterType.UrlSegment);
            request.Parameters.Sort(new QueryParameterComparer());
            request.AddParameter("oauth_signature", this.GenerateSignature(request, appScret, UserScret), ParameterType.UrlSegment);

            
            String url = API_CONTENT_ROOT + API_FILES + filePath + "/?" +NormalizeRequestParameters(request.Parameters);
            
            
            return url;

            
            //List<string> Parameters = new List<string>();
            //String path = filePath.Replace(" ", "%20");//file path
            ////String aa = HttpUtility.UrlEncode(path);   
            //String url = API_CONTENT_ROOT + API_FILES + path + "/";

            //String path2 = HttpUtility.UrlEncode(filePath.Replace(" ", "%20"));//file path
            //String url2 = API_CONTENT_ROOT + API_FILES + path2.ToUpper() + "/";

            //Parameters.Add("oauth_consumer_key=" + HttpUtility.UrlEncode(appToken));//API key
            //Parameters.Add("oauth_nonce=" + HttpUtility.UrlEncode(nonce()));
            //Parameters.Add("oauth_signature_method=HMAC-SHA1");
            //Parameters.Add("oauth_version=1.0");
            ////Parameters.Add("oauth_callback=oob");
            //Parameters.Add("oauth_timestamp=" + HttpUtility.UrlEncode(timestamp()));
            //Parameters.Add("oauth_token=" + UserToken);//user token
            //Parameters.Sort();
            //string parametersStr = string.Join("&", Parameters.ToArray());
            //string baseStr = //"GET" + "&" +
            //     //HttpUtility.UrlEncode(url).Replace("%2f", "%2F").Replace("%3a", "%3A").Replace("%3d", "%3D") + "&" +
            //     HttpUtility.UrlEncode(parametersStr).Replace("%2f", "%2F").Replace("%3a", "%3A").Replace("%3d", "%3D");
            ////baseStr = "GET" + "&" +
            ////     HttpUtility.UrlEncode(url).Replace("%2f", "%2F").Replace("%3a", "%3A").Replace("%3d", "%3D") +
            ////     HttpUtility.UrlEncode(parametersStr).Replace("%2f", "%2F").Replace("%3a", "%3A").Replace("%3d", "%3D");
            //          //"https%3A%2F%2Fapi-content.dropbox.com%2F1%2Ffiles%2Fdropbox%2F%25EC%2582%25AC%25EC%25A0%2584%2Fsamplehan.txt%2F"+
            //          //"&oauth_consumer_key%3D"+HttpUtility.UrlEncode(appToken)+
            //          //"%26oauth_nonce%3D" + HttpUtility.UrlEncode(nonce()) +
            //          //"%26oauth_signature_method%3DHMAC-SHA1" +
            //          //"%26oauth_timestamp%3D" + HttpUtility.UrlEncode(timestamp()) +
            //          //"%26oauth_token%3D" + UserToken +
            //          //"%26oauth_version%3D1.0";

            ///* create the crypto class we use to generate a signature for the request */
            //byte[] key = Encoding.UTF8.GetBytes(appScret);//app scret key
            //key = Encoding.UTF8.GetBytes(appScret + "&" + UserScret); //app scret, user secret

            //HMACSHA1 sha1 = new HMACSHA1(key);
            //byte[] baseStringBytes = Encoding.UTF8.GetBytes(baseStr);
            //byte[] baseStringHash = sha1.ComputeHash(baseStringBytes);
            //String base64StringHash = Convert.ToBase64String(baseStringHash);
            //String encBase64StringHash = HttpUtility.UrlEncode(base64StringHash);
            //Parameters.Add("oauth_signature=" + encBase64StringHash);
            //Parameters.Sort();
            //String ap = (url + "?" + string.Join("&", Parameters.ToArray()));
            //return ap;
        }
    }
}
