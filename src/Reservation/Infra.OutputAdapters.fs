module Reservation.Infra.OutputAdapters

open Reservation.Domain.Model.OutputPorts
open System
open Reservation.Domain.Model
open Npgsql.FSharp
open FSharp.Json

module DB =

  type PostgresqlTableRepository(connectionString: string) =
    
    let mapToTable (row: RowReader): Table = 
      {
        TableId = TableId <| row.uuid "table_id"
        RestaurantId = RestaurantId <| row.uuid "restaurant_id"
        Capacity = row.int "capacity"
        Date = DateOnly.FromDateTime <| row.dateTime "table_date"
        DailySchedule = row.fieldValue<string> "daily_schedule" 
                        |> (fun json -> Json.deserialize<Map<string, Reservation option>> json) 
                        |> Map.toList |> List.map (fun (k,v) -> (TimeSlot(k), v)) |> Map.ofList
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
        failwith "Not Implemented" // versions and op locking


// https://github.com/vsapronov/FSharp.Json
// https://github.com/fsprojects/awesome-fsharp#serialization
// https://github.com/Zaid-Ajaj/Npgsql.FSharp
// https://zetcode.com/csharp/postgresql/
// https://docs.microsoft.com/en-us/dotnet/api/system.transactions.transactionscope?redirectedfrom=MSDN&view=net-6.0

// https://www.c-sharpcorner.com/uploadfile/4d56e1/connection-pooling-ado-net/

// https://docs.microsoft.com/en-us/dotnet/framework/data/transactions/implementing-an-implicit-transaction-using-transaction-scope
// https://fsprojects.github.io/FSharp.Data.SqlClient/index.html
// https://fsprojects.github.io/FSharp.Data.SqlClient/comparison.html
// https://fsprojects.github.io/FSharp.Data.SqlClient/transactions.html


// https://github.com/testcontainers/testcontainers-dotnet

// transactional with computational expresion: https://en.wikibooks.org/wiki/F_Sharp_Programming/Computation_Expressions#Syntax_Sugar

// https://github.com/vsapronov/FSharp.Json