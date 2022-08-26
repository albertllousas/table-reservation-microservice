module Reservation.Infra.OutputAdaptersTests

open FSharp.Core
open System
open Expecto
open Npgsql
open Reservation.Domain.Model
open Npgsql.FSharp
open FSharp.Json
open Reservation.Infra.OutputAdapters.DB
open Reservation.Domain.Model.OutputPorts
open Reservation.Tests.Fixtures

let tableRepositoryTests setup = [

  testSequencedGroup "docker" <| testList "PostgresqlTableRepository" [

    test "Should find a table" {
      setup(
           fun _ ->         
            let reservation = { ReservationRef= ReservationRef("x456t"); Persons=3; Name="Jane Doe"; TimeSlot = TimeSlot("21:00") }
            let schedule = (Map.add (TimeSlot("21:00")) (Some reservation) Map.empty)
            let table = {
                TableId = Guid.NewGuid() |> TableId 
                RestaurantId = Guid.NewGuid() |> RestaurantId
                Capacity = 4
                Date = DateOnly.FromDateTime DateTime.Now
                DailySchedule = schedule 
              }
            DB.connectionString
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

            let repo: TableRepository = new PostgresqlTableRepository(DB.connectionString)

            let result = repo.FindBy table.TableId

            Assert.IsOk result table
        )
      }    

    test "Should not find a table when it does not exists" {
      setup(
        fun _ ->         
          let repo: TableRepository = new PostgresqlTableRepository(DB.connectionString)

          let result = repo.FindBy (Guid.NewGuid() |> TableId)

          Assert.IsError result TableNotFound
      )
      }    
    ]
  ]

[<Tests>]
let integrationTests =
    tableRepositoryTests (
      fun test ->
        use container = DB.startPostgresContainer()
        DB.migrate()
        test container
    ) |> testList "postgresql table repository tests"
    
