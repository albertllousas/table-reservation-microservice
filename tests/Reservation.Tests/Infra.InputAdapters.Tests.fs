module Reservation.Infra.InAdapters.HttpRoutesTests

open Xunit
open System
open Giraffe
open FSharp.Core
open System.Net
open Reservation.Tests.Fixtures
open Reservation.Tests
open Reservation.Infra.InputAdapters
open Reservation.Domain.Model.InputPorts

[<Fact>]
let ``Test route: POST "/restaurant/{id}/reservations"`` () =
    task {
        let restaurantId = Guid.NewGuid()
        let tableId = Guid.NewGuid() 
        let on = DateTime.Now
        let json = $"""{{ "restaurantId":"{restaurantId}", "when":"{on.ToIsoString()}", "people": 4, "name": "John Doe" }}"""
        let reserveTable : ReserveTable = fun _ -> Ok { ReservationRef ="x342"; TableId = tableId }
        use server = Fixtures.createTestServer (Http.reservationRoutes reserveTable)
        use client = server.CreateClient()

        let! response = post client $"/tables/{tableId}/reservations" json

        let! content = response |> isStatus HttpStatusCode.Created |> readText
        content 
            |> isJson $"""{{"ref":"x342", "tableId":"{tableId}"}}""" 
            |> ignore
    }
