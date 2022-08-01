module TableReservation.Domain.Model.Types

open System
open FSharpx.Result
open TableReservation.Domain.Model.Reservation

module DomainErrors =

    type DomainError = 
        | RestaurantNotFound

module InputPorts =

    open DomainErrors

    type MakeReservationRequest = { RestaurantId : Guid; When : DateTime; Persons: int; Name: string }

    type MakeReservation = (MakeReservationRequest) -> Result<Reservation, DomainError>
