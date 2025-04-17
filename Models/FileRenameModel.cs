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
        /// ����ʵ��
        /// </summary>
        private static FileRenameModel? _instance;

        /// <summary>
        /// ��ȡ����ʵ��
        /// </summary>
        public static FileRenameModel Instance => _instance ??= new FileRenameModel();

        #region ObservableProperty

        /// <summary>
        /// �ļ���·��
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ApplyRenameCommand))]
        private string? folderPath;
        /// <summary>
        /// ƥ��ģʽ
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ApplyRenameCommand))]
        private string? matchPattern;
        /// <summary>
        /// �滻�ı�
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ApplyRenameCommand))]
        private string? replacement;
        /// <summary>
        /// �ļ���·������
        /// </summary>
        [ObservableProperty]
        private string? folderPathError;
        /// <summary>
        /// ƥ��ģʽ����
        /// </summary>
        [ObservableProperty]
        private string? matchPatternError;
        /// <summary>
        /// �滻�ı�����
        /// </summary>
        [ObservableProperty]
        private string? replacementError;
        /// <summary>
        /// �Ƿ�ʹ��������ʽ
        /// </summary>
        [ObservableProperty]
        private bool useRegex;
        /// <summary>
        /// �Ƿ����ִ�Сд
        /// </summary>
        [ObservableProperty]
        private bool caseSensitive;
        /// <summary>
        /// �Ƿ�ȫ��ƥ��
        /// </summary>
        [ObservableProperty]
        private bool wholeWordMatch;
        /// <summary>
        /// ���ļ����б�
        /// </summary>
        [ObservableProperty]
        private string[]? oldFiles;

        /// <summary>
        /// ���ļ����б�
        /// </summary>
        [ObservableProperty]
        private string[]? newFiles;

        #endregion

        /// <summary>
        /// �����ť����¼�
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
        /// ʹ�õ��滻����
        /// </summary>
        private Func<string, string> ReplaceFunc => UseRegex
            ? RegexReplace
            : StringReplace;

        /// <summary>
        /// Ӧ�ð�ť����¼�
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRename))]
        private void ApplyRename()
        {
            try
            {//todo �Ż�Ϊ���ı�������ʾ��ɫ�Ĵ�����Ϣ
                //if ((FolderPath.IsNullOrWhiteSpace()
                //     || !Directory.Exists(FolderPath))
                //        .ShowErrMessage("�ļ���·����Ч��", "ʧ��") 
                //    )
                //    return;
                //if (MatchPattern.IsNullOrEmpty()
                //    .ShowErrMessage("ƥ��ģʽ����Ϊ�ա�", "ʧ��"))
                //    return;

                //if ((Replacement==null)
                //    .ShowErrMessage("�滻�ı�����Ϊnull��", "ʧ��"))
                //    return;

                _fullFiles = Directory.GetFiles(FolderPath!);

                //��ȡ���ļ����б�
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
                        MessageBox.Show($"�������ļ�{NewFiles[i]}ʱʧ��,��������ֹͣ��\r\n{e.Message}", "ʧ��", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                MessageBox.Show("�ļ���������ɣ�", "�ɹ�", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"��������: {ex.Message}", "����", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// �Ƿ�ִ����������
        /// </summary>
        /// <returns></returns>
        private bool CanRename()
        {
            if (FolderPath.IsNullOrEmpty())
            {
                FolderPathError = "�ļ���·������Ϊ��";
                return false;
            }
            if (FolderPath!.DirNotExists())
            {
                FolderPathError = "�ļ��в�����";
                return false;
            }
            if (MatchPattern.IsNullOrEmpty())
            {
                MatchPatternError = "ƥ����ı���������ʽ����Ϊ��";
                return false;
            }
            // ReSharper disable once InvertIf
            if (Replacement == null)
            {
                ReplacementError = "�滻���ı�����Ϊnull";
                return false;
            }

            return true;
        }

        /// <summary>
        /// ʹ�ü򵥵��ַ����滻���ܣ���ָ�������ַ����滻Ϊ���ַ�����
        /// </summary>
        /// <param name="originalString">ԭʼ�ַ�����</param>
        /// <param name="oldValue">Ҫ���滻�����ַ�����</param>
        /// <param name="newValue">�����滻�����ַ�����</param>
        /// <returns>�滻������ַ�����</returns>
        private string StringReplace(string originalString) => WholeWordMatch
            ? string.Join(' ',
                originalString.Split(' ')//���ո�ָ�
                .Select(t => t.Equals(MatchPattern,
                    CaseSensitive//���ִ�Сд
                    ? StringComparison.CurrentCultureIgnoreCase
                    : StringComparison.CurrentCulture)
                    ? Replacement
                    : t))
            : originalString.Replace(MatchPattern!, Replacement, CaseSensitive
                ? StringComparison.CurrentCultureIgnoreCase
                : StringComparison.CurrentCulture);

        //todo ���logo���� ������ɫ�ĳɰ�ɫ
        private string RegexReplace(string input)
        {

            return Regex.Replace(input,
                WholeWordMatch
                    ? AddWordBoundaries(MatchPattern!)// ��ӱ߽������ʵ��ȫ��ƥ��
                    : MatchPattern!,
                Replacement!, CaseSensitive //���ִ�Сд
                ? RegexOptions.IgnoreCase
                : RegexOptions.None);
        }

        /// <summary>
        /// Ϊ������ʽģʽ��ӵ��ʱ߽磨\b����
        /// </summary>
        /// <param name="pattern">ԭʼ������ʽģʽ��</param>
        /// <returns>����˵��ʱ߽��������ʽģʽ��</returns>
        private static string AddWordBoundaries(string pattern)
            => (pattern.StartsWith(@"\b") ? "" : @"\b")
               + pattern
               + (pattern.EndsWith(@"\b") ? "" : @"\b");

    }
}
