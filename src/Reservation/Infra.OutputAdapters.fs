module Reservation.Infra.OutputAdapters

open Reservation.Domain.Model.OutputPorts
open System
open Reservation.Domain.Model
open Npgsql.FSharp

module DB =

  type PostgresqlTableRepository(connectionString: string) =
    
    let mapToTable (row: RowReader) = {
                    TableId = TableId <| row.uuid "table_id"
                    RestaurantId = RestaurantId <| row.uuid "table_id"
                    Capacity = row.int "capacity"
                    Date = DateOnly.FromDateTime <| row.dateTime "date"
                    DailySchedule = row.fieldValue<Map<TimeSlot, Reservation option>> "daily_schedule"
                }

    interface TableRepository with
      
      member _.FindAllBy(restaurantId: RestaurantId) (date: DateTime): Result<Table list, DomainError> = 
        failwith "Not Implemented"

      member _.FindBy(tableId: TableId): Result<Table, DomainError> = 
        try  
          connectionString
            |> Sql.connect
            |> Sql.query "SELECT * FROM restaurant_table where id = @id"
            |> Sql.parameters [ "id", Sql.uuid tableId.Value ]
            |> Sql.executeRow mapToTable
            |> Ok
        with
          | :? NoResultsException -> Error TableNotFound

      member _.Save(table: Table): unit = 
        failwith "Not Implemented"


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