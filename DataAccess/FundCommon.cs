using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DataAccess
{
    internal static class FundCommon 
    {
        public static async Task<FundValuation> GetFundValuation(string fundId)
        {
            string timestamp = ((DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000).ToString();
            Uri fundUri = new Uri($"http://fundgz.1234567.com.cn/js/{fundId}.js?{timestamp}");
            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(fundUri);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"visit {fundUri} for fund {fundId} failed");
            }

            string content = await response.Content.ReadAsStringAsync();
            content = content.Substring(8, content.Length - 10); // content is: jsonpgz({...}), remove the characters except {...}

            return JsonConvert.DeserializeObject<FundValuation>(content);
        }

        public static async Task<FundNetValue> GetFundNetValue(string fundId)
        {
            Uri uri = new Uri($"http://hq.sinajs.cn/list=f_{fundId}");
            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"visit {uri} for fund {fundId} failed");
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            string content = Encoding.GetEncoding("GB18030").GetString(bytes);
            string fundString = content.Split('=')[1];
            string[] tempFund = fundString[new Range(1, fundString.Length - 3)].Split(',');

            return new FundNetValue
            {
                dwjz = decimal.Parse(tempFund[1]),
                jzrq = DateTime.Parse(tempFund[4])
            };
        }
    }

    internal sealed class FundValuation
    {
        public string fundcode;
        public string name;
        public DateTime jzrq;
        public decimal dwjz;
        public decimal gsz;
        public decimal gszzl;
        public DateTime gztime;

        public override string ToString()
        {
            return $"基金代码(fundcode): {fundcode}\n基金名称(name): {name}\n净值日期(jzrq): {jzrq}\n单位净值(dwjz): {dwjz}\n估算值(gsz): {gsz}\n估算增长率(gszzl): {gszzl}\n估值时间(gztime): {gztime}";
        }
    }

    internal sealed class FundNetValue
    {
        public decimal dwjz;
        public DateTime jzrq;

        public override string ToString()
        {
            return $"单位净值(dwjz): {dwjz}\n净值日期(jzrq): {jzrq}\n";
        }
    }
}
