module Reservation.Domain.Model

// open FSharpx.Result
open System

type DomainError = 
    | TableNotFound
    | TableAlreadyReserved

type ReservationRequest = ReservationRequest of persons : int * name: string * ref: string* timeSlot: string

type RestaurantId = RestaurantId of Guid
    with member this.Value = this |> fun (RestaurantId v) -> v

type TableId = TableId of Guid
    with member this.Value = this |> fun (TableId v) -> v

type ReservationRef = ReservationRef of String
    with member this.Value = this |> fun (ReservationRef v) -> v

type Reservation = {
  ReservationRef: ReservationRef
  Persons: int
  time: TimeOnly
}

type TimeSlot = TimeSlot of String

type Table = {
    TableId: TableId
    RestaurantId : Guid
    Capacity: int
    Date : DateOnly
    // DailyReservations : Reservation list
    // AvailableTimeSlots : TimeSlot list
}

module Table =

    type Reserve = ReservationRequest -> Table ->  Result<ReservationRef * Table, DomainError>

    let reserve : Reserve = failwith "Not implemented"
      // fun req table -> 
        

    //let filterAvailableFor req.Time tables // List.Filter (fun table -> Table.available req.Time table)

    //let isAvailable 

module InputPorts =

    type ReserveTableRequest = { TableId : Guid; Date : DateOnly; Persons: int; Name: string; TimeSlot: string }

    type ReserveTableResponse = { TableId: Guid; ReservationRef: String }

    type ReserveTable = (ReserveTableRequest) -> Result<ReserveTableResponse, DomainError>

module OutputPorts = 

  type TableRepository = 
    abstract member FindAllBy : RestaurantId -> DateTime -> Result<Table list, DomainError>  
    abstract member FindBy : TableId -> Result<Table, DomainError> 
    abstract member Save : Table -> unit  
    
  type IdGenerator = 
    abstract member Guid: unit -> Guid
    abstract member Hash: unit -> string   
