module Reservation.Domain.Model

open FSharpx.Result
open System
open System.Text.RegularExpressions

type DomainError = TableNotFound | NotAvailableTimeSlot | TableAlreadyReserved | InvalidTimeSlot | TableCapacityDoesNotFit

type ReservationRequest = ReservationRequest of persons : int * name: string * ref: string* timeSlot: string

type RestaurantId = RestaurantId of Guid
  with member this.Value = this |> fun (RestaurantId v) -> v

type TableId = TableId of Guid
  with member this.Value = this |> fun (TableId v) -> v

type ReservationRef = ReservationRef of String
  with member this.Value = this |> fun (ReservationRef v) -> v

type TimeSlot = TimeSlot of String

module TimeSlot =

  let create (slot : string): Result<TimeSlot, DomainError> = 
    if Regex.IsMatch( slot, "^([01][0-9]|2[0-3]):([0-5][0-9])$" ) then Ok (TimeSlot(slot))
    else Error InvalidTimeSlot

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

  type Reserve = ReservationRequest -> Table ->  Result<ReservationRef * Table, DomainError>

  let private checkAvailability reservation table : Result<unit, DomainError> = 
    if(not (Map.containsKey reservation.TimeSlot table.DailySchedule)) then Error NotAvailableTimeSlot
    else if(Option.isSome (Map.find reservation.TimeSlot table.DailySchedule)) then Error TableAlreadyReserved
    else Ok ()

  let checkCapacity persons table = 
    if (table.Capacity >= persons && table.Capacity < persons + 2 ) then Ok () 
    else Error TableCapacityDoesNotFit
  
  let reserve : Reserve = fun req table -> 
     result {
      let (ReservationRequest (persons, name, ref, timeSlot)) = req
      let! validTimeSlot = TimeSlot.create timeSlot 
      let reservation: Reservation = { ReservationRef = ReservationRef(ref); Persons = persons; TimeSlot = validTimeSlot; Name = name }
      do! checkAvailability reservation table
      do! checkCapacity persons table
      let newTable =  { table with DailySchedule = Map.add validTimeSlot (Some reservation) table.DailySchedule }
      return (reservation.ReservationRef, newTable)
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
