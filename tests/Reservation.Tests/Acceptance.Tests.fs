module Reservation.AcceptanceTests

open System
open FSharp.Core
open System.Net
open Expecto
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Reservation.Domain.Model
open Reservation.Infra.Config
open Reservation.Infra.OutputAdapters.DB
open Reservation.Domain.Model.OutputPorts
open Reservation.Tests.Fixtures
open Reservation.Tests.Fixtures.Builders.TableBuilder
open Reservation.Tests.Fixtures.Http
open Xunit
open Giraffe
open Microsoft.AspNetCore.TestHost
open Microsoft.AspNetCore.Builder
open System.Net
open System.Net.Http
open System.IO
open System.Text.Json

let setup fn =  
  use container = DB.startPostgresContainer()
  DB.migrate()
  fn()

let postRequest path body =
    let resp = task {
        use server = new TestServer(BootstrapGiraffe.createHost(WebHostBuilder().UseContentRoot(Directory.GetCurrentDirectory())))
        use client = server.CreateClient()
        let! response = post client path body
        return response
    }
    resp.Result

let givenAnExistentTable schedule = 
  let date = DateTime.Now    
  let restaurantId = Guid.NewGuid() |> RestaurantId
  let tableId = Guid.NewGuid() |> TableId 
  let table: Table = { TableId= tableId; RestaurantId= restaurantId; Capacity= 4; Date= DateOnly.FromDateTime(date); DailySchedule= schedule; Version= 1 }
  DB.insertTable table
  table

[<Tests>]
let acceptanceTests = 

  testSequencedGroup "docker" <| testList "use cases" [

    test "Should reserve a table in a restaurant" {
      setup(
        fun client ->    
            let schedule = (Map.add (TimeSlot("21:00")) (None) Map.empty)
            let table = givenAnExistentTable schedule
            let dateStr = table.Date.ToString("yyyy-MM-dd")
            let json = $"""{{"date":"{dateStr}", "persons": {table.Capacity}, "customerId": "{Guid.NewGuid()}", "timeSlot": "21:00" }}"""

            let response = postRequest $"/tables/{table.TableId.Value}/reservations" json
            // let content = response.Content.ReadAsStringAsync().Result
            // Console.WriteLine(content)
            
            Assert.Equal(response.StatusCode, HttpStatusCode.Created)
        )
      }    
    ]
    