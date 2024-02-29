namespace Handler

open System.IO;

open Facade.EnvironmentVariable;
open Repository;

type DocBankHandler (repository: IRegisteredFileRepository) =

    let mutable trainCount: int = 0;
    let mutable testCount: int = 0;
    let mutable valCount: int = 0;

    let mutable trainTreshold: int = 0;
    let mutable testTreshold: int = 0;
    let mutable valTreshold: int = 0;

    let mutable countAllData: int = 0;

    member this.TrainTreshold
        with get() = trainTreshold
        and set count = trainTreshold <- count

    member this.TestTreshold
        with get() = testTreshold
        and set count = testTreshold <- count

    member this.ValTreshold
        with get() = valTreshold
        and set count = valTreshold <- count

    member this.AllCount
        with get() = countAllData
        and set count = countAllData <- count

    (*
        DecideSplitGroup

        This function helps decide splits group generating random value.

        @return "TRAIN" | "TEST" | "VAL"
    *)
    member this.DecideSplitGroup (): string =
        let random = System.Random();
        let decision = random.Next(0, 3);

        if(decision = 0 && (trainCount+1 <= trainTreshold)) then
            trainCount <- trainCount + 1;
            "TRAIN"
        elif(decision = 1 && (testCount+1 <= testTreshold)) then
            testCount <- testCount + 1;
            "TEST"
        elif (decision = 2 && (valCount+1 <= valTreshold)) then
            valCount <- valCount + 1;
            "VAL"
        else
            this.DecideSplitGroup()


    (*
        ProcessAllFiles

        This function help to process consist of several files type below:
        1. DownUpscaled Image
        2. Original Image
        3. Ground Truth

        This is happen because three consisting files are have same name, in which 
        can be leveraged into tiding up the files into train-test-val dataset since
        there are no fixed train-test-val data configuration by DocBank.

        The algorithm within this function is as below:

        1. Take DownUpscale, Highres, and Label path
        2. Depending whether Train / Test / Val current counts are, randomize whether 
            the data decided to be split as Train / Test / Val
        3. Save the file information to database
    *)
    member this.ProcessAllFiles (fileFullPath: string) =
        // @todo: This function should try-catched

        // Call out step 1
        let fileName = Path.GetFileName(fileFullPath);

        let highresPath = EnvironmentVariable.ORIGINAL_IMAGE_DIRECTORY + "\\" + fileName;
        let labelPath = EnvironmentVariable.OCR_GROUND_TRUTH_PATH + "\\" + fileName;

        // Call out step 2
        let group: string = this.DecideSplitGroup();

        // Call out step 3
        
        repository.CreateRegisteredFile(fileFullPath, ("SRGAN_LOWRES_" + group)) |> ignore;
        repository.CreateRegisteredFile(highresPath, ("SRGAN_HIGHRES_" + group)) |> ignore;
        repository.CreateRegisteredFile(labelPath, ("SRGAN_LABEL_" + group)) |> ignore;