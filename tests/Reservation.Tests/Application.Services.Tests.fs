module Reservation.Application.Services.Tests

open Reservation.Application
open Reservation.Domain.Model

[<Fact>]
let ``Should make a restaurant reservation`` () =
    let restaurantId = RestaurantId(Guid.NewGuid())
    let reservation: Reservation = { Ref="x342"; RestaurantId=; When= DateTime.Now; Persons= 3; Name="John Doe" }
    let reservationsList: ReservationsList = type ReservationsList = { RestaurantId : restaurantId; Date : DateOnly.FromDateTime(reservation.When) }
    let accomodateStub: ReservationsList.Accomodate = fun _ -> Ok (reservation, ReservationsList) 
    let reservationsBookMock = Mock<ReservationBook>()
                .Setup(fun mock -> <@ mock.FindBy restaurantId reservation.When @>)
                .Returns(Ok reservation)
                .Create()
    let makeReservation = Services.makeReservation reservationsBookMock reservationsBookMock  
