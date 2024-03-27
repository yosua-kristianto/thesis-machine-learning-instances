namespace Handler

open System;
open System.IO;

open SixLabors.ImageSharp;
open SixLabors.ImageSharp.Processing;
open SixLabors.ImageSharp.PixelFormats;

open Newtonsoft.Json;

open Model.Entity;
open Facade.EnvironmentVariable;
open Facade;


module ImageProcessorHandler =
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
    let DownscaleImage (imagePath: string) (downscaleRatio: float): DownscaledImageDTO = 
        let image = Image.Load(imagePath);

        let fileName = Path.GetFileName(imagePath);

        // Lowres Generation

        let originalWidth = image.Width;
        let originalHeight = image.Height;

        let newWidth = int(float(image.Width) * (downscaleRatio));
        let newHeight = int(float(image.Height) * (downscaleRatio));

        image.Mutate(fun x -> ignore (x.Resize(newWidth, newHeight)));

        let lowresImagePath = EnvironmentVariable.TEMP_LOWRES_IMAGE_FOLDER_PATH + "/" + fileName;

        image.Save(lowresImagePath);

        DownscaledImageDTO(lowresImagePath, originalWidth, originalHeight);

    (*
        ScaleImageToSpecificSize

        This function help to re-scale image to specific size
    *)
    let ScaleImageToSpecificSize (imagePath: string) (targetWidth: int) (targetHeight: int) = 
        let originImage = Image.Load(imagePath);
        let fileName = Path.GetFileName(imagePath);

        originImage.Mutate(fun x -> ignore (x.Resize(targetWidth, targetHeight)));

        let rescaledImagePath = EnvironmentVariable.DOWNSCALED_UPSCALED_IMAGE_FOLDER_PATH + "/" + fileName;

        originImage.Save(rescaledImagePath);

        DownscaledImageDTO(rescaledImagePath, targetWidth, targetHeight);

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

    type TensorDatasetDTO = { LowRes: Array; Target: Array; }

    let SaveArrayToFile (imagePath: string) (lowresArrayData) (originalArrayData) =
        let tensor: TensorDatasetDTO = { LowRes = lowresArrayData; Target = originalArrayData; }
        
        let fileName: string = Path.GetFileName(imagePath);

        let tensorPayload: string = JsonConvert.SerializeObject(tensor);
        File.WriteAllText(EnvironmentVariable.LABELS_ARRAY_FOLDER_PATH+"/"+fileName+".json", tensorPayload);

    (*
    ------------------------------------------------------------------------------------------------
        <since date="20240326"/>

        CropImageTo96x96
        
        Since "Thread" required functions annotation for executing Multi-Threading in F#, instead of put this 
        line of codes to the main "HandleSuperResolutionDataset" function, it will call this function instead.

        image -> The image to be cropped
        imagePath -> The original image's path
        x -> The x starting cropping point 
        y -> The y starting cropping point
    ------------------------------------------------------------------------------------------------
    *)
    let CropImageTo96x96 (image: Image) (imagePath: string) (x: int) (y: int) =

        try
            // Wonder why the hell Clone function in F# required some configuration
            let tempImage = image.Clone(fun i -> ignore(i.Opacity(1f)));

            tempImage.Mutate(fun i -> ignore(i.Crop(new Rectangle(x, y, 96, 96))));
                
            let croppedFilePath: string = EnvironmentVariable.ORIGINAL_IMAGE_CROPPING_DIRECTORY 
                                                    + "/"
                                                    + Path.GetFileNameWithoutExtension(imagePath)
                                                    + (sprintf "_%dx%d" x y)
                                                    + Path.GetExtension(imagePath);
            tempImage.Save(croppedFilePath);

            let downscaledImage: DownscaledImageDTO = DownscaleImage croppedFilePath 0.60;

            // Revert the size into original size
            ScaleImageToSpecificSize downscaledImage.ImagePath downscaledImage.OriginalWidth downscaledImage.OriginalHeight |> ignore;
        with
        | ex -> "Invalid x y" |> ignore;

    (*
    ------------------------------------------------------------------------------------------------
        <since date="20240229"/>
        <deprecated />
    ------------------------------------------------------------------------------------------------
        HandleDownscaleUpscaleImage is a function to handle the Down-Upscaling image activity. 
        This function will directly call all functions within this Handler file that directly operates 
        DownscaleImage and UpscaleImage. Here is the algorithm below:

        1. Call DownscaleImage with parameter of the path of the image (`imagePath`) and downsizing it into 0.3 ratio
        2. Call ScaleImageToSpecificSize with parameter of the path of the Downscaled image (`downscaledImage.ImagePath`)
            and resizing it with the original size that taken from denoted downscaledImage.Width and downscaledImage.Height

    *)
    [<Obsolete("This function is deprecated. Use 'HandleSuperResolutionDataset' instead.", true)>]
    let HandleDownscaleUpscaleImage (imagePath: string) =
        let downscaledImage: DownscaledImageDTO = DownscaleImage imagePath 0.30;

        // Revert the size into original size
        ScaleImageToSpecificSize downscaledImage.ImagePath downscaledImage.OriginalWidth downscaledImage.OriginalHeight;

    (*
    ------------------------------------------------------------------------------------------------
        <since date="20240326"/>
    ------------------------------------------------------------------------------------------------
        HandleSuperResolutionDataset is the addition of HandleDownscaleUpscaleImage. The addition on this function is to add cropping the image stage by 96x96 pixels. 
        The one that saved to the disk is the 96x96 pixels version. 

        The applied changes change the whole algorithm routes would be end up like this:

        1. Retrieve the original image file
        2. Start async block
        3. For loop with configuration below:
            for i = 0; i < image.height; i+=96
                for j = 0; j < image.width; j+=96

                This way, everytime the image is cropped with valid padding method. In which, not filling the missing pixel with 0, but rather skipping it.
            3.1 Start new Asynchronous taks
            3.2 For every cropping area, save the cropped image to folder "ORIGINAL_IMAGE_CROPPING_DIRECTORY", with name postfix of _xxy (e.g. _192x192)
            3.3 Call DownscaleImage with imagePath of the cropped iamge location, and the downsize rate is 0.6
            3.4 Call ScaleImageToSpecificSize with parameter of the Downscaled image (downscaledImage.ImagePath), and resize it to 96x96.
        4. Run the async block, and wait for the operation to be done.
    *)
    let HandleSuperResolutionDataset (imagePath: string) =
        // Recall Step 1: Load the image
        let image: Image = Image.Load(imagePath)

        // Asynchronously process the image
        async {
            // Create asynchronous computations for cropping each portion of the image
            let! tasks = 
                [ for y in 0 .. 96 .. (image.Height-1) do
                    for x in 0 .. 96 .. (image.Width-1) do
                    yield async {
                        // Asynchronously invoke CropImageTo96x96
                        let! result = async { CropImageTo96x96 image imagePath x y }
                        return result
                    }] 
                |> Async.Parallel 

            // Wait for all cropping tasks to complete
            return ()
        } |> Async.RunSynchronously 