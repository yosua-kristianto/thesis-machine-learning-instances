
open System.IO;

open Model.Dto;
open Facade.EnvironmentVariable;
open Facade;
open Services;
open Handler.ImageProcessorHandler;


// Utils
open Microsoft.FSharp.Core.Operators;


let files = Directory.GetFiles(EnvironmentVariable.ORIGINAL_IMAGE_DIRECTORY);

TelegramService.SendMessage "Starting the upscale-downscale image generation operation";
Log.I "Starting the upscale-downscale image generation operation" |> ignore;

    
for i in files do
    printfn "Processing %s" i 

    try
        DownscaleUpscaleImage i |> ignore;
    with 
    | ex ->
        Log.E ("Processing " + i + " error with: " + ex.Message) |> ignore;

TelegramService.SendMessage "The upscale-downscale image generation operation has been completed";
Log.I "The upscale-downscale image generation operation has been completed" |> ignore;
