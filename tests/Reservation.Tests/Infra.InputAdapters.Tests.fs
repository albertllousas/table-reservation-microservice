module Reservation.Infra.InputAdaptersTests

open System
open FSharp.Core
open System.Net
open Reservation.Tests.Fixtures.Http
open Reservation.Infra.InputAdapters
open Reservation.Domain.Model.InputPorts
open Reservation.Domain.Model
open Expecto

let private findAvailableTables: FindAvailableTables = fun _ -> Ok []

let private tableId = Guid.NewGuid() 

let private customerId = Guid.NewGuid() 

let private date = DateTime.Now.ToString("yyyy-MM-dd")

let private json = $"""{{ "date":"{date}", "persons": 4, "customerId": "{customerId}", "timeSlot": "20:00" }}"""

[<Tests>]
let tests =

  testList "Http" [

    testList "Tests for route: POST '/tables/{id}/reservations'" [

      testTask "Should success posting a reservation for a table" {
        
        let reserveTable : ReserveTable = fun _ -> Ok { ReservationRef ="x342"; TableId = tableId }
        use server = createTestServer (Http.reservationRoutes reserveTable findAvailableTables)
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
          use server = createTestServer (Http.reservationRoutes reserveTable findAvailableTables)
          use client = server.CreateClient()

          let! (response: Http.HttpResponseMessage) = post client $"/tables/{Guid.NewGuid()}/reservations" json

          let! content = response |> isStatus code |> readText
          content 
            |> isJson payload 
            |> ignore
        }     
    ]

    testList "Tests for route: GET '/tables/available'" [

      testTask "Should success finding available tables" {
        
        let reserveTable : ReserveTable = fun _ -> Ok { ReservationRef ="x342"; TableId = tableId }
        let availableTable = { TableId= Guid.NewGuid(); AvailableTimeSlots= [{TimeSlot= "21:00"; Capacity= 4}]}
        let findAvailableTables: FindAvailableTables = fun _ -> Ok [availableTable]
        let restaurantId = Guid.NewGuid()
        let date = "2022-12-10"
        use server = createTestServer (Http.reservationRoutes reserveTable findAvailableTables)
        use client = server.CreateClient()

        let! response = get client $"/tables/available?restaurant-id={restaurantId}&date={date}"

        let! content = response |> isStatus HttpStatusCode.OK |> readText
        content 
            |> isJson $"""[{{ "tableId": "{availableTable.TableId}", "availableTimeSlots": [ {{ "timeSlot": "21:00", "capacity": 4 }} ] }}]""" 
            |> ignore
      }
    ]
  ]
