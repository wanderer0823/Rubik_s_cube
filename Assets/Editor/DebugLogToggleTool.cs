using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

/// <summary>
/// Unity Editor 工具：遍历 Assets/Csharp/Gamemanager 下的脚本
/// 支持文件夹折叠、多选、一键注释/恢复 Debug.Log 和 Debug.Warning
/// </summary>
public class DebugLogToggleTool : EditorWindow
{
    // ─────────────────────────────────────────────
    //  常量 & 路径
    // ─────────────────────────────────────────────
    private const string ROOT_PATH = "Assets/Csharp/Gamemanager";

    // 块注释标记（包裹整个 Debug 调用，支持多行）
    private const string BLOCK_START = "/*__DEBUGTOOL_START__*/";
    private const string BLOCK_END = "/*__DEBUGTOOL_END__*/";

    // ─────────────────────────────────────────────
    //  数据结构
    // ─────────────────────────────────────────────
    private class ScriptItem
    {
        public string assetPath;   // 相对于 Assets 的路径
        public string fullPath;    // 绝对路径
        public string name;        // 显示名
        public bool isToggled;     // 按钮是否被选中（已注释状态）
    }

    private class FolderNode
    {
        public string folderPath;      // 相对路径
        public string displayName;
        public bool isExpanded = true;
        public bool isFolderSelected;  // 文件夹级别多选
        public List<ScriptItem> scripts = new List<ScriptItem>();
        public List<FolderNode> subFolders = new List<FolderNode>();
    }

    // ─────────────────────────────────────────────
    //  状态
    // ─────────────────────────────────────────────
    private FolderNode _rootNode;
    private Vector2 _scrollPos;

    // 样式（延迟初始化，避免 OnGUI 之前 GUI 未就绪）
    private GUIStyle _toggledButtonStyle;
    private GUIStyle _normalButtonStyle;
    private GUIStyle _folderLabelStyle;
    private bool _stylesInitialized;

    // ─────────────────────────────────────────────
    //  菜单入口
    // ─────────────────────────────────────────────
    [MenuItem("Tools/Debug Log Toggle Tool")]
    public static void ShowWindow()
    {
        var win = GetWindow<DebugLogToggleTool>("Debug Toggle");
        win.minSize = new Vector2(340, 400);
        win.Refresh();
    }

    // ─────────────────────────────────────────────
    //  初始化
    // ─────────────────────────────────────────────
    private void OnEnable() => Refresh();

    private void Refresh()
    {
        _rootNode = BuildTree(ROOT_PATH);
        Repaint();
    }

    /// <summary>
    /// 递归构建文件夹树
    /// </summary>
    private FolderNode BuildTree(string folderAssetPath)
    {
        var node = new FolderNode
        {
            folderPath = folderAssetPath,
            displayName = Path.GetFileName(folderAssetPath)
        };

        string fullFolder = Path.Combine(Application.dataPath,
            folderAssetPath.Substring("Assets/".Length));

        if (!Directory.Exists(fullFolder))
            return node;

        // 脚本
        foreach (var file in Directory.GetFiles(fullFolder, "*.cs", SearchOption.TopDirectoryOnly)
                                      .OrderBy(f => f))
        {
            string assetPath = "Assets/" + file.Substring(Application.dataPath.Length + 1)
                                               .Replace('\\', '/');
            node.scripts.Add(new ScriptItem
            {
                assetPath = assetPath,
                fullPath = file,
                name = Path.GetFileNameWithoutExtension(file),
                isToggled = IsScriptAlreadyCommented(file)
            });
        }

        // 子文件夹
        foreach (var dir in Directory.GetDirectories(fullFolder).OrderBy(d => d))
        {
            string subAssetPath = "Assets/" + dir.Substring(Application.dataPath.Length + 1)
                                                  .Replace('\\', '/');
            node.subFolders.Add(BuildTree(subAssetPath));
        }

        return node;
    }

    // ─────────────────────────────────────────────
    //  GUI 主体
    // ─────────────────────────────────────────────
    private void OnGUI()
    {
        InitStyles();

        // ── 顶部工具栏 ──
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("  Debug Log Toggle", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50)))
            Refresh();
        EditorGUILayout.EndHorizontal();

        // 路径提示
        EditorGUILayout.HelpBox(ROOT_PATH, MessageType.None);

        // ── 批量操作 ──
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全部注释", GUILayout.Height(22)))
            SetAllToggle(true);
        if (GUILayout.Button("全部恢复", GUILayout.Height(22)))
            SetAllToggle(false);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // ── 脚本树列表 ──
        if (_rootNode == null)
        {
            EditorGUILayout.HelpBox($"路径不存在：{ROOT_PATH}", MessageType.Warning);
            return;
        }

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        DrawFolderNode(_rootNode, 0);
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 递归绘制文件夹节点
    /// </summary>
    private void DrawFolderNode(FolderNode node, int depth)
    {
        bool isRoot = depth == 0;
        float indent = depth * 14f;

        // ── 文件夹标题行 ──
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(indent);

        // 折叠箭头 + 文件夹名
        string arrow = node.isExpanded ? "▼" : "▶";
        string folderLabel = $"{arrow}  📁 {node.displayName}";
        if (GUILayout.Button(folderLabel, _folderLabelStyle, GUILayout.ExpandWidth(true)))
            node.isExpanded = !node.isExpanded;

        // 文件夹级别的多选勾选框
        if (!isRoot)
        {
            EditorGUI.BeginChangeCheck();
            bool newSel = EditorGUILayout.Toggle(node.isFolderSelected, GUILayout.Width(18));
            if (EditorGUI.EndChangeCheck())
            {
                node.isFolderSelected = newSel;
                ApplyFolderToggle(node, newSel);
            }
        }

        EditorGUILayout.EndHorizontal();

        if (!node.isExpanded) return;

        // ── 脚本按钮 ──
        foreach (var script in node.scripts)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(indent + 18);

            // 按钮颜色
            var style = script.isToggled ? _toggledButtonStyle : _normalButtonStyle;
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = script.isToggled
                ? new Color(0.35f, 0.55f, 0.35f)   // 深绿（已注释）
                : new Color(0.85f, 0.85f, 0.85f);   // 浅灰（正常）

            bool clicked = GUILayout.Button($"  📄 {script.name}", style, GUILayout.ExpandWidth(true), GUILayout.Height(22));
            GUI.backgroundColor = prevColor;

            if (clicked)
            {
                script.isToggled = !script.isToggled;
                ProcessScript(script);
            }

            // 单脚本勾选（与按钮状态同步）
            EditorGUI.BeginChangeCheck();
            bool newCheck = EditorGUILayout.Toggle(script.isToggled, GUILayout.Width(18));
            if (EditorGUI.EndChangeCheck() && newCheck != script.isToggled)
            {
                script.isToggled = newCheck;
                ProcessScript(script);
            }

            // Ping 按钮（在 Project 中定位）
            if (GUILayout.Button("◎", GUILayout.Width(22), GUILayout.Height(22)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<MonoScript>(script.assetPath);
                if (obj) EditorGUIUtility.PingObject(obj);
            }

            EditorGUILayout.EndHorizontal();
        }

        // ── 子文件夹 ──
        foreach (var sub in node.subFolders)
            DrawFolderNode(sub, depth + 1);
    }

    // ─────────────────────────────────────────────
    //  批量操作
    // ─────────────────────────────────────────────
    private void SetAllToggle(bool state)
    {
        SetNodeToggle(_rootNode, state);
        AssetDatabase.Refresh();
    }

    private void SetNodeToggle(FolderNode node, bool state)
    {
        foreach (var s in node.scripts)
        {
            if (s.isToggled != state)
            {
                s.isToggled = state;
                ProcessScript(s);
            }
        }
        foreach (var sub in node.subFolders)
        {
            sub.isFolderSelected = state;
            SetNodeToggle(sub, state);
        }
    }

    private void ApplyFolderToggle(FolderNode node, bool state)
    {
        foreach (var s in node.scripts)
        {
            s.isToggled = state;
            ProcessScript(s);
        }
        foreach (var sub in node.subFolders)
        {
            sub.isFolderSelected = state;
            ApplyFolderToggle(sub, state);
        }
        AssetDatabase.Refresh();
    }

    // ─────────────────────────────────────────────
    //  核心：注释 / 恢复 Debug.Log & Debug.Warning
    // ─────────────────────────────────────────────

    /// <summary>
    /// 判断脚本是否已被本工具注释（文件中含有标记）
    /// </summary>
    private bool IsScriptAlreadyCommented(string fullPath)
    {
        if (!File.Exists(fullPath)) return false;
        return File.ReadAllText(fullPath).Contains(BLOCK_START);
    }

    /// <summary>
    /// 根据 isToggled 状态注释或恢复脚本
    /// </summary>
    private void ProcessScript(ScriptItem script)
    {
        if (!File.Exists(script.fullPath)) return;

        if (script.isToggled)
            CommentOutDebugCalls(script.fullPath);
        else
            RestoreDebugCalls(script.fullPath);

        AssetDatabase.ImportAsset(script.assetPath, ImportAssetOptions.ForceUpdate);
    }

    // ─────────────────────────────────────────────
    //  核心：注释 / 恢复 Debug.Log & Debug.Warning
    //  策略：在原始字符串上用括号深度追踪定位完整调用（含换行），
    //        整体用块注释标记包裹；还原时精确去除标记。
    // ─────────────────────────────────────────────

    // 匹配 Debug.Log / Debug.LogWarning 起始位置（排除 Error/Exception/Assert/LogError）
    // 必须不在已有 BLOCK_START 内部（还原前检查）
    private static readonly Regex _debugStartRegex = new Regex(
        @"Debug\.Log(?!Error|Exception|Assert|Assert)[a-zA-Z]*\s*\(" +
        @"|Debug\.LogWarning\s*\(",
        RegexOptions.None
    );

    private void CommentOutDebugCalls(string fullPath)
    {
        string src = File.ReadAllText(fullPath);
        if (src.Contains(BLOCK_START)) return; // 已注释，跳过

        var sb = new System.Text.StringBuilder();
        int pos = 0;

        while (pos < src.Length)
        {
            // 找下一个 Debug.Log/LogWarning 调用起始
            var m = _debugStartRegex.Match(src, pos);
            if (!m.Success)
            {
                sb.Append(src, pos, src.Length - pos);
                break;
            }

            int matchStart = m.Index;

            // ── 检查该位置是否已在注释内（// 行注释 或 /* */ 块注释）──
            if (IsInsideComment(src, matchStart))
            {
                // 跳过这个匹配，继续往后
                sb.Append(src, pos, matchStart - pos + m.Length);
                pos = matchStart + m.Length;
                continue;
            }

            // ── 追踪括号，找到完整调用的结束位置（含分号）──
            // m 匹配到了"Debug.LogXxx("，括号深度从1开始（最后一个(）
            int callEnd = FindCallEnd(src, matchStart + m.Length - 1); // -1 指向 '('
            if (callEnd < 0)
            {
                // 找不到匹配括号，原样输出后退出
                sb.Append(src, pos, src.Length - pos);
                pos = src.Length;
                break;
            }

            // callEnd 指向 ')' 之后（即语句末尾，含可能的 ';'）
            // 把 matchStart..callEnd 整体包裹
            sb.Append(src, pos, matchStart - pos);           // 之前的内容
            sb.Append(BLOCK_START);                           // 开始标记
            sb.Append(src, matchStart, callEnd - matchStart); // 原始调用
            sb.Append(BLOCK_END);                             // 结束标记
            pos = callEnd;
        }

        string result = sb.ToString();
        if (result != src)
            File.WriteAllText(fullPath, result);
    }

    private void RestoreDebugCalls(string fullPath)
    {
        string src = File.ReadAllText(fullPath);
        if (!src.Contains(BLOCK_START)) return;

        // 直接替换：去掉标记，保留原始内容
        string result = src
            .Replace(BLOCK_START, "")
            .Replace(BLOCK_END, "");

        if (result != src)
            File.WriteAllText(fullPath, result);
    }

    // ── 从 '(' 位置开始追踪括号深度，返回完整调用结束位置（')'后，含';'如有）
    private int FindCallEnd(string src, int openParenPos)
    {
        int depth = 0;
        bool inString = false;
        bool inVerbatim = false;
        bool inChar = false;
        bool inLineComment = false;
        bool inBlockComment = false;
        char strInterp = '\0';

        for (int i = openParenPos; i < src.Length; i++)
        {
            char c = src[i];
            char next = i + 1 < src.Length ? src[i + 1] : '\0';

            // 换行时结束行注释
            if (inLineComment)
            {
                if (c == '\n') inLineComment = false;
                continue;
            }

            // 块注释结束
            if (inBlockComment)
            {
                if (c == '*' && next == '/') { inBlockComment = false; i++; }
                continue;
            }

            // 字符串内处理转义
            if (inVerbatim)
            {
                if (c == '"' && next == '"') { i++; continue; } // "" 转义
                if (c == '"') { inVerbatim = false; continue; }
                continue;
            }
            if (inString)
            {
                if (c == '\\') { i++; continue; } // 转义
                if (c == '"') { inString = false; continue; }
                continue;
            }
            if (inChar)
            {
                if (c == '\\') { i++; continue; }
                if (c == '\'') { inChar = false; continue; }
                continue;
            }

            // 检测注释开始
            if (c == '/' && next == '/') { inLineComment = true; i++; continue; }
            if (c == '/' && next == '*') { inBlockComment = true; i++; continue; }

            // 检测字符串开始
            if (c == '@' && next == '"') { inVerbatim = true; i++; continue; }
            if (c == '"') { inString = true; continue; }
            if (c == '\'') { inChar = true; continue; }

            // 括号计数
            if (c == '(') depth++;
            else if (c == ')')
            {
                depth--;
                if (depth == 0)
                {
                    // 找到匹配的 ')'，跳过可能的 ';'
                    int end = i + 1;
                    while (end < src.Length && (src[end] == ';' || src[end] == ' ' || src[end] == '\t'))
                    {
                        if (src[end] == ';') { end++; break; }
                        end++;
                    }
                    return end;
                }
            }
        }
        return -1; // 未找到匹配括号
    }

    // ── 判断 src[pos] 是否处于 // 行注释 或 /* */ 块注释内部
    private bool IsInsideComment(string src, int pos)
    {
        bool inLineComment = false;
        bool inBlockComment = false;
        bool inString = false;
        bool inVerbatim = false;
        bool inChar = false;

        for (int i = 0; i < pos && i < src.Length; i++)
        {
            char c = src[i];
            char next = i + 1 < src.Length ? src[i + 1] : '\0';

            if (inLineComment) { if (c == '\n') inLineComment = false; continue; }
            if (inBlockComment) { if (c == '*' && next == '/') { inBlockComment = false; i++; } continue; }
            if (inVerbatim) { if (c == '"' && next == '"') { i++; continue; } if (c == '"') inVerbatim = false; continue; }
            if (inString) { if (c == '\\') { i++; continue; } if (c == '"') inString = false; continue; }
            if (inChar) { if (c == '\\') { i++; continue; } if (c == '\'') inChar = false; continue; }

            if (c == '/' && next == '/') { inLineComment = true; i++; continue; }
            if (c == '/' && next == '*') { inBlockComment = true; i++; continue; }
            if (c == '@' && next == '"') { inVerbatim = true; i++; continue; }
            if (c == '"') { inString = true; continue; }
            if (c == '\'') { inChar = true; continue; }
        }

        return inLineComment || inBlockComment;
    }

    // ─────────────────────────────────────────────
    //  样式初始化
    // ─────────────────────────────────────────────
    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _normalButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            fontStyle = FontStyle.Normal,
            padding = new RectOffset(6, 6, 3, 3)
        };

        _toggledButtonStyle = new GUIStyle(_normalButtonStyle)
        {
            fontStyle = FontStyle.Bold
        };

        _folderLabelStyle = new GUIStyle(EditorStyles.foldout)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(4, 4, 3, 3)
        };
        // 使文件夹标题可点击整行
        _folderLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(4, 4, 2, 2)
        };

        _stylesInitialized = true;
    }
}