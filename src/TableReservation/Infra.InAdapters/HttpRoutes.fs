module TableReservation.Infra.InAdapters.HttpRoutes

open Giraffe
open TableReservation.Domain.Model.Reservation
open TableReservation.Domain.Model.Types.DomainErrors
open TableReservation.Domain.Model.Types.InputPorts
open System
open Microsoft.AspNetCore.Http

[<CLIMutable>]
type TableReservationHttpDto = { Id:Guid; RestaurantId : Guid; When : DateTime }

let asDto (reservation : Reservation):TableReservationHttpDto =  
    { Id=reservation.Id; RestaurantId=reservation.RestaurantId; When= reservation.When }

let createReservationHandler (makeReservation : MakeReservation) : HttpHandler = 
    fun (next : HttpFunc) (ctx : HttpContext) ->
         task {
            let! dto = ctx.BindModelAsync<TableReservationHttpDto>()
            Console.WriteLine(dto.When.ToString("s"))
            let result = makeReservation({ RestaurantId = dto.RestaurantId; When = dto.When})
            return! (match result with
                    | Ok reservation -> asDto reservation |> Successful.CREATED 
                    | Error domainError -> RequestErrors.BAD_REQUEST "") next ctx
        }

let reservationRoutes (makeReservation : MakeReservation) : HttpHandler = 
    route "/table-reservations" >=> choose [
        POST  >=> createReservationHandler makeReservation
    ]
