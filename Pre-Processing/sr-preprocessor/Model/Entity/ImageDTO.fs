namespace Model.Entity

(*
    ImageDTO

    This data transfer object represent image data by containing information of the path, width, and height.
*)
type DownscaledImageDTO (imagePath: string, width: int, height: int) =
    member this.ImagePath: string = imagePath;
    member this.OriginalWidth: int = width;
    member this.OriginalHeight: int = height;