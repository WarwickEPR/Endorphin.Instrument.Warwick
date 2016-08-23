// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Endorphin.Instrument.Warwick.PhotonCounter
open FSharp.Control.Reactive

[<EntryPoint>]
let main argv = 

    let test = async {
        use a = new PhotonCounter("COM4")
        do! a.Initialise

        a.Rate() |> Observable.add (printfn "Rate: %d")
        a.Lines() |> Observable.add (printfn "Line: %s")
        a.Rate() |> Observable.take 10 |> Observable.toArray |>  Observable.add (printfn "Ten results: %A")

        printfn "Starting to emit"
        a.InternalTrigger 100
        a.EmitRate()
        do! Async.Sleep(3000)
        a.SilenceRate()
        printfn "Silenced emission"
        printfn "Starting again"
        a.EmitRate()
        do! Async.Sleep(3000)
        printfn "Closing" }

    Async.RunSynchronously test
    0


