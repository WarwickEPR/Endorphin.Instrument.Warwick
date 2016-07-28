// Copyright (c) University of Warwick. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace Endorphin.Instrument.Warwick.PhotonCounter

open Endorphin.Core
open System
open System.IO.Ports
open System.Threading
open System.Text.RegularExpressions
open FSharp.Control.Reactive

module PhotonCounterAgent =

    type private Message =
    | ClearIncoming of AsyncReplyChannel<unit>
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



    type Agent(comPort, ?notificationContext:SynchronizationContext, ?ct:CancellationToken) =

        let logger = log4net.LogManager.GetLogger "PhotonCounter"
        let notifier = NotificationEvent<string>()
        let notify msg = match notificationContext with
                         | None -> msg |> notifier.Trigger
                         | Some cxt -> cxt.Post((fun _ -> msg |> notifier.Trigger), null)
        let emitLine = Next >> notify

        let nl = "\r\n"

        let serialConfiguration = {
            BaudRate = 115200
            DataBits = 8
            StopBits = StopBits.One
            Parity   = Parity.None }

        let serialPort = new SerialPort(comPort,
                                        serialConfiguration.BaudRate,
                                        serialConfiguration.Parity,
                                        serialConfiguration.DataBits,
                                        serialConfiguration.StopBits)

        let readLoop (mbox:Agent<Message>) (serialPort:SerialPort) = async {
            printfn "Starting read loop"
            if not serialPort.IsOpen then
                failwith "Serial port is not open"
//            serialPort.ReadTimeout <- 20
            serialPort.ReadTimeout <- -1
            let ctx = SynchronizationContext.Current
            while serialPort.IsOpen do
                do! Async.SwitchToContext ctx
                try
//                    printfn "About to read a line"
//                    let line = serialPort.ReadLine()
//                    printfn "Read a line: %s" line
//                    do! Async.SwitchToThreadPool()
//                    Receive line |> mbox.Post
                    let bufferLen = 4096
                    let buffer :byte[] = Array.zeroCreate(bufferLen)
//                    serialPort.ReadTimeout <- 100
//                    printfn "About to read"
                    let! read = serialPort.BaseStream.ReadAsync(buffer,0,bufferLen) |> Async.AwaitTask
//                    printfn "Read %d" read
                    if read > 0 then
                        let str = System.Text.Encoding.UTF8.GetString buffer.[0..read-1]
//                        printfn "Posting: %s" str
                        Receive str |> mbox.Post
//                        emitLine 
                with
                | :? TimeoutException -> Thread.Sleep 100 }
//                let bufferLen = 4096
//                let buffer :byte[] = Array.zeroCreate(bufferLen)
//                serialPort.ReadTimeout <- 100
//                printfn "About to read"
//                let! read = serialPort.BaseStream.ReadAsync(buffer,0,bufferLen) |> Async.AwaitTask
//                printfn "Read %d" read
//                if read > 0 then
//                    let str = System.Text.Encoding.UTF8.GetString buffer.[0..read-1]
//                    printfn "Posting: %s" str
//                    Receive str |> mbox.Post
//                do! Async.Sleep 200 }

        let writeImmediately msg =
            printfn "Sending command : %s" msg
            serialPort.WriteLine(msg)
//            serialPort.BaseStream.Flush()
//                    let b = System.Text.Encoding.UTF8.GetBytes (s+nl)
//                    do! Async.SwitchToThreadPool()
//                    do! serialPort.BaseStream.AsyncWrite(b)
//                    do! serialPort.BaseStream.FlushAsync() |> Async.AwaitTask


        let handler (mbox:Agent<Message>) =
            let rec loop observers (data:string) = async {
                do! Async.SwitchToThreadPool()
                let! msg = mbox.Receive()
                match msg with
                | Receive d ->
                    let rec handleData unfinished (d:string) =
                        match d.IndexOfAny([| '\r'; '\n' |]) with
                        | -1 -> // not found
                            unfinished + d
                        | i  ->
                            let line = unfinished + d.[0..i-1]
                            printfn "Identified line: %s" line
                            emitLine line
                            if d.Chars(i+1) = '\n' then
                                handleData "" <| d.Substring (i+2)
                            else
                                handleData "" <| d.Substring (i+1)
                    printfn "Received: %s" d
                    return! loop observers (handleData data d)
                | Send s -> 
                    writeImmediately s
                    return! loop observers data
                | ClearIncoming rc ->
//                    serialPort.BaseStream.Flush()
//                    serialPort.DiscardInBuffer()
                    writeImmediately "HIDE DATA"
                    do! Async.Sleep 500 // to let other end process HIDE DATA
                    printfn "About to chuck junk"
//                    let buffer : byte[] = Array.zeroCreate(2<<<16)
//                    serialPort.ReadTimeout <- 100
//                    let read = serialPort.ReadExisting()
                    serialPort.DiscardInBuffer()
//                    printfn "Read junk %s" read

//                    while (serialPort.BytesToRead > 0) && (let d = serialPort.Read(buffer,0,2<<<16)) do
//                        do! Async.Sleep 100
//                        printfn "Still reading"
//                    printfn "Junk consumed"
//                    do! serialPort.BaseStream.ReadAsync(buffer,0,2<<<16) |> Async.AwaitTask |> Async.Ignore
                    rc.Reply()
                    return! loop observers data
            }
            loop Map.empty ""

        let agent = Agent.Start handler

        do
            serialPort.NewLine <- nl
            serialPort.Open()
            writeImmediately "HIDE DATA"
            ClearIncoming |> agent.PostAndReply
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
            let r = new Regex(@"^Rate\s*=\s*(\d+\.\d+)(.)")
            x.Lines()
            |> Observable.filter (fun s -> s.StartsWith("Rate"))
            |> Observable.perform (printfn "Try to match: %s")
            |> Observable.choose (fun s ->
                printfn "Matching: %s" s
                let m = r.Match(s)
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
                    printfn "No Match"
                    None)
