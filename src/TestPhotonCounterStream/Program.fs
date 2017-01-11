// Copyright (c) University of Warwick. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open Endorphin.Instrument.Warwick.PhotonCounter
open FSharp.Control.Reactive
open Endorphin.Core

[<EntryPoint>]
let main argv = 

    let N = 100

    let test = async {
        do! Async.SwitchToNewThread()
        use a = new PhotonCounter("COM4",null)
        do! a.Initialise
        a.SilenceRate()
//        a.Rate() |> Observable.windowCount 10 |> Observable.map Observable.first |> Observable.mergeInner |> Observable.add (printfn "Rate: %d")
        let nth = a.Rate() |> Observable.timestamp |> Observable.take N |> Observable.last
        let first = a.Rate() |> Observable.timestamp |> Observable.head
        Observable.zip first nth |> Observable.add (fun (f,n) -> n.Timestamp.Subtract f.Timestamp |> (printfn "%d samples in %A" N))

        printfn "Starting to emit"
        a.InternalTrigger 100
        a.EmitRate()
//        do! Async.Sleep(3000)
//        a.SilenceRate()
//        printfn "Silenced emission"
//        printfn "Starting again"
//        a.EmitRate()
        printfn "Waiting"
        do nth |> Observable.wait |> ignore
        printfn "Closing" }

    Async.RunSynchronously test

    0


