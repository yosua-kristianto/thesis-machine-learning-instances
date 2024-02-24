namespace Config

open Deedle;

open Model.Entity;
open Facade;

(*
    Import folder settings pre-processing

    This first step of main code, is intended to take data from the csv file using Deedle data science tools in F#. 
    Unfortunately, the data that came directly from `ReadCsv` function cannot be directly used to be iteration using for loops stuffs. 
    So, what I discovered is that, the `Frame` thingy must be executed through the pipeline method in which are denoted with `|>` in this programming language. 

    Algorithm steps:
    1. Define DataFrame data structure
    2. Define an array to hold the data from the CSV
    3. ReadCsv
    4. Iterate through the Frame, and append the data to the array from number 2
*)

// Step 1

module FolderSettingExtractor =

    let ExtractCsv: DataFrame array = 
        // Step 2
        let mutable data: DataFrame array = [||];
        
        // Step 3
        // Check for data.csv availability. This part will throw an error if the data is not available
        let dataframe: Frame<int, string> = Frame.ReadCsv("../../../../data.csv");

        // Step 4
        dataframe |> Frame.mapRows(fun key row -> 
            let rowRepresentable: DataFrame = {
                FOLDER_CODE = row.GetAs<string>("FOLDER_CODE");
                FOLDER_ID = row.GetAs<string>("FOLDER_ID");
                ORIGINAL_PATH = row.GetAs<string>("ORIGINAL_PATH");
            }
        
            data <- Array.append data [| rowRepresentable |];
        ) |> ignore;

        Log.D "Folder settings are extracted from csv file";

        data;
