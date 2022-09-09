module Reservation.Application.Services

open FSharpx.Result
open Reservation.Domain.Model
open Reservation.Domain.Model.InputPorts
open Reservation.Domain.Model.OutputPorts

// let createTables (findRestaurant: FindRestaurant) (tableRepository: TableRepository) req =
//     result {
//         let restaurant = findRestaurant req.RestaurantId
//         let tables = Table.createTablesFor req.date restaurant   
//         tables |> tableRepository.Save
//         tables |> TableCreatedEvent.from |> publishEvent
//     }

let findAvailableTables (tableRepository: TableRepository) (filterAvailable: Table.FilterAvailable): FindAvailableTables = 
  fun (req: FindAvailableTablesRequest) -> 
    tableRepository.FindAllBy (RestaurantId(req.RestaurantId)) req.Date 
      |> filterAvailable req.Date
      |> List.map (fun table -> FindAvailableTableResponse.from table)   

let reserveTable (tableRepository: TableRepository)  
                 (publishEvent: PublishEvent)
                 (tx: WithinTransation<_>) 
                 (idGenerator: IdGenerator) 
                 (reserve: Table.Reserve) 
                 (req: ReserveTableRequest) = 
  tx (fun () -> 
    result {
      let! table = tableRepository.FindBy <| TableId(req.TableId)
      let reservationRequest = ReservationRequest(req.Persons, req.CustomerId, idGenerator.RandomString 5, req.TimeSlot)
      let! (ref, reservedTable) = reserve reservationRequest table 
      tableRepository.Save reservedTable
      publishEvent (TableReservedEvent.from reservedTable ref)
      return { TableId = table.TableId.Value; ReservationRef = ref.Value }
    }
  )
