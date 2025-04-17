using System.IO;
using System.Windows;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Sys.Extensions;

namespace BatchRename.Models
{
    public partial class FileRenameModel : ObservableObject
    {
        /// <summary>
        /// 单例实例
        /// </summary>
        private static FileRenameModel? _instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static FileRenameModel Instance => _instance ??= new FileRenameModel();

        #region ObservableProperty

        /// <summary>
        /// 文件夹路径
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ApplyRenameCommand))]
        private string? folderPath;
        /// <summary>
        /// 匹配模式
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ApplyRenameCommand))]
        private string? matchPattern;
        /// <summary>
        /// 替换文本
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ApplyRenameCommand))]
        private string? replacement;
        /// <summary>
        /// 文件夹路径错误
        /// </summary>
        [ObservableProperty]
        private string? folderPathError;
        /// <summary>
        /// 匹配模式错误
        /// </summary>
        [ObservableProperty]
        private string? matchPatternError;
        /// <summary>
        /// 替换文本错误
        /// </summary>
        [ObservableProperty]
        private string? replacementError;
        /// <summary>
        /// 是否使用正则表达式
        /// </summary>
        [ObservableProperty]
        private bool useRegex;
        /// <summary>
        /// 是否区分大小写
        /// </summary>
        [ObservableProperty]
        private bool caseSensitive;
        /// <summary>
        /// 是否全字匹配
        /// </summary>
        [ObservableProperty]
        private bool wholeWordMatch;
        /// <summary>
        /// 旧文件名列表
        /// </summary>
        [ObservableProperty]
        private string[]? oldFiles;

        /// <summary>
        /// 新文件名列表
        /// </summary>
        [ObservableProperty]
        private string[]? newFiles;

        #endregion

        /// <summary>
        /// 浏览按钮点击事件
        /// </summary>
        [RelayCommand]
        private void BrowseFolder()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
                FolderPath = dialog.FolderName;
        }

        private string[]? _fullFiles;
        /// <summary>
        /// 使用的替换方法
        /// </summary>
        private Func<string, string> ReplaceFunc => UseRegex
            ? RegexReplace
            : StringReplace;

        /// <summary>
        /// 应用按钮点击事件
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRename))]
        private void ApplyRename()
        {
            try
            {//todo 优化为在文本框中显示红色的错误信息
                //if ((FolderPath.IsNullOrWhiteSpace()
                //     || !Directory.Exists(FolderPath))
                //        .ShowErrMessage("文件夹路径无效。", "失败") 
                //    )
                //    return;
                //if (MatchPattern.IsNullOrEmpty()
                //    .ShowErrMessage("匹配模式不能为空。", "失败"))
                //    return;

                //if ((Replacement==null)
                //    .ShowErrMessage("替换文本不能为null。", "失败"))
                //    return;

                _fullFiles = Directory.GetFiles(FolderPath!);

                //获取旧文件名列表
                OldFiles = [.. _fullFiles
                    .Select(Path.GetFileName)
                    .Cast<string>()];

                NewFiles = [.. OldFiles.Select(t => ReplaceFunc.Invoke(t))];

                for (int i = 0; i < NewFiles.Length; i++)
                {
                    try
                    {
                        File.Move(_fullFiles[i], $@"{FolderPath}\{NewFiles[i]}");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"重命名文件{NewFiles[i]}时失败,重命名已停止：\r\n{e.Message}", "失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                MessageBox.Show("文件重命名完成！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 是否执行能重命名
        /// </summary>
        /// <returns></returns>
        private bool CanRename()
        {
            if (FolderPath.IsNullOrEmpty())
            {
                FolderPathError = "文件夹路径不能为空";
                return false;
            }
            if (FolderPath!.DirNotExists())
            {
                FolderPathError = "文件夹不存在";
                return false;
            }
            if (MatchPattern.IsNullOrEmpty())
            {
                MatchPatternError = "匹配的文本或正则表达式不能为空";
                return false;
            }
            // ReSharper disable once InvertIf
            if (Replacement == null)
            {
                ReplacementError = "替换的文本不能为null";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 使用简单的字符串替换功能，将指定的子字符串替换为新字符串。
        /// </summary>
        /// <param name="originalString">原始字符串。</param>
        /// <param name="oldValue">要被替换的子字符串。</param>
        /// <param name="newValue">用于替换的子字符串。</param>
        /// <returns>替换后的新字符串。</returns>
        private string StringReplace(string originalString) => WholeWordMatch
            ? string.Join(' ',
                originalString.Split(' ')//按空格分隔
                .Select(t => t.Equals(MatchPattern,
                    CaseSensitive//区分大小写
                    ? StringComparison.CurrentCultureIgnoreCase
                    : StringComparison.CurrentCulture)
                    ? Replacement
                    : t))
            : originalString.Replace(MatchPattern!, Replacement, CaseSensitive
                ? StringComparison.CurrentCultureIgnoreCase
                : StringComparison.CurrentCulture);

        //todo 解决logo问题 并把颜色改成白色
        private string RegexReplace(string input)
        {

            return Regex.Replace(input,
                WholeWordMatch
                    ? AddWordBoundaries(MatchPattern!)// 添加边界符号以实现全字匹配
                    : MatchPattern!,
                Replacement!, CaseSensitive //区分大小写
                ? RegexOptions.IgnoreCase
                : RegexOptions.None);
        }

        /// <summary>
        /// 为正则表达式模式添加单词边界（\b）。
        /// </summary>
        /// <param name="pattern">原始正则表达式模式。</param>
        /// <returns>添加了单词边界的正则表达式模式。</returns>
        private static string AddWordBoundaries(string pattern)
            => (pattern.StartsWith(@"\b") ? "" : @"\b")
               + pattern
               + (pattern.EndsWith(@"\b") ? "" : @"\b");

    }
}
