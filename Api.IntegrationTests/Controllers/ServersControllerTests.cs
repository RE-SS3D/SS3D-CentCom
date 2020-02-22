using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Linq;
using Xunit;
using CentCom.Models;
using CentCom.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace CentCom.IntegrationTests.Controllers
{
    public class ServersControllerTests : IClassFixture<CentComFactory<Startup>>
    {
        private static readonly Uri BASE_URI = new Uri("/api/servers/", UriKind.Relative);

        public ServersControllerTests(CentComFactory<Startup> factory)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            client = factory
                .WithWebHostBuilder(builder => // Reset the database after every request.
                    builder.ConfigureServices(services =>
                    {
                        // Build the service provider.
                        var sp = services.BuildServiceProvider();

                        using var scope = sp.CreateScope();
                        var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
                        Utilities.InitializeDbForTests(dataContext);
                    })
                )
                .CreateDefaultClient();
        }

        [Fact]
        public async void Get_ExistingServers_ReturnsAllCurrentServers()
        {
            var response = await client.GetAsync(BASE_URI).ConfigureAwait(false);
            
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsAsync<List<Server>>().ConfigureAwait(false);
            
            Assert.NotNull(content);
            Assert.Equal(3, content.Count);
            Assert.DoesNotContain(content, server => server.Name == "Server C"); // Server C was last updated over 5 mins ago.
        }

        [Fact]
        public async void Post_NewServer_AddsSuccessfully()
        {
            var serverObject = new ServerDto { Name = "My Server" };

            // Add the server
            var response = await client.PostAsJsonAsync(BASE_URI, serverObject).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            Assert.NotNull(response.Headers.Location);
            var location = response.Headers.Location;

            // Check that it is added to the server list
            var getResponse = await client.GetAsync(BASE_URI).ConfigureAwait(false);
            var content = await getResponse.Content.ReadAsAsync<List<Server>>().ConfigureAwait(false);

            Assert.Contains(content, server => server.Name == serverObject.Name);
        }

        [Fact]
        public async void Get_CreatedServer_IsSuccessful()
        {
            var serverObject = new ServerDto { Name = "My Server" };

            var postResponse = await client.PostAsJsonAsync(BASE_URI, serverObject).ConfigureAwait(false);
            var location = postResponse.Headers.Location;

            // Try get it
            var response = await client.GetAsync(location).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var server = await response.Content.ReadAsAsync<Server>().ConfigureAwait(false);
            Assert.Equal(serverObject.Name, server.Name);
        }

        [Fact]
        public async void Put_NewServer_IsSuccessful()
        {
            var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
            var serverRequest = new ServerDto { Name = "New Server" };

            var uri = new Uri(new Uri(client.BaseAddress, BASE_URI), endpoint.ToString());
            var response = await client.PutAsJsonAsync(uri, serverRequest).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            var location = response.Headers.Location;

            var getResponse = await client.GetAsync(location).ConfigureAwait(false);
            var serverResponse = await getResponse.Content.ReadAsAsync<Server>().ConfigureAwait(false);

            Assert.Equal(endpoint, serverResponse.Address);
            Assert.Equal(serverRequest.Name, serverResponse.Name);
        }

        [Fact]
        public async void Put_ExistingServerWithDifferentIp_Fails()
        {
            var endpoint = Utilities.SEED_SERVERS[0].Address;
            var serverRequest = new ServerDto { Name = Utilities.SEED_SERVERS[0].Name };

            var uri = new Uri(new Uri(client.BaseAddress, BASE_URI), endpoint.ToString());
            var response = await client.PutAsJsonAsync(uri, serverRequest).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private readonly HttpClient client;
        private readonly CentComFactory<Startup> factory;
    }
}
