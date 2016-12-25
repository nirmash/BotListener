using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, string name, TraceWriter log)
{
    string baseURL = @"d:\home\site\wwwroot\mysite\";
    string sFileName = baseURL + name + ".html";
    var response = new HttpResponseMessage(HttpStatusCode.OK);
    var stream = new FileStream(sFileName, FileMode.Open);
    response.Content = new StreamContent(stream);
    response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
    return response;
}