Here's a workaround for those of us publishing [LlamaSharp.Backend.*](https://scisharp.github.io/LLamaSharp/0.12.0/QuickStart/) packages and run into the following errors:


### Conflict NETSDK1152 publishing LlamaSharp

Microsoft.NET.ConflictResolution.targets: error NETSDK1152. Found multiple publish output files with the same relative path:

```
.nuget\packages\llamasharp.backend.cpu\runtimes\native\avx\llama.dll, 
.nuget\packages\llamasharp.backend.cpu\runtimes\native\avx2\llama.dll, 
.nuget\packages\llamasharp.backend.cpu\runtimes\native\avx512\llama.dll, 
.nuget\packages\llamasharp.backend.cpu\runtimes\native\llama.dll, 
.nuget\packages\llamasharp.backend.cpu\runtimes\native\avx\llava_shared.dll, 
.nuget\packages\llamasharp.backend.cpu\runtimes\native\avx2\llava_shared.dll, 
.nuget\packages\llamasharp.backend.cpu\runtimes\native\avx512\llava_shared.dll, 
.nuget\packages\llamasharp.backend.cpu\runtimes\native\llava_shared.dll.
```

The type initializer for 'LLama.Native.NativeApi' threw an exception. ---> LLama.Exceptions.RuntimeError: The native library cannot be correctly loaded. It could be one of the following reasons:

1. No LLamaSharp backend was installed. Please search LLamaSharp.Backend and install one of them.
2. You are using a device with only CPU but installed cuda backend. Please install cpu backend instead.
3. One of the dependency of the native library is missed. Please use `ldd` on linux, `dumpbin` on windows and `otool` to check if all the dependency of the native library is satisfied. Generally you could find the libraries under your output folder.
4. Try to compile llama.cpp yourself to generate a libllama library, then use `LLama.Native.NativeLibraryConfig.WithLibrary` to specify it at the very beginning of your code. For more information about compilation, please refer to LLamaSharp repo on github.


### Issue 382

LalamaSharp backend packages distribute llama.dll and llava_shared.dll for various runtimes to support hardware configurations (cpu, cuda, opengl). In [LlamaSharp issue 382](https://github.com/SciSharp/LLamaSharp/issues/382) is described how duplicate files in publish output cause the [NETSDK1152](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/6.0/duplicate-files-in-output) conflict above. The `dotnet publish` tool scans packages and throws the error above when it finds multiple files with the same name. As DLLs in published output are written is a flat directory structure, the duplicates would overwrite.

```shell
Example 1

dotnet clean Example1_PublishingFails
dotnet publish Example1_PublishingFails
```

You may ignore the error with [ErrorOnDuplicatePublishOutputFiles](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#erroronduplicatepublishoutputfiles) which will result in having the last exported DLLs in the output directory. LlamaSharp auto-selects the correct version of the DLLs at runtime, but would now simply have one random version to go with.


### Workaround

Let's ignore the errors on duplicate DLLs in packages. Then remove the single duplicates for llama.dll and llava_shared.dll from the publishing root folder. And, finally, copy over the full runtime folders from packages. LlamaSharp auto-selects the correct version of the DLLs at runtime.

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <ErrorOnDuplicatePublishOutputFiles>false</ErrorOnDuplicatePublishOutputFiles>
        <RestorePackagesPath>bin\$(Configuration)\.nuget\packages</RestorePackagesPath>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="LLamaSharp" Version="0.12.0" />
        <PackageReference Include="LLamaSharp.Backend.Cpu" Version="0.12.0" />
        <PackageReference Include="LLamaSharp.Backend.Cuda11" Version="0.12.0" />
        <PackageReference Include="LLamaSharp.Backend.Cuda12" Version="0.12.0" />
        <PackageReference Include="LLamaSharp.Backend.OpenCL" Version="0.12.0" />
    </ItemGroup>

    <ItemGroup>
        <LlamaSharpBackendCpu Include="$(RestorePackagesPath)\llamasharp.backend.cpu\0.12.0\runtimes\**\*.*" />
        <LlamaSharpBackendCuda11 Include="$(RestorePackagesPath)\llamasharp.backend.cuda11\0.12.0\runtimes\**\*.*" />
        <LlamaSharpBackendCuda12 Include="$(RestorePackagesPath)\llamasharp.backend.cuda12\0.12.0\runtimes\**\*.*" />
        <LlamaSharpBackendOpenCL Include="$(RestorePackagesPath)\llamasharp.backend.opencl\0.12.0\runtimes\**\*.*" />
    </ItemGroup>

    <Target Name="CopyRuntimesFolderOnBuild" AfterTargets="Build">
        <Delete Files="$(OutDir)llama.dll" />
        <Delete Files="$(OutDir)llava_shared.dll" />
        <Copy SourceFiles="@(LlamaSharpBackendCpu)" DestinationFolder="$(OutputPath)\runtimes\%(RecursiveDir)" />
        <Copy SourceFiles="@(LlamaSharpBackendCuda11)" DestinationFolder="$(OutputPath)\runtimes\%(RecursiveDir)" />
        <Copy SourceFiles="@(LlamaSharpBackendCuda12)" DestinationFolder="$(OutputPath)\runtimes\%(RecursiveDir)" />
        <Copy SourceFiles="@(LlamaSharpBackendOpenCL)" DestinationFolder="$(OutputPath)\runtimes\%(RecursiveDir)" />
    </Target>

    <Target Name="CopyRuntimesFolderOnPublish" AfterTargets="Publish">
        <Delete Files="$(PublishDir)llama.dll" />
        <Delete Files="$(PublishDir)llava_shared.dll" />
        <Copy SourceFiles="@(LlamaSharpBackendCpu)" DestinationFolder="$(PublishDir)\runtimes\%(RecursiveDir)" />
        <Copy SourceFiles="@(LlamaSharpBackendCuda11)" DestinationFolder="$(PublishDir)\runtimes\%(RecursiveDir)" />
        <Copy SourceFiles="@(LlamaSharpBackendCuda12)" DestinationFolder="$(PublishDir)\runtimes\%(RecursiveDir)" />
        <Copy SourceFiles="@(LlamaSharpBackendOpenCL)" DestinationFolder="$(PublishDir)\runtimes\%(RecursiveDir)" />
    </Target>

</Project>
```

See changes in [commit a47684f](https://github.com/plastic-plant/llamasharp-issue-382/commit/a47684f07928003ec366dc1a5f3a4c933cce2948) of [Example2.csproj](Example2_PublishingSucceeds/Example2.csproj).

```shell
Example 2.

dotnet publish Example2_PublishingSucceeds
dotnet publish Example2_PublishingSucceeds --configuration Release --runtime win-x64 --self-contained true
```
