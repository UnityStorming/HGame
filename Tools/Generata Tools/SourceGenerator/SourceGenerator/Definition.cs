using System.Collections.Generic;

namespace SourceGenerator;

public class Definition
{
    /// <summary>
    /// 指定检测的命名空间
    /// </summary>
    public static readonly List<string> TargetNameSpaces = ["GameLogic"]; //"HGame", "GameProto", "GameBattle"
    public static readonly string[] UsingNameSpace = ["UnityEngine", "UnityEngine.UI", "HGame"];//"UnityEngine", "UnityEngine.UI", "HGame"
    public const string DefaultAssemblyName = "GameLogic";
    public const string DefaultFrameworkNameSpace = "HGame";
    public const string DefaultDebugName = "HGame.DLogger";

    /// <summary>
    /// 文件生成的命名空间
    /// </summary>
    public const string NameSpace = "GameLogic";//"HGame";
    public const string AttributeName = "EventInterface";
    public const string StringToHash = "StringId.StringToHash";

    /// <summary>
    /// RuntimeInitialize文件生成的命名空间
    /// </summary>
    public static readonly List<string> RuntimeInitializeTargetNameSpaces = ["GameLogic"]; //"HGame", "GameProto", "GameBattle"
    public static readonly string[] RuntimeInitializeUsingNameSpace = ["UnityEngine"];
    public const string RuntimeInitializeNameSpace = "AOT";//"HGame";
    public const string RuntimeInitializeAttributeName = "RuntimeInitializeOnLoadMethod";

    /// <summary>
    /// RequireComponent文件生成的命名空间
    /// </summary>
    public static readonly List<string> RequireComponentTargetNameSpaces = ["GameLogic"]; //"HGame", "GameProto", "GameBattle"
    public static readonly string[] RequireComponentUsingNameSpace = ["UnityEngine"];
    public const string RequireComponentNameSpace = "AOT";//"HGame";
    public const string RequireComponentAttributeName = "RequireComponent";
}