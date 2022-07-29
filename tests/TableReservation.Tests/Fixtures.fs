module TableReservation.Tests.Fixtures

open TableReservation.Infra.InAdapters
open Xunit
open System
open System.IO
open Giraffe
open System.Threading.Tasks
open FluentAssertions
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

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe HttpRoutes.reservationRoutes

let configureServices (services : IServiceCollection) =
    services
        .AddResponseCaching()
        .AddGiraffe() |> ignore

let createHost() =
    WebHostBuilder()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(Action<IServiceCollection> configureServices)
