module Reservation.Infra.InAdapters.HttpRoutesTests

open System
open Giraffe
open FSharp.Core
open System.Net
open Reservation.Tests.Fixtures
open Reservation.Tests
open Reservation.Infra.InputAdapters
open Reservation.Domain.Model.InputPorts
open Reservation.Domain.Model
open Expecto

[<Tests>]
let tests =

  testList "Http" [

    testList "Tests for route: POST '/restaurant/{id}/reservations'" [

      testTask "Should success posting a reservation for a table" {
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

      let expectations = [ 
        (TableNotFound, HttpStatusCode.NotFound,  """{ "details": "Table not found" }""")
        (TableAlreadyReserved, HttpStatusCode.Conflict,  """{ "details": "Table already reserved" }""")
        ]
      for (error, code, payload) in expectations do 
        testTask $"Should fail posting a reservation for a table when reservation fails with {error}" {
          let json = $"""{{ "restaurantId":"{Guid.NewGuid()}", "when":"{DateTime.Now.ToIsoString()}", "people": 4, "name": "John Doe" }}"""
          let reserveTable : ReserveTable = fun _ -> Error error
          use server = Fixtures.createTestServer (Http.reservationRoutes reserveTable)
          use client = server.CreateClient()

          let! (response: Http.HttpResponseMessage) = post client $"/tables/{Guid.NewGuid()}/reservations" json

          let! content = response |> isStatus code |> readText
          content 
            |> isJson payload 
            |> ignore
        }     
    ]
  ]
