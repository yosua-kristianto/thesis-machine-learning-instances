namespace Model.Entity

(*
    ImageDTO

    This data transfer object represent image data by containing information of the path, width, and height.
*)
type ImageDTO (imagePath: string, width: int, height: int) =
    member this.ImagePath: string = imagePath;
    member this.Width: int = width;
    member this.Height: int = height;