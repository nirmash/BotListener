#r "Newtonsoft.Json"

using System;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static async Task<object> Run(HttpRequestMessage req, ICollector<string> outputQueueItem, TraceWriter log)
{
    log.Info($"Epi hook");

    string jsonContent = await req.Content.ReadAsStringAsync();

    log.Info($"Epi data: {jsonContent}");

    string sQueueMessage = SetResultsString(jsonContent);

    log.Info($"Queue message: {sQueueMessage}");

    outputQueueItem.Add(sQueueMessage);

    return req.CreateResponse(HttpStatusCode.OK, new {
        status = $"message sent"
    });
}
private static string SetResultsString(string jsn)
{
    JObject docobj = JObject.Parse(jsn);

    JEnumerable<JToken> results = docobj.Children();
    string retVal = "[";
    foreach (JToken result in results)
    {
        string key = ((JProperty)result).Name.ToString();
        string value = ((JProperty)result).Value.ToString();
        if (!key.StartsWith("SYSTEMCOLUMN_"))
        {
            retVal += "{'question':'" + key + "',";
            retVal += "'answer':'" + value + "'},";
        }
    }
    retVal += "{'question':'Rating','answer':'4'}]";
    return retVal;
}