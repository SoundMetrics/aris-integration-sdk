# SimplifiedProtocolTestWpfCore

## Build Issues

If your Visual Studio build yields the following error, you need to implement 
the solution below. Not all VS installations have this problem.

The build error message (Rebuild):

```
C:\Program Files (x86)\dotnet\sdk\3.1.200\Sdks\Microsoft.NET.Sdk.WindowsDesktop\targets\Microsoft.WinFX.targets(225,9): error MSB4062: The "Microsoft.Build.Tasks.Windows.MarkupCompilePass1" task could not be loaded from the assembly C:\Program Files %28x86%29\dotnet\sdk\3.1.200\Sdks\Microsoft.NET.Sdk.WindowsDesktop\tools\net472\PresentationBuildTasks.dll. Could not load file or assembly 'file:///C:\Program Files %28x86%29\dotnet\sdk\3.1.200\Sdks\Microsoft.NET.Sdk.WindowsDesktop\tools\net472\PresentationBuildTasks.dll' or one of its dependencies. The system cannot find the file specified. Confirm that the <UsingTask> declaration is correct, that the assembly and all its dependencies are available, and that the task contains a public class that implements Microsoft.Build.Framework.ITask.
```
To resolve:

You'll want to make a change to 
`C:\Program Files (x86)\dotnet\sdk\3.1.200\Sdks\Microsoft.NET.Sdk.WindowsDesktop\targets\Microsoft.WinFx.props`

See [this solution](https://github.com/dotnet/wpf/issues/2415#issuecomment-597133014)
to find how to modify the `Microsoft.WinFx.props` file.

This issue should be solved in .NET Core SDK 3.1.3
