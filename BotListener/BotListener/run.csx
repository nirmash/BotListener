using System;
using System.Net;
using System.Text;
using System.IO;
using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public static void Run(string myQueueItem, TraceWriter log)
{
    log.Info($"C# Queue trigger function entry: {myQueueItem}");
    //load the data from the bot
    JArray jarr = JArray.Parse(myQueueItem);
    ListQuestions questionArray = new ListQuestions();
    questionArray.loadJArray(jarr);
    log.Info($"Questions array loaded");
    //get the sentiment
    string sentiment = GetSentiment(questionArray.getAnswer("Comments"));
    questionArray.questions.Add(new QandA("Sentiment", sentiment));
    log.Info($"Sentiment processed: {sentiment}");
    //send to PBI
    string PBIPayload = GetResultsString(questionArray);
    log.Info($"PBI Payload: {PBIPayload}");
    string PBIEndPoint = ConfigurationManager.AppSettings["PBIEndpoint"];
    if (PBIEndPoint == null)
        PBIEndPoint = "https://api.powerbi.com/beta/72f988bf-86f1-41af-91ab-2d7cd011db47/datasets/7a115dff-fa60-4561-9781-080d11a6b09f/rows?key=2326%2FXlV544PTtiVwOtHjqxbgHWiXXMfAeKOgFym8ZWVy5ueyQr4fLM0hL%2BlaMGWWAPZ8wjKAD8D3%2FCw8J3aQw%3D%3D";
    string PBIResult = SendToPBI(PBIEndPoint, PBIPayload);
    log.Info($"Sent to PBI: {PBIResult}");
}
public class QandA
{
    public QandA (string question, string answer)
    {
        this.question = question;
        this.answer = answer;
    }
    public QandA()
    {

    }
    public string question { get; set; }
    public string answer { get; set; }
}
public class ListQuestions
{
    public ListQuestions()
    {
        this.questions = new List<QandA>();
    }
    public string getAnswer(string question)
    {
        foreach(QandA oQuestion in this.questions)
        {
            if (oQuestion.question == question)
                return oQuestion.answer;
        }
        return "";
    }
    public void loadJArray(JArray jarr)
    {
        foreach (JObject content in jarr.Children<JObject>())
        {
            QandA oQuestion = new QandA();
            var iCounter = 0;
            foreach (JProperty prop in content.Properties())
            {
                if (iCounter == 0)
                    oQuestion.question = prop.Value.ToString();
                else
                    oQuestion.answer = prop.Value.ToString();
                iCounter++;
            }
            this.questions.Add(oQuestion);
        }
    }
    public List<QandA> questions { get; set; }
}
public class BatchResult
{
    public List<DocumentResult> documents { get; set; }
}
public class BatchInput
{
    public List<DocumentInput> documents { get; set; }
}
public class DocumentInput
{
    public double id { get; set; }
    public string text { get; set; }
}
public class DocumentResult
{
    public double score { get; set; }
    public string id { get; set; }
}
private static string GetSentiment(string comment)
{

    string apiKey = ConfigurationManager.AppSettings["TextApiKey"];
    string queryUri = ConfigurationManager.AppSettings["TextApiUri"];

    if(apiKey==null && queryUri == null)
    {
        apiKey = "4d5a006c1e8e4f7289d007b3e9fe426e";
        queryUri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";

    }
    // Create a request using a URL that can receive a post. 
    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(queryUri);
    // Set the Method property of the request to POST.
    request.Method = "POST";
    request.Accept = "application/json";
    request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

    var sentimentInput = new BatchInput
    {
        documents = new List<DocumentInput> {
                    new DocumentInput {
                            id = 1,
                            text = comment
                        }
                    }
    };
    string postData = JsonConvert.SerializeObject(sentimentInput);

    byte[] byteArray = Encoding.UTF8.GetBytes(postData);
    // Set the ContentType property of the WebRequest.
    //request.ContentType = "application/x-www-form-urlencoded";
    // Set the ContentLength property of the WebRequest.
    request.ContentLength = byteArray.Length;
    // Get the request stream.
    Stream dataStream = request.GetRequestStream();
    // Write the data to the request stream.
    dataStream.Write(byteArray, 0, byteArray.Length);
    // Close the Stream object.
    dataStream.Close();
    // Get the response.
    WebResponse response = request.GetResponse();
    // Display the status.
    // Console.WriteLine(((HttpWebResponse)response).StatusDescription);
    // Get the stream containing content returned by the server.
    dataStream = response.GetResponseStream();
    // Open the stream using a StreamReader for easy access.
    StreamReader reader = new StreamReader(dataStream);
    // Read the content.
    string responseFromServer = reader.ReadToEnd();
    // Display the content.

    var sentimentJsonResponse = JsonConvert.DeserializeObject<BatchResult>(responseFromServer);
    var sentimentScore = sentimentJsonResponse?.documents?.FirstOrDefault()?.score ?? 0;

    string retMessage;
    if (sentimentScore > 0.7)
    {
        retMessage = $"Positive";
    }
    else if (sentimentScore < 0.3)
    {
        retMessage = $"Negative";
    }
    else
    {
        retMessage = $"Indifferent";
    }
    
    // Clean up the streams.
    reader.Close();
    dataStream.Close();
    response.Close();
    return retMessage;
}
private static string SendToPBI(string sEndPoint, string sPayLoad)
{
    //send to PBI (both)
    var httpWebRequest = (HttpWebRequest)WebRequest.Create(sEndPoint);
    httpWebRequest.ContentType = "application/json";
    httpWebRequest.Method = "POST";

    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
    {
        streamWriter.Write(sPayLoad);
    }
    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
    {
        return streamReader.ReadToEnd().ToString();
    }
    return "500";
}
private static string GetResultsString(ListQuestions questionArray)
{
    string sResultsSummaries = "[{";
    sResultsSummaries += "'Email' : '" + questionArray.getAnswer("Email") + "', ";
    sResultsSummaries += "'Comments' : '" + questionArray.getAnswer("Comments") + "', ";
    sResultsSummaries += "'Sentiment' : '" + questionArray.getAnswer("Sentiment") + "', ";
    sResultsSummaries += "'Rating' : " + questionArray.getAnswer("Rating") + "";
    sResultsSummaries += "}]";
    return sResultsSummaries;
}