using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IntergrationPoint
{
    class ServerLogInfo
    {
        public string dateTime { get; set; }
        public List<ServiceInfo> servicesInfoList { get; set; }
        public int numOfAnonymouscaller { get; set; }
        public string logInfo { get; set; }
        public List<string> similarLog { get; set; }

        public ServerLogInfo()
        {
            servicesInfoList = new List<ServiceInfo>();
            similarLog = new List<string>();
        }

        public void setServiceInfo(string info)
        {
            for (int i = 0; i < servicesInfoList.Count; i++)
            {
                if (servicesInfoList[i].name == info)
                {
                    servicesInfoList[i].increaseCount();
                    return;
                }
            }
            servicesInfoList.Add(new ServiceInfo(info, 1));
        }

        public void addToSimilarLogSet(string log, double percentage)
        {
            //if (percentage <= 95.0d)
            //    similarLog.Add(log);
            //else
            //{
            //    if (!similarLog.Contains(log)) similarLog.Add(log);
            //}

            if (!similarLog.Contains(log))
            {
                if (percentage <= 95.0d)
                    similarLog.Add(log);
            }
        }

        public string removeErrorSignature(string s)
        {
            string errorSignaturePattern = @"^\*+\s*[E|e]rror\s+[C|c]ustom\s+[L|l]og\s*\*+";
            if (new Regex(errorSignaturePattern).IsMatch(s))
            {
                int flag = 0;
                bool charFound = false;
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] != '*' && !charFound)
                    {
                        charFound = true;
                        flag = 1;
                    }
                    else if (s[i] != '*' && s[i] != ' ' && flag > 1)
                        return s.Substring(i);
                    else if (s[i] == '*' && charFound)
                        flag = 2;
                }
            }
            return s;
        }
    }
}