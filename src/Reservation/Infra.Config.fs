module Reservation.Infra.Config  

open Giraffe
open Reservation.Domain.Model
open Reservation.Domain.Model.InputPorts
open Reservation.Domain.Model.OutputPorts
open Reservation.Application.Services
open Reservation.Infra.OutputAdapters
open Reservation.Infra.InputAdapters
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open System.Net
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

module Dependencies = 

  let connectionString = "Host=localhost;Database=restaurant;Username=restaurant;Password=restaurant"

  let tableRepository: TableRepository = new DB.PostgresqlTableRepository(connectionString) 

  let idGenerator: IdGenerator = new Ids.RandomIdGenerator()

  let reserveTableService: ReserveTable = reserveTable tableRepository idGenerator Table.reserve

  let findAvailableTables: FindAvailableTables = fun _ -> []

  let reservationRoutes = Http.reservationRoutes reserveTableService findAvailableTables

module BootstrapGiraffe =

  let microservice =
    choose [ Dependencies.reservationRoutes ]

  let configureApp (app : IApplicationBuilder) =
    app.UseGiraffe microservice

  let configureServices (services : IServiceCollection) =
    services.AddGiraffe() |> ignore

  let configureLogging (builder : ILoggingBuilder) =
    let filter (l : LogLevel) = l.Equals LogLevel.Error
    builder.AddFilter(filter).AddConsole().AddDebug() |> ignore

  let createHost(webHostBuilder: IWebHostBuilder) =
    webHostBuilder
      .Configure(configureApp)
      .ConfigureServices(configureServices) 
      .ConfigureLogging(configureLogging)

  // [<EntryPoint>]
  let main _ =
    Host.CreateDefaultBuilder()
      .ConfigureWebHostDefaults( fun webHostBuilder -> createHost webHostBuilder |> ignore )
      .Build()
      .Run()
    0