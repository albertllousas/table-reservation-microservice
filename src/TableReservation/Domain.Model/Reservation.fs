module TableReservation.Domain.Model.Reservation

open System

// daily reservations
// DailyReservationLog
// ReservationBookPage

type Reservation = {
    Ref: String
    RestaurantId : Guid
    When : DateTime 
    Name: string
    Persons: int
}

// module Reservation =
    // let create = ""
