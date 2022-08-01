module Reservation.Infra.InputAdapters

open Giraffe
open Reservation.Domain.Model
open Reservation.Domain.Model.InputPorts
open System
open Microsoft.AspNetCore.Http

module Http =

    [<CLIMutable>]
    type ReservationHttpRequestDto = { When : DateTime; Name: string; Persons: int  }

    [<CLIMutable>]
    type ReservationHttpResponseDto = { Ref: String; RestaurantId: Guid; When : DateTime; Name: string; Persons: int }

    let asResponse (r : Reservation) : ReservationHttpResponseDto =  
        { Ref = r.Ref; RestaurantId = r.RestaurantId; When = r.When; Persons = r.Persons; Name = r.Name}

    let createReservationHandler (makeReservation : MakeReservation) (restaurantId:Guid) : HttpHandler = 
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let! dto = ctx.BindModelAsync<ReservationHttpRequestDto>()
                let result = makeReservation { RestaurantId = restaurantId; When = dto.When; Name = dto.Name; Persons = dto.Persons}
                return! (match result with
                        | Ok reservation -> asResponse reservation |> Successful.CREATED 
                        | Error domainError -> RequestErrors.BAD_REQUEST "") next ctx
            }

    let reservationRoutes (makeReservation : MakeReservation) : HttpHandler = 
        choose [
            POST >=> routef "/restaurant/%O/reservations" (createReservationHandler makeReservation)
        ]
