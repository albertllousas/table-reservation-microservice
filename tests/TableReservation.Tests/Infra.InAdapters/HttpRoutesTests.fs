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

let next : HttpFunc = Some >> Task.FromResult

let getBody (ctx : HttpContext) =
    ctx.Response.Body.Position <- 0L
    use reader = new StreamReader(ctx.Response.Body, System.Text.Encoding.UTF8)
    reader.ReadToEnd()

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
        Assert.Equal($"""{{"restaurantId":"{id}", "when":"{whenn}"}}""", getBody result.Value)

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

