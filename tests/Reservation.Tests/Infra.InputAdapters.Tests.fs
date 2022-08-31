module Reservation.Infra.InputAdaptersTests

open System
open FSharp.Core
open System.Net
open Reservation.Tests.Fixtures.Http
open Reservation.Infra.InputAdapters
open Reservation.Domain.Model.InputPorts
open Reservation.Domain.Model
open Expecto

let private tableId = Guid.NewGuid() 

let private customerId = Guid.NewGuid() 

let private date = DateTime.Now.ToString("yyyy-MM-dd")

let private json = $"""{{ "date":"{date}", "persons": 4, "customerId": "{customerId}", "timeSlot": "20:00" }}"""

[<Tests>]
let tests =

  testList "Http" [

    testList "Tests for route: POST '/restaurant/{id}/reservations'" [

      testTask "Should success posting a reservation for a table" {
        
        let reserveTable : ReserveTable = fun _ -> Ok { ReservationRef ="x342"; TableId = tableId }
        use server = createTestServer (Http.reservationRoutes reserveTable)
        use client = server.CreateClient()

        let! response = post client $"/tables/{tableId}/reservations" json

        let! content = response |> isStatus HttpStatusCode.Created |> readText
        content 
            |> isJson $"""{{"ref":"x342", "tableId":"{tableId}"}}""" 
            |> ignore
      }

      let expectations = [ 
        (TableNotFound, HttpStatusCode.NotFound,  """{ "details": "Table not found" }""")
        (NotAvailableTimeSlot, HttpStatusCode.NotFound,  """{ "details": "Time slot does not exists" }""")
        (TableAlreadyReserved, HttpStatusCode.Conflict,  """{ "details": "Table already reserved" }""")
        (InvalidTimeSlot, HttpStatusCode.BadRequest,  """{ "details": "Invalid time slot" }""")
        (TableCapacityDoesNotFit, HttpStatusCode.BadRequest,  """{ "details": "Table capacity does not fit with reservation persons" }""")
        ]
      for (error, code, payload) in expectations do 
        testTask $"Should fail posting a reservation for a table when reservation fails with {error}" {
          let reserveTable : ReserveTable = fun _ -> Error error
          use server = createTestServer (Http.reservationRoutes reserveTable)
          use client = server.CreateClient()

          let! (response: Http.HttpResponseMessage) = post client $"/tables/{Guid.NewGuid()}/reservations" json

          let! content = response |> isStatus code |> readText
          content 
            |> isJson payload 
            |> ignore
        }     
    ]
  ]
