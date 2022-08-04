module Reservation.Domain.ModelTests

open Reservation.Domain.Model
open Xunit
open Expecto
open Reservation.Tests.Fixtures
open Reservation.Tests.Fixtures.Builders.TableBuilder

[<Tests>]
let tests =

  testList "Table" [  

    testList "Reserve" [

      let reservation = { ReservationRef= ReservationRef("x456t"); Persons=3; Name="Jane Doe"; TimeSlot = TimeSlot("21:00") }

      let schedule = (Map.add (TimeSlot("20:00")) None Map.empty) |> Map.add (TimeSlot("21:00")) (Some reservation)

      let table = tableBuilder |> dailySchedule schedule |> buildTable

      test "Should reserve a table with an available slot" {
        let req = ReservationRequest(3, "Jane Doe", "x456t", "20:00")
        
        let result = Table.reserve req table

        let expectedReservation = { ReservationRef= ReservationRef("x456t"); Persons=3; Name="Jane Doe"; TimeSlot = TimeSlot("20:00") }
        let expectedTable = { table with DailySchedule = Map.add (TimeSlot("20:00")) (Some expectedReservation) schedule } 
        Assert.IsOk result (ReservationRef("x456t"), expectedTable)
      }

      test "Should fail reserving a table when there the slot is already reserved" {
        let req = ReservationRequest(3, "Jane Doe", "x456t", "21:00")
        
        let result = Table.reserve req table

        Assert.IsError result TableAlreadyReserved
      }

      test "Should fail reserving a table when there the slot is not available" {
        let req = ReservationRequest(3, "Jane Doe", "x456t", "22:00")
        
        let result = Table.reserve req table

        Assert.IsError result NotAvailableTimeSlot
      }

      test "Should fail reserving a table when there the slot is not a valid one" {
        let req = ReservationRequest(3, "Jane Doe", "x456t", "2:00")
        
        let result = Table.reserve req table

        Assert.IsError result InvalidTimeSlot
      }

      test "Should fail reserving a table when there the persons are not fitting with the capacity" {
        let req = ReservationRequest(2, "Jane Doe", "x456t", "20:00")
        let table = tableBuilder |> capacity 6 |> dailySchedule schedule |> buildTable
        
        let result = Table.reserve req table

        Assert.IsError result TableCapacityDoesNotFit
      }
    ]
  ]