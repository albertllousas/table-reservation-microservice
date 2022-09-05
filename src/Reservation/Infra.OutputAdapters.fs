module Reservation.Infra.OutputAdapters

open Reservation.Domain.Model.OutputPorts
open System
open Reservation.Domain.Model
open Npgsql.FSharp
open Newtonsoft.Json
open System.Transactions

module DB =

  exception ConcurrencyError 

  type PostgresqlTableRepository(connectionString: string) =
    
    let mapToTable (row: RowReader): Table = 
      {
        TableId = TableId <| row.uuid "table_id"
        RestaurantId = RestaurantId <| row.uuid "restaurant_id"
        Capacity = row.int "capacity"
        Date = DateOnly.FromDateTime <| row.dateTime "table_date"
        DailySchedule = row.fieldValue<string> "daily_schedule" 
                        |> (fun json -> JsonConvert.DeserializeObject<Map<string, Reservation option>> json) 
                        |> Map.toList |> List.map (fun (k,v) -> (TimeSlot(k), v)) |> Map.ofList
        Version = row.int64 "aggregate_version"
      }

    interface TableRepository with
      
      member _.FindAllBy(restaurantId: RestaurantId) (date: DateOnly): Table list = 
        connectionString
          |> Sql.connect
          |> Sql.query "SELECT * FROM restaurant_table where restaurant_id = @table_id AND table_date = @table_date"
          |> Sql.parameters [ "table_id", Sql.uuid restaurantId.Value; "table_date", Sql.timestamp (date.ToDateTime(TimeOnly.Parse("00:00 AM"))) ]
          |> Sql.execute mapToTable

      member _.FindBy(tableId: TableId): Result<Table, DomainError> = 
        try  
          connectionString
            |> Sql.connect
            |> Sql.query "SELECT * FROM restaurant_table where table_id = @table_id"
            |> Sql.parameters [ "table_id", Sql.uuid tableId.Value ]
            |> Sql.executeRow mapToTable
            |> Ok
        with
          | :? NoResultsException -> Error TableNotFound

      member _.Save(table: Table): unit = 
        connectionString
          |> Sql.connect
          |> Sql.query """INSERT INTO restaurant_table VALUES (@table_id, @restaurant_id, @capacity, @table_date, @daily_schedule, @aggregate_version)
                       ON CONFLICT (table_id) DO UPDATE SET restaurant_id = @restaurant_id, CAPACITY = @capacity, TABLE_DATE = @table_date, DAILY_SCHEDULE = @daily_schedule, AGGREGATE_VERSION= restaurant_table.aggregate_version + 1 
                       RETURNING *
                       """
          |> Sql.parameters [ 
              "table_id", Sql.uuid table.TableId.Value;
              "restaurant_id", Sql.uuid table.RestaurantId.Value;
              "capacity", Sql.int table.Capacity;
              "table_date", Sql.timestamp (table.Date.ToDateTime(TimeOnly.Parse("00:00 AM")));
              "aggregate_version", Sql.int64 (table.Version + 1L)
              "daily_schedule", Sql.jsonb (JsonConvert.SerializeObject (table.DailySchedule |> Map.toList |> List.map (fun (k,v) -> (k.Value, v)) |> Map.ofList))
            ]
          |> Sql.executeRow (fun row -> row.int64 "aggregate_version")
          |> (fun version -> if version <> (table.Version + 1L) then raise ConcurrencyError else () ) 

  let withinAmbientTransaction : WithinTransation<'T> = 
    fun (code: unit -> 'T) -> 
      use tran = new TransactionScope()
      let result = code()
      tran.Complete()
      result

module Ids = 

  type RandomIdGenerator() = 
    interface IdGenerator with
      member _.Guid(): Guid = Guid.NewGuid()
      member _.RandomString(size: int): string = new String(Array.init size (fun _-> char (Random().Next(97,123))))
         