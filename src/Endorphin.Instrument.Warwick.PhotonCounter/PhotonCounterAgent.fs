// Copyright (c) University of Warwick. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace Endorphin.Instrument.Warwick.PhotonCounter

open System
open Endorphin.IO.Reactive
open System.Text.RegularExpressions

module Configuration =
    open Endorphin.IO
    let serial = { Serial.DefaultSerialConfiguration with BaudRate = 460800 }

type PhotonCounter(port) as photonCounterAgent =
    inherit LineObservableSerialInstrument("Photon Counter",port,Configuration.serial)

    member x.Initialise = async {
        // initial configuration
        [ "HIDE DATA"
          "INT"
          "TB 100ms" ] |> List.iter photonCounterAgent.Send
        do! Async.Sleep 500 // Give photon counter time to process instructions
        photonCounterAgent.Serial.DiscardInBuffer() // Throw away any trailing count rates
        photonCounterAgent.StartReading() } // Start reading data

    member x.EmitRate() =
        "SHOW RATE" |> x.Send

    member x.SilenceRate() =
        "HIDE DATA" |> x.Send

    member x.InternalTrigger duration =
        duration |> sprintf "TB %dms" |> x.Send
        "INT" |> x.Send

    member x.ExternalTrigger() =
        "EXT" |> x.Send

    member x.TwoExternalTrigger() =
        "EXT FULL" |> x.Send

    member x.Rate() =

        let extractRate line =
            let r = new Regex(@"^Rate\s*=\s*(\d+\.\d+)(.)")
            let m = r.Match(line)
            if m.Success then
                let f = m.Groups.[1].Value |>  Double.Parse
                let scale = m.Groups.[2].Value
                let multiplier = match scale.Chars(0) with
                                    | 'M' -> 1e6
                                    | 'k' -> 1e3
                                    | _ -> 1.0
                let c = f * multiplier |> Math.Round |> int
                Some c
            else
                None

        x.Lines()
        |> Observable.filter (fun s -> s.StartsWith("Rate"))
        |> Observable.choose extractRate
    member x.OnFinish() = x.SilenceRate(); base.OnFinish()
    interface IDisposable with member x.Dispose() = x.OnFinish()
