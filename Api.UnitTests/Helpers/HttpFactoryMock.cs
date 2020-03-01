using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Api.UnitTests.Helpers
{
    public static class HttpFactoryMock
    {
        private class DelegatingHandlerStub : HttpMessageHandler
        {
            public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> HandlerFunc { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return HandlerFunc(request, cancellationToken);
            }
        }

        public static Mock<IHttpClientFactory> CreateMock(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            var mockFactory = new Mock<IHttpClientFactory>();
            var clientHandlerStub = new DelegatingHandlerStub { HandlerFunc = handler };
            var client = new HttpClient(clientHandlerStub);

            mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            return mockFactory;
        }
        public static Mock<IHttpClientFactory> CreateMock()
        {
            return CreateMock((a, b) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        }
    }
}
