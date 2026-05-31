using System.Net;
using System.Net.Http;

var mock = new Mockly.HttpMock();
mock.ForGet().WithPath("/api/test").WithAnyQuery().RespondsWithStatus(HttpStatusCode.OK);
var client = mock.GetClient();
await client.GetAsync("https://localhost/api/test?name=Alice");
var req = mock.Requests.First();
Console.WriteLine($"Query: '{req.Query}'");
Console.WriteLine($"Uri.Query: '{req.Uri?.Query}'");
