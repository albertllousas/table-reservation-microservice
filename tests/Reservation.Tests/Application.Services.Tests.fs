module Reservation.Application.ServicesTests

open System
open Reservation.Application
open Reservation.Domain.Model
open Expecto
open Foq
open Xunit

let private guid = Guid.NewGuid()

let private tableId = TableId(Guid.NewGuid())
      
let private date = DateOnly.FromDateTime(DateTime.UtcNow)

let private table: Table = { TableId = tableId; RestaurantId = Guid.NewGuid(); Capacity = 4; Date = date }

let private reservationRequest: InputPorts.ReserveTableRequest = { TableId = tableId.Value; Date = date; Persons = 3; Name = "John Doe"; TimeSlot = "20:00" }

let private idGenerator = {
    new OutputPorts.IdGenerator with 
      member _.Hash() = "hash"
      member _.Guid() = guid
  }

let reserveSuccesfully : Table.Reserve = fun _ _ -> Ok (ReservationRef("hash"), table)

[<Tests>]
let tests =

  testList "Reserve table application service" [

    test "Should orchestrate the reservation of a table" {
      let tableRepositoryMock= Mock<OutputPorts.TableRepository>().Setup(fun mock -> <@ mock.FindBy tableId @>).Returns(Ok table).Create()
      let reserveTable: InputPorts.ReserveTable = Services.reserveTable tableRepositoryMock idGenerator reserveSuccesfully

      let result = reserveTable reservationRequest

      let expectedResult: InputPorts.ReserveTableResponse = { TableId = tableId.Value; ReservationRef = "hash" }
      Assert.Equal(result, Ok expectedResult)
      Mock.Verify(<@ tableRepositoryMock.Save table @>)
    }

    test "Should fail orchestrating the reservation when table is not found" {
      let tableRepositoryMock= Mock<OutputPorts.TableRepository>().Setup(fun mock -> <@ mock.FindBy tableId @>).Returns(Error TableNotFound).Create()
      let reserveTable: InputPorts.ReserveTable = Services.reserveTable tableRepositoryMock idGenerator reserveSuccesfully

      let result = reserveTable reservationRequest

      Assert.Equal(result, Error TableNotFound)
      Mock.Verify(<@ tableRepositoryMock.Save table @>, never)
    }

    test "Should fail orchestrating the reservation when reserve itself fails" {
      let tableRepositoryMock= Mock<OutputPorts.TableRepository>().Setup(fun mock -> <@ mock.FindBy tableId @>).Returns(Ok table).Create()
      let reserveFails : Table.Reserve = fun _ _ -> Error TableAlreadyReserved
      let reserveTable: InputPorts.ReserveTable = Services.reserveTable tableRepositoryMock idGenerator reserveFails

      let result = reserveTable reservationRequest

      Assert.Equal(result, Error TableAlreadyReserved)
      Mock.Verify(<@ tableRepositoryMock.Save table @>, never)
    }
  ]
