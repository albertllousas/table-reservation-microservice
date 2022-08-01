module Reservation.Application.services

open FSharpx.Result
open Reservation.Domain.Model
open Reservation.Domain.Model.InputPorts
open Reservation.Domain.Model.OutputPorts
open Reservation.Domain.Model.ReservationsList

let makeReservation (reservationsBook : ReservationBook) (accomodate: Accomodate): MakeReservation = 
    fun request -> 
        result {
            let restaurantId = RestaurantId(request.RestaurantId)
            let! reservationList = reservationsBook.FindBy restaurantId request.When
            let reservationRequest = ReservationRequest(request.When, request.Persons, request.Name)
            let! (reservation, updated) = accomodate reservationRequest reservationList
            reservationsBook.Save updated 
            // publishEvent ReservationCreatedEvent.from reservation      
            return reservation //with status confirmed
        }
