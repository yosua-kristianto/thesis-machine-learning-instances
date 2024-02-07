#r "nuget: SixLabors.ImageSharp, 3.1.2"
#r "nuget: Newtonsoft.Json, 13.0.3"

open System;
open System.IO;
open SixLabors.ImageSharp;
open SixLabors.ImageSharp.Processing;
open SixLabors.ImageSharp.PixelFormats;


// Utils
open Microsoft.FSharp.Core.Operators;
open System.Net.Http;
open Newtonsoft.Json;

type RegisteredKeys = {
    ORIGINAL_IMAGE_DIRECTORY: string
    TEMP_LOWRES_IMAGE_FOLDER_PATH: string
    DOWNSCALED_UPSCALED_IMAGE_FOLDER_PATH: string
    LABELS_ARRAY_FOLDER_PATH: string
    TELEGRAM_BOT_ID: string
    CHAT_ID: int64
};

let EnvironmentVariable : RegisteredKeys =
    let envPath: string = "../../config.json";
    
    let environmentVariableJsonFile = File.ReadAllText(envPath);

    let configurationValue: RegisteredKeys = JsonConvert.DeserializeObject<RegisteredKeys>(environmentVariableJsonFile);

    configurationValue;


// Collection of helper functions

(*
    ImageDTO

    This data transfer object represent image data by containing information of the path, width, and height.
*)
type ImageDTO (imagePath: string, width: int, height: int) =
    member this.ImagePath: string = imagePath;
    member this.Width: int = width;
    member this.Height: int = height;


(*
    GetFileNameFromPath

    This fucntion help to get file name from path string

    Example:
    Given a path of D:\\some-folder\\folder-with-files\\your-file.png

    Return:
    your-file.png
*)
let GetFileNameFromPath (path: string): string = 
    // Get the actual file name
    let reversedString = (path.ToCharArray()) |> Array.rev;
    let indexOfBackSlash = String(reversedString).IndexOf("\\");
    let substringBackSlash = reversedString.[0 .. (indexOfBackSlash-1)];
    String((substringBackSlash) |> Array.rev);


(*
    DownscaleImage

    This function will shorten the code needed to downscale an image

    Param:
    - imagePath: string
        This parameter represent the image path to be downscaled

    - downscaleRatio: float
        This parameter represent the ratio of the image to be downscaled. 
        Must be a real positive number less than 1.0 otherwise the size get bigger.
*)
let DownscaleImage (imagePath: string) (downscaleRatio: float): ImageDTO = 
    let image = Image.Load(imagePath);

    let fileName = GetFileNameFromPath imagePath;

    printfn "Filename is %s" fileName;

    // Lowres Generation

    let originalWidth = image.Width;
    let originalHeight = image.Height;

    let newWidth = int(float(image.Width) * (downscaleRatio));
    let newHeight = int(float(image.Height) * (downscaleRatio));

    image.Mutate(fun x -> ignore (x.Resize(newWidth, newHeight)));

    let lowresImagePath = EnvironmentVariable.TEMP_LOWRES_IMAGE_FOLDER_PATH + "\\" + fileName;

    image.Save(lowresImagePath);

    ImageDTO(lowresImagePath, originalWidth, originalHeight);

(*
    ScaleImageToSpecificSize

    This function help to re-scale image to specific size
*)
let ScaleImageToSpecificSize (imagePath: string) (targetWidth: int) (targetHeight: int) = 
    let originImage = Image.Load(imagePath);
    let fileName = GetFileNameFromPath imagePath;

    originImage.Mutate(fun x -> ignore (x.Resize(targetWidth, targetHeight)));

    let rescaledImagePath = EnvironmentVariable.DOWNSCALED_UPSCALED_IMAGE_FOLDER_PATH + "\\" + fileName;

    originImage.Save(rescaledImagePath);

    ImageDTO(rescaledImagePath, targetWidth, targetHeight);

(*
    ConvertImageToArray

    This function convert the image to a mathematical array.
*)
let ConvertImageToArray (imagePath: string) = 
    use image = Image<Rgba32>.Load<Rgba32>(imagePath);
    let width = image.Width;
    let height = image.Height;
    
    let pixelArray = Array.zeroCreate(width * height);

    for y in 0 .. height - 1 do
        for x in 0 .. width - 1 do
            let pixelColor = image.[x, y];

            let redValue = int(pixelColor.R);
            let greenValue = int(pixelColor.G);
            let blueValue = int(pixelColor.B);

            pixelArray.[y * width + x ] <- (redValue, greenValue, blueValue);

    image.Dispose();
    pixelArray;

let SaveArrayToFile (imagePath) (lowresArrayData) (originalArrayData) =
    let tensor: TensorDatasetDTO = { LowRes = lowresArrayData; Target = originalArrayData; }

    let fileName: string = GetFileNameFromPath imagePath;

    let tensorPayload: string = JsonConvert.SerializeObject(tensor);
    File.WriteAllText(EnvironmentVariable.LABELS_ARRAY_FOLDER_PATH+"\\"+fileName+".json", tensorPayload);


type TelegramRequestDTO = { text: string; chat_id: int64;}

let telegramService (message: string) =
    
    // Request manipulation

    let currentDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    let requestMessage = "["+currentDateTime+"] "+message;
    let request: TelegramRequestDTO = { text = requestMessage; chat_id = EnvironmentVariable.CHAT_ID; };


    use httpRequest = new HttpClient();
    let json = JsonConvert.SerializeObject request;
    use content = new StringContent (json, Encoding.UTF8, "application/json")

    async {
        let! response = httpRequest.PostAsync("https://api.telegram.org/bot"+EnvironmentVariable.TELEGRAM_BOT_ID+"/sendMessage", content) |> Async.AwaitTask
        response;
    } |> Async.RunSynchronously;

let downscaleUpscaleImage (imagePath: string) =
    let downscaledImage: ImageDTO = DownscaleImage imagePath 0.30;

    // Revert the size into original size
    ScaleImageToSpecificSize downscaledImage.ImagePath downscaledImage.Width downscaledImage.Height;


let files = Directory.GetFiles(EnvironmentVariable.ORIGINAL_IMAGE_DIRECTORY);

telegramService "Starting the data pre-processing operation";

type TensorDatasetDTO = { LowRes: Array; Target: Array; }
    
for i in files do
    printfn "Processing %s" i 

    let downscaleUpscaledImagePath = downscaleUpscaleImage i;

    let lowresArrayData = ConvertImageToArray downscaleUpscaledImagePath.ImagePath;
    let targetArrayData = ConvertImageToArray i;

    SaveArrayToFile i lowresArrayData targetArrayData;


telegramService "Pre-Processing process has completed" ;
