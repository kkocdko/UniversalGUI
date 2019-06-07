using System.IO;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// Ini Manager
/// </summary>
public class IniManager
{
    #region
    /// <summary>
    /// Read value
    /// </summary>
    /// <param name="lpAppName">Section name</param>
    /// <param name="lpKeyName">Key name</param>
    /// <param name="lpDefault">Default value</param>
    /// <param name="lpReturnedString">Cache string</param>
    /// <param name="nSize">Cache size</param>
    /// <param name="lpFileName">File path</param>
    /// <returns></returns>
    [DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section, string key, string value, StringBuilder cache, int cacheSize, string path);

    /// <summary>
    /// Write value
    /// </summary>
    /// <param name="mpAppName">Section name</param>
    /// <param name="mpKeyName">Key name</param>
    /// <param name="mpDefault">Write value</param>
    /// <param name="mpFileName">File path</param>
    /// <returns>0：error 1：success</returns>
    [DllImport("kernel32")]
    private static extern long WritePrivateProfileString(string section, string key, string value, string path);
    #endregion

    /// <summary>
    /// IniManager constructor
    /// </summary>
    /// <param name="path">Ini file's full path</param>
    public IniManager(string iniFilePath)
    {
        IniFilePath = iniFilePath;
    }

    public string IniFilePath;

    /// <summary>
    /// Read ini key
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    /// <returns>Read value</returns>
    public string Read(string section, string key, int cacheSize = 1024)
    {
        var stringBuilder = new StringBuilder(cacheSize);
        GetPrivateProfileString(section, key, "", stringBuilder, cacheSize, IniFilePath);
        string value = stringBuilder.ToString();
        return value;
    }

    /// <summary>
    /// Write ini key
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    /// <param name="value">Write value</param>
    public void Write<T>(string section, string key, T value)
    {
        WritePrivateProfileString(section, key, value.ToString(), IniFilePath);
    }

    /// <summary>
    /// Delete ini key
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    public void Delete(string section, string key = null)
    {
        WritePrivateProfileString(section, key, null, IniFilePath);
    }

    /// <summary>
    /// Creat file
    /// </summary>
    public void CreatFile()
    {
        string iniFilePath = Path.GetDirectoryName(IniFilePath);
        if (Directory.Exists(iniFilePath))
        {
            var fs = File.Create(IniFilePath);
            fs.Close();
        }
        else
        {
            Directory.CreateDirectory(iniFilePath);
            CreatFile();
        }
    }
}