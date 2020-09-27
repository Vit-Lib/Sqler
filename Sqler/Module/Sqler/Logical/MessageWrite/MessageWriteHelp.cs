using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Text;
using Vit.Core.Module.Log;

namespace Sqler.Module.Sqler.Logical.MessageWrite
{
    public class MessageWriteHelp
    {
        

        public static void SendMsg(HttpResponse Response, EMsgType type, String msg, bool writeLogger=true)
        {

            if (writeLogger)
            {
                if (type == EMsgType.Err)
                    Logger.Info("[Error]" + msg);
                else
                    Logger.Info(msg);
            }

            switch (type)
            {
                case EMsgType.Html:
                    {
                        Response.WriteAsync(msg);
                        break;
                    }
                case EMsgType.Err:
                    {
                        var escapeMsg = Str2XmlStr(msg)?.Replace("\n", "<br/>");
                        Response.WriteAsync("<br/><font style='color:#f00;font-weight:bold;'>" + escapeMsg + "</font>");
                        break;
                    }
                case EMsgType.Title:
                    {
                        var escapeMsg = Str2XmlStr(msg)?.Replace("\n", "<br/>");
                        Response.WriteAsync("<br/><font style='color:#005499;font-weight:bold;'>" + escapeMsg + "</font>");
                        break;
                    }
                default:
                    {
                        var escapeMsg = Str2XmlStr(msg)?.Replace("\n", "<br/>");
                        Response.WriteAsync("<br/>" + escapeMsg);
                        break;
                    }
            }      //Response.Flush();  

        }

        static string Str2XmlStr(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"':
                        stringBuilder.Append("&quot;");
                        break;
                    case '&':
                        stringBuilder.Append("&amp;");
                        break;
                    case '<':
                        stringBuilder.Append("&lt;");
                        break;
                    case '>':
                        stringBuilder.Append("&gt;");
                        break;
                    default:
                        stringBuilder.Append(c);
                        break;
                }
            }
            return stringBuilder.ToString();
        }
    }
}
