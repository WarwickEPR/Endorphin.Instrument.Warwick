// Copyright (c) University of Warwick. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace Endorphin.Instrument.Warwick.PhotonCounter

open System
open Endorphin.IO.Reactive
open System.Text.RegularExpressions

type PhotonCounter(port) as photonCounterAgent =
    inherit SerialInstrument("Photon Counter",port)

    member x.Initialise = async {
        // initial configuration
        [ "HIDE DATA"
          "INT"
          "TB 100ms" ] |> List.iter photonCounterAgent.WriteLine
        do! Async.Sleep 500 // Give photon counter time to process instructions
        photonCounterAgent.SerialPort.DiscardInBuffer() // Throw away any trailing count rates
        photonCounterAgent.StartReading() } // Start reading data

    member x.EmitRate() =
        x.WriteLine "SHOW RATE"

    member x.SilenceRate() =
        x.WriteLine "HIDE DATA"

    member x.InternalTrigger duration =
        duration |> sprintf "TB %dms" |> x.WriteLine
        "INT" |> x.WriteLine

    member x.ExternalTrigger() =
        "EXT" |> x.WriteLine

    member x.TwoExternalTrigger() =
        "EXT FULL" |> x.WriteLine

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
                
    interface IDisposable with
        member x.Dispose() =
            x.SilenceRate()
            
                
                
                
                
                
                
