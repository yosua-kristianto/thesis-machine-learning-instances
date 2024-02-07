#r "nuget: Newtonsoft.Json, 13.0.3";

open System;
open System.IO;
open Newtonsoft.Json;
open System.Linq;
open System.Net.Http;
open System.Diagnostics;
open System.Text;

(*
    Model Part
*)

type RegisteredKeys = {
    TELEGRAM_BOT_ID: string
    CHAT_ID: int64
    YOLO_ANNOTATION_FOLDER_OUTPUT: string
    MSCOCO_ANNOTATION_FOLDER_INPUT: string
    YOLO_IMAGE_TRAIN_FOLDER: string
    YOLO_IMAGE_TEST_FOLDER: string
    YOLO_IMAGE_VAL_FOLDER: string
};

type TelegramRequestDTO = { 
    text: string 
    chat_id: int64
}

type Category = {
    id: int
    name: string
};

type Image = {
    id: int
    width: int
    height: int
    file_name: string
    doc_name: string
    page_no: int
};

type Annotation = {
    id: int
    image_id: int
    category_id: int
    bbox: float array
};

type CocoJson = {
    categories: Category array
    images: Image array
    annotations: Annotation array
};

let EnvironmentVariable : RegisteredKeys =
    let envPath: string = "../../config.json";
    
    let environmentVariableJsonFile = File.ReadAllText(envPath);

    let configurationValue: RegisteredKeys = JsonConvert.DeserializeObject<RegisteredKeys>(environmentVariableJsonFile);

    configurationValue;


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



let filesInDirectory = Directory.GetFiles(EnvironmentVariable.MSCOCO_ANNOTATION_FOLDER_INPUT);

let mutable jsonContent: CocoJson array = [||];
let mutable cocoTypes: string array = [||];
let mutable googleDriveFolderCode: string array = [||];

for i in filesInDirectory do
    let jsonString = File.ReadAllText(i);
    let fileName = Path.GetFileName(i).ToLower();

    let importedData: CocoJson = JsonConvert.DeserializeObject<CocoJson>(jsonString);

    if fileName.Contains("test") then 
        cocoTypes <- Array.append cocoTypes [| "Test" |];
        googleDriveFolderCode <- Array.append googleDriveFolderCode [| "YOLO_TEST" |];

    elif fileName.Contains("train") then
        cocoTypes <- Array.append cocoTypes [| "Train" |];
        googleDriveFolderCode <- Array.append googleDriveFolderCode [| "YOLO_TRAIN" |];
    
    elif fileName.Contains("val") then
        cocoTypes <- Array.append cocoTypes [| "Val" |];
        googleDriveFolderCode <- Array.append googleDriveFolderCode [| "YOLO_VAL" |];
    else failwithf "Invalid file name. Filename must contains either test / train / val";
    
    jsonContent <- Array.append jsonContent [| importedData |];


jsonContent.Length;

// Categories Extractor

let mutable categoriesExtrationResult: string = "";

for i in jsonContent.[0].categories do
    categoriesExtrationResult <- (categoriesExtrationResult + (i.id - 1).ToString() + " " + i.name + "\n");

use stream = new StreamWriter ("../Output/extracted-categories.txt", false);
stream.WriteLine(categoriesExtrationResult);

stream.Close();

// Image searcher repository

let FindImageById (source: int) (imageId: int): Image = 
    let resultSet = jsonContent.[source].images |> Array.filter(fun x -> x.id = imageId);

    resultSet[0];

let FindAnnotationById (source: int) (annotationId: int): Annotation = 
    let resultSet = jsonContent.[source].annotations |> Array.filter(fun x -> x.id = annotationId);

    resultSet[0];

let outputFolderPath = EnvironmentVariable.YOLO_ANNOTATION_FOLDER_OUTPUT;

let RemoveExtensionFromFileName (filename: string) =
    let reversedString = (filename.ToCharArray()) |> Array.rev;
    let indexOfDot = String(reversedString).IndexOf(".");
    let substringBackSlash = reversedString.[(indexOfDot+1) .. reversedString.Length];
    String((substringBackSlash) |> Array.rev);

let runGDriveIntegratorScript (script: string) =
    let osVersion = System.Environment.OSVersion.Platform;

    let processInfo =
        match osVersion with
        | PlatformID.Win32NT | PlatformID.Win32S | PlatformID.Win32Windows | PlatformID.WinCE ->
            printfn "Current OS: Windows"
            new ProcessStartInfo("powershell.exe", sprintf "/C %s" script)
        | PlatformID.Unix | PlatformID.MacOSX ->
            printfn "Current OS: Unix/Linux or macOS"
            new ProcessStartInfo("/bin/bash", sprintf "-c %s" script)
        | _ ->
            printfn "Unknown OS"
            failwithf "Y U N O other OS??!!"

    processInfo.RedirectStandardOutput <- true
    processInfo.RedirectStandardError <- true
    processInfo.UseShellExecute <- false
    processInfo.CreateNoWindow <- true

    use proc = new Process()
    proc.StartInfo <- processInfo
    proc.Start()
    proc.WaitForExit()

    let output = proc.StandardOutput.ReadToEnd()
    let error = proc.StandardError.ReadToEnd()

    printfn "Output: %s\n\n\n" output
    
telegramService "Starting generating Yolo Output from MSCOCO";

for src in 0 .. (jsonContent.Length - 1) do
    for i in jsonContent[src].annotations do
        printfn "Processing annotation id: %d" i.id ;

        let image = FindImageById src i.image_id;

        let annotationString = (i.category_id - 1).ToString() 
                                        + " " 
                                        + i.bbox[0].ToString() 
                                        + " " 
                                        + i.bbox[1].ToString() 
                                        + " " 
                                        + i.bbox[2].ToString() 
                                        + " " 
                                        + i.bbox[3].ToString();
        
        let folderPath = outputFolderPath + "/" + cocoTypes.[src] + "/";

        Directory.CreateDirectory(folderPath) |> ignore;

        let yoloAnnotationFilePath = folderPath + RemoveExtensionFromFileName(image.file_name) + ".txt";

        use stream = new StreamWriter (yoloAnnotationFilePath, false);
        stream.WriteLine(annotationString);

        stream.Close();


telegramService "Done generating Yolo Output from MSCOCO";