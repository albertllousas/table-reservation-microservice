module Reservation.Infra.OutputAdaptersTests

open FSharp.Core
open System
open Expecto
open Reservation.Domain.Model
open Reservation.Infra.OutputAdapters.DB
open Reservation.Domain.Model.OutputPorts
open Reservation.Tests.Fixtures
open Reservation.Tests.Fixtures.Builders.TableBuilder
open Xunit

let tableRepositoryTests setup = [

  testSequencedGroup "docker" <| testList "PostgresqlTableRepository" [

    test "Should find a table" {
      setup(
           fun _ ->         
            let reservation = { ReservationRef= ReservationRef("x456t"); Persons=3; Name="Jane Doe"; TimeSlot = TimeSlot("21:00") }
            let schedule = (Map.add (TimeSlot("21:00")) (Some reservation) Map.empty)
            let table: Table = {
                TableId = Guid.NewGuid() |> TableId 
                RestaurantId = Guid.NewGuid() |> RestaurantId
                Capacity = 4
                Date = DateOnly.FromDateTime DateTime.Now
                DailySchedule = schedule 
              }
            DB.insertTable table

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

    test "Should find tables of a restaurant by date" {
      setup(
        fun _ ->   
          let id = Guid.NewGuid()
          let d = DateOnly(2022, 1, 1)  
          let t1 = tableBuilder |> restaurantId id |> date d |> buildTable
          let t2 = tableBuilder |> buildTable
          let t3 = tableBuilder |> restaurantId id |> date d |>buildTable
          let t4 = tableBuilder |> buildTable
          DB.insertTable t1
          DB.insertTable t2
          DB.insertTable t3
          DB.insertTable t4

          let repo: TableRepository = new PostgresqlTableRepository(DB.connectionString)

          let result = repo.FindAllBy (id |> RestaurantId) d

          Assert.Equal<Table list>(result , [t1; t3])
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
    
