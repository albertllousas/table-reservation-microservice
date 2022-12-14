module Reservation.Tests.Fixtures

open Xunit
open System
open Expecto
open System.IO
open Giraffe
open FluentAssertions.Json
open Newtonsoft.Json.Linq
open System.Net
open System.Net.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Reservation.Domain.Model
open Ductus.FluentDocker.Builders
open Microsoft.Extensions.Logging
open Evolve
open Npgsql
open Npgsql.FSharp
open Newtonsoft.Json

module Http = 

  let isStatus (code : HttpStatusCode) (response : HttpResponseMessage) =
      Assert.Equal(code, response.StatusCode)
      response

  let post (client : HttpClient) (path : string) (json : string) =
      let content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
      client.PostAsync(path, content)

  let get (client : HttpClient) (path : string) = client.GetAsync(path)

  let readText (response : HttpResponseMessage) =
      response.Content.ReadAsStringAsync()

  let isJson (expected : string) (actual : string) =
      let expectedJson = JToken.Parse(expected)
      let actualJson = JToken.Parse(actual)
      actualJson.Should().BeEquivalentTo(expectedJson, "", "")

  let configureApp (routes: HttpHandler) (app : IApplicationBuilder) =
      app.UseGiraffe routes

  let configureServices (services : IServiceCollection) =
      // dependencies |> List.iter (fun dep -> services.AddSingleton(dep) |> ignore) 
      services
          .AddResponseCaching()
          .AddGiraffe() |> ignore

  let createHost (routes: HttpHandler) =
      WebHostBuilder()
          .UseContentRoot(Directory.GetCurrentDirectory())
          .Configure(Action<IApplicationBuilder> (configureApp routes))
          .ConfigureServices(Action<IServiceCollection> configureServices)

  let createTestServer routes = new TestServer(createHost routes)


module Assert =

  let IsOk (actual: Result<'a,'b>) (expected: 'a) = 
    match actual with
      | Ok r -> Expect.equal r  expected ""
      | Error e -> Expect.isTrue false $"Expected Ok {expected} but got Error {e}"
  
  let IsError (actual: Result<'a,'b>) (expected: 'b) =
    match actual with
      | Ok r -> Expect.isTrue false $"Expected Error {expected} but got Ok {r}" // Assert.True(false, $"Expected Error {expected} but got Ok {r}") 
      | Error e -> Expect.equal e  expected ""

module Builders = 

  module TableBuilder =

    type Builder = { 
      TableId: Guid option
      RestaurantId : Guid option
      Capacity: int option
      Date : DateOnly option
      DailySchedule : Map<TimeSlot, Reservation option> option
      Version : int64 option
      }

    let tableBuilder: Builder = { TableId= None; RestaurantId= None; Capacity= None; Date= None; DailySchedule= None; Version=None}

    let tableId (id: Guid) (builder: Builder) : Builder = { builder with TableId= Some id }

    let restaurantId (id: Guid) (builder: Builder) : Builder = { builder with RestaurantId= Some id }

    let capacity (value: int) (builder: Builder) : Builder = { builder with Capacity= Some value }

    let date (value: DateOnly) (builder: Builder) : Builder = { builder with Date= Some value }

    let dailySchedule (map: Map<TimeSlot, Reservation option>) (builder: Builder) : Builder = { builder with DailySchedule= Some map }

    let version (version: int64) (builder: Builder) : Builder = { builder with Version= Some version }


    let buildTable (builder: Builder) : Table = 
      {
        TableId = (Guid.NewGuid(), builder.TableId) ||> Option.defaultValue |> (fun id -> TableId(id))
        RestaurantId = (Guid.NewGuid(), builder.RestaurantId) ||> Option.defaultValue |> (fun id -> RestaurantId(id))
        Capacity = (4, builder.Capacity) ||> Option.defaultValue 
        Date = (DateOnly.FromDateTime DateTime.Now, builder.Date) ||> Option.defaultValue 
        DailySchedule = (Map.empty, builder.DailySchedule) ||> Option.defaultValue 
        Version = (0L, builder.Version) ||> Option.defaultValue
      }

module DB =

  let startPostgresContainer() = 
    Builder()
      .UseContainer()
      .UseImage("postgres")
      .ExposePort(5432, 5432)
      .WithEnvironment("POSTGRES_PASSWORD=restaurant", "POSTGRES_USER=restaurant", "POSTGRES_DB=restaurant")
      .WaitForProcess("postgres", 30000)
      .Build()
      .Start()
    

  let log = LoggerFactory.Create(fun builder -> builder.AddConsole()|> ignore).CreateLogger("Test")

  let connectionString = "Host=localhost;Database=restaurant;Username=restaurant;Password=restaurant"

  let migrate () = 
    try
      let conn = new NpgsqlConnection(connectionString)
      (new Evolve(conn, (fun msg -> log.LogInformation msg), Locations = ["db/migrations"], IsEraseDisabled = false)).Migrate()
    with ex -> log.LogError ex.Message; raise ex 

  let insertTable table = 
    connectionString
      |> Sql.connect
      |> Sql.query "INSERT INTO restaurant_table VALUES (@table_id, @restaurant_id, @capacity, @table_date, @daily_schedule, @aggregate_version)"
      |> Sql.parameters [ 
          "table_id", Sql.uuid table.TableId.Value;
          "restaurant_id", Sql.uuid table.RestaurantId.Value;
          "capacity", Sql.int table.Capacity;
          "aggregate_version", Sql.int64 table.Version
          "table_date", Sql.timestamp (table.Date.ToDateTime(TimeOnly.Parse("00:00 AM")));
          "daily_schedule", Sql.jsonb (JsonConvert.SerializeObject ( table.DailySchedule |> Map.toList |> List.map (fun (k,v) -> (k.Value, v)) |> Map.ofList))
        ]
      |> Sql.executeNonQuery 
      |> ignore
  
  let incVersion table = 
    connectionString
     |> Sql.connect
     |> Sql.query "UPDATE restaurant_table SET aggregate_version = aggregate_version + 1 WHERE table_id= @table_id"
     |> Sql.parameters [ "table_id", Sql.uuid table.TableId.Value; ]
     |> Sql.executeNonQuery 
     |> ignore
      
