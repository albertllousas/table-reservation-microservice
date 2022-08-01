module Reservation.Tests.Fixtures

open Xunit
open System
open System.IO
open Giraffe
open FluentAssertions.Json
open Newtonsoft.Json.Linq
open System.Net
open System.Net.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection

let isStatus (code : HttpStatusCode) (response : HttpResponseMessage) =
    Assert.Equal(code, response.StatusCode)
    response

let post (client : HttpClient) (path : string) (json : string) =
    let content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    client.PostAsync(path, content)

let readText (response : HttpResponseMessage) =
    response.Content.ReadAsStringAsync()

let isJson (expected : string) (actual : string) =
    let expectedJson = JToken.Parse(expected)
    let actualJson = JToken.Parse(actual)
    actualJson.Should().BeEquivalentTo(expectedJson, "", "")

let configureApp (routes: HttpHandler) (app : IApplicationBuilder) =
    app.UseGiraffe routes

let configureServices (services : IServiceCollection) =
    // dependencies |> List.iter (fun dep -> services.AddSingleton(dep) |> ignore) 
    services
        .AddResponseCaching()
        .AddGiraffe() |> ignore

let createHost (routes: HttpHandler) =
    WebHostBuilder()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .Configure(Action<IApplicationBuilder> (configureApp routes))
        .ConfigureServices(Action<IServiceCollection> configureServices)

let createTestServer routes = new TestServer(createHost routes)
