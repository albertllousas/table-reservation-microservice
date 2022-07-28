module Play.Play

open Suave  
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Utils.Collections

// https://stackoverflow.com/questions/36547475/routes-with-optional-parameters-in-suave

let greetings q =
  defaultArg (Option.ofChoice (q ^^ "name")) "World" |> sprintf "Hello %s"

let sample : WebPart = 
    path "/hello" >=> choose [
      GET  >=> request (fun r -> OK (greetings r.query))
      POST >=> request (fun r -> OK (greetings r.form))
      RequestErrors.NOT_FOUND "Found no handlers" ]

let app = choose [ sample ]

// [<EntryPoint>]
// let main args = 
//     startWebServer defaultConfig app
//     0
