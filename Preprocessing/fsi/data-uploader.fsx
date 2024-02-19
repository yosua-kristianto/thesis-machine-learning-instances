open System;
open System.Threading;

let tqdm (total : int) =
    let updateProgress (current : int) =
        let progress = float current / float total * 100.
        printf "\rProgress: [%-20s] %.2f%%" (String.replicate (current * 20 / total) "#") progress
        System.Console.Out.Flush() |> ignore

    let dispose() =
        printfn ""

    { new System.IDisposable with
        member this.Dispose() = dispose() }, updateProgress

// Example usage:
let totalIterations = 100
let disposable, updateProgress = tqdm totalIterations

for e = 1 to 100 do
    printfn "Doing iteration for process %d" e;
    for i = 1 to totalIterations do
        // Do your iteration work here
        Thread.Sleep(100);
        updateProgress i
    disposable.Dispose()
    