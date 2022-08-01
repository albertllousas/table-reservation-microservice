module Reservation.Domain.Model

// open FSharpx.Result
open System

type DomainError = 
    | RestaurantNotFound
    | DateNotOpenForReservationsYet

type ReservationRequest = ReservationRequest of date : DateTime * persons : int * name: string

type Reservation = {
    Ref: String
    RestaurantId : Guid
    When : DateTime 
    Name: string
    Persons: int
}

// module Reservation =
    // let create = ""

type RestaurantId = RestaurantId of Guid

type ReservationsList = {
    RestaurantId : Guid
    Date : DateOnly
}

module ReservationsList =

    type Accomodate = ReservationRequest -> ReservationsList -> Result<ValueTuple<Reservation, ReservationsList>, DomainError> 

    let accomodate : Accomodate = raise (NotImplementedException())

module InputPorts =

    type MakeReservationRequest = { RestaurantId : Guid; When : DateTime; Persons: int; Name: string }

    type MakeReservation = (MakeReservationRequest) -> Result<Reservation, DomainError>

module OutputPorts = 

    type ReservationBook = 
        abstract member FindBy : RestaurantId -> DateTime -> Result<ReservationsList, DomainError>  
        abstract member Save : ReservationsList -> unit  

