module Reservation.Infra.InputAdapters

open Giraffe
open Reservation.Domain.Model
open Reservation.Domain.Model.InputPorts
open System
open System.Globalization
open Microsoft.AspNetCore.Http

module Http =

    [<CLIMutable>]
    type ReserveTableHttpRequestDto = { Date : string; Name: string; Persons: int; TimeSlot: string  }

    [<CLIMutable>]
    type ReserveTableHttpResponseDto = { Ref: String; TableId: Guid }

    [<CLIMutable>]
    type HttpErrorDto = { Details: String }

    let asErrorResponse (error: DomainError) = 
        match error with
            | TableNotFound -> RequestErrors.NOT_FOUND { Details = "Table not found" }
            | TableAlreadyReserved -> RequestErrors.CONFLICT { Details = "Table already reserved" }
            | NotAvailableTimeSlot -> RequestErrors.NOT_FOUND { Details = "Time slot does not exists" }
            | InvalidTimeSlot-> RequestErrors.BAD_REQUEST { Details = "Invalid time slot" }
            | TableCapacityDoesNotFit -> RequestErrors.BAD_REQUEST { Details = "Table capacity does not fit with reservation persons" }

    let reserveTableHandler (reserveTable : ReserveTable) (tableId:Guid) : HttpHandler = 
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let! dto = ctx.BindModelAsync<ReserveTableHttpRequestDto>()
                let date = DateOnly.ParseExact(dto.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None)
                let result = reserveTable { TableId = tableId; Date = date; Name = dto.Name; Persons = dto.Persons; TimeSlot =  dto.TimeSlot}
                return! (match result with
                        | Ok r -> { Ref = r.ReservationRef; TableId = r.TableId } |> Successful.CREATED 
                        | Error e -> asErrorResponse e) next ctx
            }

    let reservationRoutes reserveTable = 
        choose [
            // GET >=> routef "/tables/available"
            POST >=> routef "/tables/%O/reservations" (reserveTableHandler reserveTable)
        ]
