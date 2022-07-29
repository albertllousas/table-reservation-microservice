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
let ``Test route: POST "/table-reservations"`` () =
    task {
        let id = Guid.NewGuid()
        let restaurantId = Guid.NewGuid()
        let when' = DateTime.Now
        Console.WriteLine(when'.ToIsoString())
        let json = $"""{{"restaurantId":"{restaurantId}", "when":"{when'.ToIsoString()}"}}"""
        let reservation : Reservation = { Id=id; RestaurantId=restaurantId; When= when' }
        let makeReservation : MakeReservation = fun _ -> Ok reservation
        use server = Fixtures.createTestServer (HttpRoutes.reservationRoutes makeReservation)
        use client = server.CreateClient()

        let! response = post client "/table-reservations" json

        let! content = response |> isStatus HttpStatusCode.Created |> readText
        content 
            |> isJson $"""{{"id":"{id}", "restaurantId":"{restaurantId}", "when":"{when'.ToIsoString()}"}}""" 
            |> ignore
    }
