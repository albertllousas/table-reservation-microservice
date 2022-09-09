module Reservation.Application.ServicesTests

open System
open Reservation.Tests.Fixtures.Builders.TableBuilder
open Reservation.Application
open Reservation.Domain.Model
open Expecto
open Foq
open Xunit

let private guid = Guid.NewGuid()

let reservation = { ReservationRef= ReservationRef("hash"); Persons=3; CustomerId= CustomerId(Guid.NewGuid()); TimeSlot = TimeSlot("21:00") }

let schedule = (Map.add (TimeSlot("20:00")) None Map.empty) |> Map.add (TimeSlot("21:00")) (Some reservation)

let private table = tableBuilder |> dailySchedule schedule |> buildTable

let private reservationRequest: InputPorts.ReserveTableRequest = { TableId = table.TableId.Value; Date = table.Date; Persons = 3; CustomerId = Guid.NewGuid(); TimeSlot = "20:00" }

let private idGenerator = {
    new OutputPorts.IdGenerator with 
      member _.RandomString(_: int) = "hash"
      member _.Guid() = guid
  }

let reserveSuccesfully : Table.Reserve = fun _ _ -> Ok (ReservationRef("hash"), table)

let tx : OutputPorts.WithinTransation<'T> = fun (code: unit -> 'T) -> code()

type SideEffect<'t> =
    abstract fn: 't -> unit

[<Tests>]
let tests =

  testList "Application services" [

    testList "Reserve table application service" [

      test "Should orchestrate the reservation of a table" {
        let tableRepositoryMock= Mock<OutputPorts.TableRepository>().Setup(fun mock -> <@ mock.FindBy table.TableId @>).Returns(Ok table).Create()
        let publishEvent = Mock.Of<SideEffect<DomainEvent>>()
        let reserveTable: InputPorts.ReserveTable = Services.reserveTable tableRepositoryMock publishEvent.fn tx idGenerator reserveSuccesfully
        let result = reserveTable reservationRequest

        let expectedResult: InputPorts.ReserveTableResponse = { TableId = table.TableId.Value; ReservationRef = "hash" }
        Assert.Equal(result, Ok expectedResult)
        Mock.Verify(<@ tableRepositoryMock.Save table @>)
        Mock.Verify(<@ publishEvent.fn (TableReservedEvent(table.TableId, table.RestaurantId, reservation)) @>)
      }

      test "Should fail orchestrating the reservation when table is not found" {
        let tableRepositoryMock= Mock<OutputPorts.TableRepository>().Setup(fun mock -> <@ mock.FindBy table.TableId @>).Returns(Error TableNotFound).Create()
        let reserveTable: InputPorts.ReserveTable = Services.reserveTable tableRepositoryMock (fun _ -> ()) tx idGenerator reserveSuccesfully

        let result = reserveTable reservationRequest

        Assert.Equal(result, Error TableNotFound)
        Mock.Verify(<@ tableRepositoryMock.Save table @>, never)
      }

      test "Should fail orchestrating the reservation when reserving the table fails" {
        let tableRepositoryMock= Mock<OutputPorts.TableRepository>().Setup(fun mock -> <@ mock.FindBy table.TableId @>).Returns(Ok table).Create()
        let reserveFails : Table.Reserve = fun _ _ -> Error NotAvailableTimeSlot
        let reserveTable: InputPorts.ReserveTable = Services.reserveTable tableRepositoryMock (fun _ -> ()) tx idGenerator reserveFails

        let result = reserveTable reservationRequest

        Assert.Equal(result, Error NotAvailableTimeSlot)
        Mock.Verify(<@ tableRepositoryMock.Save table @>, never)
      }
    ]
  
    testList "Reserve table application service" [

      test "Should orchestrate the finding of available tables of a restaurant for a given date" {
        let today = DateOnly(2022, 11, 10)
        let reservation = { ReservationRef= ReservationRef("x456t"); Persons=3; CustomerId= CustomerId(Guid.NewGuid()); TimeSlot = TimeSlot("21:00") }
        let schedule = (Map.add (TimeSlot("20:00")) None Map.empty) |> Map.add (TimeSlot("21:00")) (Some reservation)
        let table = tableBuilder |> dailySchedule schedule |> date today |> buildTable
        let tableRepositoryMock= Mock<OutputPorts.TableRepository>().Setup(fun mock -> <@ mock.FindAllBy table.RestaurantId today @>).Returns([table]).Create()
        let findAvailableTables: InputPorts.FindAvailableTables = Services.findAvailableTables tableRepositoryMock Table.filterAvailable

        let result = findAvailableTables { RestaurantId = table.RestaurantId.Value; Date = today }

        let expectedResult: InputPorts.FindAvailableTableResponse = { TableId = table.TableId.Value; Capacity= table.Capacity; AvailableTimeSlots = ["20:00"] }
        Assert.Equal<InputPorts.FindAvailableTableResponse list>(result, [expectedResult])
      }
    ]
  ]
