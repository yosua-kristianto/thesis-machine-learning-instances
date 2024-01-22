using Keras;

class KerasImporter 
{
    public static void Main(String[] args)
    {
        var model = Keras.Models.Sequential.LoadModel("somemodel.h5");
        Console.WriteLine("Hello World");
    }
}