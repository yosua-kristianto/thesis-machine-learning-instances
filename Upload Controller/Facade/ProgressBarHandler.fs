namespace Facade

open System.Threading;

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

    
