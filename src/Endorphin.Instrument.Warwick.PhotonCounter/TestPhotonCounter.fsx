// Copyright (c) University of Warwick. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

// Warning: generated file; your changes could be lost when a new file is generated.
#I __SOURCE_DIRECTORY__
#r "../../packages/Endorphin.IO/lib/net452/Endorphin.IO.dll"
#r "../../packages/Endorphin.IO.Reactive/lib/net452/Endorphin.IO.Reactive.dll"
#r "../../packages/log4net/lib/net45-full/log4net.dll"
#r "System.Core.dll"
#r "System.dll"
#r "System.Numerics.dll"
#r "../../packages/System.Reactive.Core/lib/net45/System.Reactive.Core.dll"
#r "../../packages/System.Reactive.Linq/lib/net45/System.Reactive.Linq.dll"
#r "../../packages/System.Reactive.Interfaces/lib/net45/System.Reactive.Interfaces.dll"
#r "./bin/Debug/Endorphin.Instrument.Warwick.PhotonCounter.dll"

open Endorphin.Instrument.Warwick.PhotonCounter
open System.Reactive.Concurrency
open System.Reactive.Linq

let readRates = async {
    use a = new PhotonCounter("COM4")
    do! a.Initialise

    let subscribeOn (scheduler:IScheduler) observable =
        Observable.SubscribeOn(observable,scheduler)
    
    a.Rate() |> subscribeOn Scheduler.Default |> Observable.add (printfn "Rate: %d")
    a.Lines() |> subscribeOn Scheduler.Default |> Observable.add (printfn "Line: %s")
    Observable.Take(a.Rate(),10) |> Observable.ToArray |>  Observable.add (printfn "Ten results: %A")

    printfn "Starting to emit"
    a.InternalTrigger 100
    a.EmitRate()
    do! Async.Sleep(3000)
    a.SilenceRate()
    printfn "Silenced emission"
    printfn "Starting again"
    a.EmitRate()
    do! Async.Sleep(3000)
    printfn "Closing"
    }

Async.RunSynchronously readRates


