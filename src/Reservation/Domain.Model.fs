module Reservation.Domain.Model

open FSharpx.Result
open System

type DomainError = 
  | TableNotFound
  | NotAvailableTimeSlot
  | TableAlreadyReserved

type ReservationRequest = ReservationRequest of persons : int * name: string * ref: string* timeSlot: string

type RestaurantId = RestaurantId of Guid
  with member this.Value = this |> fun (RestaurantId v) -> v

type TableId = TableId of Guid
  with member this.Value = this |> fun (TableId v) -> v

type ReservationRef = ReservationRef of String
  with member this.Value = this |> fun (ReservationRef v) -> v

type TimeSlot = TimeSlot of String

module TimeSlot =

  let create (slot : string): Result<TimeSlot, DomainError> = Ok (TimeSlot(slot)) // TODO

type Reservation = {
  ReservationRef: ReservationRef
  Persons: int
  TimeSlot: TimeSlot
  Name: string
}

type Table = {
  TableId: TableId
  RestaurantId : RestaurantId
  Capacity: int
  Date : DateOnly
  DailySchedule : Map<TimeSlot, Reservation option>
}

module Table =

  // let private remove (timeSlot: TimeSlot) (timeSlots: TimeSlot list) = List.filter (fun x -> x <> timeSlot ) timeSlots 

  // let private hasReservationFor timeslot table  = List.filter (fun r -> r.TimeSlot.Equals timeslot ) table.DailyReservations |> List.isEmpty |> not 

  type Reserve = ReservationRequest -> Table ->  Result<ReservationRef * Table, DomainError>

  let private checkAvailability (reservation: Reservation) (table: Table) : Result<unit, DomainError> = 
    if(not (Map.containsKey reservation.TimeSlot table.DailySchedule)) then Error NotAvailableTimeSlot
    else if(Option.isSome (Map.find reservation.TimeSlot table.DailySchedule)) then Error TableAlreadyReserved
    else Ok ()

  let reserve : Reserve = fun req table -> 
     result {
      let (ReservationRequest (persons, name, ref, timeSlot)) = req
      let! validTimeSlot = TimeSlot.create timeSlot 
      let reservation: Reservation = { ReservationRef = ReservationRef(ref); Persons = persons; TimeSlot = validTimeSlot; Name = name }
      do! checkAvailability reservation table
      let newTable =  { table with DailySchedule = Map.add validTimeSlot (Some reservation) table.DailySchedule }
      return (reservation.ReservationRef, newTable)
      // check capacity of the table
    } 

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
    abstract member RandomString: size : int -> string   
