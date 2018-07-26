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
        string lpAppName,
        string lpKeyName,
        string lpDefault,
        StringBuilder lpReturnedString,
        int nSize,
        string lpFileName);

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
        string mpAppName,
        string mpKeyName,
        string mpDefault,
        string mpFileName);
    #endregion

    /// <summary>
    /// Read
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    /// <returns>读取值</returns>
    public static string Read(string section, string key, string path)
    {
        StringBuilder stringBuilder = new StringBuilder(1024); //定义一个最大长度为1024的可变字符串
        GetPrivateProfileString(section, key, "", stringBuilder, 1024, path); //读取INI文件
        return stringBuilder.ToString(); //返回键值
    }

    /// <summary>
    /// Write
    /// </summary>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    /// <param name="value">Write value</param>
    public static void Write(string section, string key, string value, string path)
    {
        WritePrivateProfileString(section, key, value, path);
    }

    /// <summary>
    /// Delete key
    /// </summary>
    /// <param name="path">File path</param>
    /// <param name="section">Section name</param>
    /// <param name="key">Key name</param>
    public static void DeleteKey(string section, string key, string path)
    {
        WritePrivateProfileString(section, key, null, path); //写入
    }

    /// <summary>
    /// Delete section
    /// </summary>
    /// <param name="path">File path</param>
    /// <param name="section">Section name</param>
    public static void DeleteSection(string section, string path)
    {
        WritePrivateProfileString(section, null, null, path); //写入
    }

    /// <summary>
    /// Creat file
    /// </summary>
    /// <param name="path">File path</param>
    public static void CreatFile(string path)
    {
        if (!File.Exists(path))
        {
            FileStream fs = File.Create(path);
            fs.Close();
        }
    }
}

