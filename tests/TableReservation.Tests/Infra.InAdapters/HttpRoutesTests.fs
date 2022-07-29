module TableReservation.Infra.InAdapters.HttpRoutesTests

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
open TableReservation.Tests.Fixtures
open TableReservation.Tests

[<Fact>]
let ``Test route: POST "/table-reservations"`` () =
    task {
        let id = Guid.NewGuid()
        let when' = DateTime.Now
        let json = $"""{{"restaurantId":"{id}", "when":"2022-07-29T08:17:43.000013+02:00"}}"""
        // let json = { RestaurantId = id; When = whenn } 
        use server = Fixtures.createTestServer HttpRoutes.reservationRoutes
        use client = server.CreateClient()

        let! response = post client "/table-reservations" json

        let! content = response |> isStatus HttpStatusCode.Created |> readText
        content |> isJson json |> ignore
    }
