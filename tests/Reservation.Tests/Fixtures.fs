module Reservation.Tests.Fixtures

open Xunit
open System
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

module Http = 

  let isStatus (code : HttpStatusCode) (response : HttpResponseMessage) =
      Assert.Equal(code, response.StatusCode)
      response

  let post (client : HttpClient) (path : string) (json : string) =
      let content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
      client.PostAsync(path, content)

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

module Builders = 

  module TableBuilder =

    type Builder = { 
      TableId: Guid option
      RestaurantId : Guid option
      Capacity: int option
      Date : DateOnly option
      DailyReservations : Reservation list option
      AvailableTimeSlots : TimeSlot list option
      }

    let tableBuilder: Builder = { TableId= None; RestaurantId= None; Capacity= None; Date= None; DailyReservations= None; AvailableTimeSlots= None; }

    let tableId (id: Guid) (builder: Builder) : Builder = { builder with TableId= Some id }

    let restaurantId (id: Guid) (builder: Builder) : Builder = { builder with RestaurantId= Some id }

    let capacity (value: int) (builder: Builder) : Builder = { builder with Capacity= Some value }

    let date (value: DateOnly) (builder: Builder) : Builder = { builder with Date= Some value }

    let dailyReservations (list: Reservation list) (builder: Builder) : Builder = { builder with DailyReservations= Some list }

    let availableTimeSlots (list: TimeSlot list) (builder: Builder) : Builder = { builder with AvailableTimeSlots= Some list }

    let buildTable (builder: Builder) : Table = 
      {
        TableId = (Guid.NewGuid(), builder.TableId) ||> Option.defaultValue |> (fun id -> TableId(id))
        RestaurantId = (Guid.NewGuid(), builder.RestaurantId) ||> Option.defaultValue |> (fun id -> RestaurantId(id))
        Capacity = (2, builder.Capacity) ||> Option.defaultValue 
        Date = (DateOnly.FromDateTime DateTime.Now, builder.Date) ||> Option.defaultValue 
        DailyReservations = (List.empty, builder.DailyReservations) ||> Option.defaultValue 
        AvailableTimeSlots = (List.empty, builder.AvailableTimeSlots) ||> Option.defaultValue 
      }
