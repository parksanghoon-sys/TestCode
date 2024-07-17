using AxHWPCONTROLLib;
using HWPCONTROLLib;
using Microsoft.Win32;
using System.Reflection;


internal class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        HwpCtrl hwpCtrl = new HwpCtrl();    
        Console.WriteLine("Hello, World!");
    }
}
public class HwpCtrl
{
    private readonly AxHwpCtrl _axHwpCtrl = new AxHwpCtrl();
    private readonly string _path;
    private string ProgramDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    public HwpCtrl()
    {                
        CreateDocumentFile("Test");
        Save();
    }

    public string CreateDocumentFile(string fileName)
    {
        this.EnsureAxHwpCtrl();
        string sourceFileName = Path.Combine(this.ProgramDirectory, "Base", "BaseDocumentHwp_Style.hwp");
        string documentFile = Path.Combine(this._path, fileName + ".hwp");
        string directoryName = Path.GetDirectoryName(documentFile);
        if (directoryName != null)
            Directory.CreateDirectory(directoryName);
        if (File.Exists(documentFile))
            File.Delete(documentFile);
        File.Copy(sourceFileName, documentFile);
        this._axHwpCtrl.Open(documentFile);
        return documentFile;
    }
    private void EnsureAxHwpCtrl()
    {
        this._axHwpCtrl.CreateControl();
        Registry.SetValue("HKEY_Current_User\\Software\\HNC\\HwpCtrl\\Modules", "FilePathCheckerModuleExample", (object)(this.ProgramDirectory + "\\FilePathCheckerModuleExample.dll"));
        this._axHwpCtrl.RegisterModule("FilePathCheckDLL", (object)"FilePathCheckerModuleExample");
        this._axHwpCtrl.Clear();
    }
    public bool Save()
    {
        this.Run("MoveDocBegin");
        if (this.FindText("FunctionName"))
        {
            this.Run("MoveSelViewDown");
            this.Run("MoveSelTopLevelEnd");
            this.Run("MoveSelTopLevelEnd");
            this.Run("Delete");
        }
        return this._axHwpCtrl.Save();
    }
    private (DHwpAction act, DHwpParameterSet parameterSet) CreateActionAndParameterSet(string actId,string setId = "")
    {
        DHwpAction action = (DHwpAction)this._axHwpCtrl.CreateAction(actId);
        DHwpParameterSet dhwpParameterSet = !string.IsNullOrEmpty(setId) ? (DHwpParameterSet)this._axHwpCtrl.CreateSet(setId) : (DHwpParameterSet)action.CreateSet();
        action.GetDefault((object)dhwpParameterSet);
        return (action, dhwpParameterSet);
    }
    private bool FindText(string target)
    {
        (DHwpAction act, DHwpParameterSet parameterSet) = this.CreateActionAndParameterSet("ForwardFind", "FindReplace");
        parameterSet.SetItem("FindString", (object)target);
        parameterSet.SetItem("MatchCase", (object)true);
        parameterSet.SetItem("WholeWordOnly", (object)false);
        parameterSet.SetItem("IgnoreMessage", (object)true);
        parameterSet.SetItem("FindType", (object)false);
        return act.Execute((object)parameterSet) == 1;
    }
    private void Run(string actId) => ((DHwpAction)this._axHwpCtrl.CreateAction(actId)).Run();
}