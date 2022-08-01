module TableReservation.Infra.InAdapters.HttpRoutesTests

open TableReservation.Infra.InAdapters.HttpRoutes
open Microsoft.AspNetCore.Http
open Xunit
open NSubstitute
open System
open System.IO
open Giraffe
open System.Text
open Newtonsoft.Json
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

let next : HttpFunc = Some >> Task.FromResult

let getBody (ctx : HttpContext) =
    ctx.Response.Body.Position <- 0L
    use reader = new StreamReader(ctx.Response.Body, System.Text.Encoding.UTF8)
    reader.ReadToEnd()

let assertJson expected actual = 
    let expectedJson = JToken.Parse(expected)
    let actualJson = JToken.Parse(actual)
    actualJson.Should().BeEquivalentTo(expectedJson, "", "")

[<Fact>]
let ``route: POST "/table-reservations"`` () =
    let ctx = Substitute.For<HttpContext>()
    let id = Guid.NewGuid()
    let whenn = DateTime.Now
    let reservationRequest: TableReservationHttpDto = { RestaurantId = id; When = whenn } 
    ctx.Request.Method.ReturnsForAnyArgs "POST" |> ignore
    ctx.RequestServices.GetService(typeof<INegotiationConfig>).Returns(DefaultNegotiationConfig()) |> ignore
    ctx.RequestServices .GetService(typeof<Json.ISerializer>).Returns(NewtonsoftJson.Serializer(NewtonsoftJson.Serializer.DefaultSettings))
    ctx.Request.Path.ReturnsForAnyArgs (PathString("/table-reservations")) |> ignore
    ctx.Request.Headers.ReturnsForAnyArgs(new HeaderDictionary()) |> ignore
    ctx.Request.Body <- new MemoryStream( 
        Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(reservationRequest)) 
        ) 
    ctx.Response.Body <- new MemoryStream() 

    task {
        let! result = reservationRoutes next ctx
        Assert.True(result.IsSome)
        Assert.Equal(201, result.Value.Response.StatusCode)
        // Assert.Equal($"""{{"restaurantId":"{id}", "when":"{whenn}"}}""", getBody result.Value)
        assertJson $"""{{"restaurantId":"{id}", "when":"{whenn}"}}""" (getBody result.Value)

        // create from string
        // https://github.com/giraffe-fsharp/Giraffe/blob/master/DOCUMENTATION.md#testing
        // https://github.com/giraffe-fsharp/Giraffe/blob/dfcf55b4ee536a7bb8296f525cecf51c9ba8b004/tests/Giraffe.Tests/Helpers.fs#L185
        // https://github.com/giraffe-fsharp/samples/blob/master/demo-apps/SampleApp/SampleApp.Tests/Tests.fs
        // https://github.com/giraffe-fsharp/samples/blob/master/demo-apps/SampleApp/SampleApp.Tests/Tests.fs
    }
    // task {
    //     let! result = reservationRoutes next ctx

    //     match result with
    //     | None     -> Assert.True(false, "Result was expected to be OK")
    //     | Some ctx -> Assert.Equal(201, ctx.Response.StatusCode)
    // }

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe reservationRoutes

let configureServices (services : IServiceCollection) =
    services
        .AddResponseCaching()
        .AddGiraffe() |> ignore

let createHost() =
    WebHostBuilder()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(Action<IServiceCollection> configureServices)

let isStatus (code : HttpStatusCode) (response : HttpResponseMessage) =
    Assert.Equal(code, response.StatusCode)
    response

let post (client : HttpClient) (path : string) (json : string) =
    let content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    client.PostAsync(path, content)

let readText (response : HttpResponseMessage) =
    response.Content.ReadAsStringAsync()

let isJson (expected : string) (response : HttpResponseMessage) =
   task {
        let! jsonAsText = response.Content.ReadAsStringAsync()
        let expectedJson = JToken.Parse(expected)
        let actualJson = JToken.Parse(jsonAsText)
        actualJson.Should().BeEquivalentTo(expectedJson, "", "")
        response
   }

[<Fact>]
let ``Test route: POST "/table-reservations"`` () =
    task {
        let id = Guid.NewGuid()
        let whenn = DateTime.Now
        let json = $"""{{"restaurantId":"{id}", "when":"2022-07-29T08:17:43.000013+02:00"}}"""
        // let json = { RestaurantId = id; When = whenn } 
        use server = new TestServer(createHost())
        use client = server.CreateClient()
        let! response = post client "/table-reservations" json
        response 
            |> isStatus HttpStatusCode.Created
            |> isJson json
    }
