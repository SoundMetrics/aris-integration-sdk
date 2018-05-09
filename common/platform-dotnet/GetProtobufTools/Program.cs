namespace GetProtobufTools
{
    class Program
    {
        static void Main(string[] args)
        {
            //---------------------------------------------------------------------
            // NOTE
            //
            //  This project exists only to grab the protobuf tools down
            //  from nuget.org. Using a desktop project causes the tools to
            //  be available from within this project's folder tree.
            //  Doing so for a .NET Standard project does not place them where
            //  we can easily find them.
            //
            //  So, importantly,
            //
            //      1) this is a desktop project
            //      2) this project pulls down nuget package Google.Protobuf.Tools.
            //
            //---------------------------------------------------------------------
        }
    }
}
