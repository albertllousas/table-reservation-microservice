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
open Reservation.Domain.Model

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
        let reservation: Reservation = { Ref="x342"; RestaurantId=restaurantId; When= on; Persons= 3; Name="John Doe" }
        let makeReservation : MakeReservation = fun _ -> Ok reservation
        use server = Fixtures.createTestServer (Http.reservationRoutes makeReservation)
        use client = server.CreateClient()

        let! response = post client $"/restaurant/{restaurantId}/reservations" json

        let! content = response |> isStatus HttpStatusCode.Created |> readText
        content 
            |> isJson $"""{{"ref":"x342", "restaurantId":"{restaurantId}", "when":"{on.ToIsoString()}", "persons": 3, "name": "John Doe"}}""" 
            |> ignore
    }
