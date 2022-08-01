module TableReservation.Application.services

// open FSharpx
// open FSharpx.Result

// let makeReservation (reservationsBook : ReservationBook)
//                     (request: MakeReservationRequest)
//                     : MakeReservation = 
//     result {
//         let! reservationList = reservationList.findBy (RestaurantId request.restaurantId request.date) --> not open for reservations yet
//         let reservationRequested = Reservation.request request.numberOfPeople request.time request.name
//         let! (reservation, updated) = ReservationList.accomodate reservationRequested reservationList
//         reservationsBook.save updated 
//         publishEvent ReservationCreatedEvent.from reservation      
//         return reservation //with status confirmed
//     }

// let! tables = tableReservationRepository.find (RestaurantId request.restaurantId request.date) --> not open for reservations yet
// monthly? daily?
// let! updated = Table.reserve request availableTables

// DailyTableReservations
// reservations upfront

// 
// While shifts generally include two four-hour shifts worked back to back, some waitresses work breakfast and then return to serve during the dinner service. The term "split shift" describes servers returning to work after other waitresses take service shifts.