module TableReservation.Domain.Model.Reservation

open System

type Reservation = {
    Id: Guid
    RestaurantId : Guid
    When : DateTime 
}

module Reservation =
    let create = ""
