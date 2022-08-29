module Reservation.Domain.ModelTests

open Reservation.Domain.Model
open System
open Expecto
open Reservation.Tests.Fixtures
open Reservation.Tests.Fixtures.Builders.TableBuilder

[<Tests>]
let tests =

  testList "Domain" [  

    testList "Table" [

      let reservation = { ReservationRef= ReservationRef("x456t"); Persons=3; CustomerId= CustomerId(Guid.NewGuid()); TimeSlot = TimeSlot("21:00") }

      let schedule = (Map.add (TimeSlot("20:00")) None Map.empty) |> Map.add (TimeSlot("21:00")) (Some reservation)

      let table = tableBuilder |> dailySchedule schedule |> buildTable

      test "Should reserve a table with an available slot" {
        let customerId = Guid.NewGuid()
        let req = ReservationRequest(3, customerId, "x456t", "20:00")
        
        let result = Table.reserve req table

        let expectedReservation = { ReservationRef= ReservationRef("x456t"); Persons=3; CustomerId= CustomerId(customerId); TimeSlot = TimeSlot("20:00") }
        let expectedTable = { table with DailySchedule = Map.add (TimeSlot("20:00")) (Some expectedReservation) schedule } 
        Assert.IsOk result (ReservationRef("x456t"), expectedTable)
      }

      test "Should fail reserving a table when there the slot is already reserved" {
        let req = ReservationRequest(3, Guid.NewGuid(), "x456t", "21:00")
        
        let result = Table.reserve req table

        Assert.IsError result TableAlreadyReserved
      }

      test "Should fail reserving a table when there the slot is not available" {
        let req = ReservationRequest(3, Guid.NewGuid(), "x456t", "22:00")
        
        let result = Table.reserve req table

        Assert.IsError result NotAvailableTimeSlot
      }

      test "Should fail reserving a table when there the slot is not a valid one" {
        let req = ReservationRequest(3, Guid.NewGuid(), "x456t", "2:00")
        
        let result = Table.reserve req table

        Assert.IsError result InvalidTimeSlot
      }

      test "Should fail reserving a table when there the persons are not fitting with the capacity" {
        let req = ReservationRequest(2, Guid.NewGuid(), "x456t", "20:00")
        let table = tableBuilder |> capacity 6 |> dailySchedule schedule |> buildTable
        
        let result = Table.reserve req table

        Assert.IsError result TableCapacityDoesNotFit
      }
    ]

    testList "TimeSlot" [ 

      test "Should create a valid time slot" {
        Assert.IsOk (TimeSlot.create "22:00") (TimeSlot("22:00"))
      }   

      test "Should fail creating a time slot when the format is invalid" {
        Assert.IsError (TimeSlot.create "25:00") InvalidTimeSlot
      }   
    ]  
  ]
  