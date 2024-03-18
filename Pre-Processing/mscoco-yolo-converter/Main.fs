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
    MSCOCO_IMAGE_FOLDER: string
    YOLO_IMAGE_FOLDER: string
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


type YOLOAnnotation (imageId: int, imageFilePath: string, dataType: string, annotations: string array) = 
    member this.image_id: int = imageId
    member this.image_filepath: string = imageFilePath
    member this.data_type: string = dataType
    member val annotations: string array = annotations with get, set

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

// ETL Annotations
let mutable annotations: YOLOAnnotation array = [||];

let FindAnnotationByImageId (imageId: int): YOLOAnnotation = 
    let resultSet = annotations |> Array.filter(fun x -> x.image_id = imageId);

    resultSet[0];

let UpdateAnnotationByImageId (imageId: int) (newAnnotation: string): YOLOAnnotation = 
    let toBeInserted = [| newAnnotation |];

    let resultSet = FindAnnotationByImageId imageId;
    resultSet.annotations <- Array.append resultSet.annotations toBeInserted

    resultSet;


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

let moveFile (sourcePath: string) (targetPath: string) = 
    let fileName = Path.GetFileName(sourcePath);

    let destinationFilePath = Path.Combine(targetPath, fileName)
    
    if File.Exists(sourcePath) then
        // Check if the destination directory exists, if not, create it
        if not <| Directory.Exists(targetPath) then
            Directory.CreateDirectory(targetPath) |> ignore;
        
        // Copy the file to the destination directory
        File.Copy(sourcePath, destinationFilePath, true) // Set overwrite to true to overwrite if the file already exists
    else
        failwith ("Copying image file encounter error. Source does not exist: " + sourcePath);

let WriteLog (message: string) (logType: string) =
    let currentTime = DateTime.Now;
    let currentTimeInString = currentTime.ToString("yyyy-MM-dd H:m:s.FFFF zzz")
    let currentDateInString = currentTime.ToString("yyyyMMdd")
    let logToBeWritten = sprintf "[%s][%s][COCO_EXTRACTOR] %s" currentTimeInString logType message
    let pathToLog = Path.Combine("../../logs/", sprintf "%s.log" currentDateInString);

    printfn "%s" message;

    if not (File.Exists(pathToLog)) then
        if not (Directory.Exists("../../logs/")) then
            Directory.CreateDirectory("../../logs/") |> ignore;
        use stream = new StreamWriter(pathToLog, false);

        stream.WriteLine("");
        stream.Close();

    File.AppendAllText(pathToLog, logToBeWritten + "\n") |> ignore;

telegramService "Starting extracting MSCOCO JSON file.";
WriteLog "Starting extracting MSCOCO JSON file." "INFO" |> ignore;

for src in 0 .. (jsonContent.Length - 1) do
    telegramService (sprintf "%s annotation data: %d" cocoTypes.[src] (jsonContent.[src].annotations.Length));

    for i in jsonContent[src].annotations do
        try
            printfn "Extracting MSCOCO annotation with id %d" i.id

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

            let mutable transformAnnotation = YOLOAnnotation(i.image_id, (EnvironmentVariable.MSCOCO_IMAGE_FOLDER + "/" + image.file_name), cocoTypes.[src], [|annotationString|]);

            try 
                transformAnnotation <- FindAnnotationByImageId i.image_id;
                transformAnnotation <- UpdateAnnotationByImageId i.image_id annotationString;
            with
            | ex -> annotations <- Array.append annotations [| transformAnnotation |];
        with
        | ex ->
            WriteLog ("Processing annotation ID of " + i.id.ToString() + " error with: " + ex.Message) "ERROR" |> ignore;

telegramService "MSCOCO extraction has finished. Starting transforming extracted MSCOCO into YOLO metadata.";
WriteLog "MSCOCO extraction has finished. Starting transforming extracted MSCOCO into YOLO metadata." "INFO" |> ignore;


for e in annotations do
    try
        let fileName = Path.GetFileName(e.image_filepath);
        printfn "Transforming MSCOCO annotation into YOLO for %s Image %s with ID %d" e.data_type fileName e.image_id

        let folderPath = EnvironmentVariable.YOLO_ANNOTATION_FOLDER_OUTPUT + "/" + e.data_type + "/";

        Directory.CreateDirectory(folderPath) |> ignore;

        
        let yoloAnnotationFilePath = folderPath + RemoveExtensionFromFileName(fileName) + ".txt";
        
        let mutable annotationsString: string = "";

        for f in e.annotations do
            annotationsString <- annotationsString + f + "\n";

        use stream = new StreamWriter (yoloAnnotationFilePath, false);
        
        stream.WriteLine(annotationsString);
        stream.Close();

        // Copy file to the destinated image folder.
        moveFile (e.image_filepath) (EnvironmentVariable.YOLO_IMAGE_FOLDER + "/" + e.data_type + "/")
    with
    | ex -> WriteLog ("Transforming MSCOCO to YOLO file encounter error at Image ID " + e.image_id.ToString() + " with message: " + ex.Message) "ERROR" |> ignore;

telegramService "COCO Converter service has completed successfully";
WriteLog "COCO Converter service has completed successfully" "INFO" |> ignore;