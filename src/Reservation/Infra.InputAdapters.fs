module Reservation.Infra.InputAdapters

open Giraffe
open Reservation.Domain.Model
open Reservation.Domain.Model.InputPorts
open System
open Microsoft.AspNetCore.Http

module Http =

    [<CLIMutable>]
    type ReserveTableHttpRequestDto = { When : DateTime; Name: string; Persons: int  }

    [<CLIMutable>]
    type ReserveTableHttpResponseDto = { Ref: String; TableId: Guid;}

    let reserveTableHandler (reserveTable : ReserveTable) (tableId:Guid) : HttpHandler = 
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let! dto = ctx.BindModelAsync<ReserveTableHttpRequestDto>()
                let result = reserveTable { TableId = tableId; When = dto.When; Name = dto.Name; Persons = dto.Persons}
                return! (match result with
                        | Ok r -> { Ref = r.ReservationRef; TableId = r.TableId } |> Successful.CREATED 
                        | Error e -> RequestErrors.BAD_REQUEST "") next ctx
            }

    let reservationRoutes reserveTable = 
        choose [
            // GET >=> routef "/tables/available"
            POST >=> routef "/tables/%O/reservations" (reserveTableHandler reserveTable)
        ]
