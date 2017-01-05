// Copyright (c) University of Warwick. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Endorphin.Instrument.Warwick.PhotonCounter
open FSharp.Control.Reactive

[<EntryPoint>]
let main argv = 

    let test = async {
//        do! Async.SwitchToNewThread()
        use a = new PhotonCounter("COM4")
        do! a.Initialise

        a.Rate() |> Observable.add (printfn "Rate: %d")
        a.Rate() |> Observable.take 20 |> Observable.toArray |>  Observable.add (printfn "20 results: %A")

        printfn "Starting to emit"
        a.InternalTrigger 10
        a.EmitRate()
//        do! Async.Sleep(3000)
//        a.SilenceRate()
//        printfn "Silenced emission"
//        printfn "Starting again"
//        a.EmitRate()
        printfn "Waiting"
        do! Async.Sleep(2500)
        printfn "Closing" }

    Async.RunSynchronously test

    0


