
open System.IO;

open Model.Dto;
open Facade.EnvironmentVariable;
open Facade;
open Facade.ProgressBarHandler;
open Services;
open Handler.ImageProcessorHandler;


printfn "
 (   (      (                (                                   
 )\ ))\ )   )\ )             )\ )                                
(()/(()/(  (()/((     (     (()/((              (           (    
 /(_))(_))  /(_))(   ))\ ___ /(_))(   (    (   ))\(  (   (  )(   
(_))(_))   (_))(()\ /((_)___(_))(()\  )\   )\ /((_)\ )\  )\(()\  
/ __| _ \  | _ \((_|_))     | _ \((_)((_) ((_|_))((_|(_)((_)((_) 
\__ \   /  |  _/ '_/ -_)    |  _/ '_/ _ \/ _|/ -_|_-<_-< _ \ '_| 
|___/_|_\  |_| |_| \___|    |_| |_| \___/\__|\___/__/__|___/_|  
\n\n
"

// Utils
open Microsoft.FSharp.Core.Operators;


let files = Directory.GetFiles(EnvironmentVariable.ORIGINAL_IMAGE_DIRECTORY);

TelegramService.SendMessage "Starting the upscale-downscale image generation operation";
Log.I "Starting the upscale-downscale image generation operation" |> ignore;

let disposable, updateProgress = tqdm files.Length "SRGAN Dataset Pre-Processing";
let mutable iteration: int = 0;
    
for i in files do
    try
        HandleSuperResolutionDataset i |> ignore;
    with 
    | ex ->
        Log.E ("Processing " + i + " error with: " + ex.Message) |> ignore;

    iteration <- iteration + 1;
    updateProgress iteration;
disposable.Dispose();

TelegramService.SendMessage "The upscale-downscale image generation operation has been completed";
Log.I "The upscale-downscale image generation operation has been completed" |> ignore;
