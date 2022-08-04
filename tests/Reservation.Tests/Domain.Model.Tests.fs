module Reservation.Domain.ModelTests

open Reservation.Domain.Model
open Xunit
open Expecto
open Reservation.Tests.Fixtures.Builders.TableBuilder

[<Tests>]
let tests =

  testList "Table" [  

    testList "Reserve" [

      test "Should reserve a table without any previous reservation" {
        let table = tableBuilder |> dailySchedule (Map.add (TimeSlot("20:00")) None Map.empty) |> buildTable
        let req = ReservationRequest(3, "Jane Doe", "x456t", "20:00")
        
        let result = Table.reserve req table

        let expectedReservation = { ReservationRef= ReservationRef("x456t"); Persons=3; Name="Jane Doe"; TimeSlot = TimeSlot("20:00") }
        let expectedTable = { table with DailySchedule = (Map.add (TimeSlot("20:00")) (Some expectedReservation) Map.empty) } 
        Assert.Equal(result, Ok(ReservationRef("x456t"), expectedTable))
      }
    ]
  ]