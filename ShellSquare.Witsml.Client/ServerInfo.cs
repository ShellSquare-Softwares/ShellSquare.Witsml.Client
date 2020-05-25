using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShellSquare.Witsml.Client
{
    public class ServerInfo
    {
        public string Url { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public string ToJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"Url\":{ToEscape(Url)}");
            sb.Append($"\"UserName\":{ToEscape(UserName)}");
            sb.Append($"\"Password\":{ToEscape(Password)}");
            sb.Append("}");
            return sb.ToString();
        }



        private string ToEscape(string s)
        {
            if(s == null)
            {
                return "";
            }

            return JsonConvert.ToString(s);
        }
    }
}
