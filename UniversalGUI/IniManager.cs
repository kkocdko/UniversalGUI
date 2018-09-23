using System;
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
    private static extern int GetPrivateProfileString(
        string section, 
        string key, 
        string value, 
        StringBuilder cache, 
        int cacheSize, 
        string path);

    /// <summary>
    /// Write value
    /// </summary>
    /// <param name="mpAppName">Section name</param>
    /// <param name="mpKeyName">Key name</param>
    /// <param name="mpDefault">Write value</param>
    /// <param name="mpFileName">File path</param>
    /// <returns>0：error 1：success</returns>
    [DllImport("kernel32")]
    private static extern long WritePrivateProfileString(
        string section, 
        string key, 
        string value, 
        string path);
    #endregion

    public string IniFile { get; private set; }

    /// <summary>
    /// IniManager constructor
    /// </summary>
    /// <param name="path">Ini file's full path(Use full path!)</param>
    public IniManager(string path)
    {
        IniFile = path;
    }

    /// <summary>
    /// Read ini key
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    /// <returns>Read value</returns>
    public string Read(string section, string key)
    {
        var strBuilder = new StringBuilder(1024);
        GetPrivateProfileString(section, key, "", strBuilder, 1024, IniFile);
        string readValue = strBuilder.ToString();
        return readValue;
    }

    /// <summary>
    /// Write ini key
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    /// <param name="value">Write value</param>
    public void Write(string section, string key, string value)
    {
        WritePrivateProfileString(section, key, value, IniFile);
    }

    /// <summary>
    /// Write ini key
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    /// <param name="value">Write value</param>
    public void Write(string section, string key, double value)
    {
        string strValue = Convert.ToString(value);
        Write(section, key, strValue);
    }

    /// <summary>
    /// Write ini key
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    /// <param name="value">Write value</param>
    public void Write(string section, string key, object value)
    {
        string strValue = Convert.ToString(value);
        Write(section, key, Convert.ToString(value));
    }

    /// <summary>
    /// Write ini key
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    /// <param name="value">Write value</param>
    public void Write(string section, string key, bool value)
    {
        string strValue = Convert.ToString(value);
        Write(section, key, strValue);
    }

    /// <summary>
    /// Delete ini key
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    public void Delete(string section, string key)
    {
        WritePrivateProfileString(section, key, null, IniFile);
    }

    /// <summary>
    /// Delete ini section
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    public void Delete(string section)
    {
        WritePrivateProfileString(section, null, null, IniFile);
    }

    /// <summary>
    /// Creat file
    /// </summary>
    public void CreatFile()
    {
        string iniFilePath = Path.GetDirectoryName(IniFile);
        if (Directory.Exists(iniFilePath))
        {
            var fs = File.Create(IniFile);
            fs.Close();
        }
        else
        {
            Directory.CreateDirectory(iniFilePath);
            CreatFile();
        }
    }
}