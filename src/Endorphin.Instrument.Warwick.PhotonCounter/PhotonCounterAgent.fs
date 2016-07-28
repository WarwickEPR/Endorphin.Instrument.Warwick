// Copyright (c) University of Warwick. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace Endorphin.Instrument.Warwick.PhotonCounter

open Endorphin.Core
open System
open System.IO.Ports
open System.Threading
open System.Text.RegularExpressions

module PhotonCounterAgent =

    type private Message =
    | InitialiseDevice of AsyncReplyChannel<unit>
    | Receive of string
    | Send of string

    /// Serial port stop bit mode.
    type StopBitMode =
        | One
        | OnePointFive
        | Two

    /// Serial port parity mode.
    type ParityMode =
        | NoParity
        | Odd
        | Even

    /// Serial port configuration.
    type SerialConfiguration = {
        BaudRate    : int
        DataBits    : int
        StopBits    : StopBits
        Parity      : Parity }

    type PhotonCounterAgent(comPort, ?serialConfiguration, ?notificationContext:SynchronizationContext, ?ct:CancellationToken) =

        let logger = log4net.LogManager.GetLogger "PhotonCounter"
        let notifier = NotificationEvent<string>()
        let notify msg = match notificationContext with
                         | None -> msg |> notifier.Trigger
                         | Some cxt -> cxt.Post((fun _ -> msg |> notifier.Trigger), null)
        let emitLine = Next >> notify

        let defaultSerial = {
            BaudRate = 115200
            DataBits = 8
            StopBits = StopBits.One
            Parity   = Parity.None }
        let serialConfiguration' = match serialConfiguration with
                                   | None -> defaultSerial
                                   | Some c -> c

        let serialPort = new SerialPort(comPort,
                                        serialConfiguration'.BaudRate,
                                        serialConfiguration'.Parity,
                                        serialConfiguration'.DataBits,
                                        serialConfiguration'.StopBits)

        let readLoop (mbox:Agent<Message>) (serialPort:SerialPort) = async {
            if not serialPort.IsOpen then
                failwith "Serial port is not open"
            serialPort.ReadTimeout <- -1 // Just wait asynchronously
            let ctx = SynchronizationContext.Current
            while serialPort.IsOpen do
                do! Async.SwitchToContext ctx
                try
                    let bufferLen = 4096
                    let buffer :byte[] = Array.zeroCreate(bufferLen)
                    let! read = serialPort.BaseStream.ReadAsync(buffer,0,bufferLen) |> Async.AwaitTask
                    if read > 0 then
                        let str = System.Text.Encoding.UTF8.GetString buffer.[0..read-1]
                        logger.Debug <| sprintf "Read %d bytes: %s" read str
                        Receive str |> mbox.Post
                with
                // no timeout set at the moment
                | :? TimeoutException -> Thread.Sleep 100 }

        let writeImmediately msg =
            logger.Debug <| sprintf "Sending command : %s" msg
            serialPort.WriteLine(msg)

        let handler (mbox:Agent<Message>) =
            let rec loop (partialLine:string) = async {
                do! Async.SwitchToThreadPool()
                let! msg = mbox.Receive()
                match msg with
                | Receive newData ->
                    // Received data will not be aligned with newlines.
                    // Combine data already received with the new data and
                    // emit any completed lines.
                    // Save any incomplete line fragment to combine with the next block
                    let rec handleData unfinished (d:string) =
                        match d.IndexOfAny([| '\r'; '\n' |]) with
                        | -1 -> // not found
                            unfinished + d
                        | i  ->
                            let line = unfinished + d.[0..i-1]
                            logger.Debug <| sprintf "Complete line: %s" line
                            emitLine line
                            if d.Chars(i+1) = '\n' then
                                handleData "" <| d.Substring (i+2)
                            else
                                handleData "" <| d.Substring (i+1)
                    return! loop (handleData partialLine newData)
                | Send s -> 
                    writeImmediately s
                    return! loop partialLine
                | InitialiseDevice rc ->
                    serialPort.NewLine <- "\r\n"
                    serialPort.Open()
                    [ "HIDE DATA"
                      "INT"
                      "TB 100ms" ] |> List.iter writeImmediately
                    do! Async.Sleep 500 // Give photon counter time to process instructions
                    serialPort.DiscardInBuffer() // Discard any emissions up to now
                    rc.Reply() // Notify the caller that it can proceed
                    return! loop partialLine
            }
            loop ""

        let agent = Agent.Start handler

        do
            InitialiseDevice |> agent.PostAndReply
            let readAsync = async { do! Async.SwitchToNewThread()
                                    do! readLoop agent serialPort }
            match ct with
            | None -> Async.Start <| readAsync
            | Some ct ->
                Async.Start (readAsync,ct)

        interface IDisposable with member __.Dispose() = serialPort.Close()

        member __.Lines() =
            notifier.Publish |> Observable.fromNotificationEvent

        member __.EmitRate() =
            "SHOW RATE" |> Send |> agent.Post
//            "?" |> Send |> agent.Post

        member __.SilenceRate() =
            "HIDE DATA" |> Send |> agent.Post

        member __.InternalTrigger duration =
            duration |> sprintf "TB %dms" |> Send |> agent.Post
            "INT" |> Send |> agent.Post

        member __.ExternalTrigger() =
            "EXT" |> Send |> agent.Post

        member __.TwoExternalTrigger() =
            "EXT FULL" |> Send |> agent.Post

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
                
                
                
                
                
                
                
                
                
                
                
                
                
