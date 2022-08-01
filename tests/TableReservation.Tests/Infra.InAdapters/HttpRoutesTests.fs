module TableReservation.Infra.InAdapters.HttpRoutesTests

open TableReservation.Infra.InAdapters
open Xunit
open System
open System.IO
open Giraffe
open System.Threading.Tasks
open NSubstitute
open FluentAssertions
open FluentAssertions.Json
open Newtonsoft.Json.Linq
open FSharp.Core
open System.Net
open System.Net.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open TableReservation.Tests.Fixtures
open TableReservation.Tests
open TableReservation.Domain.Model
open TableReservation.Domain.Model.Types
open TableReservation.Domain.Model.Types.InputPorts
open TableReservation.Domain.Model.Reservation

[<Fact>]
let ``Test route: POST "/restaurant/{id}/reservations"`` () =
    task {
        let restaurantId = Guid.NewGuid()
        let on = DateTime.Now
        let json = $"""{{
            "restaurantId":"{restaurantId}", 
            "when":"{on.ToIsoString()}",
            "numberOfDiners": 4,
            "underTheName": "John Doe"
            }}"""
        let reservation = { Ref="x342"; RestaurantId=restaurantId; When= on; Persons= 3; Name="John Doe" }
        let makeReservation : MakeReservation = fun _ -> Ok reservation
        use server = Fixtures.createTestServer (HttpRoutes.reservationRoutes makeReservation)
        use client = server.CreateClient()

        let! response = post client $"/restaurant/{restaurantId}/reservations" json

        let! content = response |> isStatus HttpStatusCode.Created |> readText
        content 
            |> isJson $"""{{"ref":"x342", "restaurantId":"{restaurantId}", "when":"{on.ToIsoString()}", "persons": 3, "name": "John Doe"}}""" 
            |> ignore
    }
