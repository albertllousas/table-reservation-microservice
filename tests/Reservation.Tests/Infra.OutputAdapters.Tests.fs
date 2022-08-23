module Reservation.Infra.OutputAdaptersTests

open FSharp.Core
open System
open Expecto
open Microsoft.Extensions.Logging
open Ductus.FluentDocker.Builders;
open Evolve
open Npgsql
open Reservation.Domain.Model
open Npgsql.FSharp
open FSharp.Json
open Reservation.Infra.OutputAdapters.DB
open Reservation.Domain.Model.OutputPorts
open Reservation.Tests.Fixtures

let log = LoggerFactory.Create(fun builder -> builder.AddConsole()|> ignore).CreateLogger("Test")

let connectionString = "Host=localhost;Database=restaurant;Username=restaurant;Password=restaurant"

let migrateDB () = 
  try
    let conn = new NpgsqlConnection(connectionString)
    (new Evolve(conn, (fun msg -> log.LogInformation msg), Locations = ["db/migrations"], IsEraseDisabled = true)).Migrate()
  with ex -> log.LogError ex.Message; raise ex

[<Tests>]
let tests =

  test "this is a task test" {
    use _ = 
      Builder()
        .UseContainer()
        .UseImage("convox/postgres")
        .ExposePort(5432, 5432)
        .WithEnvironment("POSTGRES_PASSWORD=restaurant", "POSTGRES_USER=restaurant", "POSTGRES_DATABASE=restaurant")
        .WaitForProcess("postgres", 30000)
        .Build()
        .Start()
    migrateDB()

    let reservation = { ReservationRef= ReservationRef("x456t"); Persons=3; Name="Jane Doe"; TimeSlot = TimeSlot("21:00") }

    let schedule = (Map.add (TimeSlot("21:00")) (Some reservation) Map.empty)

    let table = {
      TableId = Guid.NewGuid() |> TableId 
      RestaurantId = Guid.NewGuid() |> RestaurantId
      Capacity = 4
      Date = DateOnly.FromDateTime DateTime.Now
      DailySchedule = schedule 
    }

    connectionString
      |> Sql.connect
      |> Sql.query "INSERT INTO restaurant_table VALUES (@table_id, @restaurant_id, @capacity, @table_date, @daily_schedule)"
      |> Sql.parameters [ 
          "table_id", Sql.uuid table.TableId.Value;
          "restaurant_id", Sql.uuid table.RestaurantId.Value;
          "capacity", Sql.int table.Capacity;
          "table_date", Sql.timestamp (table.Date.ToDateTime(TimeOnly.Parse("00:00 AM")));
          "daily_schedule", Sql.jsonb (Json.serialize (table.DailySchedule |> Map.toList |> List.map (fun (k,v) -> (k.Value, v)) |> Map.ofList))
        ]
      |> Sql.executeNonQuery 
      |> ignore

    let repo: TableRepository = new PostgresqlTableRepository(connectionString)

    let result = repo.FindBy table.TableId

    Assert.IsOk result table
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
// let integrationTests =
//     tableRepositoryTests (
//       fun test ->
//         let conf = new PostgreSqlTestcontainerConfiguration(Database= "restaurant_reservation", Username ="postgres",Password="postgres")
//         let db = TestcontainersBuilder<PostgreSqlTestcontainer>().WithDatabase(conf).Build()
//         db.StartAsync()
//         test db
//         db.DisposeAsync().AsTask().Wait()
//     ) |> testList "postgresql table repository tests"
