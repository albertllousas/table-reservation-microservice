module TableReservation.Infra.InAdapters.HttpRoutes

open Giraffe

let reservationRoute : HttpHandler = 
    route "/table-reservation" >=> choose [
        GET  >=> setStatusCode 200
    ]