﻿namespace Facade

open System;
open System.Diagnostics;

module TerminalHandler =
    let Execute (script: string) =

        let osVersion = System.Environment.OSVersion.Platform;

        let processInfo =
            match osVersion with
            | PlatformID.Win32NT | PlatformID.Win32S | PlatformID.Win32Windows | PlatformID.WinCE ->
                new ProcessStartInfo("powershell.exe", sprintf "/C %s" script)
            | PlatformID.Unix | PlatformID.MacOSX ->
                new ProcessStartInfo("/bin/bash", sprintf "-c %s" script)
            | _ ->
                failwithf "Y U N O other OS??!!"

        processInfo.RedirectStandardOutput <- true
        processInfo.RedirectStandardError <- true
        processInfo.UseShellExecute <- false
        processInfo.CreateNoWindow <- true

        use proc = new Process()
        proc.StartInfo <- processInfo
        proc.Start()
        proc.WaitForExit()

        //let output = proc.StandardOutput.ReadToEnd()
        let error = proc.StandardError.ReadToEnd()

        error;

module ProgressBarHandler =

    let tqdm (total : int) (threadName: string) =
        let updateProgress (current : int) =
            let progress = float current / float total * 100.
            printf "\rProgress [%s]: [%-20s] %.2f%%" threadName (String.replicate (current * 20 / total) "#") progress
            System.Console.Out.Flush() |> ignore

        let dispose() =
            printfn ""

        { new System.IDisposable with
            member this.Dispose() = dispose() }, updateProgress
