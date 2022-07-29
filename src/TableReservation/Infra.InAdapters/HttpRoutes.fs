module TableReservation.Infra.InAdapters.HttpRoutes

open Giraffe
open System
open Microsoft.AspNetCore.Http

[<CLIMutable>]
type TableReservationHttpDto = { RestaurantId : Guid; When : DateTime }

let createReservationHandler: HttpHandler = 
    fun (next : HttpFunc) (ctx : HttpContext) ->
         task {
            let! reservation = ctx.BindModelAsync<TableReservationHttpDto>()
            return! Successful.CREATED reservation next ctx
        }

let reservationRoutes : HttpHandler = 
    route "/table-reservations" >=> choose [
        POST  >=> createReservationHandler
    ]
