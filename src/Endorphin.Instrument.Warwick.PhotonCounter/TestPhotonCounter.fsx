// Copyright (c) University of Warwick. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

// Warning: generated file; your changes could be lost when a new file is generated.
#I __SOURCE_DIRECTORY__
#r "../../packages/Endorphin.Core/lib/net452/Endorphin.Core.dll"
#r "../../packages/log4net/lib/net45-full/log4net.dll"
#r "System.Core.dll"
#r "System.dll"
#r "System.Numerics.dll"
#r "../../packages/Rx-Interfaces/lib/net45/System.Reactive.Interfaces.dll"
#r "../../packages/System.Reactive.Interfaces/lib/net45/System.Reactive.Interfaces.dll"
#r "../../packages/Rx-Core/lib/net45/System.Reactive.Core.dll"
#r "../../packages/Rx-Linq/lib/net45/System.Reactive.Linq.dll"
#r "./bin/Debug/Endorphin.Instrument.Warwick.PhotonCounter.dll"
#r "../../packages/FSharp.Control.Reactive/lib/net45/FSharp.Control.Reactive.dll"

open Endorphin.Instrument.Warwick.PhotonCounter
open PhotonCounterAgent
open FSharp.Control.Reactive
open System
open System.Reactive.Concurrency

let readRates = async {
    use a = new Agent("COM4")

    a.Rate() |> Observable.subscribeOn Scheduler.Default |> Observable.add (printfn "Rate: %d")
    a.Lines() |> Observable.subscribeOn Scheduler.Default |> Observable.add (printfn "Line: %s")

    printfn "Starting to emit"
    a.InternalTrigger 100
    a.EmitRate()
    Console.ReadLine() |> ignore
//    for i in 1..1000 do
//        do! Async.Sleep(10)
//    a.SilenceRate()
//    printfn "Silenced emission"
//    do! Async.Sleep(2000)
//    printfn "Starting again"
//    a.EmitRate()
//    do! Async.Sleep(2000)
    printfn "Closing"
    }

Async.RunSynchronously readRates


