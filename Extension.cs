using System.Windows;

namespace BatchRename
{
    public static class Extension
    {
        /// <summary>
        /// 若条件为真，则显示错误消息框
        /// </summary>
        /// <param name="condition">条件</param>
        /// <param name="message">消息</param>
        /// <param name="caption">标题</param>
        /// <returns>是否显示</returns>
        public static bool ShowErrMessage(this bool condition, string message, string caption) 
        {
            if (!condition)
                return false;
            
            MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
            return true;
            
        }
    }
}
