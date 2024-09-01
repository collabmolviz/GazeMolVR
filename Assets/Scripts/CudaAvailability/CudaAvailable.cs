using System.Runtime.InteropServices;


public class CudaAvailable {

    [DllImport("CudaAvailability")]
    public static extern bool isCudaAvailable();

    public static bool canRunCuda() {
        try{
            return isCudaAvailable();
        }
        catch{
        }
        return false;
    }
}