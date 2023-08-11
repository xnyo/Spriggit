using System.Diagnostics;
using System.IO.Abstractions;
using Noggog;
using Noggog.IO;
using Noggog.WorkEngine;
using Serilog;
using Spriggit.Core;

namespace Spriggit.Engine;

public class SpriggitEngine(
    IFileSystem fileSystem,
    IWorkDropoff? workDropoff,
    ICreateStream? createStream,
    EntryPointCache entryPointCache,
    ILogger logger,
    GetMetaToUse getMetaToUse)
{
    public async Task Serialize(
        FilePath bethesdaPluginPath, 
        DirectoryPath outputFolder, 
        SpriggitMeta meta,
        CancellationToken cancel)
    {
        logger.Information("Getting entry point for {Meta}", meta);
        var entryPt = await entryPointCache.GetFor(meta, cancel);
        if (entryPt == null) throw new NotSupportedException($"Could not locate entry point for: {meta}");

        logger.Information("Starting to serialize");
        await entryPt.EntryPoint.Serialize(
            bethesdaPluginPath,
            outputFolder,
            meta.Release,
            fileSystem: fileSystem,
            workDropoff: workDropoff,
            streamCreator: createStream,
            meta: new SpriggitSource()
            {
                PackageName = entryPt.Package.Id,
                Version = entryPt.Package.Version.ToString()
            },
            cancel: cancel);
        logger.Information("Finished serializing");
    }

    public async Task Deserialize(
        string spriggitPluginPath, 
        FilePath outputFile,
        SpriggitSource? source,
        CancellationToken cancel)
    {
        logger.Information("Getting meta to use for {Source} at path {PluginPath}", source, spriggitPluginPath);
        var meta = await getMetaToUse.Get(source, spriggitPluginPath, cancel);
        
        logger.Information("Getting entry point for {Meta}", meta);
        var entryPt = await entryPointCache.GetFor(meta, cancel);
        if (entryPt == null) throw new NotSupportedException($"Could not locate entry point for: {meta}");

        cancel.ThrowIfCancellationRequested();
        
        logger.Information("Starting to deserialize");
        await entryPt.EntryPoint.Deserialize(
            spriggitPluginPath,
            outputFile,
            workDropoff: workDropoff,
            fileSystem: fileSystem,
            streamCreator: createStream,
            cancel: cancel);
        logger.Information("Finished deserializing");
    }
}