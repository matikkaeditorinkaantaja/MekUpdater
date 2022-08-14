﻿using MekUpdater.Exceptions;

namespace MekUpdater.Helpers;

public enum SetupPathMsg
{
    None, EmptyFolder, NoMatchingFolder, CantEnumerateFolders, CantEnumerateFiles,
    NoMatchingFile,
    Success
}

public class SetupPathFinderResult
{
    internal SetupPathFinderResult(bool success)
    {
        Success = success;
    }
    public bool Success { get; }
    public virtual SetupExePath? SetupPath { get; init; }
    public virtual string Message { get; init; } = string.Empty;
    public virtual SetupPathMsg SetupPathMsg { get; init; } = SetupPathMsg.None;
}

public class SetupFolderFinderResult : SetupPathFinderResult
{
    internal SetupFolderFinderResult(bool success) : base(success) { }
    public override SetupExePath? SetupPath => null;
    public virtual string? SetupFolderName { get; init; }
}

internal class SetupPathFinder
{
    internal readonly record struct SetupInfo(FolderPath ExtractionFolder, string RepoOwnerName, string RepoName);

    public SetupPathFinder(SetupInfo info)
    {
        Info = info;
    }
    
    
    public SetupInfo Info { get; }


    internal SetupPathFinderResult TryFindPath()
    {
        var folderFinderResult = GetSetupContainingFolderName(Info.ExtractionFolder);
        if (folderFinderResult.Success is false) return folderFinderResult;

        FolderPath setupFolder = new(Path.Combine(Info.ExtractionFolder.ToString(), folderFinderResult + "\\"));
        return GetSetupFileName(setupFolder);
    }

    /// <summary>
    /// Get name of name containing setup name
    /// </summary>
    /// <param name="extractPath"></param>
    /// <returns>Name of name containing setup name or null if not found</returns>
    private SetupFolderFinderResult GetSetupContainingFolderName(FolderPath extractPath)
    {
        List<string> folderNames;
        try
        {
            folderNames = Directory
                .EnumerateDirectories(extractPath.ToString())
                .Select(x => Path.GetFileName(x))                   
                .ToList();
        }
        catch (Exception ex)
        {
            return new(false)
            {
                SetupPathMsg = SetupPathMsg.CantEnumerateFolders,
                Message = $"Can't enumerate dictionaries because of exception {ex}: {ex.Message}"
            };
        }
        foreach (var name in folderNames ?? Enumerable.Empty<string>())
        {
            if (IsExtractedFolderMatch(name)) return new(true)
            {
                SetupFolderName = name
            };
        }
        return new(false)
        {
            SetupPathMsg = SetupPathMsg.NoMatchingFolder,
            Message = $"Could not find folder matching: " +
            $"path '{extractPath}', repo owner '{Info.RepoOwnerName}', repo name '{Info.RepoName}'"
        };
    }

    private bool IsExtractedFolderMatch(string folderName)
    {
        return folderName.Trim().StartsWith($"{Info.RepoOwnerName}-{Info.RepoName}-");
    }

    /// <summary>
    /// Get name of setup name
    /// </summary>
    /// <param name="setupFolder"></param>
    /// <returns>Name of setup name or null if not found</returns>
    internal static SetupPathFinderResult GetSetupFileName(FolderPath setupFolder)
    {
        List<string> fileNames;
        try
        {
            fileNames = Directory
                .EnumerateFiles(setupFolder.ToString())
                .Select(x => Path.GetFileName(x))
                .ToList();
        }
        catch (Exception ex)
        {
            return new(false)
            {
                SetupPathMsg = SetupPathMsg.CantEnumerateFiles,
                Message = "Could not enumerate files because of exception " +
                $"{ex}: {ex.Message}"
            };
        }
        foreach (var name in fileNames ?? Enumerable.Empty<string>())
        {
            try
            {
                SetupExePath result = new(Path.Combine(setupFolder.ToString(), name));
                return new(true)
                {
                    SetupPath = result,
                    Message = $"Found setup exe file at '{result}'"
                };
            }
            catch (ArgumentException)
            {
                continue;
            }
        }
        return new(false)
        {
            Message = $"Could not parse any setup file from folder {setupFolder}",
            SetupPathMsg = SetupPathMsg.NoMatchingFile
        };
    }
}
