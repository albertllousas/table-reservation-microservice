module Reservation.Domain.Model

// open FSharpx.Result
open System

type DomainError = 
    | RestaurantNotFound
    | DateNotOpenForReservationsYet

type ReservationRequest = ReservationRequest of time : TimeOnly * persons : int * name: string

type RestaurantId = RestaurantId of Guid
    with member this.Value = this |> fun (RestaurantId v) -> v

type TableId = TableId of Guid
    with member this.Value = this |> fun (TableId v) -> v

type ReservationRef = ReservationRef of String
    with member this.Value = this |> fun (ReservationRef v) -> v

type Table = {
    TableId: TableId
    RestaurantId : Guid
    Capacity: int
    date : DateOnly
}

module Table =

    let reserve (request: ReservationRequest)(table : Table) : ReservationRef * Table = raise (NotImplementedException())

    //let filterAvailableFor req.Time tables // List.Filter (fun table -> Table.available req.Time table)

    //let isAvailable 

module InputPorts =

    type ReserveTableRequest = { TableId : Guid; When : DateTime; Persons: int; Name: string }

    type ReserveTableResponse = { TableId: Guid; ReservationRef: String }

    type ReserveTable = (ReserveTableRequest) -> Result<ReserveTableResponse, DomainError>

module OutputPorts = 

    type TableRepository = 
        abstract member FindAllBy : RestaurantId -> DateTime -> Result<Table list, DomainError>  
        abstract member FindBy : TableId -> Result<Table, DomainError> 
        abstract member Save : Table -> unit  

