module Reservation.Infra.OutputAdaptersTests

open System
open FSharp.Core
open System.Net
open Reservation.Tests.Fixtures.Http
open Reservation.Infra.InputAdapters
open Reservation.Domain.Model.InputPorts
open Reservation.Domain.Model
open Expecto
open Xunit
open System.Runtime.InteropServices
open Microsoft.Extensions.Logging
open Ductus.FluentDocker.Model.Containers;
open Ductus.FluentDocker.Builders;
open Ductus.FluentDocker.Extensions;
open Ductus.FluentDocker.Services;
open Evolve
open Npgsql
open Hopac
open Logary
open Logary.Message
open Logary.Configuration
open Logary.Targets

// let logary =
//     Config.create "Logary.ConsoleApp" "laptop"
//     |> Config.target (LiterateConsole.create LiterateConsole.empty "console")
//     |> Config.ilogger (ILogger.Console Debug)
//     |> Config.build
//     |> run

// let logger: ILogger = logary.getLogger "Logary.HelloWorld"

let log = LoggerFactory.Create(fun builder -> builder.AddConsole()|> ignore).CreateLogger("Test")
    
  // logger.info (eventX "Hello world")


let connectionString = "Host=localhost;Database=restaurant;Username=restaurant;Password=restaurant"


let migrateDB () = 
  try
    let conn = new NpgsqlConnection(connectionString)
    conn.Open()
    (new Evolve(conn, (fun msg -> log.LogInformation msg), Locations = ["db/migrations"], IsEraseDisabled = true)).Migrate()
  with ex -> log.LogError ex.Message; raise ex

[<Tests>]
let tests =
  test "this is a task test" {
    use container = 
      Builder()
        .UseContainer()
        .UseImage("convox/postgres")
        .ExposePort(5432, 5432)
        .WithEnvironment("POSTGRES_PASSWORD=restaurant", "POSTGRES_USER=restaurant", "POSTGRES_DATABASE=restaurant")
        .WaitForProcess("postgres", 30000)
        .Build()
        .Start()
    migrateDB()
    let config = container.GetConfiguration(true)
    Assert.Equal(ServiceRunningState.Running, config.State.ToServiceState())
    // use loggerFactory = LoggerFactory.Create(
    //   fun builder -> 
    //     builder.AddConsole()
    //     ()
    //   )
    // TestcontainersSettings.Logger <- loggerFactory.CreateLogger()
    // let conf = new PostgreSqlTestcontainerConfiguration(Database= "restaurant_reservation", Username ="postgres",Password="postgres")
    // let db = TestcontainersBuilder<PostgreSqlTestcontainer>().WithDatabase(conf).Build()
    // do! db.StartAsync()
      // test db
      // db.DisposeAsync().AsTask().Wait()
  }

// let tableRepositoryTests setup =

// // https://github.com/haf/expecto#setup-and-teardown
// // https://github.com/testcontainers/testcontainers-dotnet#examples

//   [

//     testList "PostgresqlTableRepository" [

//       test "Should find a table" {
//         setup(
//            fun _ ->
//             Assert.True(true)
//         )
//       }    
//     ]
//   ]

// [<Tests>]
// let postgresqlTests =
//     tableRepositoryTests (
//       fun test ->
//         let conf = new PostgreSqlTestcontainerConfiguration(Database= "restaurant_reservation", Username ="postgres",Password="postgres")
//         let db = TestcontainersBuilder<PostgreSqlTestcontainer>().WithDatabase(conf).Build()
//         db.StartAsync()
//         test db
//         db.DisposeAsync().AsTask().Wait()
//     ) |> testList "postgresql table repository tests"
