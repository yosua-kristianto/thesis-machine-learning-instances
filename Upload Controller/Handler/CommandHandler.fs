namespace Handler

open System;
open System.IO;

open Model.Entity;
open Facade.EnvironmentVariable;
open Facade.ProgressBarHandler;
open Repository;
open Facade;

(*
    Define Command Pattern

    Every single dataset has their own treatment. The treatment itself, required to be matched as intended below:

    1. SRGAN
    - First of all, since the file is kind of being united within the folder, I need to define data for train-test-val. 
    So from 500K of data, I decide to split the data into 8:1:1 for train:test:val. 

    | Splits | Ratio |
    |---|---|
    | Train | 7 |
    | Test | 1.5 |
    | Val | 1.5 |

    - The data to be uploaded are determined by LowRes version of the file. Basically you take the file name, search for any FOLDER CODE of SRGAN*, with file name of it.

    2. DLA
    - This thing already splitted so I don't really need to split the data. 
    Just upload the data within the folder to the destinated Google Drive ID. 

    I need a SQLite database for this operation.
*)
module CommandHandler =

    (*
        TranslateFolderCodeToLocalPath

        This function used to translate the folder code, with the original path for the operation to be done.

        @param folderCode
        The destinated folder code

        @return string
    *)
    let TranslateFolderCodeToLocalPath(folderCode: string): string =
        match folderCode with
                            | "SRGAN_LOWRES" -> EnvironmentVariable.DOWNSCALED_UPSCALED_IMAGE_FOLDER_PATH
                            | "YOLO_TRAIN_IMAGE" -> EnvironmentVariable.YOLO_IMAGE_FOLDER + "/Train"
                            | "YOLO_TEST_IMAGE" -> EnvironmentVariable.YOLO_IMAGE_FOLDER + "/Test"
                            | "YOLO_VAL_IMAGE" -> EnvironmentVariable.YOLO_IMAGE_FOLDER + "/Val"
                            | "YOLO_TRAIN_LABEL" -> EnvironmentVariable.YOLO_ANNOTATION_FOLDER_OUTPUT + "/Train"
                            | "YOLO_TEST_LABEL" -> EnvironmentVariable.YOLO_ANNOTATION_FOLDER_OUTPUT + "/Test"
                            | "YOLO_VAL_LABEL" -> EnvironmentVariable.YOLO_ANNOTATION_FOLDER_OUTPUT + "/Val"
                            | _ -> "Nah I'd Win"

    
    (*
        CommandParser

        This function helps on controlling all uploader handlers.

        This function will skip SRGAN_HIGHRES and SRGAN_LABEL functionality since the DocBankHandler
        will handle the file directly by its file name.
    *)
    let CommandParser (dataframe: DataFrame) (repository: IRegisteredFileRepository): unit =

        let mutable isSRGANProcessed: bool = false;

        if dataframe.FOLDER_CODE.Contains "SRGAN" && dataframe.FOLDER_CODE <> "SRGAN_LOWRES" then () else
            // Get files from folder
            let folderFiles = Directory.GetFiles(TranslateFolderCodeToLocalPath dataframe.FOLDER_CODE);

            // Terminal iteration flagging
            let mutable iteration: int = 0;

            // Init The DocBankHandler and do the operation
            if dataframe.FOLDER_CODE.Equals("SRGAN_LOWRES") then
                let handler = new DocBankHandler(repository);

                // Count total files within the folder
                handler.AllCount <- folderFiles.Length;

                // Train test split
                // The test using "ceil" instead of round or floor, to round up in case count - train / 2 getting 1 modulus.
                handler.TrainTreshold <- int(round(float(handler.AllCount) * 0.7));
                handler.TestTreshold <- int(ceil(float((handler.AllCount - handler.TrainTreshold)) / 2.0));
                handler.ValTreshold <- handler.AllCount - handler.TrainTreshold - handler.TestTreshold;

                let disposable, updateProgress = tqdm handler.AllCount "SRGAN Progressor";

                // Progress Bar
                printfn "Processing all SRGAN related files.";
                for e in folderFiles do
                    handler.ProcessAllFiles(Path.GetFullPath(e));
                    iteration <- iteration + 1;
                    updateProgress iteration;
                disposable.Dispose();
            else
                let disposable, updateProgress = tqdm folderFiles.Length (sprintf "%s Progresssor" dataframe.FOLDER_CODE);

                printfn "Processing %s" dataframe.FOLDER_CODE;

                // Progress Bar
                for e in folderFiles do
                    repository.CreateRegisteredFile(e, dataframe.FOLDER_CODE) |> ignore;
                    iteration <- iteration + 1;
                    updateProgress iteration;
                disposable.Dispose();

    let InvokeUpload (repository: IRegisteredFileRepository): unit = 
        
        let unuploadedFiles: RegisteredFile array = repository.GetAllUnuploadedRegisteredFiles();

        let mutable iteration: int = 0;

        let disposable, updateProgress = tqdm unuploadedFiles.Length "GDrive Integrator Integration";

        printfn "Uploading %d files." unuploadedFiles.Length;
        for e in unuploadedFiles do
            let errorMessage: string = Facade.TerminalHandler.Execute (sprintf "cd ../../../../; conda run -n google-drive-sdk python '.\\Gdrive Integrator\\main.py' --file_origin_path='%s' --folder_code='%s'" e.FileOriginalPath e.FolderCode)

            if errorMessage.Equals("") = false then
               Log.W errorMessage;
            else
                repository.UpdateUploadedAtByRegisteredFile(e.FileId, "") |> ignore;

            iteration <- iteration + 1;
            updateProgress iteration;
        disposable.Dispose();