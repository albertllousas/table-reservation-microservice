module Reservation.Domain.Model

open FSharpx.Result
open System
open System.Text.RegularExpressions

type DomainError = TableNotFound | NotAvailableTimeSlot | TableAlreadyReserved | InvalidTimeSlot | TableCapacityDoesNotFit

type ReservationRequest = ReservationRequest of persons : int * customerId: Guid * ref: string * timeSlot: string

type RestaurantId = RestaurantId of Guid
  with member this.Value = this |> fun (RestaurantId v) -> v

type TableId = TableId of Guid
  with member this.Value = this |> fun (TableId v) -> v

type CustomerId = CustomerId of Guid
  with member this.Value = this |> fun (CustomerId v) -> v

type ReservationRef = ReservationRef of String
  with member this.Value = this |> fun (ReservationRef v) -> v

type TimeSlot = TimeSlot of String
  with member this.Value = this |> fun (TimeSlot v) -> v

module TimeSlot =

  let create (slot : string): Result<TimeSlot, DomainError> = 
    if Regex.IsMatch( slot, "^([01][0-9]|2[0-3]):([0-5][0-9])$" ) then Ok (TimeSlot(slot))
    else Error InvalidTimeSlot

type Reservation = {
  ReservationRef: ReservationRef
  Persons: int
  TimeSlot: TimeSlot
  CustomerId: CustomerId
}

type Table = {
  TableId: TableId
  RestaurantId : RestaurantId
  Capacity: int
  Date : DateOnly
  DailySchedule : Map<TimeSlot, Reservation option>
  Version : int64
}

module Table =

  type Reserve = ReservationRequest -> Table ->  Result<ReservationRef * Table, DomainError>

  let private checkAvailability reservation table : Result<unit, DomainError> = 
    if(not (Map.containsKey reservation.TimeSlot table.DailySchedule)) then Error NotAvailableTimeSlot
    else if(Option.isSome (Map.find reservation.TimeSlot table.DailySchedule)) then Error TableAlreadyReserved
    else Ok ()

  let private checkCapacity persons table = 
    if (table.Capacity >= persons && table.Capacity < persons + 2 ) then Ok () 
    else Error TableCapacityDoesNotFit
  
  let reserve : Reserve = fun req table -> 
     result {
      let (ReservationRequest (persons, customerId, ref, timeSlot)) = req
      let! validTimeSlot = TimeSlot.create timeSlot 
      let reservation: Reservation = { ReservationRef = ReservationRef(ref); Persons = persons; TimeSlot = validTimeSlot; CustomerId = CustomerId(customerId) }
      do! checkAvailability reservation table
      do! checkCapacity persons table
      let newTable =  { table with DailySchedule = Map.add validTimeSlot (Some reservation) table.DailySchedule }
      return (reservation.ReservationRef, newTable)
    } 
  
  let availableSlots (t:Table) = Map.toList t.DailySchedule 
                                  |> List.filter (fun (k,v) -> Option.isNone v)
                                  |> List.map (fun (k,v) -> k) 

  type FilterAvailable = DateOnly -> Table list -> Table list

  let filterAvailable : FilterAvailable = 
    fun date tables -> List.filter (fun t -> t.Date.CompareTo(date) = 0 && ((Map.toList t.DailySchedule |> List.filter (fun (k,v) -> Option.isSome v)).Length > 0 )) tables

type DomainEvent = TableReservedEvent of table : TableId * restaurant : RestaurantId * reservation : Reservation

module TableReservedEvent =

  let from table ref = 
    let reservation = Map.toList table.DailySchedule 
                      |> List.map (fun (k,v) -> v) 
                      |> List.filter (fun v -> Option.isSome v) 
                      |> List.map (fun v -> Option.get v)
                      |> List.find (fun r -> r.ReservationRef = ref)
    TableReservedEvent(table.TableId, table.RestaurantId, reservation)

module InputPorts =

  type ReserveTableRequest = { TableId : Guid; Date : DateOnly; Persons: int; CustomerId: Guid; TimeSlot: string }

  type ReserveTableResponse = { TableId: Guid; ReservationRef: String }

  type ReserveTable = (ReserveTableRequest) -> Result<ReserveTableResponse, DomainError>

  type FindAvailableTablesRequest = { RestaurantId : Guid; Date : DateOnly; }

  type FindAvailableTableResponse = { TableId : Guid; Capacity: int; AvailableTimeSlots : String list } 

  module FindAvailableTableResponse =

    let from (t:Table) = { TableId = t.TableId.Value; Capacity = t.Capacity; AvailableTimeSlots = Table.availableSlots t |> List.map (fun ts -> ts.Value) }

  type FindAvailableTables = (FindAvailableTablesRequest) -> FindAvailableTableResponse list

module OutputPorts = 

  type TableRepository = 
    abstract member FindAllBy : RestaurantId -> DateOnly -> Table list  
    abstract member FindBy : TableId -> Result<Table, DomainError> 
    abstract member Save : Table -> unit  
    
  type IdGenerator = 
    abstract member Guid: unit -> Guid
    abstract member RandomString: size : int -> string  

  type WithinTransation<'T> = (unit -> 'T ) -> 'T

  type PublishEvent = DomainEvent -> unit

module Result =
  open FSharpPlus

  let fold fOk fErr r = Result.either fOk fErr r

  let map2 fn r1 r2 = Result.map2 fn r1 r2 
