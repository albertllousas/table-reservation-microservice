module Reservation.Application.Services

open FSharpx.Result
open System
open Reservation.Domain.Model
open Reservation.Domain.Model.InputPorts
open Reservation.Domain.Model.OutputPorts

// let makeReservation (reservationsBook : ReservationBook) (accomodate: Accomodate): MakeReservation = 
//     fun request -> 
//         result {
//             let restaurantId = RestaurantId(request.RestaurantId)
//             let! dailyReservationsList = reservationsBook.FindBy restaurantId request.When
//             let reservationRequest = ReservationRequest(request.When, request.Persons, request.Name)
//             let! (reservation, updated: ReservationsList) = accomodate reservationRequest dailyReservationsList
//             reservationsBook.Save updated 
//             // publishEvent ReservationCreatedEvent.from reservation      
//             return reservation //with status confirmed
//         }

// let createTables (findRestaurant: FindRestaurant) (tableRepository: TableRepository) req =
//     result {
//         let restaurant = findRestaurant req.RestaurantId
//         let tables = Table.createTablesFor req.date restaurant   
//         tables |> tableRepository.Save
//         tables |> TableCreatedEvent.from |> publishEvent
//     }
    

// let findAvailableTables (tableRepository: TableRepository) req = 
    // let! tables = tableRepository.FindBy req.restaurantId req.date 
    // let availableTables = Table.filterAvailableFor req.Time tables // List.Filter (fun table -> Table.available req.Time table)
    // return availableTables     

let reserveTable (tableRepository: TableRepository) (idGenerator: IdGenerator) (reserve: Table.Reserve): ReserveTable = 
    fun (req: ReserveTableRequest) -> 
        result {
            let! table = tableRepository.FindBy <| TableId(req.TableId)
            let reservationRequest = ReservationRequest(req.Persons, req.Name, idGenerator.Hash(), req.TimeSlot)
            let! (ref, reservedTable) = reserve reservationRequest table 
            tableRepository.Save reservedTable
            // publishEvent TableReservedEvent.from reservedTable ref
            return { TableId = table.TableId.Value; ReservationRef = ref.Value }
        }
