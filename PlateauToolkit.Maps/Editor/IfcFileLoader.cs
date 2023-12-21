#if (UNITY_EDITOR)
using System.IO;
using UnityEngine;

using UnityEditor;
using System;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// class to load IFC files.
/// </summary>
public class IfcFileLoader : IDisposable
{
    /// <summary>
    /// path for the config file
    /// </summary>
    string m_OutputPath;
    string m_IfcExePath;

    /// <summary>
    /// default constructor
    /// </summary>
    public IfcFileLoader()
    {
        m_OutputPath = EditorUserSettings.GetConfigValue("PlateauToolkit.Maps.IfcOutputPath");
        m_IfcExePath = EditorUserSettings.GetConfigValue("PlateauToolkit.Maps.IfcExePath");

        if (string.IsNullOrEmpty(m_OutputPath))
        {
            m_OutputPath = Directory.GetCurrentDirectory().ToString().Replace("\\", "/") + "/Assets/Meshes";
            EditorUserSettings.SetConfigValue("PlateauToolkit.Maps.IfcOutputPath", m_OutputPath);
        }

        if (string.IsNullOrEmpty(m_IfcExePath))
        {
            string fullPath = Directory.GetCurrentDirectory().ToString().Replace("\\", "/") + "/Assets/IfcConvert";
            m_IfcExePath = fullPath + "/IfcConvert.exe";
        }
        EditorUserSettings.SetConfigValue("PlateauToolkit.Maps.IfcExePath", m_IfcExePath);
    }

    public void SetPathsAutomatically()
    {
        m_OutputPath = Directory.GetCurrentDirectory().ToString().Replace("\\", "/") + "/Assets/Meshes";
        EditorUserSettings.SetConfigValue("PlateauToolkit.Maps.IfcOutputPath", m_OutputPath);

        string fullPath = Directory.GetCurrentDirectory().ToString().Replace("\\", "/") + "/Assets/IfcConvert";
#if UNITY_EDITOR && UNITY_EDITOR_WIN
        m_IfcExePath = fullPath + "/IfcConvert.exe";
#elif UNITY_EDITOR && !UNITY_EDITOR_WIN
        m_IfcExePath = fullPath + "/IfcConvert";
#endif
        EditorUserSettings.SetConfigValue("PlateauToolkit.Maps.IfcExePath", m_IfcExePath);
    }

    /// <summary>
    /// sets the used path for IfcConvert
    /// </summary>
    /// <param name="ifcConvertPath"></param>
    public void SetIfcConvertPath(string ifcConvertPath)
    {
        m_IfcExePath = ifcConvertPath;
        EditorUserSettings.SetConfigValue("PlateauToolkit.Maps.IfcExePath", m_IfcExePath);
    }

    /// <summary>
    /// sets the output path of IfcConvert
    /// </summary>
    /// <param name="outputPath"></param>
    public void SetOutputPath(string outputPath)
    {
        m_OutputPath = outputPath;
        EditorUserSettings.SetConfigValue("PlateauToolkit.Maps.IfcOutputPath", m_OutputPath);
    }

    public string GetOutputPath()
    {
        return m_OutputPath;
    }

    public string GetIfcExePath()
    {
        return m_IfcExePath;
    }

    /// <summary>
    /// Loads the IFC file from the given path and returns the generated game object
    /// </summary>
    /// <param name="ifcFilePath"></param>
    /// <returns></returns>
    public bool LoadIfcFile(string ifcFilePath)
    {
        if (!File.Exists(m_IfcExePath))
        {
            return false;
        }

        if (!Directory.Exists(m_OutputPath))
        {
            Debug.Log("Output directory " + m_OutputPath + " does not exist. Create...");
            Directory.CreateDirectory(m_OutputPath);
        }

        Debug.Log("IFC file Path: " + ifcFilePath);
        string ifcFileName = Path.GetFileName(ifcFilePath);
        string glbOutputFileName = ifcFileName.Replace("ifc", "glb");
        string xmlOutputFileName = ifcFileName.Replace("ifc", "xml");
        string glbOutputPath = Path.Combine(m_OutputPath, glbOutputFileName);
        string xmlOutputPath = Path.Combine(m_OutputPath, xmlOutputFileName);

        Debug.Log("output path: " + glbOutputPath);

        //copy ifc file if necessary
        string fullStreamingAssetPath = Application.streamingAssetsPath;
        if (!ifcFilePath.StartsWith(fullStreamingAssetPath))
        {
            string cwd = Directory.GetCurrentDirectory();
            string relativeStreamingAssetPath = Path.GetRelativePath(cwd, fullStreamingAssetPath);
            if (!AssetDatabase.IsValidFolder(Path.GetRelativePath(cwd, Application.streamingAssetsPath)))
            {
                AssetDatabase.CreateFolder("Assets", relativeStreamingAssetPath.Replace("Assets" + Path.DirectorySeparatorChar, ""));
            }
            string ifcFolderPath = Path.Combine(relativeStreamingAssetPath, "ifc");
            if (!AssetDatabase.IsValidFolder(ifcFolderPath))
            {
                AssetDatabase.CreateFolder(relativeStreamingAssetPath, "ifc");
            }
            string targetPath = Path.Combine(ifcFolderPath, ifcFileName);

            if (File.Exists(targetPath))
            {
                Debug.LogWarning("File " + targetPath + " exists already. Overwriting...");
                File.Delete(targetPath);
            }
            File.Copy(ifcFilePath, targetPath);
            ifcFilePath = targetPath;
        }

        // delete file if exists
        DeleteFile(glbOutputPath);
        DeleteFile(xmlOutputPath);
        AssetDatabase.Refresh();

        //define process for ifcconvert with arguments and obj output
        System.Diagnostics.ProcessStartInfo ifcProcessInfo = GenerateProcessInformation(ifcFilePath, glbOutputPath);
        System.Diagnostics.ProcessStartInfo xmlProcessInfo = GenerateProcessInformation(ifcFilePath, xmlOutputPath);

        //start glb generation
        StartConversion(ifcProcessInfo);
        StartConversion(xmlProcessInfo);

        //load model into scene
        if (!File.Exists(glbOutputPath))
        {
            Debug.LogError("Could not find glb file: " + glbOutputPath);
            return false;
        }
        if (!File.Exists(xmlOutputPath))
        {
            Debug.LogError("Could not find xml file: " + xmlOutputPath);
            return false;
        }
        AssetDatabase.Refresh();

        //Mark scene as unsafed
        Scene activeScene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);

        return true;
    }

    /// <summary>
    /// helper function to start process and log results
    /// </summary>
    /// <param name="processInfo"></param>
    void StartConversion(System.Diagnostics.ProcessStartInfo processInfo)
    {
        using (System.Diagnostics.Process ifcProcess = System.Diagnostics.Process.Start(processInfo))
        {
            ifcProcess.WaitForExit();
        }
    }

    /// <summary>
    /// Helper function to generate process information
    /// </summary>
    /// <param name="ifcFilePath">Path to the source ifc file</param>
    /// <param name="outputPath">Path for the output file</param>
    /// <returns></returns>
    System.Diagnostics.ProcessStartInfo GenerateProcessInformation(string ifcFilePath, string outputPath)
    {
        System.Diagnostics.ProcessStartInfo ifcProcessInfo =
            new System.Diagnostics.ProcessStartInfo(m_IfcExePath)
            {
                CreateNoWindow = false,
                UseShellExecute = true
            };
        string arguments = "--use-element-guids \"" + ifcFilePath + "\" \"" + outputPath + "\"";
        Debug.Log("ifcconvert arguments: " + arguments);
        ifcProcessInfo.Arguments = arguments;
        return ifcProcessInfo;
    }


    /// <summary>
    /// delete file if exists
    /// </summary>
    /// <param name="filePath"></param>
    void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            Debug.Log("File " + filePath + " already exists. deleting");
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// unregister event handler
    /// </summary>
    public void Dispose()
    {

    }
}

#endif