module Reservation.Infra.InputAdapters

open Giraffe
open Reservation.Domain.Model
open Reservation.Domain.Model.InputPorts
open System
open System.Globalization
open Microsoft.AspNetCore.Http

module Http =

  [<CLIMutable>]
  type ReserveTableHttpRequestDto = { Date : string; CustomerId: Guid; Persons: int; TimeSlot: string  }

  [<CLIMutable>]
  type ReserveTableHttpResponseDto = { Ref: String; TableId: Guid }

  [<CLIMutable>]
  type HttpErrorDto = { Details: String }

  let private dateOnlyFrom str = DateOnly.ParseExact(str, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None)

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
        let date = dateOnlyFrom dto.Date
        let result = reserveTable { TableId = tableId; Date = date; CustomerId = dto.CustomerId; Persons = dto.Persons; TimeSlot =  dto.TimeSlot}
        return! Result.fold (fun r -> Successful.CREATED  { Ref = r.ReservationRef; TableId = r.TableId } next ctx) (fun e -> asErrorResponse e next ctx) result
      }

  let findAvailableTablesHandler (findAvailableTables : FindAvailableTables): HttpHandler = 
    fun (next : HttpFunc) (ctx : HttpContext) ->
      let queryParams = Result.map2 (fun r d -> (r,d)) (ctx.GetQueryStringValue "restaurant-id") (ctx.GetQueryStringValue "date")
      match queryParams with
                | Ok (r,d) -> findAvailableTables { RestaurantId = new Guid(r); Date = dateOnlyFrom d } |> (fun res -> Successful.OK res next ctx)
                | Error _ -> RequestErrors.BAD_REQUEST "" next ctx

  let reservationRoutes reserveTable findAvailableTables = 
    let h: HttpHandler = findAvailableTablesHandler findAvailableTables
    choose [
        GET >=> route "/tables/available" >=> h
        POST >=> routef "/tables/%O/reservations" (reserveTableHandler reserveTable)
    ]
