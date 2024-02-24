namespace Handler

open Model.Entity;

(*
    Define Command Pattern

    Every single dataset has their own treatment. The treatment itself, required to be matched as intended below:

    1. SRGAN
    - First of all, since the file is kind of being united within the folder, I need to define data for train-test-val. 
    So from 500K of data, I decide to split the data into 8:1:1 for train:test:val. 

    | Splits | Ratio |
    |---|---|
    | Train | 8 |
    | Test | 1 |
    | Val | 1 |

    - The data to be uploaded are determined by LowRes version of the file. Basically you take the file name, search for any FOLDER CODE of SRGAN*, with file name of it.

    2. DLA
    - This thing already splitted so I don't really need to split the data. 
    Just upload the data within the folder to the destinated Google Drive ID. 

    I need a SQLite database for this operation.
*)
module CommandHandler =

    let CommandParser (folderCode: string) (dataframe: DataFrame) =

        if dataframe.FOLDER_CODE.Contains("SRGAN") then
            1+1
        elif dataframe.FOLDER_CODE.Contains("YOLO") then
            2+2
        else
            3+3

