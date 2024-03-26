﻿namespace Handler

open System;
open System.IO;

open SixLabors.ImageSharp;
open SixLabors.ImageSharp.Processing;
open SixLabors.ImageSharp.PixelFormats;

open Newtonsoft.Json;

open Model.Entity;
open Facade.EnvironmentVariable;


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
        <since date="20240229"/>
    ------------------------------------------------------------------------------------------------
        HandleDownscaleUpscaleImage is a function to handle the Down-Upscaling image activity. 
        This function will directly call all functions within this Handler file that directly operates 
        DownscaleImage and UpscaleImage. Here is the algorithm below:

        1. Call DownscaleImage with parameter of the path of the image (`imagePath`) and downsizing it into 0.3 ratio
        2. Call ScaleImageToSpecificSize with parameter of the path of the Downscaled image (`downscaledImage.ImagePath`)
            and resizing it with the original size that taken from denoted downscaledImage.Width and downscaledImage.Height
    *)
    let HandleDownscaleUpscaleImage (imagePath: string) =
        let downscaledImage: DownscaledImageDTO = DownscaleImage imagePath 0.30;

        // Revert the size into original size
        ScaleImageToSpecificSize downscaledImage.ImagePath downscaledImage.OriginalWidth downscaledImage.OriginalHeight;

    