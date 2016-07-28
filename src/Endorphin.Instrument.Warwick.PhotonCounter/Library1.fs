// Copyright (c) University of Warwick. All Rights Reserved. Licensed under the Apache License, Version 2.0. See LICENSE.txt in the project root for license information.

namespace Endorphin.Instrument.Warwick.PhotonCounter

open System.Reactive.Linq
open System
open System.IO
open System.IO.Ports
open System.Reactive.Threading.Tasks

module General =
    let streamBytes (sp:SerialPort) =
        let blocks = sp.BaseStream.AsyncRead(sp.ReadBufferSize)
        ()

    let testObs = 
        let createSubscription (o : IObserver<_>) =
            o.OnNext("foo")
            o.OnNext("bar")
            o.OnCompleted()
            
            { new IDisposable with member x.Dispose() = () }
        
        Observable.Create(createSubscription)



    let private readLineAsync (reader:StreamReader) =
        reader.ReadLineAsync() |> Async.AwaitTask


module Serial = 

    let openSerialPort port =
        let sp = new SerialPort(port,115200)
        sp

    let lineModeWriter (sp:SerialPort) =
        new StreamWriter(sp.BaseStream)

    let lineModeReader (sp:SerialPort) =
        new StreamReader(sp.BaseStream)

    
    Observable.Create(
module Tcpip =
    
    open System.Net.Sockets

    let openSocket (host:string) port = async {
        let s = new TcpClient()
        do! Async.FromBeginEnd((fun (callback,state) -> s.BeginConnect(host,port,callback,state)),s.EndConnect)
        return s }

    let lineModeWriter (client:TcpClient) =
        new StreamWriter(client.GetStream())

    let lineModeReader (client:TcpClient) =
        new StreamReader(client.GetStream())

    let firstLineTest host port = async {
        let! s = openSocket host port
        let! line = (lineModeReader s).ReadLineAsync() |> Async.AwaitTask
        return line }
